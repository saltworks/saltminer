# Engagement-Scoped Attachment Storage + Scheduled Cleanup Service

## Context

The UI API (`Saltworks.SaltMiner.Ui.Api`) stores all engagement/issue attachment files and
markdown images in a single **flat directory** (`UiApiConfig.FileRepository`, e.g.
`../ui-files/uploads`), named `{GUID}.{ext}` ("FileId"). Metadata lives in the Elasticsearch
`attachments` index as `Attachment` records (`Saltminer.Attachment.FileId/FileName`,
`Saltminer.Engagement.Id`, `Saltminer.Issue.Id`, `IsMarkdown`).

JobManager keeps **nothing permanent locally** — it writes temp files (reports under `Output`,
imports under `File`) and uploads everything to the UI API via multipart, deleting the temp copies
after (`ReportProcessor.cs:428`, `ImportProcessor.cs:128`). So this work is confined to the UI API
and its client/helper libraries. No JobManager changes are needed (confirmed).

Three problems with the current flat layout:
1. **No file→engagement back-reference on disk** — relationship is one-way (engagement → file via
   the `Attachment` record), making cleanup hard.
2. **Flat directory** will accumulate enormous file counts in one OS dir.
3. **Cleanup is an awkward CLI verb** (`CleanUpProcessor` / `cleanup` command) that scans the flat
   dir and queries the `attachments` index per file by `FileName` — expensive and incorrect at scale
   (it even matches on the original filename, not the on-disk FileId).

Desired outcome: engagement-scoped folders on disk, a scheduled background cleanup service that works
per-engagement, and a one-time Python migration of the existing flat files.

## Decisions (confirmed with user)

- **Upload→folder:** *Pending folder + move on associate.* Uploads land in a reserved `_pending`
  folder; the physical file is moved into `{engagementId}/` when `SetAttachments` associates it. The
  upload API contract is unchanged. (Engagement ID is genuinely unknown at upload time — the
  frontend uploads first, associates on save.)
- **Old cleanup:** *Retire the `cleanup` CLI verb / `CleanUpProcessor` fully.* The hosted service is
  the single cleanup path.
- **Migration tool:** *Python `RunUtil*.py`* reusing the existing `Core/ElasticClient.py`
  `SearchScroll` over the `attachments` index — not a standalone raw-Elasticsearch script.

## On-disk layout (target)

```
{FileRepository}/
  _pending/{xx}/{fileId}            # uploaded, not yet associated (xx = first 2 chars of fileId)
  {engagementId}/{fileId}           # associated (default)
  {engagementId}/{xx}/{fileId}      # if FileShardEngagementFolders = true
  {fileId}                          # LEGACY flat files (read-compatible; cleanup leaves them, only warns)
```

`_pending` is always 2-char sharded (it's the one unbounded dir). Engagement folders get an optional
second-level shard only when the config toggle is on. Engagement IDs are GUID-like and can never
collide with the reserved literal `_pending`. Reads always fall back, so toggling the shard or
running mid-migration never strands a file.

Why engagement-folder (not deeper) sharding: this mirrors the git-objects / Squid fan-out approach
(intermediate dir from the id prefix) which is the real-world answer to "too many files per OS dir";
files are already naturally partitioned per engagement, so the engagement folder is sufficient and
makes per-engagement cleanup O(one engagement) instead of O(whole index). Issue attachments need no
separate layer: `SetAttachments` always carries the engagement id for issue attachments
(`IssueController`/`IssueContext` pass `engagement.Id`), so deleting an engagement folder correctly
removes its issues' files too.

## Implementation

### 1. Config — `Saltworks.SaltMiner.Ui.Api/Models/UiApiConfig.cs` + appsettings

Add a new enum file `Models/FileCleanupScheduleMode.cs`:
```csharp
public enum FileCleanupScheduleMode { Disabled, Daily, Weekly }
```
Add to `UiApiConfig` (binds by name from appsettings via the existing `config.Bind` mechanism):
```csharp
public FileCleanupScheduleMode FileCleanupSchedule { get; set; } = FileCleanupScheduleMode.Disabled;
public string FileCleanupStartTime { get; set; } = "02:00";        // local HH:mm
public DayOfWeek FileCleanupWeeklyDay { get; set; } = DayOfWeek.Sunday;
public int FileCleanupPendingMaxAgeHours { get; set; } = 72;        // reap age for unassociated _pending files
public bool FileShardEngagementFolders { get; set; } = false;
```
Add the corresponding keys under the `UiApiConfig` section of `appsettings.json` (and
`appsettings.VM.json`). Default `Disabled` preserves current behavior until explicitly enabled.

### 2. New path helper — `Saltworks.SaltMiner.UiApiClient/Helpers/FilePathResolver.cs`

Centralizes all path math so the rest of the code is layout-agnostic. Static methods:
- `BuildPath(fileRepo, engagementId, fileId, shard)` → canonical destination path.
- `PendingPath(fileRepo, fileId)` → `_pending/{xx}/{fileId}` (always sharded).
- `Resolve(fileRepo, engagementId, fileId, shard)` / `ResolveByFileId(fileRepo, fileId, engagementIdOrNull, shard)`
  → first existing path, probing in order: engagement folder (sharded **and** unsharded) →
  `_pending` (sharded and unsharded) → legacy flat `{fileRepo}/{fileId}`; returns `null` if none.

`fileId` is always normalized with `Path.GetFileName(...)` as the existing code already does.

### 3. `Saltworks.SaltMiner.UiApiClient/Helpers/FileHelper.cs`

- **`CreateFileAsync(IFormFile, user, userName, fileRepo, isAttachment)`** — when `isAttachment`,
  write into `FilePathResolver.PendingPath(...)` instead of the flat root (Attachment record still
  created with no engagement, unchanged). Leave the `isAttachment == false` generic-upload path
  writing flat (those aren't engagement-scoped). Controllers already `Path.GetFileName` the result,
  so the return contract is unaffected.
- **New `MoveToEngagementFolder(fileId, engagementId, fileRepo, shard)`** — resolve current location
  via `ResolveByFileId`, compute dest via `BuildPath`, `Directory.CreateDirectory` + `File.Move`.
  Idempotent: `src == dest` is a no-op; dest-already-exists is treated as success (multi-reference /
  race). Only delete a leftover `src` when it sits in `_pending` (never delete a legacy-flat source
  other records may still resolve to). Missing src → log warning, return. Wrap in try/catch.
- **`DeleteFile(fileId, fileRepo, isAttachment, engagementId, shard)`** (extend signature) — keep the
  existing record lookup; capture `Saltminer.Engagement.Id` before deleting the record; resolve the
  physical path via `ResolveByFileId` and `File.Delete` it. Add a guard: if another attachment record
  still references the same FileId, delete only the record and leave the file.
- **`SearchFile(fileId, fileRepo, engagementId, shard)`** (new overload) → `ResolveByFileId(...)`;
  keep the old 2-arg overload delegating with `engagementId = null` for legacy/pending fallback.
- **`CloneFile(...)` + private `CreateFileAsync(fileName,...,fileStream)`** — add `engagementId` +
  `shard` params so clone flows (which already know the new engagement id) write straight into
  `{engagementId}/`. Update callers `AttachmentHelper.CloneEngagementAttachmentsAsync` /
  `CloneIssueAttachmentsAsync` to pass the engagement id. (While here, the pre-existing
  `File.Delete(oldUri)` bug in `AttachmentHelper` that deletes a bare filename relative to CWD should
  be fixed to resolve via `ResolveByFileId` of the old engagement.)
- **`ListAllFiles`** — make recursive (`SearchOption.AllDirectories`) so `FileController.List`
  reflects the new tree.

### 4. `Saltworks.SaltMiner.Ui.Api/Contexts/ContextBase.cs` + `FileContext.cs`

- **`SetAttachments`** (`ContextBase.cs:158`) — after setting `Engagement`/`Issue`/`IsMarkdown` and
  calling `AttachmentAddUpdate`, when `engagementId` is non-empty call
  `FileHelper.MoveToEngagementFolder(attachment.FileId, engagementId, Config.FileRepository, Config.FileShardEngagementFolders)`
  inside try/catch (metadata write is source of truth; a filesystem hiccup must not fail association,
  and resolve-fallback keeps the file reachable). This single point covers engagement, issue, report,
  and markdown callers — all pass `engagementId`.
- Add `internal string GetEngagementIdByFileId(string fileId)` next to `GetAttachmentByFileId`
  (`ContextBase.cs:283`) returning `Saltminer.Engagement?.Id` from the record.
- **`ContextBase.DeleteFile`** (`:135`) — look up engagement id (when `isAttachment`) and thread it +
  `Config.FileShardEngagementFolders` into `FileHelper.DeleteFile`.
- **`FileContext.SearchFile`** (`FileContext.cs:27`) — resolve engagement id first, pass it +
  shard flag into `FileHelper.SearchFile`.
- **`FileController`** needs **no signature changes** — `Download`/`Check`/`Delete`/`DownloadAttachment`
  already call the context methods above. This is the payoff of the pending+resolve design: the public
  file API is unchanged.

### 5. New hosted service — `Saltworks.SaltMiner.Ui.Api/Services/FileCleanupService.cs`

`class FileCleanupService : BackgroundService` modeled on `DataApi/LicenseService.cs`. Inject
`UiApiConfig`, `DataClientFactory<DataClient.DataClient>` (call `.GetClient()` — same pattern as
`ContextBase.cs:42`), and `ILogger<FileCleanupService>`.

`ExecuteAsync` loop:
- If `FileCleanupSchedule == Disabled` → sleep ~1h and re-check.
- Else compute next run via `ComputeNextRun(now, schedule, FileCleanupStartTime, FileCleanupWeeklyDay)`
  (Daily = today/tomorrow at `HH:mm`; Weekly = next `FileCleanupWeeklyDay` at `HH:mm`), `Task.Delay`
  in ≤1h chunks until the time (tolerates clock changes + prompt shutdown), then `RunCleanup(token)`
  in try/catch. (Config bound once at startup; changing schedule needs a restart — note in docs.)

`RunCleanup`:
1. **Legacy detection:** if any top-level (non-recursive) files exist in `FileRepository`,
   `LogWarning` that the legacy flat layout is present and the migration utility should be run.
   **Do not delete flat files** (operate only on the new structure, per requirement).
2. **`_pending`:** for each file (through shards), look up its Attachment record by FileId. If a record
   exists with an engagement id → self-heal by moving it into `{engagementId}/`. If no record and
   `LastWriteTimeUtc` older than `FileCleanupPendingMaxAgeHours` → delete (never-associated orphan).
   Younger files are left (may be mid-association).
3. **Each `{engagementId}` folder:** existence check via `DataClient.EngagementGet(id)` (exists =
   `Success && Data != null`). Be conservative — only a **definitive not-found** deletes; on transport
   error skip the folder this run. If the engagement is gone → `Directory.Delete(folder, recursive)`
   (engagement deletion already cascades attachment records via `AttachmentDeleteAllEngagement`). If it
   exists → build the valid FileId set from `DataClient.AttachmentSearch` filtered by
   `Saltminer.Engagement.Id == engagementId` (paged, like `GetAllEngagementAttachments`,
   `ContextBase.cs:391`) and delete on-disk files not in the set — now cheap because scoped to one
   engagement. Apply a ~1h `LastWriteTimeUtc` grace so a freshly-moved file racing the scan isn't
   reaped. `try/catch` per delete (skip-on-IOException, retry next run). Honor `stoppingToken`.

Register in `Program.cs` `ConfigureServices` (web host only, after the DataClient registration around
line 160): `builder.Services.AddHostedService<FileCleanupService>();` — runs under `HandleMain`, not
the console path.

### 6. Retire the old cleanup path

Remove: the `cleanup` verb + `HandleCleanUp` in `Program.cs` (~107-118, ~436), the
`CleanUpProcessor` registration in `ConfigureConsoleApp` (~461), the dispatch in `ConsoleApp.Run`
(`ConsoleApp.cs:48-49`), and delete `CleanUpProcessor.cs`.

### 7. Python migration utility — `Saltworks.SaltMiner.Python/RunUtilMigrateAttachments.py`

Follow the established `RunUtil*.py` pattern (see `RunUtilCleanOrphanAppVersionsV3.py`): construct
`Application`, get `es = app.GetElasticClient()`, and use `es.SearchScroll('attachments', query, scrollSize)`
with the scroller (`with es.SearchScroll(...) as scroller: ... scroller.GetNext()`).

Algorithm:
1. Scroll the entire `attachments` index once, building `{ fileId: engagementId }` from
   `_source.Saltminer.Attachment.FileId` + `_source.Saltminer.Engagement.Id`.
2. Walk the flat `FileRepository` top level (skip `_pending` and existing engagement dirs → idempotent).
3. Per file: if mapped with an engagement → move to `{repo}/{engagementId}/[{xx}/]{fileId}` (sharding
   matching `FilePathResolver.BuildPath`). If unmapped or no engagement (orphan / not-yet-associated
   markdown) → move to `{repo}/_pending/{xx}/{fileId}` so the C# pending reaper governs it by age.
   Never delete — migration is non-destructive; the cleanup service decides removal later.
4. Args: `--repo`, `--shard-engagement` (must match config), `--dry-run` (default; require `--commit`
   to move), optional `--report`. Idempotent (skip when dest exists); dry-run prints planned moves +
   summary counts (to-engagement / to-pending / skipped / unmapped).

## Edge cases
- **Unassociated markdown** stays in `_pending`; downloads resolve via fallback; reaped only after
  `FileCleanupPendingMaxAgeHours` if still record-less; moved once `SetAttachments` runs.
- **Issue-only attachment** carries engagement id → foldered under engagement (verified in
  `IssueController`/`IssueContext`).
- **FileId referenced by multiple records** — `MoveToEngagementFolder` is dest-exists-tolerant;
  `DeleteFile` checks for other referencing records before unlinking; cleanup's valid set (union for
  the engagement) keeps it.
- **Cleanup vs live upload/associate** — pending isolation + LastWriteTime grace + per-delete
  try/catch + skip-engagement-on-lookup-error.
- **Generic (`isAttachment == false`) uploads** stay flat and are ignored by cleanup (only warned on).

## Verification
- **Unit:** `FilePathResolver` (build/resolve probe order + fallback); `MoveToEngagementFolder`
  idempotency (src==dest, dest-exists, missing src); `FileHelper.CreateFileAsync` → `_pending`;
  `DeleteFile` resolve+multi-ref guard; `ComputeNextRun` for Daily/Weekly/Disabled incl. midnight cross.
- **`FileCleanupService`** (factor `RunCleanup` with injectable clock + mock DataClient): nonexistent
  engagement folder removed; existing engagement removes only orphans; `_pending` age reaping; grace
  window protects fresh files; legacy-flat warning emitted and flat files untouched.
- **Integration smoke:** run UI API with `FileCleanupSchedule=Daily` and start time ≈ now+1min;
  upload an attachment (lands in `_pending`) → associate to an engagement (moves) → download (resolves)
  → delete the engagement record, let cleanup run (folder removed); confirm a legacy flat file warns
  and survives.
- **Migration:** `--dry-run` against a copy of a real flat repo + ES snapshot; cross-check sample
  destinations equal `BuildPath`; `--commit` on a copy → confirm C# download resolves every migrated
  file; re-run → zero moves (idempotent).

## Critical files
- `Saltworks.SaltMiner.Ui.Api/Models/UiApiConfig.cs` (+ new `Models/FileCleanupScheduleMode.cs`)
- `Saltworks.SaltMiner.Ui.Api/appsettings.json` / `appsettings.VM.json`
- `Saltworks.SaltMiner.UiApiClient/Helpers/FileHelper.cs` (+ new `Helpers/FilePathResolver.cs`)
- `Saltworks.SaltMiner.UiApiClient/Helpers/AttachmentHelper.cs` (clone-flow callers + bug fix)
- `Saltworks.SaltMiner.Ui.Api/Contexts/ContextBase.cs`, `Contexts/FileContext.cs`
- `Saltworks.SaltMiner.Ui.Api/Services/FileCleanupService.cs` (new)
- `Saltworks.SaltMiner.Ui.Api/Program.cs`, `ConsoleApp.cs`, delete `CleanUpProcessor.cs`
- `Saltworks.SaltMiner.Python/RunUtilMigrateAttachments.py` (new)
