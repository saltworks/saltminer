# Source Adapter Template

A copy-and-fill adapter folder. The template carries the structural decisions so
you supply only the source-specific logic: the vendor API client and the field
mapping. KB 15 is the governing convention document; this folder implements it as
ready-to-copy code.

Three files (deliberately no more):

| File | Classes | Role |
| ---- | ------- | ---- |
| `TemplateClient.py` | `SourceExceptions` (+ subclasses), `TemplateClient`, `MockTemplateClient` | All HTTP/auth/paging against the vendor, and the exception taxonomy every failure is translated into. Nothing else in the adapter talks to the vendor or catches broadly. |
| `TemplateAdapter.py` | `TemplateAdapter`, `SourceLoader`, `SourceWorker` (+ `SourceWorkerFactory`) | The SourceMapping functions (vendor payloads → queue documents via the shared DTOs, index-name derivation, the source-side metric), the work-list builder that owns the NeedsUpdate gate, and the threaded worker script. |
| `TemplateRunner.py` | `TemplateRunner` | Entry glue invoked by `RunPythonAdapter.py`. Wires config, client, loader, adapter. Runnable directly for a no-op mock check. |

Shared change detection lives in `Core/SourceMetric.py` (`SourceMetric`,
`needs_update()`, `derive_local_metrics()`) — one implementation, no per-adapter
overrides.

---

## 1. Classify your source first

Every adapter is classified on **both** axes before any code is written. Declare
the classification in the `TemplateAdapter.py` module docstring (and here in your
copied README) — these are deliberately **not** config keys.

**Axis 1 — Processing model**

| Model | Definition | What you use |
| ----- | ---------- | ------------ |
| **Single-asset** | The source can fetch/process one asset at a time; each asset is an independent unit of work. | The threaded path: `SourceLoader.load_queue()` + `Core.Agent` + `SourceWorker`. |
| **Batch** | The source is pulled and processed as one run. | The non-threaded path: `SourceLoader.run()` + `TemplateAdapter` only. Delete the `SourceWorker` section and `TemplateRunner.run_sync` threaded body from your copy. |

**Axis 2 — Write semantics**

| Semantics | Definition | Detection |
| --------- | ---------- | --------- |
| **Replacement** | Issues are fully replaced per run; the adapter reports complete current state. | Default. |
| **Update** | Only deltas are reported. | The source's issue payloads carry a state/status lifecycle. |

Reference examples: Snyk is batch + replacement; Tanium is single-asset +
replacement. The template as shipped is single-asset + replacement.

---

## 2. Copy-and-fill walkthrough

1. **Copy the folder**: `Sources/Template/` → `Sources/Acme/`. Copy
   `Config/Sources/SourceTemplate.json` → `Config/Sources/Acme.json`.
2. **Rename**: classes `TemplateClient`/`TemplateAdapter`/`TemplateRunner` →
   `AcmeClient`/`AcmeAdapter`/`AcmeRunner`; files to match; fix the imports.
3. **Classify** on both axes (§1) and declare it in the adapter docstring. If
   non-threaded, delete the worker section and the threaded `run_sync`.
4. **Set the preset fields** at the top of the adapter: `VENDOR`, `PRODUCT`,
   `SOURCE_TYPE` (`"Saltworks.Acme"`), `ASSET_TYPE`, `ASSESSMENT_TYPE`,
   `LAST_UPDATED_ATTRIBUTE` (`"acme_last_updated"`), `QUEUE_TARGET_TYPE`.
5. **Fill the client**: auth headers, `get_assets_generator()`, `get_asset()`,
   `get_issues_generator()`. Keep the generator pattern and the exception
   translation; every request keeps its timeout.
6. **Fill the mapping**: `map_scan` / `map_asset` / `map_issue` plus the two
   `map_*_attributes` methods, and `build_source_metric()` from whatever the
   vendor's asset listing provides. `MockTemplateClient`'s payloads show the
   shape the shipped mappings read — change the mappings to your vendor's field
   names, not the other way around.
7. **Declare config**: `Config/Sources/Acme.json` with `Source`, `SourceName`,
   `Enabled`, `Instance`, `NeedsUpdateFields`, plus your vendor keys (§4).
8. **Register the entry point**: add your elif to `Sources/RunPythonAdapter.py`,
   passing the optional instance argument through:

   ```python
   elif prm_source.lower() == "acme":
       adapter = AcmeRunner(app, source_name=prm_instance)
   ```
9. **Check it runs**: `python Sources/Acme/AcmeRunner.py` from the repo root runs
   the mock dry run — mappings validated through the real DTOs, nothing sent.

---

## 3. The NeedsUpdate gate and the retirement rule

The queue chain is strictly ordered: **Create Scan → Create Asset (carries
QueueScanID) → Create Issues (carry QueueScanID + QueueAssetID)**. The gate sits
**before Create Scan**, inside `SourceLoader`. An asset that compares equal
produces *nothing* — no QueueScan, no QueueAsset, no QueueIssues — and the
Manager never processes it.

**The retirement rule (binding): skip at asset granularity only; never submit a
partial issue list.** The Manager reconciles only inside a submitted queue scan —
it matches the asset's existing issues on `Vulnerability.Scanner.Id` against the
submitted list and marks the misses removed. Therefore:

- An asset with no scan submitted is untouched. Whole-asset skip is safe.
- A submitted scan carrying a subset of the asset's real issues **retires the
  absent issues of that asset**. When an asset fails the gate, report its **full**
  current issue set. Never put incremental (`updated_after`) filters in
  `get_issues_generator()`.

Batch/replacement adapters satisfy the rule by construction; it is live for
single-asset adapters, which is exactly where the gate operates.

### How the local side is derived

There is no SQL database and no metrics index. `SourceLoader` derives the local
`SourceMetric` per asset by aggregating the source's **final issues index** — the
sanctioned direct-Elasticsearch verification read (`DataClient` remains
insert-only). That makes the comparison *delivery* truth: a downstream drop reads
as a mismatch and re-sends next run; pointing at a new instance finds an empty
index and triggers a full sync. Removed issues are excluded from the derived
counts, since the source reports current state.

The `last_scan` round trip: `build_source_metric()` takes the vendor's
last-updated value, `map_issue_attributes()` writes the same value onto every
issue as `LAST_UPDATED_ATTRIBUTE`, and the aggregation reads its max back out.
Only fields both sides can supply belong in `NeedsUpdateFields` — a field only
one side fills is a permanent mismatch and the gate never skips anything.
`derive_local_metrics` cannot reproduce metric `attributes`, so leave the source
side's `attributes` at `None` (both `None` compares equal) or drop `"Attributes"`
from `NeedsUpdateFields`.

---

## 4. Config reference (`Config/Sources/SourceTemplate.json`)

JSON carries no comments, so the keys are documented here instead.

| Key | Purpose |
| --- | ------- |
| `Source` | Source identity, ex `"ACME"`. |
| `SourceName` | Instance name and the config lookup key (`settings.GetSource(SourceName, ...)`), ex `"ACME1"`. Becomes the `Instance` field on every queued document. |
| `Enabled` | Ships `false`; operators enable at deployment. |
| `Instance` | The index-derivation instance segment, ex `"acme1"`. Defaults to `SourceName` lowercased when omitted; flag it if those ever diverge in practice. |
| `NeedsUpdateFields` | Which `SourceMetric` fields the gate compares for this source. Ships with the full set; remove fields your source or the local derivation cannot supply. Valid names: `LastScan`, `IssueCount`, `Critical`, `High`, `Medium`, `Low`, `Instance`, `SourceId`, `SourceType`, `IsNotScanned`, `Attributes`. Unknown names raise; an empty list raises. |
| `BaseUrl`, `ApiKey` | Vendor keys — replace with whatever your client needs. Keys ending in `ApiKey`/`Token`/`Password`/`Secret` are auto-encrypted. |
| `WorkerCount`, `WorkerErrorThreshold`, `PollingIntervalSecs`, `AgentId` | Threaded path only; delete for non-threaded adapters. |

### Running a second instance of the same source

The preset fields are the *source's* identity and never change between
instances — two Snyk deployments are both `Saltworks.Snyk`. Everything that
does differ per instance comes from config, keyed by `SourceName`:

1. Copy the instance config: `Acme1.json` → `Acme2.json`, setting `SourceName`
   `"ACME2"`, `Instance` `"acme2"`, and that instance's vendor credentials.
2. Run it by naming the instance in the CLI (fourth positional argument):

   ```
   python Sources/RunPythonAdapter.py acme true '' ACME2
   ```

No instance argument runs the adapter's first instance (`{SOURCE}1`). Each
instance gets its own derived indices (`issues_app_saltworks.acme_acme2`),
its own queue tag, and its own `Instance` field on every document.

---

## 5. Index-name derivation

The final index name is **derived, never written literally** (the CASE-024 fix,
generalized). One derivation per adapter — `derive_index_name()` in the adapter,
anchored to the Manager's own parse of `issues_[assetType]_[sourceType]_[instance]`:

```
issues_{asset_type}_{source_type}_{instance}
ex:  issues_app_saltworks.template_template1
```

`asset_type` and `source_type` are preset fields; `instance` (config `Instance`)
is the only per-deployment variable. Scans indices use the same shape with the
`scans_` prefix.

---

## 6. Assessment Type catalog

`SAST` (static analysis), `DAST` (dynamic), `Open` (SCA/open source), `IAC`,
`Cloud`, `License`, `Network`, `Custom`. If the source mixes types per issue, map
per-issue in `map_issue` and set the scan-level type to the dominant one.

---

## 7. What deliberately is NOT here

Harness/Core concerns; porting them into an adapter reproduces the C# base-class
coupling this architecture exists to avoid:

- Retirement logic (Manager-owned; §3 is a *reporting* rule)
- Backpressure, send-failure lifecycle/retry counters, cancellation plumbing
- Zero-issue / no-scan record generation (shared helper, tracked separately)
- Batch sizing and orchestration knobs beyond the worker settings above
