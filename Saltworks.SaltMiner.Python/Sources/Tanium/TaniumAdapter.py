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
Tanium source adapter.

Collection is two Tanium calls, not one:

  1. TaniumQueueLoader walks the endpoints cursor selecting ONLY `id`, and puts
     one thin work item per endpoint on a bounded queue.  The walk is sequential
     - Relay cursors are opaque, so page N+1 does not exist until page N returns
     - but it is cheap, because the full node is ~461 KB and this pass carries a
     single string per endpoint.
  2. Each worker fetches its own endpoint by id and maps it.  This is the half
     that parallelises, and it is why enumeration is worth a separate pass.

Load shape is 1 Scan : 1 Asset : many Issues, all scoped to one endpoint.  No
scan document is shared between workers, so there is no cross-worker ordering to
coordinate.

Written to move onto the SMQ agent framework (Core/Agent + Core/Worker) with a
small diff: TaniumWorkItem mirrors the accessors SyncWorker uses on a
QueueClientDto, TaniumAdapter exposes the surface Core/Agent gives a worker, and
_resolve_endpoint / _emit are the two methods that change.  See
docs/plans/2026-08-21-tanium-smq-ready-design.md.
'''

import json
import logging
import queue
import threading
from datetime import datetime, timezone

from Core.Application import Application
from Core.DataClient import DataClient, QueueStatus
from Core.SmDocsAndDTOs import SnykDocs, MapAssetDocDTO, MapIssueDocDTO, MapScanDocDTO
from Utility.SaltminerExceptions import SaltminerException

from Sources.Tanium.TaniumClient import TaniumClient, TaniumException

logger = logging.getLogger(__name__)


# ===========================================================================
# exceptions
# ===========================================================================

class TaniumAdapterExceptions(SaltminerException):
    ''' Base for all Tanium adapter failures - config, mapping, and queue orchestration alike. '''
    pass


class TaniumAdapterConfigException(TaniumAdapterExceptions):
    ''' Adapter-level configuration is missing or invalid (worker count, queue size, etc). '''
    pass


class TaniumAdapterMappingException(TaniumAdapterExceptions):
    ''' An endpoint or finding could not be mapped to a Scan/Asset/Issue doc. '''
    pass


class TaniumWorkerException(TaniumAdapterExceptions):
    ''' A work item failed.  Mirrors Core.Worker.WorkerException, which replaces this under SMQ. '''
    pass


# ===========================================================================
# mapping constants
# ===========================================================================

VENDOR          = "Tanium"
PRODUCT         = "Tanium"
INSTANCE        = "Tanium1"
SOURCE_TYPE     = "Saltworks.Tanium"
ASSESSMENT_TYPE = "NET"
ASSET_TYPE      = "NET"
PRODUCT_TYPE    = "Application"


def _utc_now():
    ''' Timestamp format every adapter in this repo writes. '''
    return datetime.now(timezone.utc).strftime("%Y-%m-%dT%H:%M:%S.%f")[:-3] + "Z"


def _attr_value(value):
    '''
    Coerces one attribute value to a string.

    Attributes is Dictionary<string, string> on the API.  A list or a bool there
    fails deserialization outright - "The JSON value could not be converted to
    System.String" - and neither the doc template nor the pydantic DTO models the
    value type, so nothing local catches it first.

    Lists become a delimited string, following the convention SnykAdapter uses for
    its `dependencies` attribute.  Bools are lowercased because that is how they
    read back in search and filters.
    '''
    if isinstance(value, bool):
        return "true" if value else "false"
    if isinstance(value, (list, tuple, set)):
        return ", ".join(_attr_value(v) for v in value)
    if isinstance(value, dict):
        return json.dumps(value, default=str)
    return str(value)


def _attrs(mapping: dict) -> dict:
    '''
    Builds an Attributes dict with every value stringified and every None dropped.

    Omitting nulls rather than writing "None" keeps the document smaller - which
    matters at ~1000 issues per endpoint - and an absent attribute and a null one
    are equivalent to anything reading them back.
    '''
    return {k: _attr_value(v) for k, v in mapping.items() if v is not None}


# ===========================================================================
# queue contract - shaped to match SMQ so the migration is a small diff
# ===========================================================================

class TaniumQueueType:
    TANIUM = "TANIUM"


class TaniumQueueStage:
    SCAN   = "Scan"
    ASSET  = "Asset"
    ISSUES = "Issues"


class TaniumQueueData:
    '''
    Work item payload.  Same four scalar fields as Sources/SyncWorker.SyncQueueData,
    so the queue document contract is already the one SMQ expects.

    target_id is stringified because it maps as a keyword under SMQ and has to be
    consistent across every caller.
    '''

    def __init__(self, dto: dict = None):
        self.target_id = None
        self.target_type = None
        self.target_instance = None
        self.force = False
        if dto:
            self.map(dto)

    def map(self, dto: dict):
        self.target_id = str(dto.get("target_id")) if dto.get("target_id") is not None else None
        self.target_type = dto.get("target_type")
        self.target_instance = dto.get("target_instance")
        self.force = bool(dto.get("force", False))
        return self

    def to_dto(self) -> dict:
        return {
            "target_id": self.target_id,
            "target_type": self.target_type,
            "target_instance": self.target_instance,
            "force": self.force
        }


class _WorkDoc:
    ''' The `.doc` half of the DTO shape - key, data, stage. '''

    def __init__(self, key, data, stage=None):
        self.key = key
        self.data = data
        self.stage = stage


class TaniumWorkItem:
    '''
    Stand-in for Core.QueueClient.QueueClientDto.

    Exposes only the accessors a worker actually touches - .id, .doc.data,
    .doc.stage - so the worker body is identical whether the item came off an
    in-memory queue or out of elasticsearch.

    `node` is transient and in-memory only.  It is deliberately NOT part of
    .doc.data: a measured ~461 KB per endpoint cannot go into a queue document.
    Under the current design it is always None, and the worker fetches by id.
    '''

    def __init__(self, key, data: dict, node: dict = None):
        self.id = key
        self.doc = _WorkDoc(key, data)
        self.node = node

    def describe(self) -> str:
        return f"key: {self.doc.key}"


# ===========================================================================
# TaniumQueueLoader - producer: enumerates endpoint ids onto the work queue
# ===========================================================================

class TaniumQueueLoader:
    '''
    Sequential producer.  Walks the endpoints cursor selecting only `id` and
    emits one work item per endpoint.

    Delegates the walk to TaniumClient.IterEndpointIds(), which already owns the
    cursor lifetime guards and the id checkpoint - no reason to re-walk pages here
    and drift from that already-debugged logic.

    Sentinels are pushed in a finally block, so a mid-walk failure still lets
    every worker exit cleanly instead of blocking on queue.get() forever.  The
    exception still propagates to the caller afterwards.
    '''

    # How long a single put() waits before re-checking that consumers still exist.
    PUT_TIMEOUT_SEC = 1.0

    def __init__(self, client: TaniumClient, work_queue: "queue.Queue", worker_count: int,
                 source_name: str = "Tanium", page_size: int = None, force: bool = False,
                 can_continue=None):
        '''
        :param client: constructed TaniumClient to walk
        :param work_queue: shared queue.Queue that workers consume from
        :param worker_count: number of workers to send stop sentinels to when the walk ends
        :param source_name: becomes target_instance on the payload
        :param page_size: `first` per enumeration page; defaults to the schema maximum
        :param force: sets force on every emitted payload
        :param can_continue: callable returning False once no consumer is left alive.
            Without it a full queue plus dead workers is a permanent deadlock - see _put.
        '''
        self._client = client
        self._queue = work_queue
        self._worker_count = worker_count
        self._source_name = source_name
        self._page_size = page_size
        self._force = force
        self._can_continue = can_continue
        self._endpoints_loaded = 0

    @property
    def endpoints_loaded(self) -> int:
        return self._endpoints_loaded

    @property
    def checkpoint_id(self):
        ''' Furthest endpoint id enumerated; resume point if the walk is cut short. '''
        return self._client.CheckpointId

    def run(self, resume_from=None):
        ''' Enumerates every endpoint id and emits one work item each. '''
        try:
            for endpoint_id in self._client.IterEndpointIds(first=self._page_size,
                                                            resume_from=resume_from):
                self._emit(endpoint_id)
        finally:
            self._enqueue_sentinels()
        logger.info("[Tanium Loader] Enumerated %s endpoint(s).", self._endpoints_loaded)

    def _emit(self, endpoint_id):
        '''
        The one producer seam.

        Today: an in-memory work item, with put() blocking for backpressure.
        Under SMQ: batch into QueueClient.insert_queue() with this same key and
        payload, the way Utility/QueueLoader does for SSC.
        '''
        key = f"{TaniumQueueType.TANIUM}|{self._source_name}|{endpoint_id}"
        data = {
            "target_id": str(endpoint_id),
            "target_type": TaniumQueueType.TANIUM,
            "target_instance": self._source_name,
            "force": self._force
        }
        self._put(TaniumWorkItem(key, data))
        self._endpoints_loaded += 1

    def _put(self, item):
        '''
        Blocking put, but with a liveness check between attempts.

        A plain put() is exactly the backpressure this design wants, right up until
        every consumer stops - then nothing drains the queue and the producer waits
        on a full one forever.  That is not hypothetical: workers stop themselves
        once they hit their consecutive-error threshold, so a source-wide failure
        kills all of them and hangs the run instead of reporting it.
        '''
        while True:
            if self._can_continue is not None and not self._can_continue():
                raise TaniumAdapterExceptions(
                    f"No workers left to consume the queue - stopping enumeration after "
                    f"{self._endpoints_loaded} endpoint(s). Resume from id {self.checkpoint_id}.")
            try:
                self._queue.put(item, timeout=self.PUT_TIMEOUT_SEC)
                return
            except queue.Full:
                continue

    def _enqueue_sentinels(self):
        '''
        One None per worker so every worker thread can exit its loop cleanly.

        Best effort: if nothing is draining, a sentinel cannot be delivered anyway,
        and _shutdown_workers tops them up after the fact for threads still alive.
        '''
        for _ in range(self._worker_count):
            try:
                self._queue.put(None, timeout=self.PUT_TIMEOUT_SEC)
            except queue.Full:
                break


# ===========================================================================
# TaniumAdapterWorker - consumer: fetches one endpoint, maps + sends its docs
# ===========================================================================

class TaniumAdapterWorker:
    '''
    Consumes work items until it receives a stop sentinel (None).

    Owns its own DataClient rather than sharing one.  Two independent reasons:
    DataClient wraps a persistent asyncio loop and run_until_complete() against
    one loop from several threads is not safe; and its issue_batch is instance
    state, so a shared client would interleave issues from different scans into
    one bulk POST.

    Method shapes deliberately match Core.Worker.Worker - run(), _process(item),
    heartbeat() - so moving onto SMQ is a base class change rather than a rewrite.
    '''

    def __init__(self, id: int, agent: "TaniumAdapter", err_threshold: int = 5):
        '''
        :param id: worker index, used for logging and beat records
        :param agent: the TaniumAdapter, which provides .app, .queue, and the
            update()/complete() surface Core/Agent gives a worker
        :param err_threshold: consecutive processing errors before this worker stops itself
        '''
        self._id = id
        self._agent = agent
        self._processed = 0
        self._dropped = 0
        self._data_client = DataClient(agent.app)
        self._sm_docs = SnykDocs()
        self._error_count = 0
        self._error_threshold = err_threshold
        self._abandon = threading.Event()
        self._logger = None

    # -- Core.Worker-shaped accessors -----------------------------------------

    @property
    def id(self) -> int:
        return self._id

    @property
    def agent(self) -> "TaniumAdapter":
        return self._agent

    @property
    def logger(self) -> logging.Logger:
        if self._logger is None:
            self._logger = self._agent.app.logging_provider.get_thread_logger(self.id)
        return self._logger

    @property
    def error_count(self) -> int:
        return self._error_count

    @property
    def error_threshold(self) -> int:
        return self._error_threshold

    @property
    def abandoned(self) -> bool:
        return self._abandon.is_set()

    def abandon(self):
        self._abandon.set()

    def heartbeat(self):
        ''' In-memory liveness only.  Never writes - that is what agent.update() is for. '''
        self._agent.record_beat(self.id)

    # -- loop -----------------------------------------------------------------

    def run(self):
        '''
        Pull one item, process it, repeat until the stop sentinel arrives.

        Consecutive errors stop the worker at err_threshold; a success resets the
        count, because the threshold is for a worker that is broken, not for one
        that met a few bad endpoints.
        '''
        self.logger.info("Worker %s started.", self.id)
        while True:
            if self.abandoned:
                self.logger.warning("Worker %s abandoned, stopping.", self.id)
                break

            item = self._agent.queue.get()
            try:
                if item is None:
                    break
                if self.abandoned:
                    self.logger.warning("Worker %s abandoned while waiting, stopping.", self.id)
                    break

                self._agent.record_beat(self.id, item=item)
                self._process(item)
                self._error_count = 0
                self._processed += 1
            except Exception as ex:                      # noqa: BLE001 - drop and continue
                # A record that fails is logged, dropped, and the worker moves on.
                # One bad endpoint is not a reason to retire a worker and shrink the
                # pool for every endpoint behind it.
                self._error_count += 1
                self._dropped += 1
                self.logger.error("Worker %s dropped a record after failure: [%s] %s",
                                  self.id, type(ex).__name__, ex)
                if self._error_count == self._error_threshold:
                    # Not fatal, but N failures with no success between them is a
                    # source-wide problem rather than N unlucky endpoints - say so
                    # once instead of burying it in per-record errors.
                    self.logger.error(
                        "Worker %s has failed %s consecutive record(s). Continuing, but this "
                        "usually means the source or the mapping is broken, not the data.",
                        self.id, self._error_count)
            finally:
                self._agent.queue.task_done()
                self._agent.record_beat(self.id, item=None)

        self.close()
        self.logger.info("Worker %s stopped: %s endpoint(s) processed, %s dropped.",
                         self.id, self._processed, self._dropped)

    # -- processing -----------------------------------------------------------

    def _process(self, item):
        '''
        One work item, end to end.  Under SMQ `item` is a QueueClientDto and this
        signature does not change.

        On failure the item is completed as an error and the exception re-raised:
        the agent needs the terminal state, and run() needs the raise to count it
        toward the error threshold.
        '''
        data = TaniumQueueData(item.doc.data)
        try:
            node = self._resolve_endpoint(item, data)
            self._process_endpoint(item, data, node)
        except Exception as ex:                          # noqa: BLE001 - re-raised below
            self.logger.error("Error processing Tanium endpoint '%s' at stage %s: [%s] %s",
                              data.target_id, item.doc.stage, type(ex).__name__, ex)
            try:
                self._agent.complete(item, is_error=True, reason=str(ex))
            except Exception as ex2:                     # noqa: BLE001 - never mask the original
                self.logger.error("Error recording failure for '%s': %s", data.target_id, ex2)
            raise TaniumWorkerException(
                f"Error processing Tanium endpoint '{data.target_id}'") from ex

    def _resolve_endpoint(self, item, data) -> dict:
        '''
        The single point where the fetch model differs.

        Today the loader carries ids only and the worker fetches its own endpoint.
        If a node is ever attached to the item - a loader that already holds it, or
        an SMQ staging index - it is used as-is and no fetch happens.
        '''
        if item.node is not None:
            return item.node

        envelope = self._agent.client.GetEndpointById(data.target_id)
        edges = (envelope or {}).get("edges") or []
        if not edges:
            raise TaniumAdapterMappingException(
                f"Endpoint id '{data.target_id}' returned no rows on refetch. It may have been "
                f"removed between enumeration and processing.")
        node = (edges[0] or {}).get("node")
        if node is None:
            raise TaniumAdapterMappingException(
                f"Endpoint id '{data.target_id}' returned an edge with no node.")
        return node

    def _process_endpoint(self, item, data, node: dict):
        '''
        Scan, then Asset, then Issues, then release the scan to the manager.

        The scan is created in Loading status and flipped to Pending only after
        every issue is written.  That transition is the whole safety mechanism -
        the manager cannot see a half-loaded scan.
        '''
        report_id = f"{data.target_id} | {_utc_now()}"

        # Set the stage before the call it describes, so a failure reports where it
        # happened.  Without this every pre-asset failure logs "stage None".
        self._agent.update(item, TaniumQueueStage.SCAN, data.to_dto())
        scan = self._data_client.queue_scan_add_update(self._map_scan(node, report_id))
        queue_scan_id = scan["id"]
        scan_report_id = scan["saltminer"]["scan"]["reportId"]
        self._agent.update(item, TaniumQueueStage.ASSET, data.to_dto())

        asset = self._data_client.queue_asset_add_update(self._map_asset(node, queue_scan_id))
        queue_asset_id = asset["id"]
        self._agent.update(item, TaniumQueueStage.ISSUES, data.to_dto())

        findings = self._iter_findings(node)
        issue_count = 0
        for finding in findings:
            self._data_client.queue_issue_add_update_batch(
                self._map_issue(node, finding, queue_scan_id, queue_asset_id, scan_report_id))
            issue_count += 1
            # One endpoint can carry >1000 findings; prove liveness while batching.
            if issue_count % 100 == 0:
                self.heartbeat()

        # Flush the partial batch before releasing, or the tail of the scan is lost.
        self._data_client.queue_issue_add_update_batch(None)
        self._data_client.queue_scan_update_status(queue_scan_id, QueueStatus.PENDING)
        self._hand_off_to_manager([queue_scan_id])

        self.logger.info("Worker %s completed endpoint %s (%s): %s issue(s).",
                         self.id, data.target_id, node.get("name"), issue_count)
        self._agent.complete(item, stage="", is_error=False, issue_count=issue_count)

    @staticmethod
    def _iter_findings(node: dict):
        '''
        Findings for one endpoint, without collapsing null into empty.
        A missing compliance block and an empty one both yield nothing here; the
        census exists to tell them apart, the mapping does not need to.
        '''
        compliance = node.get("compliance")
        if compliance is None:
            return []
        return compliance.get("cveFindings") or []

    def _hand_off_to_manager(self, queue_scan_ids):
        '''
        Process these queue scans now rather than waiting for the manager cron.

        They are already Pending by this point, so a no-op here is correct and
        simply defers to the cron.  Later: a dotnet shell-out the way
        SyncWorker._run_manager does it, or the python manager port.
        '''
        return

    # -- mapping --------------------------------------------------------------

    def _map_scan(self, node: dict, report_id: str) -> dict:
        doc = self._sm_docs.map_scan_doc()
        doc["Timestamp"] = _utc_now()
        doc["Saltminer"]["Internal"]["IssueCount"] = -1      # disables count validation
        doc["Saltminer"]["Internal"]["ReplaceIssues"] = True  # each run is current-state truth
        # Required on submit - the DataApi does NOT default it.  Omitting it returns
        # "<blank> is not a valid Queue Scan Status" and the scan never lands.  It is
        # also what keeps a half-written scan invisible: the manager only picks up
        # Pending, and the status flips there once every issue has been sent.
        doc["Saltminer"]["Internal"]["QueueStatus"] = QueueStatus.LOADING
        scan = doc["Saltminer"]["Scan"]
        scan["AssessmentType"] = ASSESSMENT_TYPE
        scan["ProductType"]    = PRODUCT_TYPE
        scan["Product"]        = PRODUCT
        scan["Vendor"]         = VENDOR
        scan["ReportId"]       = report_id
        scan["ScanDate"]       = node.get("eidLastSeen")
        scan["SourceType"]     = SOURCE_TYPE
        scan["AssetType"]      = ASSET_TYPE
        scan["Instance"]       = INSTANCE
        MapScanDocDTO(**doc)
        return doc

    def _map_asset(self, node: dict, queue_scan_id: str) -> dict:
        doc = self._sm_docs.map_asset_doc()
        doc["Timestamp"] = _utc_now()
        doc["Saltminer"]["Internal"]["QueueScanId"] = queue_scan_id
        asset = doc["Saltminer"]["Asset"]
        asset["Name"]        = node.get("name")
        asset["Description"] = " ".join(
            x for x in (node.get("manufacturer"), node.get("model")) if x) or None
        asset["Ip"]          = node.get("ipAddress")
        asset["Version"]     = node.get("name")
        asset["VersionId"]   = str(node.get("id"))
        asset["SourceId"]    = str(node.get("id"))
        asset["SourceType"]  = SOURCE_TYPE
        asset["AssetType"]   = ASSET_TYPE
        asset["Instance"]    = INSTANCE
        # Port is System.Int32 on the API and is NOT in the python DTO, so pydantic
        # cannot catch this - the template's null only fails once it reaches C#.
        # A Tanium endpoint is a host and has no port, and 0 is the value that
        # survives the conversion.  Do not use the string "None" here: that is what
        # TenableAdapter:164 does and it fails the same way null does.
        asset["Port"]        = 0
        # systemUUID and serialNumber are carried deliberately.  `id` is the
        # identity key by decision, but endpointIdChanges exists precisely because
        # EIDs merge and rename - these make a re-key possible without recollecting.
        asset["Attributes"] = _attrs({
            "SystemUuid":         node.get("systemUUID"),
            "SerialNumber":       node.get("serialNumber"),
            "ComputerId":         node.get("computerID"),
            "DomainName":         node.get("domainName"),
            "Namespace":          node.get("namespace"),
            "IpAddresses":        node.get("ipAddresses"),
            "MacAddresses":       node.get("macAddresses"),
            "Manufacturer":       node.get("manufacturer"),
            "Model":              node.get("model"),
            "ChassisType":        node.get("chassisType"),
            "IsVirtual":          node.get("isVirtual"),
            "EidLastSeen":        node.get("eidLastSeen"),
            "EntityProviderName": node.get("entityProviderName"),
            "EntityProviderType": node.get("entityProviderType"),
        })
        MapAssetDocDTO(**doc)
        return doc

    def _map_issue(self, node: dict, finding: dict, queue_scan_id: str,
                   queue_asset_id: str, report_id: str) -> dict:
        doc = self._sm_docs.map_issue_doc()
        doc["Timestamp"] = _utc_now()
        asset_id = str(node.get("id"))
        cve = finding.get("cveId")

        sm = doc["Saltminer"]
        sm["QueueScanId"] = queue_scan_id
        sm["QueueAssetId"] = queue_asset_id
        sm["Source"]["Analyzer"] = finding.get("scanType")

        vuln = doc["Vulnerability"]
        vuln["Id"]             = [cve] if cve else ["None"]
        vuln["Name"]           = cve
        # Description is typed `str` on the DTO, not Optional[str] - passing None fails
        # validation and would error the whole endpoint over one finding with no summary,
        # costing every other issue on it.  "None" matches what the other adapters write
        # for an absent value.
        vuln["Description"]    = finding.get("summary") or "None"
        vuln["Recommendation"] = finding.get("remediation")
        vuln["FoundDate"]      = finding.get("absoluteFirstFoundDate")
        vuln["Severity"]       = finding.get("severityV3") or finding.get("severity")
        vuln["SourceSeverity"] = finding.get("severity")
        vuln["Location"]       = node.get("name")
        vuln["LocationFull"]   = "{} | {}".format(
            node.get("name"), ", ".join(finding.get("detectedProducts") or []))
        vuln["ReportId"]       = report_id
        vuln["Enumeration"]    = cve or ""
        vuln["IsSuppressed"]   = bool(finding.get("excepted"))
        vuln["IsActive"]       = True
        vuln["IsRemoved"]      = False

        # Stable across scans: Tanium reports one finding per (endpoint, CVE), so
        # this is unique without detectedProducts - and must stay that way.  Keying
        # on the product list would close and reopen an issue every time one of two
        # affected products got patched.
        scanner = vuln["Scanner"]
        scanner["Id"]             = f"{cve} | {finding.get('absoluteFirstFoundDate')} | {asset_id}"
        scanner["AssessmentType"] = ASSESSMENT_TYPE
        scanner["Product"]        = PRODUCT
        scanner["Vendor"]         = VENDOR

        score = vuln["Score"]
        if finding.get("cvssScoreV3") is not None:
            score["Base"], score["Version"] = finding["cvssScoreV3"], "3.x"
        elif finding.get("cvssScore") is not None:
            score["Base"], score["Version"] = finding["cvssScore"], "2.0"
        score["Temporal"] = finding.get("cvssTemporalScoreV3") or 0

        sm["Attributes"] = _attrs({
            "CveYear":          finding.get("cveYear"),
            "CvssScoreV2":      finding.get("cvssScore"),
            "CvssScoreV3":      finding.get("cvssScoreV3"),
            "SeverityV2":       finding.get("severity"),
            "FirstFound":       finding.get("firstFound"),
            "LastFound":        finding.get("lastFound"),
            "DetectedProducts": finding.get("detectedProducts"),
            "DetectedCpes":     finding.get("detectedCPEs"),
            "Cpes":             finding.get("cpes"),
            "EpssScore":        finding.get("epssScore"),
            "EpssPercentile":   finding.get("epssPercentile"),
            "IsCisaKev":        finding.get("isCisaKev"),
            "MaxMaturity":      finding.get("maxMaturity"),
            "Excepted":         finding.get("excepted"),
            "ScanType":         finding.get("scanType"),
        })
        MapIssueDocDTO(**doc)
        return doc

    def close(self):
        ''' Flush any batched issues and release this worker's DataClient. '''
        try:
            self._data_client.queue_issue_add_update_batch(None)
        except Exception as ex:                          # noqa: BLE001 - close must not raise
            self.logger.error("Worker %s failed flushing its issue batch: %s", self.id, ex)
        try:
            self._data_client.close()
        except Exception as ex:                          # noqa: BLE001 - close must not raise
            self.logger.error("Worker %s failed closing its DataClient: %s", self.id, ex)


# ===========================================================================
# TaniumAdapter - owns the queue, the loader, and the workers
# ===========================================================================

class TaniumAdapter:
    '''
    Entry point.

    Also stands in for Core.Agent.Agent: workers reach .app, .queue, .client and
    the update()/complete()/record_beat() surface through this object, so moving
    to SMQ means deleting these shims and passing a real Agent instead.
    '''

    def __init__(self, app: Application, source_name: str = "Tanium"):
        '''
        :param app: Application instance (provides .Settings)
        :param source_name: config source section name, defaults to "Tanium"
        '''
        self._app = app
        self._source_name = source_name
        settings = app.Settings

        self._client = TaniumClient(settings)
        self._worker_count = int(settings.GetSource(source_name, "Worker_Count", None) or 5)
        self._queue_max_size = int(settings.GetSource(source_name, "Queue_Max_Size", None) or 20)
        self._worker_error_threshold = int(
            settings.GetSource(source_name, "Worker_Error_Threshold", None) or 5)
        if self._worker_count < 1:
            raise TaniumAdapterConfigException(
                f"Worker_Count must be at least 1, got {self._worker_count}.")

        self._queue = queue.Queue(maxsize=self._queue_max_size)
        self._workers: list[TaniumAdapterWorker] = []
        self._threads: list[threading.Thread] = []

        self._beats = {}
        self._beats_lock = threading.Lock()
        self._completed = 0
        self._errored = 0
        self._issues = 0
        self._tally_lock = threading.Lock()

    # -- Core.Agent-shaped surface -------------------------------------------

    @property
    def app(self) -> Application:
        return self._app

    @property
    def queue(self) -> "queue.Queue":
        return self._queue

    @property
    def client(self) -> TaniumClient:
        return self._client

    @property
    def completed(self) -> int:
        return self._completed

    @property
    def errored(self) -> int:
        return self._errored

    def record_beat(self, worker_id, item=None):
        ''' In-memory liveness only.  Core/Agent uses this for defunct detection. '''
        with self._beats_lock:
            self._beats[worker_id] = (datetime.now(timezone.utc), item)

    def update(self, item, stage=None, data=None):
        '''
        Matches Agent.update().  No queue document exists yet, so this advances
        local stage and liveness; under SMQ it becomes a set_progress write and
        every call site stays as written.
        '''
        if stage is not None:
            item.doc.stage = stage
        if data is not None:
            item.doc.data = data
        self.record_beat(threading.current_thread().name, item=item)

    def complete(self, item, stage=None, data=None, is_error=True, reason=None, issue_count=0):
        '''
        Matches Agent.complete(), including is_error defaulting to True - callers
        must pass is_error=False for a success.  The default reads backwards on
        purpose: it is what Core/Agent does, and a shim that disagreed would turn
        every success into an error the day this moves to SMQ.
        '''
        if stage is not None:
            item.doc.stage = stage
        with self._tally_lock:
            if is_error:
                self._errored += 1
            else:
                self._completed += 1
                self._issues += issue_count
        if is_error and reason:
            logger.warning("[Tanium Adapter] Item %s errored: %s", item.describe(), reason)

    # -- orchestration --------------------------------------------------------

    def run_sync(self, first_load: bool = None, resume_from=None, force: bool = False):
        '''
        Start workers, enumerate endpoints, drain, finalize.

        The loader runs on this thread rather than its own: it is the producer,
        and blocking here on a full queue is exactly the backpressure wanted.

        :param first_load: accepted for signature parity with the other adapters; unused.
        :param resume_from: resume enumeration after this endpoint id.
        :param force: set force on every emitted work item.
        '''
        started = datetime.now(timezone.utc)
        loader = TaniumQueueLoader(self._client, self._queue, self._worker_count,
                                   source_name=self._source_name, force=force,
                                   can_continue=self._any_worker_alive)
        logger.info("[Tanium Adapter] Starting: %s worker(s), queue max %s.",
                    self._worker_count, self._queue_max_size)
        # Resolve extension fields here rather than letting the first by-id fetch
        # trigger it: on the worker threads every one of them arrives at once and
        # introspects separately, and a schema problem should fail the run before
        # any thread has started rather than N times afterwards.
        keep_ep, keep_fi = self._client.ResolveFields()
        logger.info("[Tanium Adapter] Extension fields: %s endpoint, %s finding.",
                    len(keep_ep), len(keep_fi))
        self._start_workers()
        try:
            loader.run(resume_from=resume_from)
        except (TaniumException, TaniumAdapterExceptions) as ex:
            # Sentinels are already queued by loader's finally block, so workers
            # will drain what was enumerated and exit rather than hang.
            logger.error("[Tanium Adapter] Enumeration failed after %s endpoint(s): [%s] %s. "
                         "Resume from id %s.", loader.endpoints_loaded,
                         type(ex).__name__, ex, loader.checkpoint_id)
            raise
        finally:
            self._shutdown_workers()
            self._finalize()
            elapsed = (datetime.now(timezone.utc) - started).total_seconds()
            logger.info("[Tanium Adapter] Done in %.1fs. Enumerated %s, completed %s, errored %s, "
                        "issues %s. Page size %s (%s shrink event(s)). Checkpoint %s.",
                        elapsed, loader.endpoints_loaded, self._completed, self._errored,
                        self._issues, self._client.PageSize, len(self._client.ShrinkEvents),
                        loader.checkpoint_id)
        return {"enumerated": loader.endpoints_loaded, "completed": self._completed,
                "errored": self._errored, "issues": self._issues,
                "checkpoint_id": loader.checkpoint_id,
                "page_size": self._client.PageSize,
                "shrink_events": self._client.ShrinkEvents}

    def _any_worker_alive(self) -> bool:
        ''' False once every worker thread has stopped - see TaniumQueueLoader._put. '''
        return any(t.is_alive() for t in self._threads)

    def _start_workers(self):
        ''' Constructs the workers and starts one thread each. '''
        for worker_id in range(self._worker_count):
            worker = TaniumAdapterWorker(worker_id, self,
                                         err_threshold=self._worker_error_threshold)
            thread = threading.Thread(target=worker.run, name=f"TaniumWorker-{worker_id}",
                                      daemon=True)
            self._workers.append(worker)
            self._threads.append(thread)
            thread.start()

    def _shutdown_workers(self):
        '''
        Join every worker thread.

        A worker that stopped early on its error threshold left its sentinel
        unread, so top up the queue with one per thread still alive - otherwise
        the remaining workers block on get() forever and the join never returns.
        '''
        for thread in self._threads:
            if thread.is_alive():
                try:
                    self._queue.put(None, timeout=TaniumQueueLoader.PUT_TIMEOUT_SEC)
                except queue.Full:
                    pass
        for thread in self._threads:
            thread.join(timeout=60)
            if thread.is_alive():
                logger.error("[Tanium Adapter] %s did not stop within 60s; abandoning it.",
                             thread.name)

    def _finalize(self):
        '''
        Drain anything left unprocessed so the queue does not hold references,
        and report it - unprocessed items are silent data loss otherwise.
        '''
        for worker in self._workers:
            worker.abandon()
        leftover = 0
        while True:
            try:
                if self._queue.get_nowait() is not None:
                    leftover += 1
            except queue.Empty:
                break
        if leftover:
            logger.warning("[Tanium Adapter] %s work item(s) were never processed.", leftover)
