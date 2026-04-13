"""
GHASAdapter.py
==============
SaltMiner source adapter for GitHub Advanced Security (GHAS).

Implements the two-phase sync model:
  Phase 1 — Cheap change detection via local watermark (one API call per repo/engine)
  Phase 2 — Full alert fetch + ReplaceIssues=True for changed scopes only

Uses DataClient async API throughout. No ElasticClient dependency.
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
from Core.DataClient import DataClient, QueueStatus

logger = logging.getLogger(__name__)

# ── State Manager ─────────────────────────────────────────────────────────────

class GHASStateManager:
    """
    Manages the local JSON state file storing per-{org/repo/engine} watermarks.

    Watermark values are GitHub API updated_at timestamps (ISO8601 strings).
    Writes are atomic: temp file → rename. Concurrent writes are serialised
    by the caller via asyncio.Lock.

    State file schema:
    {
        "schema_version": 1,
        "last_updated": "<ISO8601>",
        "watermarks": {
            "<org>/<repo>/<engine>": "<ISO8601>",
            ...
        }
    }
    """

    SCHEMA_VERSION = 1

    def __init__(self, state_file_path: str):
        self._path = state_file_path
        self._data: dict = {"schema_version": self.SCHEMA_VERSION, "watermarks": {}}
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

        if data.get("schema_version") != self.SCHEMA_VERSION:
            logger.warning(
                "State file schema version %s does not match expected %s — starting fresh.",
                data.get("schema_version"), self.SCHEMA_VERSION
            )
            self._loaded = True
            return

        self._data = data
        self._loaded = True
        watermark_count = len(self._data.get("watermarks", {}))
        logger.info("Loaded state file with %d watermarks from '%s'.", watermark_count, self._path)

    def get_watermark(self, repo_full_name: str, engine: str) -> Optional[str]:
        """Return the stored watermark for this repo/engine, or None if not present."""
        key = f"{repo_full_name}/{engine}"
        return self._data.get("watermarks", {}).get(key)

    def set_watermark(self, repo_full_name: str, engine: str, timestamp: str):
        """Update the in-memory watermark. Call save_async() to persist."""
        key = f"{repo_full_name}/{engine}"
        if "watermarks" not in self._data:
            self._data["watermarks"] = {}
        self._data["watermarks"][key] = timestamp
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
      - State: local JSON file, atomic writes, asyncio.Lock for concurrent updates
    """

    # Default engines if not specified in config
    DEFAULT_ENGINES = ["code_scanning", "secret_scanning", "dependabot"]

    def __init__(self, app):
        self.client = GHASClient(app.Settings)
        self.sm_docs = SnykDocs()
        self._data_client = DataClient(app)

        self.instance = app.Settings.GetSource("GHAS", "SourceName") or "GHAS1"
        self.org = app.Settings.GetSource("GHAS", "Org")
        self.engines = app.Settings.GetSource("GHAS", "Engines") or self.DEFAULT_ENGINES
        self.include_sarif = app.Settings.GetSource("GHAS", "IncludeSarif") or False
        self.concurrency_limit = int(app.Settings.GetSource("GHAS", "ConcurrencyLimit") or 10)

        # Exclusion filters
        self.exclude_repos = set(app.Settings.GetSource("GHAS", "ExcludeRepos") or [])
        self.exclude_topics = set(app.Settings.GetSource("GHAS", "ExcludeTopics") or [])
        exclude_pat = app.Settings.GetSource("GHAS", "ExcludePattern") or ""
        self.exclude_pattern = re.compile(exclude_pat) if exclude_pat else None

        # State management
        state_file = app.Settings.GetSource("GHAS", "StateFile") or "./ghas-state.json"
        self._state = GHASStateManager(state_file)
        self._state_lock = asyncio.Lock()

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
            self._data_client.close()

    async def _run_async(self):
        async with self.client:
            await self.get_sync_async()

    # ── Main sync loop ─────────────────────────────────────────────────────

    async def get_sync_async(self):
        """
        Discover all repos, apply exclusions, then run concurrent repo/engine sync.
        """
        logger.info("Starting GHAS sync for org '%s'.", self.org)

        repos = await self.client.get_repos_async()
        repos = self._apply_exclusions(repos)
        logger.info("%d repositories to process after exclusions.", len(repos))

        sem = asyncio.Semaphore(self.concurrency_limit)
        tasks = []

        for repo in repos:
            try:
                enablement = await self.client.get_repo_enablement_async(repo["full_name"])
            except Exception as exc:
                logger.error("Failed enablement check for %s: %s — skipping.", repo["full_name"], exc)
                continue

            for engine in self.engines:
                if not enablement.get(engine, False):
                    logger.debug("Engine '%s' not enabled on %s — skipping.", engine, repo["full_name"])
                    continue
                tasks.append(self._sync_with_semaphore(sem, repo, engine))

        logger.info("Dispatching %d repo/engine sync tasks (concurrency limit: %d).", len(tasks), self.concurrency_limit)
        results = await asyncio.gather(*tasks, return_exceptions=True)

        failures = [r for r in results if isinstance(r, Exception)]
        if failures:
            logger.warning("%d sync task(s) failed out of %d total.", len(failures), len(tasks))
        else:
            logger.info("All %d sync tasks completed successfully.", len(tasks))

    async def _sync_with_semaphore(self, sem: asyncio.Semaphore, repo: dict, engine: str):
        async with sem:
            await self.sync_repo_engine_async(repo, engine)

    # ── Repo/engine sync ───────────────────────────────────────────────────

    async def sync_repo_engine_async(self, repo: dict, engine: str):
        """
        Two-phase sync for a single repo/engine combination.

        Phase 1: Fetch the most recent alert's updated_at, compare to watermark.
                 Skip if unchanged.
        Phase 2: Full alert fetch, queue Scan→Asset→Issues, advance watermark.
        """
        full_name = repo["full_name"]

        try:
            # ── Phase 1: Change detection ──────────────────────────────────
            watermark = self._state.get_watermark(full_name, engine)

            if watermark:
                latest_ts = await self.client.get_latest_alert_timestamp_async(full_name, engine)
                if latest_ts and latest_ts <= watermark:
                    logger.debug("No changes for %s/%s (latest=%s, watermark=%s) — skipping.",
                                 full_name, engine, latest_ts, watermark)
                    return
                logger.debug("Changes detected for %s/%s (latest=%s > watermark=%s).",
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
                logger.info("No alerts or SARIF findings for %s/%s — skipping queue.", full_name, engine)
                # Still advance watermark to now so we don't re-check immediately
                async with self._state_lock:
                    self._state.set_watermark(
                        full_name, engine,
                        datetime.now(timezone.utc).strftime("%Y-%m-%dT%H:%M:%SZ")
                    )
                    await self._state.save_async()
                return

            run_id = str(uuid.uuid4())
            report_id = f"{self.org}/{full_name}/{engine}/{run_id}"
            max_ts = max((a["updated_at"] for a in alerts), default=None)

            logger.info("Queueing %d alerts + %d SARIF findings for %s/%s.",
                        len(alerts), len(sarif_issues), full_name, engine)

            # ── 1: Scan ───────────────────────────────────────────────────
            mapped_scan = self.map_scan(repo, engine, report_id, alerts)
            queue_scan = await self._data_client.queue_scan_add_update_async(
                json.loads(mapped_scan.model_dump_json())
            )

            # ── 2: Asset ──────────────────────────────────────────────────
            mapped_asset = self.map_asset(repo, queue_scan["id"])
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

            # ── 5: Advance watermark ──────────────────────────────────────
            if max_ts:
                async with self._state_lock:
                    self._state.set_watermark(full_name, engine, max_ts)
                    await self._state.save_async()
                logger.debug("Watermark advanced for %s/%s → %s", full_name, engine, max_ts)

        except Exception as exc:
            logger.error(
                "Sync failed for %s/%s: %s", full_name, engine, exc, exc_info=True
            )

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
            logger.info("Excluded %d repo(s) by filter rules.", excluded_count)
        return filtered

    # ── DTO Mapping ────────────────────────────────────────────────────────

    def map_scan(self, repo: dict, engine: str, report_id: str, alerts: list) -> MapScanDocDTO:
        """Map repo + engine metadata to a SaltMiner scan document."""
        doc = self.sm_docs.GetScanDoc()
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
        doc["Saltminer"]["Scan"]["ScanDate"] = self._max_updated_at(alerts) or now
        doc["Saltminer"]["Scan"]["SourceType"] = "Saltworks.GHAS"
        doc["Saltminer"]["Scan"]["AssetType"] = "app"
        doc["Saltminer"]["Scan"]["Instance"] = self.instance

        return MapScanDocDTO(**doc)

    def map_asset(self, repo: dict, queue_scan_id: str) -> MapAssetDocDTO:
        """Map a GitHub repository to a SaltMiner asset document."""
        doc = self.sm_docs.GetAssetDoc()

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

        doc["Saltminer"]["Asset"]["Attributes"] = {
            "ghas_org": self.org,
            "ghas_repo_id": repo.get("id"),
            "ghas_repo_full_name": full_name,
            "ghas_default_branch": repo.get("default_branch") or "main",
            "ghas_visibility": repo.get("visibility") or "private",
            "ghas_topics": ",".join(topics) if topics else "",
        }

        return MapAssetDocDTO(**doc)

    def map_issue(
        self,
        alert: dict,
        engine: str,
        queue_scan_id: str,
        queue_asset_id: str,
        report_id: str,
    ) -> MapIssueDocDTO:
        """Map a raw GitHub alert to a SaltMiner issue document."""
        doc = self.sm_docs.GetIssueDoc()
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

        # Code Scanning location
        instance = alert.get("most_recent_instance") or {}
        if instance.get("location"):
            loc = instance["location"]
            path = loc.get("path", "")
            start_line = loc.get("start_line", "")
            start_col = loc.get("start_column", "")
            vuln["Location"] = path
            if path and start_line:
                vuln["LocationFull"] = f"{path}:{start_line}:{start_col}" if start_col else f"{path}:{start_line}"

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
        doc = self.sm_docs.GetIssueDoc()
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
        vuln["Location"] = sarif_result.get("location_path", "")
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
            "secret_scanning": "Custom",
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
            "ghas_alert_number": alert.get("number"),
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
