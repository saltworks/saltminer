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
        :agent_id: optional id identifying this agent, stamped onto every queue doc it locks so a
        finished item can be traced back to the agent (and worker) that handled it.  Distinct from the
        queue client's per-run session uuid, which is used as the lock id and cleared on completion.
        :source_limits: optional dict of {source key: max concurrent items}, e.g. {"FOD": 2}.  Caps how
        many items of one source can be in flight at once, for sources whose api will not tolerate the
        full pool (rate limiting).  A source not named here is uncapped and may use the whole pool, so
        the fast ones need no entry.  The cap is applied when items are fetched, not when they are
        picked up - fetching is what locks a queue document, so anything fetched must be runnable.
        :source_field: optional dotted path to the field holding the source key, both in the queue
        document (for the elasticsearch term filter) and in its parsed dto.  Defaults to
        "data.target_type".  Only used when source_limits is set.
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
        self._agent_id = kwargs.get("agent_id")
        self._source_field = kwargs.get("source_field") or "data.target_type"
        self.source_limits = kwargs.get("source_limits")

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

    @property
    def agent_id(self):
        return self._agent_id
    @agent_id.setter
    def agent_id(self, value):
        self._agent_id = value

    @property
    def source_limits(self) -> dict:
        return self._source_limits
    @source_limits.setter
    def source_limits(self, value:dict):
        # Normalized once here rather than defended against on every feed: config is hand-edited json,
        # so a string count or a stray null is a live possibility and a bad entry must not silently
        # become "uncapped" (which is the one outcome the setting exists to prevent).
        limits = {}
        for key, count in (value or {}).items():
            try:
                count = int(count)
            except (TypeError, ValueError):
                logging.error("Ignoring source worker limit for '%s': '%s' is not a number.", key, count)
                continue
            if count < 0:
                logging.error("Ignoring source worker limit for '%s': %s is negative.", key, count)
                continue
            if count == 0:
                logging.warning("Source worker limit for '%s' is 0 - no items for that source will be processed.", key)
            limits[str(key)] = count
        self._source_limits = limits

    @property
    def source_field(self) -> str:
        return self._source_field
    @source_field.setter
    def source_field(self, value:str):
        self._source_field = value or "data.target_type"


class Agent():
    """Agent class for multi-threaded processing queue items."""

    # Sentinel for _record_beat: "update the timestamp but leave the current item unchanged".
    _KEEP = object()

    # Used to order a feed pass when a doc carries no priority; matches QueueClient's own default.
    _DEFAULT_PRIORITY = 5

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
        # Run tally, for the end-of-run summary.  Completed/errored are counted where the agent is told
        # the outcome (complete()), so they reflect what workers actually recorded, not what was fed.
        self._items_fed = 0
        self._items_completed = 0
        self._items_errored = 0
        # Per-worker liveness: {worker_id: {'last_heartbeat': monotonic, 'item': dto|None}}.
        # Written by workers (heartbeats) and read/pruned by the agent thread, so guarded by a lock.
        self._worker_state: dict[int, dict] = {}
        self._state_lock = threading.Lock()
        self._next_worker_id = 0  # next id to hand out; advances past worker_count as replacements spawn
        self._queue_client = None
        # True when the last feed fetched nothing only because a capped source was at its limit -
        # work remains, so an empty queue must not be read as a drained one.
        self._feed_withheld = False

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
            self._queue_client = QueueClient(self.app, self.args.queue_index_pattern_tag, batch_size=self.args.queue_batch_size,
                                             agent_id=self.args.agent_id)
        return self._queue_client
    
    @property
    def worker_factory(self) -> WorkerFactory:
        return self._wrk_factory


    def _item_source(self, item:QueueClientDto) -> str:
        """Source key of a queue item, read by args.source_field path (e.g. data.target_type).
        None when the item has no value there - such items are never capped."""
        if item is None:
            return None
        try:
            cur = item.doc.dto()
            for part in self.args.source_field.split('.'):
                if not isinstance(cur, dict):
                    return None
                cur = cur.get(part)
            return cur if isinstance(cur, str) else None
        except Exception:
            return None


    def _source_in_use_counts(self) -> dict:
        """Items per source that this agent is already committed to: in flight on a worker, plus
        fetched-and-waiting in the internal queue.  Both count against a source's limit - a queued
        item is already locked in elasticsearch and will run as soon as a worker frees up."""
        counts = {}
        with self._state_lock:
            in_flight = [st.get('item') for st in self._worker_state.values()]
        # Snapshot under the queue's own mutex; the deque behind Queue is not safe to read without it.
        with self._queue.mutex:
            waiting = list(self._queue.queue)
        for item in in_flight + waiting:
            src = self._item_source(item)
            if src is not None:
                counts[src] = counts.get(src, 0) + 1
        return counts


    def _fetch_batch(self, size:int, custom_body:dict=None) -> list:
        """Lock up to size pending items in elasticsearch and return them (not yet queued)."""
        if size <= 0:
            return []
        work_items, _ = self.queue_client.get_next_queue_batch(self.args.new_queue_item_stage, size, custom_body=custom_body)
        # get_next_queue_batch answers (None, None) when the search comes back without its aggregation.
        return work_items or []


    @staticmethod
    def _order_key(item:QueueClientDto):
        """Sort key matching the queue's own ordering - priority first (lower runs sooner), then age.
        created is an iso-8601 string as elasticsearch returned it, which sorts correctly as text."""
        doc = item.doc
        pri = doc.priority if isinstance(doc.priority, int) else Agent._DEFAULT_PRIORITY
        return (pri, str(doc.created or ""))


    def _enqueue(self, items:list) -> int:
        """Put a feed pass's items on the internal queue in queue order.

        Sorted because a capped feed asks for each source separately, so the results arrive grouped by
        source: queued as-is, every capped source's items would run ahead of older work from every other
        source, on every pass.  Sorting restores fifo across the items this pass admitted.  It cannot
        restore fifo across the ones it *didn't* - a source at its limit is passed over, and the items
        pulled forward in its place are by definition younger.  That is the trade the limit buys.
        """
        items.sort(key=Agent._order_key)
        for item in items:
            self.queue.put(item)
        self._items_fed += len(items)
        return len(items)


    def _feed_queue(self) -> int:
        """Fetch a batch of pending items from ES for each source and enqueue them. Returns total enqueued, or -1 on error."""
        try:
            batch_size = self.args.worker_count * 2
            limits = self.args.source_limits
            self._feed_withheld = False
            if not limits:
                return self._enqueue(self._fetch_batch(batch_size))

            # Capped sources are fetched one query at a time, each sized to that source's remaining
            # headroom.  The cap has to be applied in the query: get_next_queue_batch locks every
            # document it returns, so anything fetched and then discarded would be left locked.
            in_use = self._source_in_use_counts()
            fetched_items = []
            capped = sorted(limits.keys())
            for src in capped:
                headroom = min(limits[src] - in_use.get(src, 0), batch_size - len(fetched_items))
                if headroom <= 0:
                    if in_use.get(src, 0) > 0:
                        # At the cap because we're busy, not because the source is drained.  Say so, or
                        # run() reads a zero fetch on an empty queue as "nothing left" and shuts down
                        # with the rest of this source's backlog still pending.
                        self._feed_withheld = True
                        logging.debug("Source '%s' is at its limit of %s, not fetching more this pass.", src, limits[src])
                    continue
                body = self.queue_client.get_next_queue_batch_body()
                body['query']['bool']['must'].append({"term": {self.args.source_field: src}})
                batch = self._fetch_batch(headroom, body)
                if batch:
                    logging.debug("Fetched %s item(s) for source '%s' (limit %s, %s already in use).",
                                  len(batch), src, limits[src], in_use.get(src, 0))
                fetched_items.extend(batch)

            # Everything else, with the capped sources excluded.  Without that exclusion a large backlog
            # for a capped source fills this query and starves the uncapped ones out of the fifo.
            if batch_size - len(fetched_items) > 0:
                body = self.queue_client.get_next_queue_batch_body()
                body['query']['bool']['must_not'].append({"terms": {self.args.source_field: capped}})
                fetched_items.extend(self._fetch_batch(batch_size - len(fetched_items), body))
            return self._enqueue(fetched_items)
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
                                                 reason=f"Released by agent: worker {wid} became defunct (no heartbeat for {age:.0f}s)",
                                                 worker_id=wid)
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
        if is_error:
            self._items_errored += 1
        else:
            self._items_completed += 1
        return dto


    def run(self, stop_when_empty:bool=False):
        """Main orchestration loop: start workers, feed ES items into the queue, drain on exit."""
        # Anything still In Progress under our agent id is ours and nobody is working it - we haven't
        # started yet.  On a clean start this finds nothing; anything it does find was stranded by a
        # previous run that died holding locked items.
        self.queue_client.reset_in_progress("Released at agent startup - held by a previous run")
        self._start_workers()
        feed_error_count = 0
        started = time.monotonic()
        # Why we stopped, for the end-of-run summary.  Set at each exit; anything left as the default
        # means we fell out of the loop by a route nobody described, which is worth saying out loud.
        stop_reason = "unknown"
        clean = False
        try:
            while True:
                if self._queue.qsize() < self.args.low_threshold_count:
                    fetched = self._feed_queue()
                    if fetched == -1:
                        feed_error_count += 1
                        if feed_error_count >= 3:
                            logging.error("Queue feed failed %d times in a row, shutting down.", feed_error_count)
                            stop_reason = f"queue feed failed {feed_error_count} times in a row"
                            break
                    else:
                        feed_error_count = 0
                    if fetched == 0 and self._feed_withheld:
                        logging.info("Nothing fetched this pass - every source with work left is at its worker limit.")
                    if fetched == 0 and not self._feed_withheld and self._queue.empty() and stop_when_empty:
                        logging.info("No more items to process and stop_when_empty is True, shutting down.")
                        stop_reason = "queue drained"
                        clean = True
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
                    stop_reason = (f"all {self.args.worker_count} worker(s) stopped - each hit the "
                                   f"consecutive error threshold (WorkerErrorThreshold={self.args.worker_error_threshold})")
                    break
        except KeyboardInterrupt:
            logging.info("Agent interrupted, draining queue...")
            stop_reason = "interrupted"
            clean = True
        except Exception as ex:
            stop_reason = f"unexpected error: [{type(ex).__name__}] {ex}"
            raise
        finally:
            left_in_queue = self._queue.qsize()
            self._shutdown_workers()
            # Workers are stopped, so nothing is mid-flight: whatever is still In Progress under our id
            # was locked into the in-memory queue and never processed.  Release it or it stays locked
            # and invisible to every future run.  Matters most when the pool stopped on errors
            # (WorkerErrorThreshold) with a full queue behind it.
            released = self.queue_client.reset_in_progress("Released at agent shutdown - not processed")
            summary = ("Agent run finished: %s.  Items fed: %s, completed: %s, errored: %s, "
                       "unprocessed at shutdown: %s (released: %s).  Elapsed: %s sec.")
            args = (stop_reason, self._items_fed, self._items_completed, self._items_errored,
                    left_in_queue, released, int(time.monotonic() - started))
            if clean and not self._items_errored:
                logging.info(summary, *args)
            elif clean:
                logging.warning(summary, *args)
            else:
                logging.error(summary, *args)

