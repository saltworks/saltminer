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

# SyncAgent class - used to run multi-threaded processing of sync/refresh.

from __future__ import annotations

import logging
import queue
import threading
import time
from typing import TYPE_CHECKING

from .Application import Application
from .ElasticClient import ElasticClient
from .QueueClient import QueueClient, QueueClientDto

if TYPE_CHECKING:
    # Import for type hints only - importing at runtime creates a circular
    # dependency because Worker imports Agent for its isinstance() check.
    from .Worker import WorkerFactory

class AgentArgs():
    """Arguments for Agent."""
    def __init__(self, queue_index_pattern_tag:str, **kwargs):
        """
        Initialize AgentArgs with optional keyword arguments.

        :queue_index_pattern_tag: required string indicating the index pattern tag for the queue
        :low_threshold_count: optional integer for low threshold count, defaults to 10
        :worker_count: optional integer for number of workers, defaults to 5
        :polling_interval_secs: optional integer for polling interval in seconds, defaults to 30
        :new_queue_item_stage: optional string for the stage to set when new queue items are created
        :queue_batch_size: optional integer for the batch size when fetching queue items from elasticsearch
        :worker_error_threshold: optional integer for the number of consecutive errors a worker can encounter before it is stopped, defaults to 3
        """
        self._queue_index_pattern_tag = queue_index_pattern_tag
        self._low_threshold_count = kwargs.get("low_threshold_count", 10)
        self._worker_count = kwargs.get("worker_count", 5)
        self._polling_interval_secs = kwargs.get("polling_interval_secs", 30)
        self._new_queue_item_stage = kwargs.get("new_queue_item_stage")
        self._queue_batch_size = kwargs.get("queue_batch_size")
        self._worker_error_threshold = kwargs.get("worker_error_threshold", 3)

    @property
    def queue_index_pattern_tag(self) -> str:
        return self._queue_index_pattern_tag
    @queue_index_pattern_tag.setter
    def queue_index_pattern_tag(self, value:str):
        self._queue_index_pattern_tag = value

    @property
    def low_threshold_count(self) -> int:
        return self._low_threshold_count
    @low_threshold_count.setter
    def low_threshold_count(self, value:int):
        self._low_threshold_count = value

    @property
    def worker_count(self) -> int:
        return self._worker_count
    @worker_count.setter
    def worker_count(self, value:int):
        self._worker_count = value

    @property
    def worker_error_threshold(self) -> int:
        return self._worker_error_threshold
    @worker_error_threshold.setter
    def worker_error_threshold(self, value:int):
        self._worker_error_threshold = value

    @property
    def polling_interval_secs(self) -> int:
        return self._polling_interval_secs
    @polling_interval_secs.setter
    def polling_interval_secs(self, value:int):
        self._polling_interval_secs = value

    @property
    def new_queue_item_stage(self) -> str:
        return self._new_queue_item_stage
    @new_queue_item_stage.setter
    def new_queue_item_stage(self, value:str):
        self._new_queue_item_stage = value

    @property
    def queue_batch_size(self) -> int:
        return self._queue_batch_size
    @queue_batch_size.setter
    def queue_batch_size(self, value:int):
        self._queue_batch_size = value


class Agent():
    """Agent class for multi-threaded processing queue items."""

    def __init__(self, app:Application, args:AgentArgs, wrk_factory:WorkerFactory):
        """
        Initialize the Agent with the given Application, AgentArgs, and WorkerFactory.

        :app: Application instance providing access to settings and clients
        :args: AgentArgs instance containing configuration for the agent
        :wrk_factory: WorkerFactory should be a subclass, like SyncWorkerFactory, that creates the appropriate Worker instances for processing the queue items.
        """
        self._app = app
        self._args = args
        self._wrk_factory = wrk_factory
        self._es = app.GetElasticClient()
        self._queue = queue.Queue()
        self._workers: list[threading.Thread] = []
        self._queue_client = None

    @property
    def app(self) -> Application:
        return self._app
    
    @property
    def es(self) -> ElasticClient:
        return self._es

    @property
    def queue(self) -> queue.Queue:
        return self._queue
    
    @property
    def args(self) -> AgentArgs:
        return self._args

    @property
    def queue_client(self) -> QueueClient:
        if self._queue_client is None:
            self._queue_client = QueueClient(self.app, self.args.queue_index_pattern_tag, batch_size=self.args.queue_batch_size)
        return self._queue_client
    
    @property
    def worker_factory(self) -> WorkerFactory:
        return self._wrk_factory


    def _feed_queue(self) -> int:
        """Fetch a batch of pending items from ES for each source and enqueue them. Returns total enqueued, or -1 on error."""
        try:
            batch_size = self.args.worker_count * 2
            work_items, _ = self.queue_client.get_next_queue_batch(self.args.new_queue_item_stage, batch_size)
            for item in work_items:
                self.queue.put(item)
            return len(work_items)
        except Exception:
            logging.exception("Error feeding queue from elasticsearch")
            return -1


    def _start_workers(self):
        """Start worker threads."""
        for i in range(self.args.worker_count):
            worker = self.worker_factory.create_worker(i, self)
            worker.error_threshold = self.args.worker_error_threshold
            t = threading.Thread(target=worker.run, daemon=True)
            self._workers.append(t)
            t.start()


    def _shutdown_workers(self):
        """Block until all queued items are processed, then stop workers."""
        if any(t.is_alive() for t in self._workers):
            self._queue.join()
        else:
            # All workers died (e.g. error threshold) - drain unprocessed items so join() can't block forever
            while True:
                try:
                    self._queue.get_nowait()
                    self._queue.task_done()
                except queue.Empty:
                    break
        for _ in self._workers:
            self._queue.put(None)
        for t in self._workers:
            t.join()
        self._workers.clear()


    def update(self, dto:QueueClientDto, stage:str=None, data:dict=None) -> QueueClientDto:
        """Update the given queue item stage/data using the QueueClient."""
        self.queue_client.set_progress(dto, stage, data)
        return dto
    
    
    def complete(self, dto:QueueClientDto, stage:str=None, data:dict=None, is_error:bool=True, reason:str=None) -> QueueClientDto:
        """Mark the given queue item as complete using the QueueClient."""
        self.queue_client.set_complete(dto, stage, data, is_error, reason)
        return dto


    def run(self, stop_when_empty:bool=False):
        """Main orchestration loop: start workers, feed ES items into the queue, drain on exit."""
        self._start_workers()
        feed_error_count = 0
        try:
            while True:
                if self._queue.qsize() < self.args.low_threshold_count:
                    fetched = self._feed_queue()
                    if fetched == -1:
                        feed_error_count += 1
                        if feed_error_count >= 3:
                            logging.error("Queue feed failed %d times in a row, shutting down.", feed_error_count)
                            break
                    else:
                        feed_error_count = 0
                    if fetched == 0 and self._queue.empty() and stop_when_empty:
                        logging.info("No more items to process and stop_when_empty is True, shutting down.")
                        break
                logging.info("Internal queue size: %s. Sleeping %ss", self._queue.qsize(), self.args.polling_interval_secs)
                time.sleep(self.args.polling_interval_secs)
                active = sum(1 for t in self._workers if t.is_alive())
                logging.debug("Active workers: %d", active)
                if active == 0:
                    logging.info("No active workers, queue size: %d, shutting down.  This might be caused by worker errors, check logs.", self._queue.qsize())
                    break
        except KeyboardInterrupt:
            logging.info("Agent interrupted, draining queue...")
        finally:
            self._shutdown_workers()

