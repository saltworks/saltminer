''' --[auto-generated, do not modify this block]--
*
* Copyright (c) 2025 Saltworks Security, LLC
*
* Use of this software is governed by the Business Source License included
* in the LICENSE file.
*
* Change Date: 2029-12-09
*
* On the date above, in accordance with the Business Source License, use
* of this software will be governed by version 2 or later of the General
* Public License.
*
* ----
'''

'''
SyncAgent class - used to run multi-threaded processing of sync/refresh.
'''
import logging
import queue
import threading
import time
import datetime

from Core.Application import Application
from Core.ElasticClient import ElasticClient
from Utility.SyncQueueHelper import SyncQueueHelper
from .Worker import Worker
from .QueueClient import QueueClient

_log = logging.getLogger(__name__)

class AgentArgs():
    """Arguments for Agent."""
    def __init__(self):
        self.__queueIndexPattern = None
        self.__lowThresholdCount = 10
        self.__workerCount = 5
        self.__pollingIntervalSecs = 30
    
    @property
    def QueueIndexPattern(self) -> str:
        return self.__queueIndexPattern
    @QueueIndexPattern.setter
    def QueueIndexPattern(self, value:str):
        self.__queueIndexPattern = value

    @property
    def LowThresholdCount(self) -> int:
        return self.__lowThresholdCount
    @LowThresholdCount.setter
    def LowThresholdCount(self, value:int):
        self.__lowThresholdCount = value

    @property
    def WorkerCount(self) -> int:
        return self.__workerCount
    @WorkerCount.setter
    def WorkerCount(self, value:int):
        self.__workerCount = value

    @property
    def PollingIntervalSecs(self) -> int:
        return self.__pollingIntervalSecs
    @PollingIntervalSecs.setter
    def PollingIntervalSecs(self, value:int):
        self.__pollingIntervalSecs = value

class AgentQueueItemStatus():
    """Enum for AgentQueueItem status."""
    NEW = "New"
    IN_PROGRESS = "In Progress"
    COMPLETE = "Complete"
    FAILED = "Failed"

    @staticmethod
    def parse(value:str):
        value = value.lower()
        if value == "new":
            return AgentQueueItemStatus.NEW
        elif value == "in progress":
            return AgentQueueItemStatus.IN_PROGRESS
        elif value == "complete":
            return AgentQueueItemStatus.COMPLETE
        elif value == "failed":
            return AgentQueueItemStatus.FAILED
        else:
            raise ValueError(f"Invalid AgentQueueItemStatus: {value}")

class AgentQueueItem():
    """Represents an item in the Agent's processing queue."""
    def __init__(self):
        self._source = None
        self._key = None
        self._status = AgentQueueItemStatus.NEW
        self._status_reason = None
        self._stage = None
        self._data = None
        self._created = None
        self._completed = None
        self._locked = None
        self.lock_id = None
        self._priority = None
        self._change_reason = None
        self._change_trigger = None

    @property
    def source(self) -> str:
        return self._source
    @source.setter
    def source(self, value:str):
        self._source = value

    @property
    def key(self) -> str:
        return self._key
    @key.setter
    def key(self, value:str):
        self._key = value

    @property
    def status(self) -> str:
        return self._status
    @status.setter
    def status(self, value:str):
        self._status = value

    @property
    def status_reason(self) -> str:
        return self._status_reason
    @status_reason.setter
    def status_reason(self, value:str):
        self._status_reason = value

    @property
    def stage(self) -> str:
        return self._stage
    @stage.setter
    def stage(self, value:str):
        self._stage = value

    @property
    def created(self):
        return self._created
    @created.setter
    def created(self, value):
        self._created = value

    @property
    def completed(self):
        return self._completed
    @completed.setter
    def completed(self, value):
        self._completed = value

    @property
    def locked(self):
        return self._locked
    @locked.setter
    def locked(self, value):
        self._locked = value

    @property
    def priority(self) -> int:
        return self._priority
    @priority.setter
    def priority(self, value:int):
        self._priority = value

    @property
    def change_reason(self) -> str:
        return self._change_reason
    @change_reason.setter
    def change_reason(self, value:str):
        self._change_reason = value

    @property
    def change_trigger(self) -> str:
        return self._change_trigger
    @change_trigger.setter
    def change_trigger(self, value:str):
        self._change_trigger = value

    @property
    def data(self) -> dict:
        return self._data
    @data.setter
    def data(self, value:dict):
        self._data = value

    def to_dict(self) -> dict:
        return {
            "source": self.source,
            "key": self.key,
            "status": self.status,
            "status_reason": self.status_reason,
            "stage": self.stage,
            "data": self.data,
            "created": self.created,
            "completed": self.completed,
            "locked": self.locked,
            "lock_id": self.lock_id,
            "priority": self.priority,
            "change_reason": self.change_reason,
            "change_trigger": self.change_trigger
        }

    @staticmethod
    def from_dict(d:dict) -> "AgentQueueItem":
        item = AgentQueueItem()
        item.source = d.get("source")
        item.key = d.get("key")
        item.status = d.get("status", AgentQueueItemStatus.NEW)
        item.status_reason = d.get("status_reason")
        item.stage = d.get("stage")
        item.data = d.get("data")
        item.created = d.get("created", datetime.datetime.now(datetime.UTC))
        item.completed = d.get("completed")
        item.locked = d.get("locked")
        item.lock_id = d.get("lock_id")
        item.priority = d.get("priority", 5)
        item.change_reason = d.get("change_reason")
        item.change_trigger = d.get("change_trigger")
        return item
    

class Agent():
    """Agent class for multi-threaded processing queue items."""

    def __init__(self, app:Application, args:AgentArgs):
        self._app = app
        self._args = args
        self._es = app.GetElasticClient()
        self._queue = queue.Queue()
        self._workers: list[threading.Thread] = []
        self._queue_client = None

    @property
    def app(self) -> Application:
        return self._app

    @property
    def queue(self) -> queue.Queue:
        return self._queue
    
    @property
    def args(self) -> AgentArgs:
        return self._args

    @property
    def queue_client(self) -> QueueClient:
        if self._queue_client is None:
            self._queue_client = QueueClient(self._es, self.args.QueueIndexPattern)
        return self._queue_client

    def _fetch_from_es(self) -> int:
        """Fetch a batch of pending items from ES for each source and enqueue them. Returns total enqueued."""
        total = 0
        scroller = self._es.SearchScroll()

    def _start_workers(self):
        """Start worker threads."""
        for i in range(self.args.WorkerCount):
            worker = Worker(i, self)
            t = threading.Thread(target=worker.run, daemon=True)
            self._workers.append(t)
            t.start()

    def _shutdown_workers(self):
        """Block until all queued items are processed, then stop workers."""
        self._queue.join()
        for _ in self._workers:
            self._queue.put(None)
        for t in self._workers:
            t.join()
        self._workers.clear()

    def run(self):
        """Main orchestration loop: start workers, feed ES items into the queue, drain on exit."""
        self._start_workers()
        try:
            while True:
                if self._queue.qsize() < self.args.LowThresholdCount:
                    fetched = self._fetch_from_es()
                    if fetched == 0 and self._queue.empty():
                        _log.debug("Queue empty and no new ES items, sleeping %ss", self.args.PollInterval)
                        time.sleep(self.args.PollInterval)
                else:
                    time.sleep(self.args.PollInterval)
        except KeyboardInterrupt:
            _log.info("SyncAgent interrupted, draining queue...")
        finally:
            self._shutdown_workers()

