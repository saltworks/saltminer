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

Compatible with SaltMiner 3.4 (uses SmDataClient — the synchronous PascalCase
SaltMiner client). GitHub API access remains async via aiohttp; SmDataClient
calls are dispatched to a thread pool via asyncio.to_thread() and serialised
across concurrent repo/engine tasks by a per-instance asyncio.Lock to avoid a
known concurrency hazard in SmDataClient's internal batch buffer.

No ElasticClient dependency — state tracking is via a local JSON file.

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

from Sources.GHAS.GHASClient import GHASClient
from Core.SmDocsAndDTOs import SnykDocs, MapAssetDocDTO, MapIssueDocDTO, MapScanDocDTO
from Core.SmDataClient import SmDataClient

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
    GitHub REST API and queues them to SaltMiner via SmDataClient (3.4 client).
    GitHub fetches remain async via aiohttp; SmDataClient calls are dispatched
    to threads via asyncio.to_thread() and serialised by self._queue_lock.

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
            app: Application context (provides Settings and the platform-wide
                 SMv3 configuration consumed by SmDataClient).
            source_name: The lookup key used by app.Settings.GetSource(...).
                         Should match the SourceName field in the corresponding
                         config file (e.g. "ghas1" → loads ghas1.json with
                         SourceName == "ghas1"). Defaults to "GHAS" for
                         backward compatibility with single-instance deployments.
        """
        self._source_name = source_name

        self.client = GHASClient(app.Settings, source_name)
        self.sm_docs = SnykDocs()
        # SmDataClient takes (appSettings, sourceName) and reads connection
        # config from the platform-level "SMv3" section.
        self._data_client = SmDataClient(app.Settings, source_name)

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

        # Serialises all SmDataClient calls across concurrent repo/engine tasks.
        # SmDataClient maintains an internal batch buffer (__IssueBatch) that is
        # not thread-safe — concurrent threads from asyncio.to_thread() could
        # race the threshold check and double-send. Holding this lock around
        # the entire scan→asset→issues→flush→finalize sequence eliminates the
        # hazard. Wall-clock impact is small because GitHub fetch time
        # dominates queue time.
        self._queue_lock = asyncio.Lock()

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
            asyncio.run(self._run_async())
        finally:
            # SmDataClient has no close() method; the underlying RestClient
            # (sync, requests-based) cleans up via garbage collection.
            pass

    async def _run_async(self):
        async with self.client:
            await self.get_sync_async()

    # ── Main sync loop ─────────────────────────────────────────────────────

    async def get_sync_async(self):
        """
        Discover all repos, apply exclusions, then run concurrent repo/engine sync.
        """
        logger.info("[%s] Starting GHAS sync for org '%s'.", self.instance, self.org)

        repos = await self.client.get_repos_async()
        repos = self._apply_exclusions(repos)
        logger.info("[%s] %d repositories to process after exclusions.", self.instance, len(repos))

        sem = asyncio.Semaphore(self.concurrency_limit)

        # Dispatch all engines for all repos. We deliberately do NOT pre-check
        # security_and_analysis on the repo metadata: that field is only
        # returned by GitHub to tokens with administrative permissions, so
        # tokens with the documented read-only alert permissions would falsely
        # report all engines disabled. Instead, the per-engine alert endpoint
        # in GHASClient returns 404 when an engine is not enabled, which
        # get_alerts_async() and get_latest_alert_timestamp_async() handle
        # gracefully (see architecture doc §9.4).
        tasks = [
            self._sync_with_semaphore(sem, repo, engine)
            for repo in repos
            for engine in self.engines
        ]

        logger.info(
            "[%s] Dispatching %d repo/engine sync tasks (concurrency limit: %d).",
            self.instance, len(tasks), self.concurrency_limit
        )
        results = await asyncio.gather(*tasks, return_exceptions=True)

        failures = [r for r in results if isinstance(r, Exception)]
        if failures:
            logger.warning(
                "[%s] %d sync task(s) failed out of %d total.",
                self.instance, len(failures), len(tasks)
            )
        else:
            logger.info("[%s] All %d sync tasks completed successfully.", self.instance, len(tasks))

    async def _sync_with_semaphore(self, sem: asyncio.Semaphore, repo: dict, engine: str):
        async with sem:
            await self.sync_repo_engine_async(repo, engine)

    # ── Repo/engine sync ───────────────────────────────────────────────────

    async def sync_repo_engine_async(self, repo: dict, engine: str):
        """
        Two-phase sync for a single repo/engine combination.

        Phase 1: Fetch the most recent alert's updated_at, compare to watermark.
                 If unchanged, fall through to the clean-scan path (ENH-004) —
                 the engine may still need a clean-scan re-queue if execution
                 evidence has advanced.
        Phase 2: Full alert fetch.
          - If alerts present → queue Scan→Asset→Issues normally, advance state.
          - If zero alerts → evaluate clean-scan path (ENH-004 Option B).
        """
        full_name = repo["full_name"]

        try:
            # ── Phase 1: Change detection ──────────────────────────────────
            watermark = self._state.get_watermark(full_name, engine)

            if watermark:
                latest_ts = await self.client.get_latest_alert_timestamp_async(full_name, engine)
                if latest_ts and latest_ts <= watermark:
                    logger.debug("No alert changes for %s/%s (latest=%s, watermark=%s).",
                                 full_name, engine, latest_ts, watermark)
                    # Alerts unchanged — but clean-scan may need re-queuing if
                    # engine-execution evidence has advanced.
                    await self._maybe_queue_clean_scan_async(repo, engine)
                    return
                logger.debug("Alert changes detected for %s/%s (latest=%s > watermark=%s).",
                             full_name, engine, latest_ts, watermark)
            else:
                logger.info("No watermark for %s/%s — first sync, fetching all alerts.", full_name, engine)

            # ── Phase 2: Full fetch ────────────────────────────────────────
            alerts = []
            async for alert in self.client.get_alerts_async(full_name, engine):
                alerts.append(alert)

            # Collect SARIF suppressed findings (Code Scanning only, opt-in)
            sarif_issues = []
            if self.include_sarif and engine == "code_scanning":
                sarif_issues = await self._collect_sarif_issues_async(full_name)

            if not alerts and not sarif_issues:
                logger.info("No alerts or SARIF findings for %s/%s — evaluating clean-scan path.",
                            full_name, engine)
                await self._maybe_queue_clean_scan_async(repo, engine)
                return

            # Resolve Code Scanning analyses metadata up front for Asset
            # Attributes and (when present) honest ScanDate.
            latest_analysis = None
            if engine == "code_scanning":
                latest_analysis = await self._get_latest_analysis_async(full_name)

            run_id = str(uuid.uuid4())
            report_id = f"{self.org}/{full_name}/{engine}/{run_id}"
            scan_date = self._resolve_scan_date(repo, engine, latest_analysis) or self._now()
            max_ts = max((a["updated_at"] for a in alerts), default=None)

            logger.info("Queueing %d alerts + %d SARIF findings for %s/%s (ScanDate=%s).",
                        len(alerts), len(sarif_issues), full_name, engine, scan_date)

            # SmDataClient is sync and not thread-safe across its internal
            # batch buffer. Hold the queue lock around the whole scan→asset→
            # issues→flush→finalize sequence so concurrent repo/engine tasks
            # cannot interleave and double-send batched issues.
            async with self._queue_lock:
                # ── 1: Scan ───────────────────────────────────────────────
                # SmDataClient.AddQueueScan sets QueueStatus="Loading" itself.
                mapped_scan = self.map_scan(repo, engine, report_id, scan_date)
                queue_scan = await asyncio.to_thread(
                    self._data_client.AddQueueScan,
                    json.loads(mapped_scan.model_dump_json())
                )

                # ── 2: Asset ──────────────────────────────────────────────
                mapped_asset = self.map_asset(repo, queue_scan["id"], engine, latest_analysis)
                queue_asset = await asyncio.to_thread(
                    self._data_client.AddQueueAsset,
                    json.loads(mapped_asset.model_dump_json())
                )

                # ── 3: Issues ─────────────────────────────────────────────
                # AddQueueIssue batches internally (BatchSize from SMv3 config).
                for alert in alerts:
                    mapped_issue = self.map_issue(
                        alert, engine, queue_scan["id"], queue_asset["id"], report_id
                    )
                    await asyncio.to_thread(
                        self._data_client.AddQueueIssue,
                        json.loads(mapped_issue.model_dump_json())
                    )

                for sarif_result in sarif_issues:
                    mapped_issue = self.map_sarif_issue(
                        sarif_result, engine, queue_scan["id"], queue_asset["id"], report_id
                    )
                    await asyncio.to_thread(
                        self._data_client.AddQueueIssue,
                        json.loads(mapped_issue.model_dump_json())
                    )

                # ── 4: Flush remaining batch and finalize the scan ────────
                # SendAllBatchIssues drains any queued issues below batch size.
                # FinalizeQueue flips QueueStatus from "Loading" to "Pending"
                # via GET /queuescan/status/{id}/Pending.
                await asyncio.to_thread(self._data_client.SendAllBatchIssues)
                await asyncio.to_thread(
                    self._data_client.FinalizeQueue, queue_scan["id"]
                )

            # ── 5: Advance state ──────────────────────────────────────────
            async with self._state_lock:
                if max_ts:
                    self._state.set_watermark(full_name, engine, max_ts)
                # Record the ScanDate so future clean-scan re-queues are
                # mitigated consistently regardless of whether the prior run
                # had alerts.
                self._state.set_last_scan_date(full_name, engine, scan_date)
                await self._state.save_async()
            logger.debug("State advanced for %s/%s — watermark=%s, last_scan_date=%s.",
                         full_name, engine, max_ts, scan_date)

        except Exception as exc:
            logger.error(
                "Sync failed for %s/%s: %s", full_name, engine, exc, exc_info=True
            )

    # ── Clean-scan path (ENH-004) ──────────────────────────────────────────

    async def _maybe_queue_clean_scan_async(self, repo: dict, engine: str):
        """
        Consider queuing an empty Scan+Asset for a repo with no alerts.

        Skips entirely when:
          - We have no honest scan-execution evidence (engine appears unrun:
            for code_scanning, no analyses endpoint records; for the others,
            null pushed_at).
          - The execution evidence hasn't advanced past the last clean-scan
            ScanDate we recorded for this scope.

        Stamps the Asset with engine-specific "recently scanned" attributes.

        Queues are dispatched via the same _queue_lock + asyncio.to_thread
        pattern as the alerts-present path to preserve SmDataClient's
        batch-buffer safety invariant.
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

        last_queued = self._state.get_last_scan_date(full_name, engine)
        if last_queued and not self._is_strictly_newer(scan_date, last_queued):
            logger.debug(
                "Clean scan for %s/%s already current (scan_date=%s, last_queued=%s) — skipping.",
                full_name, engine, scan_date, last_queued,
            )
            return

        run_id = str(uuid.uuid4())
        report_id = f"{self.org}/{full_name}/{engine}/{run_id}"

        logger.info(
            "Queueing clean Scan+Asset for %s/%s (zero alerts, ScanDate=%s).",
            full_name, engine, scan_date,
        )

        async with self._queue_lock:
            # ── 1: Scan ───────────────────────────────────────────────────
            mapped_scan = self.map_scan(repo, engine, report_id, scan_date)
            queue_scan = await asyncio.to_thread(
                self._data_client.AddQueueScan,
                json.loads(mapped_scan.model_dump_json())
            )

            # ── 2: Asset ──────────────────────────────────────────────────
            mapped_asset = self.map_asset(repo, queue_scan["id"], engine, latest_analysis)
            queue_asset = await asyncio.to_thread(
                self._data_client.AddQueueAsset,
                json.loads(mapped_asset.model_dump_json())
            )

            # ── 3: Issues (none) — flush remaining batch and finalize ─────
            # SendAllBatchIssues is a no-op when the buffer is empty, but we
            # call it anyway for symmetry with the alerts-present path.
            # FinalizeQueue flips QueueStatus from "Loading" to "Pending".
            await asyncio.to_thread(self._data_client.SendAllBatchIssues)
            await asyncio.to_thread(
                self._data_client.FinalizeQueue, queue_scan["id"]
            )

        # ── 4: Record last clean-scan ScanDate ────────────────────────────
        async with self._state_lock:
            self._state.set_last_scan_date(full_name, engine, scan_date)
            await self._state.save_async()
        logger.debug("Clean scan recorded for %s/%s → %s", full_name, engine, scan_date)

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
                    sarif_issues.append({
                        "_sarif": True,
                        "rule_id": rule_id,
                        "rule_name": rule_meta.get("shortDescription", {}).get("text", rule_id),
                        "rule_description": rule_meta.get("fullDescription", {}).get("text", ""),
                        "tool_name": tool_name,
                        "location_path": loc_path,
                        "level": result.get("level", "warning"),
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
        # QueueStatus is set by SmDataClient.AddQueueScan itself (to "Loading"
        # when immediate=False, the default). FinalizeQueue() flips it to
        # "Pending" at the end of the queueing sequence. The adapter does not
        # touch this field directly under the 3.4 client.

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
        }

        # ENH-004: engine-specific "recently scanned" enrichment.
        if engine == "code_scanning" and latest_analysis:
            attrs.update(self._code_scanning_asset_attrs(latest_analysis))
        elif engine == "dependabot":
            pushed_at = repo.get("pushed_at")
            attrs["ghas_dependabot_enabled"] = True
            if pushed_at:
                attrs["ghas_repo_pushed_at"] = pushed_at
        elif engine == "secret_scanning":
            pushed_at = repo.get("pushed_at")
            attrs["ghas_secret_scanning_enabled"] = True
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
        vuln["FoundDate"] = alert.get("created_at") or self._now()
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
        vuln["Severity"] = self._sarif_level_to_severity(sarif_result.get("level", "warning"))
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

        doc["Saltminer"]["Attributes"] = {
            "ghas_engine": engine,
            "ghas_alert_state": "suppressed",
            "ghas_rule_id": sarif_result.get("rule_id", ""),
            "ghas_tool_name": sarif_result.get("tool_name", ""),
            "ghas_suppression_reason": sarif_result.get("suppression_reason", ""),
            "ghas_org": self.org,
        }

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
    def _max_updated_at(alerts: list) -> Optional[str]:
        timestamps = [a.get("updated_at") for a in alerts if a.get("updated_at")]
        return max(timestamps) if timestamps else None

    @staticmethod
    def _normalize_severity(alert: dict, engine: str) -> str:
        """Normalise GitHub severity to SaltMiner title-cased five-value scale."""
        if engine == "secret_scanning":
            return "High"  # Secret Scanning has no severity field — default High

        # Code Scanning uses alert['rule']['severity'] or alert['rule']['security_severity_level']
        if engine == "code_scanning":
            sev = (
                (alert.get("rule") or {}).get("security_severity_level")
                or (alert.get("rule") or {}).get("severity")
                or ""
            ).lower()

        # Dependabot uses alert['security_advisory']['severity']
        elif engine == "dependabot":
            sev = ((alert.get("security_advisory") or {}).get("severity") or "").lower()
        else:
            sev = ""

        mapping = {
            "critical": "Critical",
            "high": "High",
            "medium": "Medium",
            "moderate": "Medium",
            "low": "Low",
            "note": "Info",
            "warning": "Low",
            "info": "Info",
            "error": "High",
        }
        return mapping.get(sev, "Medium")

    @staticmethod
    def _sarif_level_to_severity(level: str) -> str:
        return {
            "error": "High",
            "warning": "Medium",
            "note": "Info",
        }.get((level or "").lower(), "Medium")

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
        }

        if engine == "code_scanning":
            attrs["ghas_rule_id"] = (alert.get("rule") or {}).get("id") or ""
            attrs["ghas_tool_name"] = (alert.get("tool") or {}).get("name") or ""
            if alert.get("dismissed_reason"):
                attrs["ghas_dismissed_reason"] = alert["dismissed_reason"]

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
