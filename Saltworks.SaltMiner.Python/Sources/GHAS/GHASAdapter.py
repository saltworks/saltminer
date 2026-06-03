"""
GHASAdapter.py
==============
SaltMiner source adapter for GitHub Advanced Security (GHAS).

Implements the two-phase sync model:
  Phase 1 — Cheap change detection via local watermark (one API call per repo/engine)
  Phase 2 — Full alert fetch + ReplaceIssues=True for changed scopes only

Clean-scan visibility (ENH-004, Option B):
  When an engine is enabled on a repo but produces zero alerts, the adapter still
  queues an empty Scan+Asset so SaltMiner can show the repo as "scanned and clean."
  ScanDate reflects honest per-engine execution evidence:
    - code_scanning  → most recent analysis.created_at (from /analyses endpoint)
    - dependabot     → repo.pushed_at
    - secret_scanning → repo.pushed_at
  Empty scans are only re-queued when this timestamp advances past the last
  queued value (state-file mitigation). Engines that appear unrun (null
  pushed_at, or no analyses records) are skipped entirely.

Uses DataClient async API throughout. No ElasticClient dependency.

Multi-instance support:
  The adapter is instantiated with a `source_name` that identifies which
  configured instance it represents (e.g. "ghas1", "ghas2"). All settings
  lookups use this key, so multiple instances can run from a single deployment
  by providing one config file per instance, each with a unique SourceName.
"""

import asyncio
import json
import logging
import os
import re
import tempfile
import uuid
from datetime import datetime, timezone
from typing import Optional

from Sources.GHAS.GHASClient import GHASClient, GHASEngineInaccessibleError, GHASRateLimitError
from Core.SmDocsAndDTOs import SnykDocs, MapAssetDocDTO, MapIssueDocDTO, MapScanDocDTO
from Core.DataClient import DataClient, QueueStatus

logger = logging.getLogger(__name__)

# ── State Manager ─────────────────────────────────────────────────────────────

class GHASStateManager:
    """
    Manages the local JSON state file storing per-{org/repo/engine} watermarks
    and last-queued scan dates.

    Two maps are tracked:
      watermarks     — max alert.updated_at observed per scope. Drives Phase 1
                       change detection. ISO8601 strings.
      last_scan_dates — the ScanDate value of the most recent clean Scan+Asset
                       queued for this scope. Drives ENH-004 clean-scan re-queue
                       mitigation: if a new ScanDate hasn't advanced past this,
                       a clean scan is not re-queued. ISO8601 strings.

    Writes are atomic: temp file → rename. Concurrent writes are serialised
    by the caller via asyncio.Lock.

    State file schema (v2):
    {
        "schema_version": 2,
        "last_updated": "<ISO8601>",
        "watermarks":      {"<org>/<repo>/<engine>": "<ISO8601>", ...},
        "last_scan_dates": {"<org>/<repo>/<engine>": "<ISO8601>", ...}
    }

    Schema v1 files (no last_scan_dates) load successfully and gain an empty
    last_scan_dates map. On first save the file is rewritten as v2.

    Each adapter instance has its own state file (path is per-instance), so
    instances do not share or contend for state.
    """

    SCHEMA_VERSION = 2
    SUPPORTED_SCHEMA_VERSIONS = (1, 2)

    def __init__(self, state_file_path: str):
        self._path = state_file_path
        self._data: dict = {
            "schema_version": self.SCHEMA_VERSION,
            "watermarks": {},
            "last_scan_dates": {},
        }
        self._loaded = False

    def load(self):
        """Load state from disk. Call once at startup before sync begins."""
        if not os.path.exists(self._path):
            logger.info("State file not found at '%s' — starting fresh.", self._path)
            self._loaded = True
            return

        try:
            with open(self._path, "r", encoding="utf-8") as f:
                data = json.load(f)
        except (json.JSONDecodeError, OSError) as exc:
            raise RuntimeError(
                f"State file '{self._path}' exists but cannot be parsed: {exc}. "
                "Delete or repair the file before running the adapter."
            ) from exc

        version = data.get("schema_version")
        if version not in self.SUPPORTED_SCHEMA_VERSIONS:
            logger.warning(
                "State file '%s' schema version %s not supported (expected one of %s) — starting fresh.",
                self._path, version, self.SUPPORTED_SCHEMA_VERSIONS,
            )
            self._loaded = True
            return

        # Forward-compatible upgrade from v1 → v2: ensure both maps exist.
        if "last_scan_dates" not in data:
            data["last_scan_dates"] = {}
        if "watermarks" not in data:
            data["watermarks"] = {}

        if version != self.SCHEMA_VERSION:
            logger.info(
                "Upgrading state file '%s' from schema v%s to v%s (existing data preserved).",
                self._path, version, self.SCHEMA_VERSION,
            )
            data["schema_version"] = self.SCHEMA_VERSION

        self._data = data
        self._loaded = True
        wm_count = len(self._data.get("watermarks", {}))
        sd_count = len(self._data.get("last_scan_dates", {}))
        logger.info(
            "Loaded state file from '%s' (%d watermarks, %d clean-scan dates).",
            self._path, wm_count, sd_count,
        )

    # ── Watermark accessors (alert.updated_at tracking) ────────────────────

    def get_watermark(self, repo_full_name: str, engine: str) -> Optional[str]:
        """Return the stored alert-updated watermark for this scope, or None."""
        key = f"{repo_full_name}/{engine}"
        return self._data.get("watermarks", {}).get(key)

    def watermark_count(self) -> int:
        """Number of watermarks currently loaded. Zero ⇒ empty/new state file
        ⇒ a re-baseline run (every scope will first-sync)."""
        return len(self._data.get("watermarks", {}))

    def clear_watermark(self, repo_full_name: str, engine: str):
        """Remove a scope's watermark (used after tombstoning an archived repo
        so the empty-replacement is not re-fired every subsequent run)."""
        key = f"{repo_full_name}/{engine}"
        if key in self._data.get("watermarks", {}):
            del self._data["watermarks"][key]
            self._data["last_updated"] = datetime.now(timezone.utc).strftime("%Y-%m-%dT%H:%M:%SZ")

    def set_watermark(self, repo_full_name: str, engine: str, timestamp: str):
        """Update the in-memory alert-updated watermark. Call save_async() to persist."""
        key = f"{repo_full_name}/{engine}"
        if "watermarks" not in self._data:
            self._data["watermarks"] = {}
        self._data["watermarks"][key] = timestamp
        self._data["last_updated"] = datetime.now(timezone.utc).strftime("%Y-%m-%dT%H:%M:%SZ")

    # ── Last-scan-date accessors (clean-scan re-queue mitigation) ──────────

    def get_last_scan_date(self, repo_full_name: str, engine: str) -> Optional[str]:
        """Return the ScanDate of the most recent clean scan queued for this scope, or None."""
        key = f"{repo_full_name}/{engine}"
        return self._data.get("last_scan_dates", {}).get(key)

    def set_last_scan_date(self, repo_full_name: str, engine: str, timestamp: str):
        """Record the ScanDate of a just-queued clean scan. Call save_async() to persist."""
        key = f"{repo_full_name}/{engine}"
        if "last_scan_dates" not in self._data:
            self._data["last_scan_dates"] = {}
        self._data["last_scan_dates"][key] = timestamp
        self._data["last_updated"] = datetime.now(timezone.utc).strftime("%Y-%m-%dT%H:%M:%SZ")

    async def save_async(self):
        """Atomically write the current state to disk (temp file → rename)."""
        dir_name = os.path.dirname(os.path.abspath(self._path))
        os.makedirs(dir_name, exist_ok=True)

        fd, tmp_path = tempfile.mkstemp(dir=dir_name, suffix=".tmp")
        try:
            with os.fdopen(fd, "w", encoding="utf-8") as f:
                json.dump(self._data, f, indent=2)
            os.replace(tmp_path, self._path)
        except Exception:
            try:
                os.unlink(tmp_path)
            except OSError:
                pass
            raise

# ── Adapter ───────────────────────────────────────────────────────────────────

class GHASAdapter:
    """
    SaltMiner source adapter for GitHub Advanced Security.

    Collects Code Scanning, Secret Scanning, and Dependabot alerts from the
    GitHub REST API and queues them to SaltMiner via the DataClient async API.

    Sync model:
      - Phase 1: one API call per repo/engine to check for changes since watermark
      - Phase 2: full alert fetch + ReplaceIssues=True for changed scopes only
      - State: local JSON file per instance, atomic writes,
               asyncio.Lock for concurrent updates within a single instance
    """

    # Default engines if not specified in config
    DEFAULT_ENGINES = ["code_scanning", "secret_scanning", "dependabot"]

    def __init__(self, app, source_name: str = "GHAS"):
        """
        Construct an adapter for the named GHAS instance.

        Args:
            app: Application context (provides Settings and DataClient context).
            source_name: The lookup key used by app.Settings.GetSource(...).
                         Should match the SourceName field in the corresponding
                         config file (e.g. "ghas1" → loads ghas1.json with
                         SourceName == "ghas1"). Defaults to "GHAS" for
                         backward compatibility with single-instance deployments.
        """
        self._source_name = source_name

        self.client = GHASClient(app.Settings, source_name)
        self.sm_docs = SnykDocs()
        self._data_client = DataClient(app)

        self.instance = app.Settings.GetSource(source_name, "SourceName") or source_name
        self.org = app.Settings.GetSource(source_name, "Org")
        self.engines = app.Settings.GetSource(source_name, "Engines") or self.DEFAULT_ENGINES
        self.include_sarif = app.Settings.GetSource(source_name, "IncludeSarif") or False
        self.concurrency_limit = int(app.Settings.GetSource(source_name, "ConcurrencyLimit") or 10)

        # Exclusion filters
        self.exclude_repos = set(app.Settings.GetSource(source_name, "ExcludeRepos") or [])
        self.exclude_topics = set(app.Settings.GetSource(source_name, "ExcludeTopics") or [])
        exclude_pat = app.Settings.GetSource(source_name, "ExcludePattern") or ""
        self.exclude_pattern = re.compile(exclude_pat) if exclude_pat else None

        # State management — default path includes the instance name so multiple
        # instances running from the same working directory do not collide.
        state_file = (
            app.Settings.GetSource(source_name, "StateFile")
            or f"./ghas-state-{self.instance}.json"
        )
        self._state = GHASStateManager(state_file)
        self._state_lock = asyncio.Lock()

        # Tracks scopes skipped because the engine is not accessible (403/404
        # on alert endpoints) for a PERMISSION/ENABLEMENT reason. Surfaced in
        # the run summary, distinct from rate-limited and archived scopes.
        self._inaccessible_scopes: list = []
        # Tracks scopes that could not complete because GitHub's SECONDARY rate
        # limit could not be cleared within the retry budget. These are NOT
        # permission problems — the data is accessible, we were throttled. They
        # keep their watermark (no advance) so they re-attempt next run.
        self._rate_limited_scopes: list = []
        # Tracks archived repos that were tombstoned (findings purged) this run.
        self._archived_purged_scopes: list = []
        # Counts scopes that ingested alerts vs. recorded a clean/heartbeat scan.
        self._ingested_scopes: list = []
        self._clean_scopes: list = []
        # Scopes whose WRITE to SaltMiner raised (e.g. a bulk issue batch
        # rejected by the DataApi). Surfaced by name in the summary so a
        # write-side data loss can never again hide behind a clean run.
        self._write_failed_scopes: list = []   # (scope, error) tuples

        # Run-tag: a single id/timestamp for this whole run, stamped onto every
        # queued asset/issue doc (see map_asset/_issue_attributes) so the
        # orphan-cleanup script can find docs NOT written by the latest run.
        self._run_id = str(uuid.uuid4())
        self._run_ts = self._now()

        # is_rebaseline is a SNAPSHOT taken once at run_sync start (after state
        # load, before any sync mutates state). Fixed for the entire run so the
        # archived-tombstone behaviour and the client's rate-limit posture are
        # consistent across the concurrent fan-out. See run_sync.
        self._is_rebaseline = False

    # ── Entry points ───────────────────────────────────────────────────────

    def run_sync(self, first_load=False):
        """
        Synchronous entry point called by RunGHASAdapter.py.

        first_load parameter accepted for interface compatibility but not used —
        absence of a watermark in the state file drives first-load behaviour
        automatically. To force a re-baseline, delete keys from the state file.
        """
        try:
            self._state.load()

            # is_rebaseline SNAPSHOT: empty state (zero watermarks) at startup
            # means this is a cold full-pull — every scope will first-sync. Fix
            # this boolean now, before any task mutates state, so all archived /
            # rate-limit decisions are consistent across the concurrent run.
            self._is_rebaseline = (self._state.watermark_count() == 0)
            if self._is_rebaseline:
                logger.warning(
                    "[%s] Empty state file → RE-BASELINE run. Archived repos will be "
                    "tombstoned (findings purged) and the client will use "
                    "rate-limit-safe code-scanning settings.",
                    self.instance,
                )
                # Switch the client into avoidance-first + deep-retry posture so
                # code-scanning tries to complete in a single pass.
                self.client.enable_rebaseline_mode()

            asyncio.run(self._run_async())
        finally:
            self._data_client.close()

    async def _run_async(self):
        async with self.client:
            await self.get_sync_async()

    # ── Main sync loop ─────────────────────────────────────────────────────

    async def get_sync_async(self):
        """
        Discover all repos, apply exclusions, divert archived repos to the
        tombstone/purge path, then run concurrent repo/engine sync for the
        active repos.
        """
        logger.info("[%s] Starting GHAS sync for org '%s'.", self.instance, self.org)

        repos = await self.client.get_repos_async()
        repos = self._apply_exclusions(repos)

        # Split archived from active. Archived repos are handled by the
        # tombstone path (purge findings) and are NEVER sent through the alert
        # fetch — so an archived repo can never produce a 403/404 that would be
        # misread as "engine inaccessible". `archived` comes straight from the
        # repo metadata (get_repos_async uses type=all and includes archived).
        active_repos = [r for r in repos if not r.get("archived")]
        archived_repos = [r for r in repos if r.get("archived")]

        logger.info(
            "[%s] %d repositories after exclusions (active: %d, archived: %d).",
            self.instance, len(repos), len(active_repos), len(archived_repos),
        )

        sem = asyncio.Semaphore(self.concurrency_limit)

        # ── Active repos: normal engine fan-out ────────────────────────────
        # We deliberately do NOT pre-check security_and_analysis on the repo
        # metadata: that field is only returned to tokens with administrative
        # permissions, so read-only alert tokens would falsely report all
        # engines disabled. The per-engine alert endpoint returns 404 when an
        # engine is genuinely not enabled, handled gracefully downstream.
        # Code-scanning request volume is additionally paced/concurrency-gated
        # inside the client to respect GitHub's secondary rate limit.
        active_tasks = [
            self._sync_with_semaphore(sem, repo, engine)
            for repo in active_repos
            for engine in self.engines
        ]

        # ── Archived repos: tombstone (purge) fan-out ──────────────────────
        # On a re-baseline run, tombstone every archived repo unconditionally
        # (no watermark exists to gate on, and the point is to scrub stale
        # findings). On a normal incremental run, the tombstone is watermark-
        # gated inside _tombstone_archived_async (only purge scopes we actually
        # collected before), so never-collected archived repos stay untouched.
        archived_tasks = [
            self._tombstone_with_semaphore(sem, repo, engine)
            for repo in archived_repos
            for engine in self.engines
        ]

        all_tasks = active_tasks + archived_tasks
        logger.info(
            "[%s] Dispatching %d task(s): %d active sync + %d archived-tombstone "
            "(concurrency limit: %d%s).",
            self.instance, len(all_tasks), len(active_tasks), len(archived_tasks),
            self.concurrency_limit,
            ", RE-BASELINE rate-limit posture" if self._is_rebaseline else "",
        )
        results = await asyncio.gather(*all_tasks, return_exceptions=True)

        failures = [r for r in results if isinstance(r, Exception)]
        self._log_run_summary(len(all_tasks), len(active_tasks), len(archived_tasks), failures)

    def _log_run_summary(self, total_tasks, active_tasks, archived_tasks, failures):
        """Emit a single human-readable end-of-run summary block.

        Categories are mutually meaningful and chosen so a glance tells you
        whether the run was healthy. Crucially, RATE-LIMITED is reported
        separately from INACCESSIBLE: the former is a throttling/tuning issue
        (data is reachable, we were slowed), the latter a permission/enablement
        issue. Conflating them (as the old summary did) is what made a
        rate-limit problem look like a PAT-permissions problem.
        """
        ingested = len(self._ingested_scopes)
        clean = len(self._clean_scopes)
        archived_purged = len(self._archived_purged_scopes)
        rate_limited = len(self._rate_limited_scopes)
        inaccessible = len(self._inaccessible_scopes)
        failed = len(failures)
        alerts_total = sum(n for _, n in self._ingested_scopes)

        bar = "=" * 58
        lines = [
            "",
            bar,
            f"  GHAS SYNC SUMMARY — {self.instance} (org: {self.org})",
            f"  Mode: {'RE-BASELINE (cold full pull)' if self._is_rebaseline else 'incremental'}",
            bar,
            f"  Tasks dispatched     : {total_tasks:>6}   (active sync: {active_tasks}, archived tombstone: {archived_tasks})",
            f"    ingested w/ alerts : {ingested:>6}   ({alerts_total} alerts queued)",
            f"    clean (heartbeat)  : {clean:>6}   (zero open alerts, scan recorded)",
            f"    archived (purged)  : {archived_purged:>6}   (findings tombstoned via ReplaceIssues)",
            f"    RATE-LIMITED       : {rate_limited:>6}   {'← retried next run; data IS reachable' if rate_limited else ''}",
            f"    INACCESSIBLE       : {inaccessible:>6}   {'← permission/enablement; see note below' if inaccessible else ''}",
            f"    failed (error)     : {failed:>6}   {'← unhandled errors; check traceback logs' if failed else ''}",
            f"    WRITE-FAILED       : {len(self._write_failed_scopes):>6}   {'← scope(s) raised on write; data NOT persisted — see list below' if self._write_failed_scopes else ''}",
        ]

        # Per-engine alert breakdown (only the engines that ingested anything).
        if self._ingested_scopes:
            by_engine = {}
            for scope, n in self._ingested_scopes:
                eng = scope.rsplit("/", 1)[-1]
                by_engine[eng] = by_engine.get(eng, 0) + n
            eng_str = "   ".join(f"{e}: {c}" for e, c in sorted(by_engine.items()))
            lines.append(f"  Alerts by engine     : {eng_str}")

        lines.append(bar)
        logger.info("\n".join(lines))

        # Explicit, greppable per-scope detail for the actionable categories.
        if self._rate_limited_scopes:
            logger.warning(
                "[%s] RATE-LIMITED scopes (not collected this run — will retry next run): %s",
                self.instance, ", ".join(self._rate_limited_scopes),
            )
        if self._inaccessible_scopes:
            logger.info(
                "[%s] INACCESSIBLE scopes (permission/enablement; prior data preserved): %s",
                self.instance, ", ".join(self._inaccessible_scopes),
            )
        if self._write_failed_scopes:
            logger.error(
                "[%s] WRITE-FAILED scopes (data NOT persisted this run — investigate): %s",
                self.instance,
                "; ".join(f"{scope} ({err})" for scope, err in self._write_failed_scopes),
            )

        # Systemic diagnostics, now correctly attributing the likely cause.
        if rate_limited and rate_limited >= max(1, total_tasks // 4):
            cs_share = sum(1 for s in self._rate_limited_scopes if s.endswith("/code_scanning"))
            logger.warning(
                "[%s] %d scope(s) hit GitHub's SECONDARY RATE LIMIT this run "
                "(%d on code_scanning). This is a throttling/tuning issue, NOT a "
                "permissions problem — the data is reachable. Lower "
                "CodeScanningConcurrencyLimit and/or raise CodeScanningMinIntervalMs "
                "(or the Rebaseline* equivalents) and re-run. Affected scopes kept "
                "their state and will be retried on the next run.",
                self.instance, rate_limited, cs_share,
            )
        if inaccessible and inaccessible >= max(1, total_tasks // 2):
            logger.warning(
                "[%s] %d of %d scopes are INACCESSIBLE (permission/enablement). "
                "If this is unexpected, verify the PAT has read access to code "
                "scanning, secret scanning, and dependabot alerts on org '%s': "
                "fine-grained PAT missing alert-read permissions; SAML SSO not "
                "authorized; fine-grained PAT awaiting org admin approval; PAT "
                "repo-selection scope too narrow. NOTE: secondary-rate-limit "
                "skips are counted separately as RATE-LIMITED above, so a high "
                "count here is genuinely about access, not throttling. "
                "See architecture doc §9.3.1.",
                self.instance, inaccessible, total_tasks, self.org,
            )

    async def _sync_with_semaphore(self, sem: asyncio.Semaphore, repo: dict, engine: str):
        async with sem:
            await self.sync_repo_engine_async(repo, engine)

    async def _tombstone_with_semaphore(self, sem: asyncio.Semaphore, repo: dict, engine: str):
        async with sem:
            await self._tombstone_archived_async(repo, engine)

    # ── Repo/engine sync ───────────────────────────────────────────────────

    async def sync_repo_engine_async(self, repo: dict, engine: str):
        """
        Two-phase sync for a single repo/engine combination.

        Phase 1: Fetch the most recent alert's updated_at across ALL states
                 (FIX-001 — must include closed states to detect dismissals).
                 Compare to watermark.
                 If unchanged, fall through to the heartbeat clean-scan path
                 (ENH-004) — the engine may still need a clean-scan re-queue
                 if execution evidence has advanced.
        Phase 2: Full alert fetch — open-state only (FIX-001).
          - If alerts present → queue Scan→Asset→Issues normally, advance state.
            Watermark advances to latest_ts_from_phase1 (or alerts max when
            Phase 1 didn't run), so it never regresses below a closed-state
            timestamp that Phase 2 can't see.
          - If zero alerts AND watermark was non-None on entry → REPLACEMENT
            event. All previously-open alerts have transitioned to closed.
            Queue an unconditional empty Scan+Asset replacement so SaltMiner
            removes the prior alerts by absence (ReplaceIssues=True).
          - If zero alerts AND no prior watermark → HEARTBEAT event. First-time
            sync of a scope that was always clean. Evaluate the existing
            ENH-004 strict-newer gated clean-scan path.
        """
        full_name = repo["full_name"]

        try:
            # ── Phase 1: Change detection ──────────────────────────────────
            watermark = self._state.get_watermark(full_name, engine)
            latest_ts_from_phase1: Optional[str] = None

            if watermark:
                latest_ts_from_phase1 = await self.client.get_latest_alert_timestamp_async(
                    full_name, engine
                )
                if latest_ts_from_phase1 and latest_ts_from_phase1 <= watermark:
                    logger.debug("No alert changes for %s/%s (latest=%s, watermark=%s).",
                                 full_name, engine, latest_ts_from_phase1, watermark)
                    # Alerts unchanged — but clean-scan may need re-queuing if
                    # engine-execution evidence has advanced.
                    await self._maybe_queue_clean_scan_async(repo, engine)
                    return
                logger.debug("Alert changes detected for %s/%s (latest=%s > watermark=%s).",
                             full_name, engine, latest_ts_from_phase1, watermark)
            else:
                logger.info("No watermark for %s/%s — first sync, fetching all alerts.", full_name, engine)

            # ── Phase 2: Full fetch (open-only per FIX-001) ────────────────
            alerts = []
            async for alert in self.client.get_alerts_async(full_name, engine):
                alerts.append(alert)

            # Collect SARIF suppressed findings (Code Scanning only, opt-in)
            sarif_issues = []
            if self.include_sarif and engine == "code_scanning":
                sarif_issues = await self._collect_sarif_issues_async(full_name)

            if not alerts and not sarif_issues:
                if watermark is not None:
                    # FIX-001 replacement event: previously-queued open alerts
                    # have all transitioned to closed states. Phase 1 detected
                    # the state change. Queue an unconditional empty Scan+Asset
                    # so SaltMiner removes the prior alerts by absence.
                    logger.info(
                        "Open alerts have all closed for %s/%s — queueing empty replacement.",
                        full_name, engine,
                    )
                    # latest_ts_from_phase1 may be None if Phase 1 returned no
                    # alerts in any state (alerts physically gone from GHAS,
                    # e.g. analysis retention). Fall back to now() so we don't
                    # re-trigger on the same event.
                    watermark_target = latest_ts_from_phase1 or self._now()
                    await self._maybe_queue_clean_scan_async(
                        repo, engine,
                        unconditional=True,
                        watermark_advance=watermark_target,
                    )
                else:
                    # Heartbeat: first sync of a scope that was always clean.
                    logger.info(
                        "No alerts on first sync for %s/%s — evaluating clean-scan heartbeat.",
                        full_name, engine,
                    )
                    await self._maybe_queue_clean_scan_async(repo, engine)
                return

            # Resolve Code Scanning analyses metadata up front for Asset
            # Attributes and (when present) honest ScanDate.
            latest_analysis = None
            if engine == "code_scanning":
                latest_analysis = await self._get_latest_analysis_async(full_name)

            run_id = str(uuid.uuid4())
            report_id = f"{full_name}/{engine}/{run_id}"  # full_name already includes the org (org/repo)
            scan_date = self._resolve_scan_date(repo, engine, latest_analysis) or self._now()

            # FIX-001: advance watermark to Phase 1's latest_ts (which reflects
            # max across ALL states) rather than max over the open-only Phase 2
            # set. Without this, the watermark regresses any time an alert
            # newer than the open set transitions to a closed state, causing
            # spurious re-triggers next sync. Falls back to alerts max only
            # when Phase 1 wasn't run (first-time sync with no prior watermark).
            new_watermark = latest_ts_from_phase1 or self._max_updated_at(alerts)

            logger.info("Queueing %d alerts + %d SARIF findings for %s/%s (ScanDate=%s).",
                        len(alerts), len(sarif_issues), full_name, engine, scan_date)

            # ── 1: Scan ───────────────────────────────────────────────────
            mapped_scan = self.map_scan(repo, engine, report_id, scan_date)
            queue_scan = await self._data_client.queue_scan_add_update_async(
                json.loads(mapped_scan.model_dump_json())
            )

            # ── 2: Asset ──────────────────────────────────────────────────
            mapped_asset = self.map_asset(repo, queue_scan["id"], engine, latest_analysis)
            queue_asset = await self._data_client.queue_asset_add_update_async(
                json.loads(mapped_asset.model_dump_json())
            )

            # ── 3: Issues ─────────────────────────────────────────────────
            for alert in alerts:
                mapped_issue = self.map_issue(
                    alert, engine, queue_scan["id"], queue_asset["id"], report_id
                )
                await self._data_client.queue_issue_add_update_batch_async(
                    json.loads(mapped_issue.model_dump_json())
                )

            for sarif_result in sarif_issues:
                mapped_issue = self.map_sarif_issue(
                    sarif_result, engine, queue_scan["id"], queue_asset["id"], report_id
                )
                await self._data_client.queue_issue_add_update_batch_async(
                    json.loads(mapped_issue.model_dump_json())
                )

            # ── 4: Flush and mark pending ─────────────────────────────────
            await self._data_client.queue_issue_add_update_batch_async(None)
            await self._data_client.queue_scan_update_status_async(
                queue_scan["id"], QueueStatus.PENDING
            )

            # ── 5: Advance state ──────────────────────────────────────────
            async with self._state_lock:
                if new_watermark:
                    self._state.set_watermark(full_name, engine, new_watermark)
                # Record the ScanDate so future clean-scan re-queues are
                # mitigated consistently regardless of whether the prior run
                # had alerts.
                self._state.set_last_scan_date(full_name, engine, scan_date)
                await self._state.save_async()
            logger.debug("State advanced for %s/%s — watermark=%s, last_scan_date=%s.",
                         full_name, engine, new_watermark, scan_date)
            self._ingested_scopes.append((f"{full_name}/{engine}", len(alerts) + len(sarif_issues)))

        except GHASRateLimitError as exc:
            # GitHub's SECONDARY rate limit could not be cleared within the
            # retry budget for this scope. This is a THROTTLING condition, not
            # a permission/enablement one — the data is reachable, we were
            # slowed. Critically, we do NOT advance state and do NOT fire the
            # replacement/heartbeat paths: the scope keeps its prior watermark
            # (or stays first-sync) so it re-attempts cleanly on the next run.
            # Counted separately from inaccessible so the summary attributes the
            # cause correctly (this is the misclassification the whole fix
            # exists to prevent).
            logger.warning(
                "Rate-limited %s/%s: %s. Scope NOT collected this run; state "
                "preserved for retry next run.",
                full_name, engine, exc,
            )
            self._rate_limited_scopes.append(f"{full_name}/{engine}")
            return

        except GHASEngineInaccessibleError as exc:
            # Engine not enabled / not accessible on this repo (403 or 404 from
            # the alert endpoints). Skip the scope without advancing state
            # and without firing replacement or heartbeat — see the exception
            # class docstring for the full rationale. Log at INFO so this
            # doesn't generate alarm-worthy log noise on every sync.
            logger.info(
                "Skipping %s/%s: %s. State and prior SaltMiner data preserved.",
                full_name, engine, exc,
            )
            self._inaccessible_scopes.append(f"{full_name}/{engine}")
            return

        except Exception as exc:
            # Log per-scope context (which scope failed, with traceback), record
            # the scope by name, then re-raise so
            # asyncio.gather(return_exceptions=True) counts it as a failure.
            # Without the re-raise, gather sees None and the run summary reports
            # "All tasks completed successfully" even when most scopes errored.
            # Recording the scope+error here makes a WRITE failure (e.g. a bulk
            # issue batch rejected by the DataApi) visible BY NAME in the summary
            # instead of an anonymous count — the all-or-nothing bulk batch once
            # dropped 203 alerts while the summary read "failed: 0".
            logger.error(
                "Sync failed for %s/%s: %s", full_name, engine, exc, exc_info=True
            )
            self._write_failed_scopes.append((f"{full_name}/{engine}", str(exc)))
            raise

    # ── Clean-scan path (ENH-004) ──────────────────────────────────────────

    async def _maybe_queue_clean_scan_async(
        self,
        repo: dict,
        engine: str,
        *,
        unconditional: bool = False,
        watermark_advance: Optional[str] = None,
    ):
        """
        Consider queueing an empty Scan+Asset for a repo with no alerts.

        Two callers:

        Heartbeat (default — unconditional=False, watermark_advance=None):
          ENH-004 Option B. Alerts unchanged on Phase 1, or first-time sync
          of a scope that was always clean. Strict-newer gate applies — only
          re-queues when execution evidence has actually advanced past the
          prior last_scan_date.

        Replacement (FIX-001 — unconditional=True, watermark_advance set):
          Phase 1 detected an alert state transition, Phase 2 returned zero
          alerts, watermark was non-None on entry. All previously-queued
          open alerts have transitioned to closed states. Bypasses the
          strict-newer gate because the trigger is a state-transition event,
          not a heartbeat tick — ReplaceIssues=True is the right tool to
          remove the prior alerts by absence. Also advances the watermark
          (in addition to last_scan_date) to the supplied target so the
          same dismissal event does not re-trigger on the next sync.

        Skips entirely (both modes) when there's no honest scan-execution
        evidence (engine appears unrun: for code_scanning, no analyses
        records; for the others, null pushed_at).

        Stamps the Asset with engine-specific "recently scanned" attributes.
        """
        full_name = repo["full_name"]

        latest_analysis = None
        if engine == "code_scanning":
            latest_analysis = await self._get_latest_analysis_async(full_name)

        scan_date = self._resolve_scan_date(repo, engine, latest_analysis)
        if scan_date is None:
            logger.info(
                "No execution evidence for %s/%s — engine appears unrun or "
                "repo has null pushed_at. Skipping clean-scan queue.",
                full_name, engine,
            )
            return

        if not unconditional:
            last_queued = self._state.get_last_scan_date(full_name, engine)
            if last_queued and not self._is_strictly_newer(scan_date, last_queued):
                logger.debug(
                    "Clean scan for %s/%s already current (scan_date=%s, last_queued=%s) — skipping.",
                    full_name, engine, scan_date, last_queued,
                )
                return

        run_id = str(uuid.uuid4())
        report_id = f"{full_name}/{engine}/{run_id}"  # full_name already includes the org (org/repo)

        kind = "empty replacement" if unconditional else "clean Scan+Asset (heartbeat)"
        logger.info(
            "Queueing %s for %s/%s (zero alerts, ScanDate=%s).",
            kind, full_name, engine, scan_date,
        )

        # ── 1: Scan ───────────────────────────────────────────────────────
        mapped_scan = self.map_scan(repo, engine, report_id, scan_date)
        queue_scan = await self._data_client.queue_scan_add_update_async(
            json.loads(mapped_scan.model_dump_json())
        )

        # ── 2: Asset ──────────────────────────────────────────────────────
        mapped_asset = self.map_asset(repo, queue_scan["id"], engine, latest_analysis)
        queue_asset = await self._data_client.queue_asset_add_update_async(
            json.loads(mapped_asset.model_dump_json())
        )

        # ── 3: Issues (none) — flush batch and mark pending ───────────────
        await self._data_client.queue_issue_add_update_batch_async(None)
        await self._data_client.queue_scan_update_status_async(
            queue_scan["id"], QueueStatus.PENDING
        )

        # ── 4: Record state ───────────────────────────────────────────────
        async with self._state_lock:
            if watermark_advance:
                self._state.set_watermark(full_name, engine, watermark_advance)
            self._state.set_last_scan_date(full_name, engine, scan_date)
            await self._state.save_async()
        logger.debug(
            "Clean scan recorded for %s/%s — last_scan_date=%s, watermark=%s.",
            full_name, engine, scan_date, watermark_advance,
        )
        self._clean_scopes.append(f"{full_name}/{engine}")

    async def _tombstone_archived_async(self, repo: dict, engine: str):
        """
        Purge a SaltMiner scope for an ARCHIVED GitHub repository by queueing an
        empty Scan+Asset with ReplaceIssues=True (the tombstone). SaltMiner
        removes the scope's prior alerts by absence.

        Archived repos reject the code-scanning alert endpoint with 403 and are
        excluded from org-level alert lists, so their findings would otherwise
        linger in SaltMiner forever (the "phantom" rows). This path is the
        deliberate, in-architecture purge.

        Firing policy (driven by the is_rebaseline snapshot taken at startup):
          - RE-BASELINE run (empty state): tombstone EVERY archived repo/engine
            unconditionally. No watermark exists to gate on, and the goal is to
            scrub all stale findings as part of the clean rebuild. A brand-new
            deployment will therefore queue empty Scan+Asset docs for already-
            archived repos it never collected — accepted as benign noise (the
            asset simply shows "archived, no findings").
          - INCREMENTAL run (state present): tombstone ONLY scopes we have a
            watermark for (previously collected). An archived repo we never
            collected has no watermark and is left untouched, so we don't
            manufacture empty assets for repos with no SaltMiner presence.

        ScanDate: a tombstone is a delete instruction, not a real scan — its
        date is semantically irrelevant. We deliberately use run-clock now()
        (no analyses/pushed_at lookup, no null-skip), so the purge can never be
        suppressed by missing execution evidence the way a clean scan can.

        After a successful tombstone the scope's watermark is cleared so the
        empty replacement is not re-fired on every subsequent run.
        """
        full_name = repo["full_name"]

        if not self._is_rebaseline:
            if self._state.get_watermark(full_name, engine) is None:
                logger.debug(
                    "Archived repo %s/%s has no watermark (never collected) — "
                    "nothing to purge, skipping tombstone.",
                    full_name, engine,
                )
                return

        try:
            run_id = str(uuid.uuid4())
            report_id = f"{full_name}/{engine}/{run_id}"  # full_name already includes the org (org/repo)
            scan_date = self._now()  # run-clock; see docstring

            logger.info(
                "Tombstoning archived repo %s/%s (purging findings via empty "
                "ReplaceIssues, ScanDate=%s).",
                full_name, engine, scan_date,
            )

            mapped_scan = self.map_scan(repo, engine, report_id, scan_date)
            queue_scan = await self._data_client.queue_scan_add_update_async(
                json.loads(mapped_scan.model_dump_json())
            )

            mapped_asset = self.map_asset(repo, queue_scan["id"], engine, None)
            queue_asset = await self._data_client.queue_asset_add_update_async(
                json.loads(mapped_asset.model_dump_json())
            )

            # No issues — flush empty batch and finalize. Empty replacement set
            # + ReplaceIssues=True removes prior alerts by absence.
            await self._data_client.queue_issue_add_update_batch_async(None)
            await self._data_client.queue_scan_update_status_async(
                queue_scan["id"], QueueStatus.PENDING
            )

            async with self._state_lock:
                self._state.clear_watermark(full_name, engine)
                await self._state.save_async()

            self._archived_purged_scopes.append(f"{full_name}/{engine}")

        except Exception as exc:
            logger.error(
                "Failed to tombstone archived %s/%s: %s",
                full_name, engine, exc, exc_info=True,
            )
            raise

    async def _get_latest_analysis_async(self, full_name: str) -> Optional[dict]:
        """
        Fetch the single most recent code-scanning analysis record, or None
        if the repo has never had an analysis run (or the engine is disabled —
        the analyses endpoint returns 404 in that case, which the client
        gracefully treats as "no records").
        """
        try:
            async for analysis in self.client.get_analyses_async(full_name):
                return analysis  # generator yields newest-first; first record suffices
        except Exception as exc:
            logger.warning("Failed to fetch analyses for %s: %s", full_name, exc)
        return None

    def _resolve_scan_date(
        self, repo: dict, engine: str, latest_analysis: Optional[dict]
    ) -> Optional[str]:
        """
        Determine the honest ScanDate for this repo/engine.

        - code_scanning: analysis.created_at from the most recent analysis.
                         No analyses → None (skip the clean-scan path).
        - dependabot, secret_scanning: repo.pushed_at.
                         Null pushed_at → None (skip the clean-scan path).
        """
        if engine == "code_scanning":
            if latest_analysis:
                ts = latest_analysis.get("created_at")
                return ts or None
            return None

        # Continuous engines key off repo activity as the scan-time proxy.
        pushed_at = repo.get("pushed_at")
        return pushed_at or None

    @staticmethod
    def _is_strictly_newer(candidate: str, baseline: str) -> bool:
        """
        Return True iff candidate represents a strictly newer instant than baseline.
        Both inputs are ISO8601 strings; falls back to string comparison if
        either fails to parse (defensive — GitHub timestamps are well-formed).
        """
        try:
            c = datetime.fromisoformat(candidate.replace("Z", "+00:00"))
            b = datetime.fromisoformat(baseline.replace("Z", "+00:00"))
            return c > b
        except (ValueError, AttributeError):
            return candidate > baseline

    # ── SARIF collection ───────────────────────────────────────────────────

    async def _collect_sarif_issues_async(self, full_name: str) -> list:
        """
        Fetch SARIF documents for the most recent analyses and extract
        suppressed results (results filtered out by GitHub's API layer).
        Returns a list of synthetic issue dicts ready for map_sarif_issue().
        """
        sarif_issues = []
        seen_rule_location = set()

        async for analysis in self.client.get_analyses_async(full_name):
            sarif_doc = await self.client.get_sarif_async(full_name, analysis["id"])
            if not sarif_doc:
                continue

            for run in sarif_doc.get("runs", []):
                tool_name = run.get("tool", {}).get("driver", {}).get("name", "Unknown")
                rules = {r["id"]: r for r in run.get("tool", {}).get("driver", {}).get("rules", [])}

                for result in run.get("results", []):
                    # SARIF suppressed results have a suppressions array
                    if not result.get("suppressions"):
                        continue

                    rule_id = result.get("ruleId", "unknown")
                    locations = result.get("locations", [])
                    loc_path = ""
                    if locations:
                        loc_path = (
                            locations[0]
                            .get("physicalLocation", {})
                            .get("artifactLocation", {})
                            .get("uri", "")
                        )

                    dedup_key = f"{rule_id}:{loc_path}"
                    if dedup_key in seen_rule_location:
                        continue
                    seen_rule_location.add(dedup_key)

                    rule_meta = rules.get(rule_id, {})
                    # SARIF carries the numeric security-severity (0.0–10.0) in
                    # the rule's properties block when the rule is security-
                    # relevant; quality rules omit it. Preserve it so the mapper
                    # can reproduce GitHub's bucket and classify security vs
                    # quality (mirrors the alert-path security_severity_level).
                    security_severity = (rule_meta.get("properties") or {}).get("security-severity")
                    sarif_issues.append({
                        "_sarif": True,
                        "rule_id": rule_id,
                        "rule_name": rule_meta.get("shortDescription", {}).get("text", rule_id),
                        "rule_description": rule_meta.get("fullDescription", {}).get("text", ""),
                        "tool_name": tool_name,
                        "location_path": loc_path,
                        "level": result.get("level", "warning"),
                        "security_severity": security_severity,
                        "suppression_reason": result.get("suppressions", [{}])[0].get("justification", ""),
                    })

        logger.debug("Collected %d SARIF suppressed findings for %s.", len(sarif_issues), full_name)
        return sarif_issues

    # ── Exclusion filtering ────────────────────────────────────────────────

    def _apply_exclusions(self, repos: list) -> list:
        """
        Filter the repo list applying all configured exclusion mechanisms.
        A repo is excluded if it matches ANY of the three criteria.
        """
        filtered = []
        for repo in repos:
            full_name = repo.get("full_name", "")
            name = repo.get("name", "")
            topics = set(repo.get("topics") or [])

            if full_name in self.exclude_repos or name in self.exclude_repos:
                logger.debug("Excluding repo '%s' (explicit list).", full_name)
                continue

            if self.exclude_topics and topics & self.exclude_topics:
                matched = topics & self.exclude_topics
                logger.debug("Excluding repo '%s' (topics: %s).", full_name, matched)
                continue

            if self.exclude_pattern and self.exclude_pattern.search(full_name):
                logger.debug("Excluding repo '%s' (pattern match).", full_name)
                continue

            filtered.append(repo)

        excluded_count = len(repos) - len(filtered)
        if excluded_count:
            logger.info("[%s] Excluded %d repo(s) by filter rules.", self.instance, excluded_count)
        return filtered

    # ── DTO Mapping ────────────────────────────────────────────────────────

    def map_scan(self, repo: dict, engine: str, report_id: str, scan_date: str) -> MapScanDocDTO:
        """
        Map repo + engine metadata to a SaltMiner scan document.

        scan_date is the resolved per-engine execution timestamp (see
        _resolve_scan_date). Caller guarantees it is non-None before reaching
        this method.
        """
        doc = self.sm_docs.map_scan_doc()
        now = self._now()

        doc["Timestamp"] = now
        doc["Saltminer"]["Internal"]["IssueCount"] = -1
        doc["Saltminer"]["Internal"]["ReplaceIssues"] = True
        doc["Saltminer"]["Internal"]["QueueStatus"] = QueueStatus.LOADING

        doc["Saltminer"]["Scan"]["AssessmentType"] = self._assessment_type(engine)
        doc["Saltminer"]["Scan"]["ProductType"] = "Application"
        doc["Saltminer"]["Scan"]["Product"] = "GitHub Advanced Security"
        doc["Saltminer"]["Scan"]["Vendor"] = "GitHub"
        doc["Saltminer"]["Scan"]["ReportId"] = report_id
        doc["Saltminer"]["Scan"]["ScanDate"] = scan_date
        doc["Saltminer"]["Scan"]["SourceType"] = "Saltworks.GHAS"
        doc["Saltminer"]["Scan"]["AssetType"] = "app"
        doc["Saltminer"]["Scan"]["Instance"] = self.instance

        return MapScanDocDTO(**doc)

    def map_asset(
        self,
        repo: dict,
        queue_scan_id: str,
        engine: Optional[str] = None,
        latest_analysis: Optional[dict] = None,
    ) -> MapAssetDocDTO:
        """
        Map a GitHub repository to a SaltMiner asset document.

        When engine and latest_analysis are supplied, engine-specific
        "recently scanned" attributes are stamped onto the asset (ENH-004).
        """
        doc = self.sm_docs.map_asset_doc()

        full_name = repo.get("full_name", "")
        short_name = repo.get("name", full_name.split("/")[-1] if "/" in full_name else full_name)
        topics = repo.get("topics") or []

        doc["Timestamp"] = self._now()
        doc["Saltminer"]["Internal"]["QueueScanId"] = queue_scan_id

        doc["Saltminer"]["Asset"]["Name"] = short_name
        doc["Saltminer"]["Asset"]["VersionId"] = full_name
        doc["Saltminer"]["Asset"]["Version"] = repo.get("default_branch") or "main"
        doc["Saltminer"]["Asset"]["SourceId"] = str(repo.get("id", ""))
        doc["Saltminer"]["Asset"]["SourceType"] = "Saltworks.GHAS"
        doc["Saltminer"]["Asset"]["AssetType"] = "app"
        doc["Saltminer"]["Asset"]["Instance"] = self.instance

        attrs = {
            "ghas_org": self.org,
            "ghas_repo_id": str(repo.get("id", "")),
            "ghas_repo_full_name": full_name,
            "ghas_default_branch": repo.get("default_branch") or "main",
            "ghas_visibility": repo.get("visibility") or "private",
            "ghas_topics": ",".join(topics) if topics else "",
            # Run-tag: stamps every asset with the current run's id/timestamp so
            # the companion orphan-cleanup script can identify assets NOT seen
            # in the most recent run (repos deleted/renamed/transferred at the
            # GitHub side, which the adapter can no longer enumerate and so
            # cannot tombstone in-band — Category-2 orphans).
            "ghas_run_id": self._run_id,
            "ghas_run_ts": self._run_ts,
        }

        # ENH-004: engine-specific "recently scanned" enrichment.
        # NOTE: Saltminer.Asset.Attributes.* values must all be strings — the
        # DataApi rejects booleans and integers (cf. ghas_repo_id, ghas_alert_number
        # already string-coerced elsewhere). Boolean "enabled" flags are
        # stringified to "true" rather than left as Python True.
        if engine == "code_scanning" and latest_analysis:
            attrs.update(self._code_scanning_asset_attrs(latest_analysis))
        elif engine == "dependabot":
            pushed_at = repo.get("pushed_at")
            attrs["ghas_dependabot_enabled"] = "true"
            if pushed_at:
                attrs["ghas_repo_pushed_at"] = pushed_at
        elif engine == "secret_scanning":
            pushed_at = repo.get("pushed_at")
            attrs["ghas_secret_scanning_enabled"] = "true"
            if pushed_at:
                attrs["ghas_repo_pushed_at"] = pushed_at

        doc["Saltminer"]["Asset"]["Attributes"] = attrs
        return MapAssetDocDTO(**doc)

    @staticmethod
    def _code_scanning_asset_attrs(analysis: dict) -> dict:
        """Build the ghas_last_code_scan_* attribute block from an analysis record."""
        tool = analysis.get("tool") or {}
        out = {
            "ghas_last_code_scan_at": analysis.get("created_at") or "",
            "ghas_last_code_scan_tool": tool.get("name") or "",
            "ghas_last_code_scan_tool_version": tool.get("version") or "",
            "ghas_last_code_scan_ref": analysis.get("ref") or "",
            "ghas_last_code_scan_commit_sha": analysis.get("commit_sha") or "",
        }
        env = analysis.get("environment")
        if env:
            out["ghas_last_code_scan_environment"] = (
                env if isinstance(env, str) else json.dumps(env)
            )
        return out

    def map_issue(
        self,
        alert: dict,
        engine: str,
        queue_scan_id: str,
        queue_asset_id: str,
        report_id: str,
    ) -> MapIssueDocDTO:
        """Map a raw GitHub alert to a SaltMiner issue document."""
        doc = self.sm_docs.map_issue_doc()
        assessment_type = self._assessment_type(engine)

        doc["Timestamp"] = self._now()
        doc["Saltminer"]["QueueScanId"] = queue_scan_id
        doc["Saltminer"]["QueueAssetId"] = queue_asset_id
        doc["Saltminer"]["IssueType"] = assessment_type

        # ── Vulnerability fields ───────────────────────────────────────────
        vuln = doc["Vulnerability"]
        # FIX-003: normalise to a single canonical UTC suffix. GitHub usually
        # returns created_at as "...Z", but some payloads/paths carry "+00:00"
        # and a naive "+ Z" elsewhere produced a "+00:00Z" double-suffix. The
        # helper collapses any of {Z, +00:00, +00:00Z, naive} to exactly one Z.
        vuln["FoundDate"] = self._iso_utc(alert.get("created_at")) or self._now()
        vuln["Name"] = self._alert_name(alert, engine)
        vuln["Severity"] = self._normalize_severity(alert, engine)
        vuln["IsRemoved"] = False
        vuln["Id"] = self._alert_ids(alert, engine)
        vuln["ReportId"] = report_id

        # Scanner sub-block
        vuln["Scanner"]["Id"] = str(alert.get("number", ""))
        vuln["Scanner"]["AssessmentType"] = assessment_type
        vuln["Scanner"]["Product"] = self._tool_name(alert, engine)
        vuln["Scanner"]["Vendor"] = "GitHub"
        vuln["Scanner"]["GuiUrl"] = alert.get("html_url")

        # Optional fields
        if alert.get("rule", {}).get("description"):
            vuln["Details"] = alert["rule"]["description"]
        if alert.get("rule", {}).get("help_uri"):
            vuln["Recommendation"] = alert["rule"]["help_uri"]

        # Location fields are required on every issue document by the platform
        # schema. Default both to "N/A" (non-empty placeholder per project
        # convention) and overwrite per engine when real data is available.
        vuln["Location"] = "N/A"
        vuln["LocationFull"] = "N/A"

        # Code Scanning location (most_recent_instance.location)
        if engine == "code_scanning":
            instance = alert.get("most_recent_instance") or {}
            loc = instance.get("location") or {}
            path = loc.get("path") or ""
            if path:
                vuln["Location"] = path
                start_line = loc.get("start_line")
                start_col = loc.get("start_column")
                if start_line:
                    vuln["LocationFull"] = (
                        f"{path}:{start_line}:{start_col}" if start_col else f"{path}:{start_line}"
                    )
                else:
                    vuln["LocationFull"] = path

        # ENH-001: Dependabot location from dependency block.
        elif engine == "dependabot":
            dep = alert.get("dependency") or {}
            manifest_path = dep.get("manifest_path") or ""
            pkg = dep.get("package") or {}
            ecosystem = pkg.get("ecosystem") or ""
            pkg_name = pkg.get("name") or ""
            if manifest_path:
                vuln["Location"] = manifest_path
                if ecosystem and pkg_name:
                    vuln["LocationFull"] = f"{manifest_path} ({ecosystem}: {pkg_name})"
                elif pkg_name:
                    vuln["LocationFull"] = f"{manifest_path} ({pkg_name})"
                else:
                    vuln["LocationFull"] = manifest_path
            elif pkg_name:
                # Manifest path missing but we still know the package — better
                # than "N/A" for downstream filtering.
                vuln["Location"] = pkg_name
                vuln["LocationFull"] = (
                    f"{ecosystem}: {pkg_name}" if ecosystem else pkg_name
                )

        # Secret Scanning location stays "N/A" until ENH-002 implements the
        # per-alert /locations endpoint fan-out.

        # CVSS score (Dependabot)
        if engine == "dependabot":
            cvss = (alert.get("security_advisory") or {}).get("cvss") or {}
            if cvss.get("score") is not None:
                doc["Vulnerability"]["Score"] = doc["Vulnerability"].get("Score", {})
                doc["Vulnerability"]["Score"]["Base"] = float(cvss["score"])

        # ── Saltminer attributes ───────────────────────────────────────────
        doc["Saltminer"]["Attributes"] = self._issue_attributes(alert, engine)

        return MapIssueDocDTO(**doc)

    def map_sarif_issue(
        self,
        sarif_result: dict,
        engine: str,
        queue_scan_id: str,
        queue_asset_id: str,
        report_id: str,
    ) -> MapIssueDocDTO:
        """Map a SARIF suppressed finding to a SaltMiner issue document."""
        doc = self.sm_docs.map_issue_doc()
        assessment_type = self._assessment_type(engine)
        now = self._now()

        doc["Timestamp"] = now
        doc["Saltminer"]["QueueScanId"] = queue_scan_id
        doc["Saltminer"]["QueueAssetId"] = queue_asset_id
        doc["Saltminer"]["IssueType"] = assessment_type

        vuln = doc["Vulnerability"]
        vuln["FoundDate"] = now
        vuln["Name"] = sarif_result.get("rule_name") or sarif_result.get("rule_id", "Unknown")
        # SARIF severity: if the rule carries a numeric security-severity, map it
        # to GitHub's bucket (true security finding, real severity preserved);
        # otherwise fall back to the error/warning/note level (quality finding).
        _sarif_score = sarif_result.get("security_severity")
        _sarif_bucket = self._sarif_security_severity_to_bucket(_sarif_score)
        if _sarif_bucket is not None:
            vuln["Severity"] = _sarif_bucket
            _sarif_finding_class = "security"
        else:
            vuln["Severity"] = self._sarif_level_to_severity(sarif_result.get("level", "warning"))
            _sarif_finding_class = "quality"
        vuln["IsRemoved"] = False
        vuln["IsSuppressed"] = True
        vuln["Id"] = []
        vuln["Details"] = sarif_result.get("rule_description", "")
        # Location fields are required on every issue. Default to "N/A" and
        # overwrite when the SARIF result actually carries a location path.
        sarif_location = sarif_result.get("location_path") or ""
        vuln["Location"] = sarif_location if sarif_location else "N/A"
        vuln["LocationFull"] = sarif_location if sarif_location else "N/A"
        vuln["ReportId"] = report_id

        vuln["Scanner"]["Id"] = f"sarif:{sarif_result.get('rule_id', 'unknown')}"
        vuln["Scanner"]["AssessmentType"] = assessment_type
        vuln["Scanner"]["Product"] = sarif_result.get("tool_name", "GitHub Advanced Security")
        vuln["Scanner"]["Vendor"] = "GitHub"

        sarif_attrs = {
            "ghas_engine": engine,
            "ghas_alert_state": "suppressed",
            "ghas_rule_id": sarif_result.get("rule_id", ""),
            "ghas_tool_name": sarif_result.get("tool_name", ""),
            "ghas_suppression_reason": sarif_result.get("suppression_reason", ""),
            "ghas_org": self.org,
            "ghas_run_id": self._run_id,
            "ghas_run_ts": self._run_ts,
            # Security-vs-quality classification (Option C). Filterable once the
            # issue index template maps these ghas_* attributes.
            "ghas_finding_class": _sarif_finding_class,
            "ghas_codeql_level": (sarif_result.get("level") or "").lower(),
        }
        if _sarif_score is not None and str(_sarif_score).strip() != "":
            sarif_attrs["ghas_security_severity_score"] = str(_sarif_score)
        doc["Saltminer"]["Attributes"] = sarif_attrs

        return MapIssueDocDTO(**doc)

    # ── Mapping helpers ────────────────────────────────────────────────────

    @staticmethod
    def _assessment_type(engine: str) -> str:
        return {
            "code_scanning": "SAST",
            "dependabot": "Open",
            "secret_scanning": "Secrets",
        }.get(engine, "Custom")

    @staticmethod
    def _now() -> str:
        return datetime.now(timezone.utc).strftime("%Y-%m-%dT%H:%M:%S.%f")[:-3] + "Z"

    @staticmethod
    def _iso_utc(value: Optional[str]) -> Optional[str]:
        """
        FIX-003: Return an ISO8601 UTC timestamp with exactly one trailing 'Z'
        suffix, regardless of the input's suffix form. Collapses all of:
            "2024-03-08T22:14:49Z"        → "2024-03-08T22:14:49Z"
            "2024-03-08T22:14:49+00:00"   → "2024-03-08T22:14:49Z"
            "2024-03-08T22:14:49+00:00Z"  → "2024-03-08T22:14:49Z"   (the bug)
            "2024-03-08T22:14:49"         → "2024-03-08T22:14:49Z"
        Non-UTC offsets are converted to UTC. Returns None for falsy input so
        the caller can fall back. On any parse failure the original value is
        returned unchanged (never worse than the input).
        """
        if not value:
            return None
        v = value.strip()
        # Strip the specific malformed double-suffix first.
        if v.endswith("+00:00Z"):
            v = v[:-1]  # drop the stray trailing Z → "...+00:00"
        try:
            dt = datetime.fromisoformat(v.replace("Z", "+00:00"))
            if dt.tzinfo is None:
                dt = dt.replace(tzinfo=timezone.utc)
            dt = dt.astimezone(timezone.utc)
            # Emit canonical Z form; preserve sub-seconds only if present.
            if dt.microsecond:
                return dt.strftime("%Y-%m-%dT%H:%M:%S.%f")[:-3] + "Z"
            return dt.strftime("%Y-%m-%dT%H:%M:%S") + "Z"
        except (ValueError, TypeError):
            return value

    @staticmethod
    def _max_updated_at(alerts: list) -> Optional[str]:
        timestamps = [a.get("updated_at") for a in alerts if a.get("updated_at")]
        return max(timestamps) if timestamps else None

    # Map a GitHub security-severity bucket (critical/high/medium/low) to the
    # SaltMiner title-cased scale. Shared by the alert and SARIF paths.
    _SECURITY_SEVERITY_MAP = {
        "critical": "Critical",
        "high": "High",
        "medium": "Medium",
        "moderate": "Medium",
        "low": "Low",
    }

    @staticmethod
    def _is_code_scanning_security(alert: dict) -> bool:
        """
        A code-scanning alert is a SECURITY finding iff GitHub assigned it a
        security_severity_level (critical/high/medium/low). Quality rules
        (maintainability, correctness, style) carry no security_severity_level
        — GitHub's security-severity views never count them, so SaltMiner must
        not place them in the Critical/High/Medium/Low buckets either. Null,
        empty, and absent are all treated as "non-security" (quality).
        """
        ssl = (alert.get("rule") or {}).get("security_severity_level")
        return bool(ssl) and str(ssl).strip().lower() in GHASAdapter._SECURITY_SEVERITY_MAP

    @staticmethod
    def _normalize_severity(alert: dict, engine: str) -> str:
        """
        Normalise GitHub severity to the SaltMiner title-cased five-value scale.

        Code-scanning classification (security vs quality):
          - security_severity_level present  -> map that bucket faithfully
            (this is a SECURITY finding; matches GitHub's security-severity view).
          - security_severity_level absent/null -> "Info" (this is a QUALITY
            finding; the legacy error/warning/note fallback is NOT used for the
            security bucket, so quality findings no longer inflate High/Medium).
        The original error/warning/note level is preserved separately as the
        ghas_codeql_level attribute (see _issue_attributes).
        """
        if engine == "secret_scanning":
            # GitHub classifies all exposed secrets as CRITICAL in its security
            # overview, and the REST secret-scanning alert object carries no
            # per-alert severity field. Use a blanket Critical default to match
            # GitHub's UI (was previously a blanket High, one notch low).
            return "Critical"

        if engine == "code_scanning":
            ssl = ((alert.get("rule") or {}).get("security_severity_level") or "").strip().lower()
            if ssl in GHASAdapter._SECURITY_SEVERITY_MAP:
                return GHASAdapter._SECURITY_SEVERITY_MAP[ssl]
            # No security severity -> quality finding -> Info (not error->High).
            return "Info"

        if engine == "dependabot":
            sev = ((alert.get("security_advisory") or {}).get("severity") or "").lower()
            return {
                "critical": "Critical", "high": "High", "medium": "Medium",
                "moderate": "Medium", "low": "Low",
            }.get(sev, "Medium")

        return "Medium"

    @staticmethod
    def _sarif_level_to_severity(level: str) -> str:
        return {
            "error": "High",
            "warning": "Medium",
            "note": "Info",
        }.get((level or "").lower(), "Medium")

    @staticmethod
    def _sarif_security_severity_to_bucket(score: Optional[str]) -> Optional[str]:
        """
        Map a SARIF rule's numeric `security-severity` (0.0–10.0) to the
        SaltMiner bucket using GitHub's own documented cutoffs:
            >= 9.0  Critical
            7.0–8.9 High
            4.0–6.9 Medium
            < 4.0   Low
        Returns None when the score is absent or unparseable, signalling the
        caller to treat the finding as quality (level-based) instead.
        """
        if score is None or str(score).strip() == "":
            return None
        try:
            v = float(score)
        except (ValueError, TypeError):
            return None
        if v >= 9.0:
            return "Critical"
        if v >= 7.0:
            return "High"
        if v >= 4.0:
            return "Medium"
        return "Low"

    @staticmethod
    def _alert_name(alert: dict, engine: str) -> str:
        if engine == "code_scanning":
            return (alert.get("rule") or {}).get("description") or \
                   (alert.get("rule") or {}).get("id") or "Unknown Rule"
        if engine == "secret_scanning":
            return alert.get("secret_type_display_name") or alert.get("secret_type") or "Secret Detected"
        if engine == "dependabot":
            return (alert.get("security_advisory") or {}).get("summary") or \
                   (alert.get("dependency") or {}).get("package", {}).get("name") or "Vulnerable Dependency"
        return "Unknown"

    @staticmethod
    def _alert_ids(alert: dict, engine: str) -> list:
        """Return a list of CVE/CWE IDs associated with the alert."""
        if engine == "dependabot":
            ids = []
            for identifier in (alert.get("security_advisory") or {}).get("identifiers", []):
                if identifier.get("type") in ("CVE", "GHSA"):
                    ids.append(identifier.get("value", ""))
            return [i for i in ids if i]
        if engine == "code_scanning":
            cwes = []
            for tag in (alert.get("rule") or {}).get("tags", []):
                if tag.startswith("external/cwe/"):
                    cwes.append(tag.replace("external/cwe/", "").upper())
            return cwes
        return []

    @staticmethod
    def _tool_name(alert: dict, engine: str) -> str:
        if engine == "code_scanning":
            return (alert.get("tool") or {}).get("name") or "CodeQL"
        if engine == "secret_scanning":
            return "GitHub Secret Scanning"
        if engine == "dependabot":
            return "Dependabot"
        return "GitHub Advanced Security"

    def _issue_attributes(self, alert: dict, engine: str) -> dict:
        attrs = {
            "ghas_engine": engine,
            "ghas_alert_state": alert.get("state") or "",
            "ghas_alert_number": str(alert.get("number", "")),
            "ghas_org": self.org,
            "ghas_repo": alert.get("repository", {}).get("full_name") or "",
            "ghas_run_id": self._run_id,
            "ghas_run_ts": self._run_ts,
        }

        if engine == "code_scanning":
            attrs["ghas_rule_id"] = (alert.get("rule") or {}).get("id") or ""
            attrs["ghas_tool_name"] = (alert.get("tool") or {}).get("name") or ""
            if alert.get("dismissed_reason"):
                attrs["ghas_dismissed_reason"] = alert["dismissed_reason"]
            # Security-vs-quality classification (Option C). security_severity_level
            # present => security finding (mapped to its bucket); absent => quality
            # finding (severity forced to Info). Preserve the raw error/warning/note
            # level so no signal is lost. Filterable once the index template maps
            # these ghas_* attributes.
            attrs["ghas_finding_class"] = (
                "security" if self._is_code_scanning_security(alert) else "quality"
            )
            attrs["ghas_codeql_level"] = ((alert.get("rule") or {}).get("severity") or "").lower()
            _ssl = (alert.get("rule") or {}).get("security_severity_level")
            if _ssl:
                attrs["ghas_security_severity_level"] = str(_ssl).lower()

        elif engine == "secret_scanning":
            attrs["ghas_rule_id"] = alert.get("secret_type") or ""
            if alert.get("resolution"):
                attrs["ghas_dismissed_reason"] = alert["resolution"]

        elif engine == "dependabot":
            dep = (alert.get("dependency") or {}).get("package") or {}
            attrs["ghas_package_name"] = dep.get("name") or ""
            attrs["ghas_package_ecosystem"] = dep.get("ecosystem") or ""
            attrs["ghas_rule_id"] = (alert.get("security_advisory") or {}).get("ghsa_id") or ""
            if alert.get("dismissed_reason"):
                attrs["ghas_dismissed_reason"] = alert["dismissed_reason"]

        return attrs
