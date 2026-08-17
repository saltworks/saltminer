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
from .QueueClient import QueueClient, QueueClientDto, describe_item

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
        :defunct_worker_timeout_secs: optional integer, defaults to 120; if a worker goes this many seconds without a heartbeat while holding an item, the agent releases its item and abandons it. 0 disables defunct-worker detection.  Must exceed the longest expected time a single item can spend between heartbeats (heartbeats fire on item pickup, on each agent.update()/complete(), and - for workers that pass a Core.Heartbeat delegate to their collaborators - as those make progress).  Note this must clear the source API clients' own timeout and retry budgets, since no beat can fire from inside a blocking request or a retry sleep.
        """
        self._queue_index_pattern_tag = queue_index_pattern_tag
        self._low_threshold_count = kwargs.get("low_threshold_count", 10)
        self._worker_count = kwargs.get("worker_count", 5)
        self._polling_interval_secs = kwargs.get("polling_interval_secs", 30)
        self._new_queue_item_stage = kwargs.get("new_queue_item_stage")
        self._queue_batch_size = kwargs.get("queue_batch_size")
        self._worker_error_threshold = kwargs.get("worker_error_threshold", 3)
        self._defunct_worker_timeout_secs = kwargs.get("defunct_worker_timeout_secs", 120)

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

    @property
    def defunct_worker_timeout_secs(self) -> int:
        return self._defunct_worker_timeout_secs
    @defunct_worker_timeout_secs.setter
    def defunct_worker_timeout_secs(self, value:int):
        self._defunct_worker_timeout_secs = value


class Agent():
    """Agent class for multi-threaded processing queue items."""

    # Sentinel for _record_beat: "update the timestamp but leave the current item unchanged".
    _KEEP = object()

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
        self._abandoned_workers: list[threading.Thread] = []
        # Per-worker liveness: {worker_id: {'last_heartbeat': monotonic, 'item': dto|None}}.
        # Written by workers (heartbeats) and read/pruned by the agent thread, so guarded by a lock.
        self._worker_state: dict[int, dict] = {}
        self._state_lock = threading.Lock()
        self._next_worker_id = 0  # next id to hand out; advances past worker_count as replacements spawn
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


    def _spawn_worker(self, worker_id:int) -> threading.Thread:
        """Create, register, and start a single worker thread with the given id."""
        worker = self.worker_factory.create_worker(worker_id, self)
        worker.error_threshold = self.args.worker_error_threshold
        t = threading.Thread(target=worker.run, daemon=True, name=f"worker-{worker_id}")
        t.worker_id = worker_id
        t._worker = worker  # kept so the agent can signal (abandon) the worker instance
        with self._state_lock:
            self._worker_state[worker_id] = {'last_heartbeat': time.monotonic(), 'item': None}
        self._workers.append(t)
        t.start()
        return t


    def _start_workers(self):
        """Start the initial pool of worker threads."""
        for i in range(self.args.worker_count):
            self._spawn_worker(i)
        self._next_worker_id = self.args.worker_count


    def _record_beat(self, worker_id:int, item=_KEEP):
        """Record a worker heartbeat. Pass item to set the worker's current item (None = idle);
        omit it to just refresh the timestamp (progress within an item)."""
        now = time.monotonic()
        with self._state_lock:
            st = self._worker_state.get(worker_id)
            if st is None:
                st = {'last_heartbeat': now, 'item': None}
                self._worker_state[worker_id] = st
            st['last_heartbeat'] = now
            if item is not Agent._KEEP:
                st['item'] = item


    def _beat_current(self):
        """Refresh the heartbeat for the worker running on the current thread (no-op off a worker thread)."""
        wid = getattr(threading.current_thread(), 'worker_id', None)
        if wid is not None:
            self._record_beat(wid)


    def _running_worker_ids(self) -> list[int]:
        """Return the ids of worker threads that are still alive, sorted."""
        return sorted(t.worker_id for t in self._workers if t.is_alive())


    def _reap_defunct_workers(self, context:str="run"):
        """Release the in-progress item of, and abandon, any worker that has stopped heartbeating
        while holding an item.  No-op unless defunct_worker_timeout_secs is set.  Idle workers are
        never reaped (a worker blocked waiting for work has item=None)."""
        timeout = self.args.defunct_worker_timeout_secs
        if not timeout or timeout <= 0:
            return
        now = time.monotonic()
        for t in list(self._workers):
            if not t.is_alive():
                continue
            wid = t.worker_id
            with self._state_lock:
                st = self._worker_state.get(wid)
                item = st.get('item') if st else None
                last = st.get('last_heartbeat', now) if st else now
            if item is None:
                continue
            age = now - last
            if age > timeout:
                logging.error("Worker %d appears defunct (%s): no heartbeat for %.0fs (timeout %ds) while processing item %s. Releasing its item and abandoning the worker.",
                              wid, context, age, timeout, describe_item(item))
                self._release_defunct_item(wid, item, age)
                self._abandon_worker(t, wid)
                # Replace the lost capacity if there's still work waiting (never during shutdown).
                if context == "run" and not self._queue.empty():
                    new_id = self._next_worker_id
                    self._next_worker_id += 1
                    logging.info("Spawning replacement worker %d for abandoned worker %d (work still queued).", new_id, wid)
                    self._spawn_worker(new_id)


    def _release_defunct_item(self, wid:int, item:QueueClientDto, age:float):
        """Mark a defunct worker's stuck in-progress item as errored so it isn't left locked forever."""
        try:
            # Call the queue client directly so we can see the outcome: set_complete returns None
            # on a version conflict (UpdateWithLocking answers {"result": "noop"} rather than
            # raising), which would leave the document locked and invisible to the queue forever.
            # That must never pass silently.
            rsp = self.queue_client.set_complete(item, is_error=True,
                                                 reason=f"Released by agent: worker {wid} became defunct (no heartbeat for {age:.0f}s)")
            if rsp is None:
                logging.error("Could not release defunct worker %d's queue item %s - the write was rejected "
                              "(stale version or already completed). The document may be left locked; check it manually.",
                              wid, describe_item(item))
            else:
                logging.info("Released defunct worker %d's in-progress queue item %s (marked error).", wid, describe_item(item))
        except Exception:
            logging.exception("Failed to release defunct worker %d's queue item %s", wid, describe_item(item))


    def _abandon_worker(self, t:threading.Thread, wid:int):
        """Stop tracking a worker so it no longer counts as active nor blocks shutdown.  The
        underlying daemon thread (which we cannot force-kill) exits when the process does; if it
        ever unblocks, its abandon flag makes it stop instead of picking up more work."""
        w = getattr(t, '_worker', None)
        if w is not None:
            w.abandon()
        self._forget_worker(t)
        if t not in self._abandoned_workers:
            self._abandoned_workers.append(t)
        logging.warning("Worker %d abandoned; %d worker(s) still tracked.", wid, len(self._workers))


    def _forget_worker(self, t:threading.Thread):
        """Remove a worker thread from tracking and drop its liveness state."""
        try:
            self._workers.remove(t)
        except ValueError:
            pass
        with self._state_lock:
            self._worker_state.pop(getattr(t, 'worker_id', None), None)


    def _drain_queue(self):
        """Best-effort drain of any items left in the internal queue so counts stay clean."""
        while True:
            try:
                self._queue.get_nowait()
                self._queue.task_done()
            except queue.Empty:
                break


    def _shutdown_workers(self):
        """Signal workers to stop and wait for them.  Workers still making progress (heartbeating)
        are waited on; workers that go defunct are reaped, so a hung worker can never block shutdown."""
        # Enough sentinels to unblock every tracked worker (plus a margin for any resumed abandoned thread).
        for _ in range(len(self._workers) + len(self._abandoned_workers) + 1):
            self._queue.put(None)
        if self.args.defunct_worker_timeout_secs and self.args.defunct_worker_timeout_secs > 0:
            while self._workers:
                for t in list(self._workers):
                    t.join(timeout=1.0)
                    if not t.is_alive():
                        self._forget_worker(t)
                self._reap_defunct_workers(context="shutdown")
        else:
            # Defunct detection disabled: wait for workers as before (a truly hung worker can block here).
            for t in list(self._workers):
                t.join()
                self._forget_worker(t)
        self._drain_queue()
        self._workers.clear()


    def update(self, dto:QueueClientDto, stage:str=None, data:dict=None) -> QueueClientDto:
        """Update the given queue item stage/data using the QueueClient."""
        self._beat_current()  # progress within an item counts as a heartbeat
        self.queue_client.set_progress(dto, stage, data)
        return dto


    def complete(self, dto:QueueClientDto, stage:str=None, data:dict=None, is_error:bool=True, reason:str=None) -> QueueClientDto:
        """Mark the given queue item as complete using the QueueClient."""
        self._beat_current()
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
                running = self._running_worker_ids()
                running_desc = str(running) if len(running) <= 10 else f"{len(running)} workers"
                logging.info("Internal queue size: %s. Running workers: %s. Sleeping %ss", self._queue.qsize(), running_desc, self.args.polling_interval_secs)
                time.sleep(self.args.polling_interval_secs)
                self._reap_defunct_workers()
                running = self._running_worker_ids()
                logging.debug("Active workers (%d): %s", len(running), running)
                if not running:
                    logging.info("No active workers, queue size: %d, shutting down.  This might be caused by worker errors, check logs.", self._queue.qsize())
                    break
        except KeyboardInterrupt:
            logging.info("Agent interrupted, draining queue...")
        finally:
            self._shutdown_workers()

