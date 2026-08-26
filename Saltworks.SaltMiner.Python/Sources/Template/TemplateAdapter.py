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
Template source adapter - mapping, loader, and worker in one file.

CLASSIFICATION (declare yours here when copying; these are not config keys):
- Processing model: single-asset (each asset is an independent unit of work)
- Write semantics:  replacement (each run reports complete current state)

Three classes, in dependency order:

- TemplateAdapter: the SourceMapping functions (vendor payloads -> queue
  documents via the shared DTOs), build_source_metric() for the NeedsUpdate
  gate, the index-name derivation, and sync_asset() - the three-tier chain
  for one asset: Create Scan -> Create Asset (carries QueueScanID) -> Create
  Issues (carry QueueScanID + QueueAssetID) -> flush -> set scan Pending.

- SourceLoader: builds the work list and owns the NeedsUpdate gate.  For
  threaded adapters, load_queue() fills the SMQ queue for the workers; for
  non-threaded adapters (incl. all batch adapters), run() drives the whole
  run directly and no worker is ever instantiated.

- SourceWorker (+ SourceWorkerFactory): the "script" for one threaded worker
  under Core.Agent/Core.Worker.  Processes exactly one asset per invocation.
  Non-threaded adapters can delete the worker section when copying.

THE RETIREMENT RULE (binding - see the folder README for the evidence):
The Manager reconciles only inside a submitted queue scan.  A submitted scan
carrying a subset of an asset's real issues RETIRES the absent issues of that
asset.  So: skip at asset granularity only (the gate, before Create Scan),
and when an asset does get processed, sync_asset() must receive that asset's
FULL current issue set.
'''

import logging
from datetime import datetime, timezone

from Core.Agent import Agent
from Core.DataClient import DataClient, QueueStatus
from Core.QueueClient import QueueClient, QueueClientDto
from Core.SmDocsAndDTOs import SmDocsAndDTOs, MapAssetDocDTO, MapIssueDocDTO, MapScanDocDTO, attrs
from Core.SourceMetric import NEEDS_UPDATE_FIELDS, SourceMetric, derive_local_metrics, needs_update
from Core.Worker import Worker, WorkerFactory

from Sources.Template.TemplateClient import (
    TemplateClient,
    SourceMappingException,
    SourceWorkerException,
)

logger = logging.getLogger(__name__)


# ===========================================================================
# preset fields - the identity of this source.  Set once when copying.
# ===========================================================================

VENDOR          = "Template"                 # vendor company name
PRODUCT         = "Template"                 # vendor product name
SOURCE_TYPE     = "Saltworks.Template"       # always "Saltworks.<ProductName>"
ASSET_TYPE      = "app"                      # "app" for application security tools
ASSESSMENT_TYPE = "Open"                     # see the Assessment Type catalog in the README
PRODUCT_TYPE    = "Application"

# The issue attribute build_source_metric()'s last_scan is written to and read
# back from - keep the two ends of this contract in one constant.
LAST_UPDATED_ATTRIBUTE = "template_last_updated"

# Queue item contract for the threaded path.  Same key/payload shape as
# Utility/QueueLoader.format_item and SyncQueueData, so the queue documents
# read like every other SMQ source's.
QUEUE_TARGET_TYPE = "TEMPLATE"


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
    Source-specific mapping and the per-asset queue chain.  Orchestration
    lives in TemplateRunner/SourceLoader/SourceWorker; HTTP lives in
    TemplateClient.  Keep it that way when copying.

    :app: Application instance
    :source_name: config lookup key (the SourceName value), ex "TEMPLATE1".
        Becomes the Instance field on every document.
    :data_client: optional pre-built DataClient.  Threaded workers pass their
        own - DataClient wraps a persistent asyncio loop and batches issues as
        instance state, so it must never be shared across threads.
    :dry_run: map and validate only; send nothing.  Used by the mock run.
    '''

    def __init__(self, app, source_name: str, data_client: DataClient = None,
                 dry_run: bool = False):
        settings = app.Settings
        self._app = app
        self._source_name = source_name
        self._sm_docs = SmDocsAndDTOs()
        self._dry_run = dry_run
        self._data_client = data_client
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

        Fill in what the vendor's asset listing actually provides.  Whatever is
        set here must be derivable on the local side too (see
        Core.SourceMetric.derive_local_metrics), or the field must be left out
        of NeedsUpdateFields in config - a field only one side can supply is a
        permanent mismatch and the gate never skips anything.

        last_scan uses the vendor's last-updated value, which map_issue_attributes
        writes to every issue as LAST_UPDATED_ATTRIBUTE - that round trip is
        what makes the two sides comparable.  attributes stays None unless your
        local derivation can reproduce it.
        '''
        return SourceMetric(
            source_id=str(asset["id"]),
            source_type=SOURCE_TYPE,
            instance=self._source_name,
            last_scan=asset.get("updated_at"),
            issue_count=0,       # fill from the vendor's counts if it provides them,
            critical=0,          # and add the fields you fill to NeedsUpdateFields;
            high=0,              # counts the vendor cannot provide must also be
            medium=0,            # removed from NeedsUpdateFields in config.
            low=0,
            is_not_scanned=False,
            attributes=None
        )

    # -- the per-asset queue chain --------------------------------------------

    def sync_asset(self, asset: dict, issues_iterable) -> int:
        '''
        The strictly ordered chain for one asset:
        Create Scan -> Create Asset -> Create Issues -> flush -> scan Pending.

        :asset: the vendor asset payload
        :issues_iterable: the asset's FULL current issue set (retirement rule -
            never a delta, never a page)
        Returns the number of issues sent.
        '''
        asset_id = str(asset["id"])
        report_id = f"{asset_id}|{_utc_now()}"

        mapped_scan = self.map_scan(asset, report_id)
        if self._dry_run:
            return self._dry_run_asset(asset, issues_iterable, report_id)

        queue_scan = self.data_client.queue_scan_add_update(mapped_scan)
        queue_scan_id = queue_scan["id"]
        scan_report_id = queue_scan["saltminer"]["scan"]["reportId"]

        queue_asset = self.data_client.queue_asset_add_update(
            self.map_asset(asset, queue_scan_id))
        queue_asset_id = queue_asset["id"]

        issue_count = 0
        for issue in issues_iterable:
            self.data_client.queue_issue_add_update_batch(
                self.map_issue(issue, queue_scan_id, queue_asset_id, scan_report_id, asset))
            issue_count += 1

        # Flush the partial batch before releasing, or the tail of the scan is
        # lost; Pending is what makes the scan visible to the Manager.
        self.data_client.queue_issue_add_update_batch(None)
        self.data_client.queue_scan_update_status(queue_scan_id, QueueStatus.PENDING)
        logger.info("[TemplateAdapter] Asset %s released to Manager: %s issue(s).",
                    asset_id, issue_count)
        return issue_count

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
        scan["ScanDate"]       = asset.get("updated_at") or _utc_now()
        scan["SourceType"]     = SOURCE_TYPE
        scan["AssetType"]      = ASSET_TYPE
        scan["Instance"]       = self._source_name
        MapScanDocDTO(**doc)
        return doc

    def map_asset(self, asset: dict, queue_scan_id: str) -> dict:
        doc = self._sm_docs.map_asset_doc()
        doc["Timestamp"] = _utc_now()
        doc["Saltminer"]["Internal"]["QueueScanId"] = queue_scan_id
        mapped = doc["Saltminer"]["Asset"]
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
        doc = self._sm_docs.map_issue_doc()
        doc["Timestamp"] = _utc_now()
        sm = doc["Saltminer"]
        sm["QueueScanId"] = queue_scan_id
        sm["QueueAssetId"] = queue_asset_id
        sm["IssueType"] = ASSESSMENT_TYPE

        vuln = doc["Vulnerability"]
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
        # Stable unique id for this issue on this asset - the Manager matches on
        # this to decide update vs retire, so it must not change between runs.
        scanner["Id"]             = f"{issue['id']}|{asset['id']}"
        scanner["AssessmentType"] = ASSESSMENT_TYPE
        scanner["Product"]        = PRODUCT
        scanner["Vendor"]         = VENDOR
        scanner["GuiUrl"]         = asset.get("gui_url")

        self.map_issue_attributes(issue, doc)
        MapIssueDocDTO(**doc)
        return doc

    def map_asset_attributes(self, asset: dict, doc: dict) -> dict:
        '''
        Source-specific asset metadata (team, owner, tags, ...).  Separate from
        map_asset because these are the fields most likely to change.
        '''
        doc["Saltminer"]["Asset"]["Attributes"] = attrs({
            # "team": asset.get("team"),
        })
        return doc

    def map_issue_attributes(self, issue: dict, doc: dict) -> dict:
        '''
        Source-specific issue metadata.  LAST_UPDATED_ATTRIBUTE is mandatory -
        it is the field the local-metric aggregation reads last_scan back from.
        '''
        doc["Saltminer"]["Attributes"] = attrs({
            LAST_UPDATED_ATTRIBUTE: issue.get("updated_at"),
            "status": issue.get("status"),
        })
        return doc

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
    - Threaded single-asset adapter: load_queue() inserts one SMQ queue item
      per asset that needs an update; Core.Agent + SourceWorker drain them.
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

        :first_load: True skips the local-metric derivation entirely and yields
            every asset - the full-sync path.
        '''
        self._skipped = 0
        self._matched = 0
        local_metrics = {} if first_load else self.get_local_metrics()
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

    # -- threaded single-asset mode -------------------------------------------

    def load_queue(self, first_load: bool = False, batch_size: int = 500) -> int:
        '''
        Inserts one SMQ queue item per asset that needs an update, for
        Core.Agent + SourceWorker to drain.  Items carry the asset id only -
        workers re-fetch their own payloads, so nothing large sits in a queue
        document.  Returns the number of items submitted.
        '''
        queue_client = QueueClient(self._app, self._source_name.lower())
        loaded = 0
        batch = {}
        for asset in self.iter_assets_needing_update(first_load=first_load):
            asset_id = asset["id"]
            batch[f"{QUEUE_TARGET_TYPE}|{self._source_name}|{asset_id}"] = {
                "target_id": str(asset_id),
                "target_type": QUEUE_TARGET_TYPE,
                "target_instance": self._source_name,
                "force": first_load,
            }
            if len(batch) >= batch_size:
                queue_client.insert_queue(f"{self._source_name} Loader", batch)
                loaded += len(batch)
                batch = {}
        if batch:
            queue_client.insert_queue(f"{self._source_name} Loader", batch)
            loaded += len(batch)
        logger.info("[SourceLoader] Queued %s asset(s) for workers (%s skipped by gate).",
                    loaded, self._skipped)
        return loaded


# ===========================================================================
# SourceWorker - one asset per invocation, threaded single-asset mode only
# ===========================================================================

class SourceWorkerFactory(WorkerFactory):
    '''
    Creates SourceWorker instances for Core.Agent.

    :source_name: config lookup key (SourceName), ex "TEMPLATE1"
    :client_factory: optional callable (settings, source_name) -> client, so a
        mock client can be injected; defaults to TemplateClient.
    '''

    def __init__(self, source_name: str, client_factory=None):
        self._source_name = source_name
        self._client_factory = client_factory or TemplateClient

    def create_worker(self, id: int, agent: Agent, **kwargs) -> Worker:
        return SourceWorker(id, agent, source_name=self._source_name,
                            client_factory=self._client_factory, **kwargs)


class SourceWorker(Worker):
    '''
    Processes exactly one asset per queue item: fetch the payload by id, then
    run the adapter's sync chain with the asset's FULL current issue set
    (retirement rule).  The queue item carries only the asset id - payloads
    are re-fetched here so nothing large ever sits in a queue document, and
    the fetch half is what parallelises.

    Each worker owns its own TemplateAdapter (and therefore its own
    DataClient).  DataClient wraps a persistent asyncio loop and batches
    issues as instance state - shared across threads it would interleave
    issues from different scans.
    '''

    def __init__(self, id: int, agent: Agent, **kwargs):
        self._source_name = kwargs.pop("source_name")
        self._client_factory = kwargs.pop("client_factory", TemplateClient)
        super().__init__(id, agent, **kwargs)
        # Built lazily on the worker's own thread, one of each per worker.
        self._client = None
        self._adapter = None

    @property
    def client(self):
        if self._client is None:
            self._client = self._client_factory(self.agent.app.Settings, self._source_name)
        return self._client

    @property
    def adapter(self) -> TemplateAdapter:
        if self._adapter is None:
            self._adapter = TemplateAdapter(self.agent.app, self._source_name)
        return self._adapter

    def _process(self, item: QueueClientDto):
        '''
        One work item, end to end.  On failure the item is completed as an
        error and the exception re-raised: the agent needs the terminal state,
        and Core.Worker.run() needs the raise to count it toward the error
        threshold.
        '''
        target_id = (item.doc.data or {}).get("target_id")
        try:
            if not target_id:
                raise SourceWorkerException(
                    f"Queue item {item.id} has no target_id in its data payload.")
            asset = self.client.get_asset(target_id)
            # FULL current issue set - never a delta (retirement rule).
            issue_count = self.adapter.sync_asset(
                asset, self.client.get_issues_generator(target_id))
            self.agent.complete(item, is_error=False,
                                data=dict(item.doc.data, issue_count=issue_count))
        except Exception as ex:
            self.logger.error("Worker %s failed asset '%s': [%s] %s",
                              self.id, target_id, type(ex).__name__, ex)
            try:
                self.agent.complete(item, is_error=True, reason=str(ex))
            except Exception as ex2:  # noqa: BLE001 - never mask the original failure
                self.logger.error("Worker %s could not record failure for '%s': %s",
                                  self.id, target_id, ex2)
            raise SourceWorkerException(
                f"Error processing asset '{target_id}'") from ex
