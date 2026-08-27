''' --[auto-generated, do not modify this block]--
*
* SaltMiner - The open source vulnerability and pen testing management platform
* Copyright (C) 2024-2026 Saltworks Security, LLC
*
* This program is free software: you can redistribute it and/or modify
* it under the terms of the GNU General Public License as published by
* the Free Software Foundation, either version 3 of the License.
*
* This program is distributed in the hope that it will be useful,
* but WITHOUT ANY WARRANTY; without even the implied warranty of
* MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the
* GNU General Public License for more details.
*
* You should have received a copy of the GNU General Public License
* along with this program. If not, see <https://www.gnu.org/licenses/>.
*
* ----
'''

'''
Template source adapter - entry point, mapping, and loader in one file.
The folder is two files: this one and TemplateClient.py (all vendor HTTP).

CLASSIFICATION (WRITE: declare your classification - these are not config keys):
- Processing model: single-asset (each asset is an independent unit of work)
- Write semantics:  replacement (each run reports complete current state)
This records the DECLARED model.  The EXECUTED path is chosen by the Threaded
config key that run_sync() reads (see the RULINGS block below).

Two classes, in dependency order:

- TemplateAdapter: two roles in one class.  The ENTRY instance (built by
  RunPythonAdapter.py or by main() below) wires client + loader and owns
  run_sync(); WORKER instances (built per thread by SourceLoader.run_threaded)
  are mapping-only and never call run_sync().  The mapping role is the
  SourceMapping functions (vendor payloads -> queue documents via the shared
  DTOs), build_source_metric() for the NeedsUpdate gate, the index-name
  derivation, and sync_asset() - the three-tier chain
  for one asset: Create Scan -> Create Asset (carries QueueScanID) -> Create
  Issues (carry QueueScanID + QueueAssetID) -> flush -> set scan Pending.
  Cancel-on-failure contract (not vendor-specific, keep it when copying):
  any exit from that chain other than the Pending release sets the scan
  Loading -> Cancel so the Manager's default cleanup reaps it.

- SourceLoader: builds the work list and owns the NeedsUpdate gate.  run()
  drives the whole run in this thread (non-threaded adapters, incl. all batch
  adapters).  run_threaded() fans the gated assets out to an in-memory pool
  of worker threads, each owning its own client + adapter + DataClient;
  batch adapters delete it when copying.

RULINGS (Cameron, 2026-08-27):
- Threaded mode is in-memory only for this build - a bounded stdlib queue
  inside one process run, no persistence, no resume.  SMQ (persisted
  queue-index) integration is deliberately absent and may return later.  The
  DataApi queue_* staging indices are the adapter's only write path; the
  scheduled Manager takes over from there.
- The executed path is chosen by config, not by code a copier renames:
  Threaded=true (default) runs SourceLoader.run_threaded(); Threaded=false
  runs SourceLoader.run().  run_sync() is the single entry either way.

THE RETIREMENT RULE (binding - see the folder README for the evidence):
The Manager reconciles only inside a submitted queue scan.  A submitted scan
carrying a subset of an asset's real issues RETIRES the absent issues of that
asset.  So: skip at asset granularity only (the gate, before Create Scan),
and when an asset does get processed, sync_asset() must receive that asset's
FULL current issue set.
'''

import logging
import os
import queue      # DELETE-IF-BATCH: optional - queue/threading only serve run_threaded; Threaded=false is the supported route
import sys
import threading
from datetime import datetime, timezone

# Repo root, three levels up from Sources/Template/, so the mock check
# (python Sources/Template/TemplateAdapter.py) works standalone.
sys.path.insert(0, os.path.dirname(os.path.dirname(os.path.dirname(os.path.abspath(__file__)))))

from Core.Application import Application
from Core.DataClient import DataClient, QueueStatus
from Core.SmDocsAndDTOs import SmDocsAndDTOs, MapAssetDocDTO, MapIssueDocDTO, MapScanDocDTO, attrs
from Core.SourceMetric import NEEDS_UPDATE_FIELDS, SourceMetric, derive_local_metrics, needs_update

from Sources.Template.TemplateClient import (
    TemplateClient,
    MockTemplateClient,
    SourceMappingException,
)

logger = logging.getLogger(__name__)


# ===========================================================================
# preset fields - the identity of this source.  Set once when copying.
# ===========================================================================

# WRITE: set the six identity constants; SOURCE_TYPE is always "Saltworks.<ProductName>".
VENDOR          = "Template"                 # vendor company name
PRODUCT         = "Template"                 # vendor product name
SOURCE_TYPE     = "Saltworks.Template"       # always "Saltworks.<ProductName>"
ASSET_TYPE      = "app"                      # "app" for application security tools
ASSESSMENT_TYPE = "Open"                     # see the Assessment Type catalog in the README
PRODUCT_TYPE    = "Application"

# WRITE: rename to "<source>_last_updated" - both ends of the last_scan round trip read this constant.
# (build_source_metric()'s last_scan is written to every issue under this
# attribute by map_issue_attributes and read back by the local aggregation.)
LAST_UPDATED_ATTRIBUTE = "template_last_updated"

# Entry identity: SOURCE matches the Source value in the config file, and the
# default instance is the first config file's SourceName by convention.
# WRITE: set SOURCE to your source's uppercase name; the default instance is "{SOURCE}1".
SOURCE = "TEMPLATE"
DEFAULT_SOURCE_NAME = f"{SOURCE}1"


def derive_index_name(prefix: str, asset_type: str, source_type: str, instance: str) -> str:
    '''
    The one index-name derivation (CASE-024 fix, generalized).  Mirrors the
    Manager's own parse of its IssuesActiveIndexTemplate:

        {prefix}_{asset_type}_{source_type}_{instance}
        ex: issues_app_saltworks.template_template1

    Never write a literal final-index name anywhere in an adapter - always call
    this with the preset fields above plus the config Instance.  The only
    per-deployment variable is instance.
    '''
    if not all([prefix, asset_type, source_type, instance]):
        raise SourceMappingException(
            f"Cannot derive index name - missing segment(s) in "
            f"prefix={prefix!r} asset_type={asset_type!r} source_type={source_type!r} "
            f"instance={instance!r}.")
    return f"{prefix}_{asset_type.lower()}_{source_type.lower()}_{instance.lower()}"


def _utc_now():
    ''' Timestamp format every adapter in this repo writes. '''
    return datetime.now(timezone.utc).strftime("%Y-%m-%dT%H:%M:%S.%f")[:-3] + "Z"


# ===========================================================================
# TemplateAdapter - SourceMapping functions and the per-asset queue chain
# ===========================================================================

class TemplateAdapter:
    '''
    Two roles, one class - read this before copying:

    ENTRY role.  The instance RunPythonAdapter.py (or main() below) builds:
        adapter = TemplateAdapter(app, source_name=prm_instance)
        summary = adapter.run_sync(first_load=...)
    It wires the vendor client and the SourceLoader lazily and owns
    run_sync(), which reads its marching orders (Threaded, WorkerCount) from
    the source config file and dispatches to the loader.  Only app, an
    optional source_name and first_load can arrive from the CLI; everything
    else must come from config.

    WORKER role.  The instances SourceLoader.run_threaded() builds, one per
    thread, are mapping-only: they run sync_asset() and never call run_sync().
    Each owns its own DataClient - DataClient wraps a persistent asyncio loop
    and batches issues as instance state, so it must never be shared across
    threads.

    Orchestration lives in SourceLoader; HTTP lives in TemplateClient.  Keep
    it that way when copying.

    :app: Application instance
    :source_name: config lookup key (the SourceName value), ex "TEMPLATE1".
        Becomes the Instance field on every document.  None defaults to
        DEFAULT_SOURCE_NAME ({SOURCE}1).
    :data_client: optional pre-built DataClient (tests inject a mock).  Left
        None, the adapter builds its own on first use.
    :client: optional pre-built vendor client (a MockTemplateClient for the
        mock check).  Left None, TemplateClient(app.Settings, source_name) is
        built when first needed - entry role only.
    :dry_run: map and validate only; send nothing.  Used by the mock run.
    '''

    def __init__(self, app, source_name: str = None, data_client: DataClient = None,
                 client=None, dry_run: bool = False):
        source_name = source_name or DEFAULT_SOURCE_NAME
        settings = app.Settings
        self._app = app
        self._source_name = source_name
        self._sm_docs = SmDocsAndDTOs()
        self._dry_run = dry_run
        self._data_client = data_client
        self._client = client
        self._loader = None
        # Instance segment for index derivation defaults to SourceName
        # lowercased; config Instance overrides (flag it if those ever collide).
        self._instance = settings.GetSource(source_name, "Instance", None) or source_name.lower()

    # -- identity -------------------------------------------------------------

    @property
    def instance(self) -> str:
        ''' Index-segment instance, from config (default SourceName lowercased). '''
        return self._instance

    @property
    def issues_index(self) -> str:
        ''' Derived final issues index for this source instance. '''
        return derive_index_name("issues", ASSET_TYPE, SOURCE_TYPE, self._instance)

    @property
    def scans_index(self) -> str:
        ''' Derived final scans index for this source instance. '''
        return derive_index_name("scans", ASSET_TYPE, SOURCE_TYPE, self._instance)

    @property
    def last_updated_field(self) -> str:
        ''' Full ES path of the last-updated issue attribute, for the local metric aggregation. '''
        return f"saltminer.attributes.{LAST_UPDATED_ATTRIBUTE}"

    @property
    def data_client(self) -> DataClient:
        if self._data_client is None:
            if self._dry_run:
                return None
            self._data_client = DataClient(self._app)
        return self._data_client

    # -- the NeedsUpdate gate's source side -----------------------------------

    def build_source_metric(self, asset: dict) -> SourceMetric:
        '''
        The source-side SourceMetric for one asset, from the vendor payload.

        Inputs: the vendor asset payload as the client's asset listing yields it
            (no extra fetch - this runs for every asset, before the gate).
        Must return: a SourceMetric whose source_id is the vendor asset id
            (the same value map_asset writes to VersionId/SourceId, so it
            matches the local-metric bucket key) and whose last_scan is the
            vendor's last-updated value.
        Invariants: every field set here must be derivable on the local side
            too (Core.SourceMetric.derive_local_metrics), or be left out of
            NeedsUpdateFields in config - a field only one side can supply is
            a permanent mismatch and the gate never skips anything.  last_scan
            round-trips through LAST_UPDATED_ATTRIBUTE: map_issue_attributes
            writes it to every issue and the local aggregation reads its max
            back out.  attributes stays None unless your local derivation can
            reproduce it.
        '''
        return SourceMetric(
            source_id=str(asset["id"]),
            source_type=SOURCE_TYPE,
            instance=self._source_name,
            last_scan=asset.get("updated_at"),
            # WRITE: fill the counts the vendor's asset listing provides and add those fields
            # to NeedsUpdateFields; counts the vendor cannot provide must be removed from
            # NeedsUpdateFields in config (a placeholder 0 is a permanent mismatch).
            issue_count=0,
            critical=0,
            high=0,
            medium=0,
            low=0,
            is_not_scanned=False,
            attributes=None
        )

    # -- the per-asset queue chain --------------------------------------------

    def sync_asset(self, asset: dict, issues_iterable) -> int:
        '''
        The strictly ordered chain for one asset:
        Create Scan -> Create Asset -> Create Issues -> flush -> scan Pending.

        Inputs: the vendor asset payload; issues_iterable - the asset's FULL
            current issue set (retirement rule - never a delta, never a page).
        Must return: the number of issues sent (mapped and validated, in dry
            run).
        Invariants: the chain order above is strict - the asset carries the
            QueueScanId, the issues carry QueueScanId + QueueAssetId, and the
            partial batch is flushed before Pending.  Any exit other than the
            Pending release - including KeyboardInterrupt/SystemExit, hence
            BaseException - sets the scan Loading -> Cancel and re-raises the
            original failure unchanged.  Past Pending the scan belongs to the
            Manager and is never cancelled.  All writes go through DataClient.
        '''
        asset_id = str(asset["id"])
        report_id = f"{asset_id}|{_utc_now()}"

        mapped_scan = self.map_scan(asset, report_id)
        if self._dry_run:
            return self._dry_run_asset(asset, issues_iterable, report_id)

        queue_scan = self.data_client.queue_scan_add_update(mapped_scan)
        queue_scan_id = queue_scan["id"]
        # From here until the Pending release the scan is ours at Loading, a
        # status the Manager's cleanup deliberately never reaps.  Any exit
        # other than Pending - vendor error, mapping error, Ctrl-C - hands the
        # scan to the reaper as Cancel and re-raises unchanged.  BaseException
        # on purpose: KeyboardInterrupt/SystemExit are the graceful-shutdown
        # case and plain Exception would miss them.
        try:
            scan_report_id = queue_scan["saltminer"]["scan"]["reportId"]

            queue_asset = self.data_client.queue_asset_add_update(
                self.map_asset(asset, queue_scan_id))
            queue_asset_id = queue_asset["id"]

            issue_count = 0
            for issue in issues_iterable:
                self.data_client.queue_issue_add_update_batch(
                    self.map_issue(issue, queue_scan_id, queue_asset_id, scan_report_id, asset))
                issue_count += 1

            # Flush the partial batch before releasing, or the tail of the scan
            # is lost; Pending is what makes the scan visible to the Manager.
            # Pending stays inside the try: a scan that failed to go Pending is
            # still Loading and still ours to cancel.
            self.data_client.queue_issue_add_update_batch(None)
            self.data_client.queue_scan_update_status(queue_scan_id, QueueStatus.PENDING)
        except BaseException:
            self._cancel_abandoned_scan(queue_scan_id)
            raise
        # Past Pending the scan belongs to the Manager - never cancel it.
        logger.info("[TemplateAdapter] Asset %s released to Manager: %s issue(s).",
                    asset_id, issue_count)
        return issue_count

    def _cancel_abandoned_scan(self, queue_scan_id: str):
        '''
        Best-effort Loading -> Cancel on a scan this run abandoned mid-chain, so
        the Manager's CleanUpProcessor reaps it by default (Cancel is cleaned
        under CleanupCompleteAfterHours; Loading is disabled for cleanup).  The
        queue asset/issues are left alone - the reaper's orphan sweeps take
        them once the scan goes.  Never raises; the original failure must
        propagate unchanged.
        '''
        if self._data_client is None:
            return
        try:
            self._data_client.queue_scan_update_status(queue_scan_id, QueueStatus.CANCEL)
            logger.warning("[TemplateAdapter] Scan %s abandoned mid-chain, set to Cancel "
                           "for cleanup.", queue_scan_id)
        except Exception as ex:  # noqa: BLE001 - cleanup must not mask the original failure
            logger.error("[TemplateAdapter] Could not cancel abandoned scan %s: %s "
                         "(it will sit at Loading until manually cleaned).",
                         queue_scan_id, ex)

    def _dry_run_asset(self, asset: dict, issues_iterable, report_id: str) -> int:
        ''' Maps and validates everything, sends nothing.  Mock-run support. '''
        self.map_asset(asset, "dry-run-scan-id")
        issue_count = 0
        for issue in issues_iterable:
            self.map_issue(issue, "dry-run-scan-id", "dry-run-asset-id", report_id, asset)
            issue_count += 1
        logger.info("[TemplateAdapter] DRY RUN - asset %s mapped and validated: %s issue(s), "
                    "nothing sent.", asset.get("id"), issue_count)
        return issue_count

    # -- SourceMapping functions ----------------------------------------------
    #
    # Everything below maps one vendor payload shape (see MockTemplateClient
    # for samples) onto the shared document templates, validates via the
    # pydantic DTOs, and returns the dict.  Validation here means a document
    # the DataApi would reject fails with a stack trace pointing at the mapping
    # line instead of a DataClientException three network hops away.

    def map_scan(self, asset: dict, report_id: str) -> dict:
        '''
        Inputs: the vendor asset payload; report_id composed by sync_asset
            ("<asset id>|<utc now>"), unique per run.
        Must return: the queue-scan document dict, Internal.QueueStatus at
            Loading (sync_asset releases it to Pending at the end).
        Invariants: IssueCount -1 and ReplaceIssues True are the replacement
            semantics this template declares; the preset identity constants
            fill the Scan block; DTO validation stays as the last line before
            return.
        '''
        doc = self._sm_docs.map_scan_doc()
        doc["Timestamp"] = _utc_now()
        doc["Saltminer"]["Internal"]["IssueCount"] = -1       # disables count validation
        doc["Saltminer"]["Internal"]["ReplaceIssues"] = True  # replacement semantics
        doc["Saltminer"]["Internal"]["QueueStatus"] = QueueStatus.LOADING
        scan = doc["Saltminer"]["Scan"]
        scan["AssessmentType"] = ASSESSMENT_TYPE
        scan["ProductType"]    = PRODUCT_TYPE
        scan["Product"]        = PRODUCT
        scan["Vendor"]         = VENDOR
        scan["ReportId"]       = report_id
        # WRITE: map ScanDate from the vendor's scan / last-updated timestamp.
        scan["ScanDate"]       = asset.get("updated_at") or _utc_now()
        scan["SourceType"]     = SOURCE_TYPE
        scan["AssetType"]      = ASSET_TYPE
        scan["Instance"]       = self._source_name
        MapScanDocDTO(**doc)
        return doc

    def map_asset(self, asset: dict, queue_scan_id: str) -> dict:
        '''
        Inputs: the vendor asset payload; the QueueScan id returned by the
            DataApi for this asset's scan.
        Must return: the queue-asset document dict carrying that QueueScanId
            in Internal.
        Invariants: VersionId and SourceId are both the vendor asset id - the
            local-metric bucket key and build_source_metric().source_id must
            agree with them; attributes go through map_asset_attributes; DTO
            validation stays as the last line before return.
        '''
        doc = self._sm_docs.map_asset_doc()
        doc["Timestamp"] = _utc_now()
        doc["Saltminer"]["Internal"]["QueueScanId"] = queue_scan_id
        mapped = doc["Saltminer"]["Asset"]
        # WRITE: map Name/Version/VersionId/SourceId from the vendor asset payload.
        # (VersionId and SourceId: the same stable vendor id the local-metric bucket key uses.)
        mapped["Name"]       = asset.get("name")
        mapped["Version"]    = asset.get("version")
        mapped["VersionId"]  = str(asset["id"])
        mapped["SourceId"]   = str(asset["id"])
        mapped["SourceType"] = SOURCE_TYPE
        mapped["AssetType"]  = ASSET_TYPE
        mapped["Instance"]   = self._source_name
        self.map_asset_attributes(asset, doc)
        MapAssetDocDTO(**doc)
        return doc

    def map_issue(self, issue: dict, queue_scan_id: str, queue_asset_id: str,
                  report_id: str, asset: dict) -> dict:
        '''
        Inputs: one vendor issue payload; the QueueScan and QueueAsset ids for
            its asset; the scan's report id as the DataApi returned it; the
            vendor asset payload (for Scanner.Id and the GUI link).
        Must return: the queue-issue document dict carrying QueueScanId +
            QueueAssetId, IssueType, the Vulnerability block and Scanner block.
        Invariants: Scanner["Id"] must be stable across runs for the same
            issue+asset - it is the key the Manager matches on to decide
            update vs retire.  IsRemoved is written False and RemovedDate is
            set only when the vendor reports the issue resolved (setting
            RemovedDate is what flips IsRemoved inside SaltMiner).  Attributes
            go through map_issue_attributes; DTO validation stays as the last
            line before return.
        '''
        doc = self._sm_docs.map_issue_doc()
        doc["Timestamp"] = _utc_now()
        sm = doc["Saltminer"]
        sm["QueueScanId"] = queue_scan_id
        sm["QueueAssetId"] = queue_asset_id
        sm["IssueType"] = ASSESSMENT_TYPE

        vuln = doc["Vulnerability"]
        # WRITE: map the vendor issue payload onto the vuln fields; keep the `or "None"` fallbacks
        # for fields the vendor lacks.
        vuln["Name"]           = issue.get("title")
        vuln["FoundDate"]      = issue.get("created_at")
        vuln["Severity"]       = (issue.get("severity") or "Info").title()
        vuln["SourceSeverity"] = issue.get("severity")
        vuln["Id"]             = issue.get("cve_ids") or []
        vuln["Description"]    = issue.get("description") or "None"
        vuln["Recommendation"] = issue.get("recommendation")
        vuln["Location"]       = issue.get("location") or "None"
        vuln["LocationFull"]   = issue.get("location") or "None"
        vuln["ReportId"]       = report_id
        vuln["IsRemoved"]      = False
        # Setting RemovedDate is what flips IsRemoved inside SaltMiner.
        if issue.get("resolved_at"):
            vuln["RemovedDate"] = issue["resolved_at"]

        scanner = vuln["Scanner"]
        # WRITE: compose Scanner["Id"] from the vendor issue id + asset id - stable across runs
        # (the Manager's update-vs-retire match key; see the docstring contract).
        scanner["Id"]             = f"{issue['id']}|{asset['id']}"
        scanner["AssessmentType"] = ASSESSMENT_TYPE
        scanner["Product"]        = PRODUCT
        scanner["Vendor"]         = VENDOR
        scanner["GuiUrl"]         = self.build_gui_url(asset, issue)

        self.map_issue_attributes(issue, doc)
        MapIssueDocDTO(**doc)
        return doc

    def map_asset_attributes(self, asset: dict, doc: dict) -> dict:
        '''
        Source-specific asset metadata (team, owner, tags, ...).  Separate from
        map_asset because these are the fields most likely to change.

        Inputs: the vendor asset payload; the queue-asset document being built.
        Must set: Saltminer.Asset.Attributes via attrs() (an empty dict is
            valid).  Returns the document.
        Invariants: attribute values are metadata only - nothing the gate or
            the Manager keys on lives here.
        '''
        doc["Saltminer"]["Asset"]["Attributes"] = attrs({
            # WRITE: add vendor asset metadata here; "team" is the worked example.
            # "team": asset.get("team"),
        })
        return doc

    def map_issue_attributes(self, issue: dict, doc: dict) -> dict:
        '''
        Source-specific issue metadata.

        Inputs: the vendor issue payload; the queue-issue document being built.
        Must set: Saltminer.Attributes via attrs(), always including
            LAST_UPDATED_ATTRIBUTE.  Returns the document.
        Invariants: LAST_UPDATED_ATTRIBUTE is mandatory - it is the field the
            local-metric aggregation reads last_scan back from, and it must
            carry the same vendor last-updated value build_source_metric uses.
        '''
        doc["Saltminer"]["Attributes"] = attrs({
            LAST_UPDATED_ATTRIBUTE: issue.get("updated_at"),
            # WRITE: add further issue metadata after LAST_UPDATED_ATTRIBUTE; "status" is the worked example.
            # "status": issue.get("status"),
        })
        return doc

    def build_gui_url(self, asset: dict, issue: dict = None) -> str:
        '''
        The deep-link URL written to Scanner["GuiUrl"] on every issue.

        Inputs: the vendor asset payload; the vendor issue payload when called
            from map_issue.  issue is there for issue-level deep links and may
            be ignored.
        Must return: a URL string, or None when the source has no UI to link
            to.
        Invariants: pure composition, no HTTP.  Most real sources do not hand
            back a link directly - the pattern is host + project/asset path +
            issue anchor, usually reverse-engineered from the vendor UI, and
            this method is where that composition lives.
        '''
        # WRITE: compose the deep-link URL for your source if the API does not supply gui_url directly.
        return asset.get("gui_url")

    def close(self):
        ''' Flush any batched issues and release the DataClient.  Never raises. '''
        if self._data_client is None:
            return
        try:
            self._data_client.queue_issue_add_update_batch(None)
        except Exception as ex:  # noqa: BLE001 - close must not raise
            logger.error("[TemplateAdapter] Failed flushing issue batch on close: %s", ex)
        try:
            self._data_client.close()
        except Exception as ex:  # noqa: BLE001 - close must not raise
            logger.error("[TemplateAdapter] Failed closing DataClient: %s", ex)

    # -- entry role: client + loader wiring and run_sync ---------------------

    @property
    def client(self):
        ''' The vendor client, built from config on first use unless injected. '''
        if self._client is None:
            self._client = TemplateClient(self._app.Settings, self._source_name)
        return self._client

    @property
    def loader(self) -> "SourceLoader":
        ''' The SourceLoader over this adapter and its client, built on first use. '''
        if self._loader is None:
            self._loader = SourceLoader(self._app, self.client, self, self._source_name)
        return self._loader

    def run_sync(self, first_load: bool = False) -> dict:
        '''
        The single entry for a run - the only method RunPythonAdapter.py calls.

        Inputs: first_load (second CLI argument) - True bypasses the
            NeedsUpdate gate and loads everything the source has.  Every other
            parameter comes from the source config file, read in the block
            below.
        Must return: the loader's summary dict
            {"skipped", "completed", "errored", "issues"} from either path.
        Invariants: the executed path is chosen by the Threaded config key -
            true runs SourceLoader.run_threaded() with WorkerCount threads,
            false runs SourceLoader.run() in this thread.  Either way this
            entry adapter is closed in a finally (worker adapters close
            themselves on thread exit).  The entry instance never runs
            sync_asset() on a worker thread; worker instances never call this.
        '''
        settings = self._app.Settings
        # -- config keys read by run_sync (Config/Sources/<Source>.json) ------
        # WRITE: adjust the Threaded / WorkerCount defaults to the vendor API's tolerance; document
        # any source-specific keys you add beside them.
        threaded = str(settings.GetSource(self._source_name, "Threaded", True)).lower() == "true"
        worker_count = int(settings.GetSource(self._source_name, "WorkerCount", 5))
        # ---------------------------------------------------------------------
        try:
            if threaded:
                return self.loader.run_threaded(worker_count, first_load=first_load,
                                                client_factory=type(self.client))
            return self.loader.run(first_load=first_load)
        finally:
            self.close()


# ===========================================================================
# SourceLoader - builds the work list and owns the NeedsUpdate gate
# ===========================================================================

class SourceLoader:
    '''
    The gate sits BEFORE Create Scan, here and nowhere else.  An asset that
    compares equal produces *nothing*: no QueueScan, therefore no QueueAsset,
    no QueueIssues, and the Manager never touches it - which is exactly why
    whole-asset skips are safe (the Manager reconciles only inside a submitted
    scan).

    Two composition modes:
    - Threaded single-asset adapter: run_threaded() fans the gated assets out
      to an in-memory pool of worker threads, each owning its own client +
      adapter + DataClient.  In-process only; no SMQ, no persistence.
    - Non-threaded adapter (incl. all batch adapters): run() drives the whole
      run directly through TemplateAdapter.

    The local side of the comparison is derived from the source's final issues
    index (Core.SourceMetric.derive_local_metrics) - the sanctioned direct-ES
    verification read; DataClient remains insert-only.  There is no local
    metric store to maintain or wipe.

    :app: Application instance
    :client: constructed TemplateClient (or MockTemplateClient)
    :adapter: constructed TemplateAdapter - supplies the index derivation, the
        source-side metric, and (non-threaded mode) the sync chain
    :source_name: config lookup key (SourceName), ex "TEMPLATE1"
    '''

    def __init__(self, app, client, adapter, source_name: str):
        self._app = app
        self._client = client
        self._adapter = adapter
        self._source_name = source_name
        self._needs_update_fields = app.Settings.GetSource(
            source_name, "NeedsUpdateFields", list(NEEDS_UPDATE_FIELDS))
        self._skipped = 0
        self._matched = 0

    @property
    def skipped(self) -> int:
        ''' Assets the gate skipped this run. '''
        return self._skipped

    @property
    def matched(self) -> int:
        ''' Assets that passed the gate this run. '''
        return self._matched

    # -- the gate -------------------------------------------------------------

    def get_local_metrics(self) -> dict:
        '''
        {version_id: SourceMetric} derived from the final issues index.  The
        index name comes from the adapter's derivation helper - never a
        literal.  An absent index returns {} and every asset syncs in full.
        VersionId and SourceId are both the vendor asset id in this template,
        so the bucket key matches build_source_metric().source_id.
        '''
        return derive_local_metrics(
            self._app.GetElasticClient(),
            self._adapter.issues_index,
            self._adapter.last_updated_field)

    def iter_assets_needing_update(self, first_load: bool = False):
        '''
        Yields every vendor asset that fails the NeedsUpdate comparison (i.e.
        needs processing).  Skips happen at asset granularity only - an asset
        either passes through whole or produces nothing at all.

        :first_load: True bypasses the gate entirely - no local-metric
            derivation, no source metric built, no comparison.  Everything the
            source has is loaded.
        '''
        self._skipped = 0
        self._matched = 0
        if first_load:
            logger.info("[SourceLoader] First load - NeedsUpdate gate bypassed, "
                        "loading everything the source has.")
            for asset in self._client.get_assets_generator():
                self._matched += 1
                yield asset
            return
        local_metrics = self.get_local_metrics()
        for asset in self._client.get_assets_generator():
            source_metric = self._adapter.build_source_metric(asset)
            result = needs_update(source_metric,
                                  local_metrics.get(source_metric.source_id),
                                  self._needs_update_fields)
            if result.is_equal:
                self._skipped += 1
                logger.debug("[SourceLoader] Asset %s unchanged, skipping.",
                             source_metric.source_id)
                continue
            self._matched += 1
            logger.info("[SourceLoader] Asset %s needs update: %s",
                        source_metric.source_id, "; ".join(result.messages))
            yield asset

    # -- non-threaded mode (incl. all batch adapters) -------------------------

    def run(self, first_load: bool = False) -> dict:
        '''
        Drives the run directly: gate each asset, then hand the survivors to
        the adapter one at a time with their FULL issue set (retirement rule).
        Returns a summary dict.

        A single asset failure is logged and does not stop the run - it left
        nothing half-visible, because the scan only turns Pending at the end of
        a fully successful sync_asset().
        '''
        completed = 0
        errored = 0
        issues = 0
        for asset in self.iter_assets_needing_update(first_load=first_load):
            asset_id = asset.get("id")
            try:
                issues += self._adapter.sync_asset(
                    asset, self._client.get_issues_generator(asset_id))
                completed += 1
            except Exception as ex:  # noqa: BLE001 - per-asset boundary, logged and counted
                errored += 1
                logger.error("[SourceLoader] Asset %s failed: [%s] %s",
                             asset_id, type(ex).__name__, ex)
        summary = {"skipped": self._skipped, "completed": completed,
                   "errored": errored, "issues": issues}
        logger.info("[SourceLoader] Run finished: %s skipped, %s completed, %s errored, "
                    "%s issue(s).", self._skipped, completed, errored, issues)
        return summary

    # -- threaded single-asset mode (in-memory) ----------------------------

    # DELETE-IF-BATCH: optional - run_threaded (the in-memory threaded driver); Threaded=false is the supported route
    def run_threaded(self, worker_count: int, first_load: bool = False,
                     client_factory=None) -> dict:
        '''
        In-memory fan-out of the gated work list across worker threads, in
        this one process.  Returns the same summary dict as run().

        Inputs: worker_count threads; first_load bypasses the gate exactly as
            in run(); client_factory - callable (settings, source_name) ->
            client, one per thread, defaulting to TemplateClient (run_sync
            passes the entry client's class so a mock entry fans out mocks).
        Must return: {"skipped", "completed", "errored", "issues"}.
        Invariants:
        - This thread gates and feeds a bounded queue of asset payloads; a
          slow sink backpressures the vendor paging instead of buffering the
          whole listing.  worker_count threads consume it.
        - One client + one TemplateAdapter + one DataClient per thread, built
          on that thread and never shared: DataClient wraps a persistent
          asyncio loop and batches issues as instance state.  The asset
          payload rides the queue; issues are fetched on the worker's own
          client so every sync gets the asset's FULL issue set.
        - Per-asset failure boundary, as run(): a failed asset is logged and
          counted, never stops the run, and left nothing half-visible
          (sync_asset cancels its own scan).
        - All data writes go through the DataApi (each thread's DataClient);
          nothing here touches Elasticsearch or any queue index.
        - Graceful shutdown: KeyboardInterrupt/SystemExit in this thread stops
          the feed, lets each worker's in-flight asset finish (or cancel via
          sync_asset's BaseException handling), joins and closes the workers,
          then re-raises.  Un-started assets still in the queue are dropped.
        - No persistence and no resume: a killed run loses only its in-flight
          work list; the next run's gate re-derives what still needs doing.
        '''
        worker_count = max(1, int(worker_count))
        client_factory = client_factory or TemplateClient
        work = queue.Queue(maxsize=worker_count * 2)
        stop = threading.Event()
        stop_item = object()      # one per worker tells it the feed is finished
        results = []
        threads = [
            threading.Thread(target=self._worker_thread, name=f"{self._source_name}-worker-{i}",
                             args=(i, work, stop, stop_item, results, client_factory), daemon=True)
            for i in range(worker_count)
        ]
        for thread in threads:
            thread.start()
        try:
            for asset in self.iter_assets_needing_update(first_load=first_load):
                self._feed(work, threads, asset)
            for _ in threads:
                self._feed(work, threads, stop_item)
        except BaseException:
            # Stop consuming; whatever a worker is mid-way through completes
            # or cancels on its own, then the join below lets it finish.
            stop.set()
            raise
        finally:
            for thread in threads:
                thread.join()
        completed = sum(r["completed"] for r in results)
        errored = sum(r["errored"] for r in results)
        issues = sum(r["issues"] for r in results)
        summary = {"skipped": self._skipped, "completed": completed,
                   "errored": errored, "issues": issues}
        logger.info("[SourceLoader] Threaded run finished (%s worker(s)): %s skipped, "
                    "%s completed, %s errored, %s issue(s).",
                    len(threads), self._skipped, completed, errored, issues)
        return summary

    @staticmethod
    def _feed(work, threads, item):
        '''
        Blocking put with backpressure that stays responsive: the short
        timeout keeps Ctrl-C deliverable to this thread and notices when every
        worker has died (nothing would ever drain the queue).
        '''
        while True:
            try:
                work.put(item, timeout=0.5)
                return
            except queue.Full:
                if not any(t.is_alive() for t in threads):
                    raise RuntimeError(
                        "[SourceLoader] All worker threads exited; feed aborted.")

    # DELETE-IF-BATCH: optional - _worker_thread (the worker-thread body behind run_threaded)
    def _worker_thread(self, worker_id: int, work, stop, stop_item, results: list, client_factory):
        '''
        One worker's whole life: build its own client and (mapping-only)
        adapter, drain assets until the sentinel or the stop flag, flush/close
        the adapter, report its counts.  Exceptions from a single asset are
        the per-asset boundary (logged, counted); anything else ends this
        thread after the adapter is closed, and the feeder notices if none are
        left.
        '''
        completed = errored = issues = 0
        client = client_factory(self._app.Settings, self._source_name)
        adapter = TemplateAdapter(self._app, self._source_name, dry_run=self._adapter._dry_run)
        try:
            while not stop.is_set():
                try:
                    asset = work.get(timeout=0.5)
                except queue.Empty:
                    continue
                if asset is stop_item:
                    break
                asset_id = asset.get("id")
                try:
                    # FULL current issue set, fetched on this thread's client
                    # (retirement rule).
                    issues += adapter.sync_asset(asset, client.get_issues_generator(asset_id))
                    completed += 1
                except Exception as ex:  # noqa: BLE001 - per-asset boundary, logged and counted
                    errored += 1
                    logger.error("[SourceLoader] Worker %s: asset %s failed: [%s] %s",
                                 worker_id, asset_id, type(ex).__name__, ex)
        finally:
            adapter.close()
            results.append({"completed": completed, "errored": errored, "issues": issues})


# ===========================================================================
# Mock check - python Sources/Template/TemplateAdapter.py
# ===========================================================================

def main():
    ''' No-op template check: mock client + dry run, nothing sent anywhere. '''
    app = Application()
    adapter = TemplateAdapter(app, client=MockTemplateClient(source_name=DEFAULT_SOURCE_NAME),
                              dry_run=True)
    summary = adapter.run_sync(first_load=True)
    logging.info("[TemplateAdapter] Mock dry run complete: %s", summary)
    print(f"Mock dry run complete: {summary}")


if __name__ == "__main__":
    main()
