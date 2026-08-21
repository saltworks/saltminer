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

Architecture: one TaniumQueueLoader (producer) walks the Tanium endpoints
cursor sequentially - required, since GraphQL cursor pagination here is
opaque and can't be parallelized (see docs/plans discussion). Each fetched
page is unpacked immediately: every edge's node goes onto a bounded
queue.Queue individually, one endpoint per queue item, rather than the whole
page as one item - so a 500-endpoint page and a 1-endpoint page distribute
evenly across workers instead of handing one worker a 500x pile. N
TaniumAdapterWorker threads (consumers) pull single endpoints off that queue
and map/send Scan, Asset, and Issue docs to the DataApi. Backpressure is just
queue.Queue(maxsize=...) blocking on put(); no dedicated queue wrapper class
is needed for that.
'''

import logging
import queue
import threading

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


# ===========================================================================
# TaniumQueueLoader - controller: walks pages, feeds the work queue
# ===========================================================================

class TaniumQueueLoader:
    '''
    Sequential controller. Owns the cursor walk by delegating to
    TaniumClient.GetEndpointsGenerator(), which already handles the
    idle/absolute cursor lifetime guards - no need to re-walk
    GetEndpointsPage() here and risk drifting from that already-debugged
    logic. GetEndpointsGenerator's job stops at "here is the next page";
    unpacking a page's edges into individual queue items is this class's
    job, not the client's, so the full raw page is always visible here for
    inspection without touching TaniumClient.
    '''

    def __init__(self, client: TaniumClient, work_queue: "queue.Queue", worker_count: int, page_size: int = None):
        '''
        :param client: constructed TaniumClient to walk
        :param work_queue: shared queue.Queue that workers consume from
        :param worker_count: number of workers to send stop sentinels to when the walk ends
        :param page_size: `first` per page; defaults to client.page_size if None
        '''
        self._client = client
        self._queue = work_queue
        self._worker_count = worker_count
        self._page_size = page_size
        self._pages_loaded = 0
        self._endpoints_loaded = 0

    @property
    def pages_loaded(self) -> int:
        return self._pages_loaded

    @property
    def endpoints_loaded(self) -> int:
        return self._endpoints_loaded

    def run(self):
        '''
        Walks the full cursor page by page. Between pages, unpacks that
        page's edges and enqueues each node individually. Sentinels are
        pushed in a finally block, so a mid-walk failure (e.g.
        TaniumCursorExpired) still lets every worker exit cleanly instead of
        blocking on queue.get() forever - the exception itself still
        propagates to the caller after that.
        '''
        try:
            for page in self._client.GetEndpointsGenerator(first=self._page_size):
                self._pages_loaded += 1
                self._enqueue_endpoints(page)
        finally:
            self._enqueue_sentinels()

    def _enqueue_endpoints(self, page: dict):
        '''
        Unpacks one raw page's edges and puts each node onto work_queue
        individually. Each put() blocks (backpressure) if the queue is full.
        '''
        for edge in (page.get("edges") or []):
            node = edge.get("node")
            if node is None:
                continue
            self._queue.put(node)
            self._endpoints_loaded += 1

    def _enqueue_sentinels(self):
        ''' Pushes one None per worker so every worker thread can exit its loop cleanly. '''
        for _ in range(self._worker_count):
            self._queue.put(None)


# ===========================================================================
# TaniumAdapterWorker - consumer: maps + sends Scan/Asset/Issue docs
# ===========================================================================

class TaniumAdapterWorker:
    '''
    Consumes single endpoint nodes from work_queue until it receives a stop
    sentinel (None). One queue item is always exactly one endpoint - the
    loader unpacks pages before they ever reach the queue - so no worker can
    end up holding a disproportionate share of one oversized page.

    Owns its own DataClient instance rather than sharing one with other
    workers or the controller: DataClient wraps a persistent asyncio event
    loop, and run_until_complete() against one loop from multiple threads
    concurrently is not safe.

    Load shape is 1 Scan : 1 Asset : many Issues, all three scoped to a
    single endpoint. Each worker creates its own new Scan doc per endpoint -
    nothing is shared across workers, so there's no cross-worker ordering to
    coordinate.
    '''

    def __init__(self, id: int, app: Application, work_queue: "queue.Queue", error_threshold: int = 5):
        '''
        :param id: worker index, used for logging
        :param app: Application instance (provides .Settings)
        :param work_queue: shared queue.Queue that the loader publishes pages to
        :param error_threshold: consecutive processing errors before this worker stops itself
        '''
        self._id = id
        self._app = app
        self._queue = work_queue
        self._data_client = DataClient(app)
        self._sm_docs = SnykDocs()
        self._error_count = 0
        self._error_threshold = error_threshold
        self._logger = logging.getLogger(f"{__name__}.Worker{id}")

    @property
    def id(self) -> int:
        return self._id

    @property
    def error_count(self) -> int:
        return self._error_count

    def run(self):
        ''' Main loop: pull one endpoint node, process it, repeat until the stop sentinel arrives. '''
        raise NotImplementedError

    def _process_endpoint(self, node: dict):
        '''
        One endpoint, in order: create its Scan doc and take the resulting
        queue_scan_id, map + queue its Asset doc with that scan id and take
        the resulting queue_asset_id, then map + queue one Issue doc per
        finding referencing both ids.
        '''
        raise NotImplementedError

    def _map_scan(self, node: dict) -> MapScanDocDTO:
        raise NotImplementedError

    def _map_asset(self, node: dict, queue_scan_id: str) -> MapAssetDocDTO:
        raise NotImplementedError

    def _map_issue(self, node: dict, finding: dict, queue_scan_id: str, queue_asset_id: str) -> MapIssueDocDTO:
        raise NotImplementedError

    def close(self):
        ''' Flushes any remaining batched issues and closes this worker's DataClient. '''
        raise NotImplementedError


# ===========================================================================
# TaniumAdapter - entry point: owns the queue, starts the loader + workers
# ===========================================================================

class TaniumAdapter:
    '''
    Entry point, analogous to AxoniusAdapter / GHASAdapter but multi-threaded.

    Owns a plain queue.Queue(maxsize=...) shared between one TaniumQueueLoader
    (producer, runs on the calling thread) and N TaniumAdapterWorker threads
    (consumers).
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
        self._worker_error_threshold = int(settings.GetSource(source_name, "Worker_Error_Threshold", None) or 5)

        self._queue = queue.Queue(maxsize=self._queue_max_size)
        self._loader = TaniumQueueLoader(self._client, self._queue, self._worker_count, page_size=self._client.page_size)
        self._workers: list[TaniumAdapterWorker] = []
        self._threads: list[threading.Thread] = []

    @property
    def client(self) -> TaniumClient:
        return self._client

    @property
    def queue(self) -> "queue.Queue":
        return self._queue

    def run_sync(self, first_load: bool = None):
        ''' Orchestration: start workers, run the loader, drain, finalize. '''
        raise NotImplementedError

    def _start_workers(self):
        ''' Constructs TaniumAdapterWorker instances and starts one thread per worker. '''
        raise NotImplementedError

    def _shutdown_workers(self):
        ''' Joins all worker threads after the loader has pushed stop sentinels. '''
        raise NotImplementedError

    def _finalize(self):
        ''' Calls close() on every worker (flush + DataClient cleanup) once all threads have joined. '''
        raise NotImplementedError
