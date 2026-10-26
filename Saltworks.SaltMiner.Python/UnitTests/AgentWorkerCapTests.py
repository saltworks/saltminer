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

import json
import logging
import os
import shutil
import tempfile
import threading
import time
import unittest
import unittest.mock

from Core.Agent import Agent
from Core.ApplicationSettings import ApplicationSettings
from Core.QueueClient import QueueClientDto
from Core.Worker import Worker, WorkerFactory
from RunAgentService import BuildAgentArgs, ResolveSourceNameLimits

SHIPPED_CONFIG = os.path.join(os.path.dirname(os.path.abspath(__file__)), "..", "Config", "SyncAgent.json")


class _FakeApp(object):
    ''' Just enough Application for Agent: no Elasticsearch. '''
    def GetElasticClient(self):
        return None


class _FakeQueue(object):
    '''
    An in-memory sync queue.  Evaluates the term, terms and exists clauses the agent builds, and
    locks what get_next_queue_batch returns, as the real queue client does.
    '''
    def __init__(self):
        self.lock = threading.Lock()
        self.docs = []
        self.next_id = 0

    def add(self, count:int, source_type:str, source_name:str):
        with self.lock:
            for _ in range(count):
                self.next_id += 1
                self.docs.append({"_id": f"q{self.next_id}", "_seq_no": 1, "_primary_term": 1, "_index": "sm_queue_sync",
                                  "_source": {"key": f"k{self.next_id}", "priority": 5,
                                              "created": f"2026-10-26T00:00:{self.next_id:02d}",
                                              "data": {"target_type": source_type, "target_instance": source_name}}})

    @staticmethod
    def _field(source:dict, path:str):
        cur = source
        for part in path.split('.'):
            if not isinstance(cur, dict):
                return None
            cur = cur.get(part)
        return cur

    def _matches(self, source:dict, body:dict) -> bool:
        bool_q = body['query']['bool']
        for clause in bool_q['must']:
            ((field, value),) = clause['term'].items()
            if self._field(source, field) != value:
                return False
        for clause in bool_q['must_not']:
            if 'exists' in clause:
                if self._field(source, clause['exists']['field']) is not None:
                    return False
            else:
                ((field, values),) = clause['terms'].items()
                if self._field(source, field) in values:
                    return False
        return True

    def _select(self, body:dict, size:int) -> list:
        hits = [d for d in self.docs if self._matches(d['_source'], body)]
        hits.sort(key=lambda d: (d['_source']['priority'], d['_source']['created']))
        return hits[:size]

    def reset_in_progress(self, reason:str) -> int:
        return 0

    def get_next_queue_batch_body(self) -> dict:
        return {"aggs": {}, "query": {"bool": {"must": [], "must_not": [
                    {"exists": {"field": "completed"}}, {"exists": {"field": "lock_id"}}], "should": []}},
                "sort": ["priority", "created"]}

    def get_search_body(self, exclude_completed:bool=False, exclude_locked:bool=False) -> dict:
        body = {"query": {"bool": {"must": [], "must_not": [], "should": []}}, "sort": ["priority", "created"]}
        if exclude_completed:
            body['query']['bool']['must_not'].append({"exists": {"field": "completed"}})
        if exclude_locked:
            body['query']['bool']['must_not'].append({"exists": {"field": "lock_id"}})
        return body

    def get_next_queue_batch(self, stage:str=None, size:int=None, custom_body:dict=None):
        with self.lock:
            hits = self._select(custom_body or self.get_next_queue_batch_body(), size)
            for d in hits:
                d['_source']['lock_id'] = "test"
            return [QueueClientDto(d) for d in hits], len(hits)

    def get_search(self, size:int=None, custom_body:dict=None, exclude_completed:bool=False, exclude_locked:bool=False):
        with self.lock:
            hits = self._select(custom_body, size)
        for d in hits:
            yield QueueClientDto(d)

    def set_progress(self, dto, stage=None, data=None):
        return dto

    def set_complete(self, dto, stage=None, data=None, is_error=True, reason=None, worker_id=None):
        with self.lock:
            for d in self.docs:
                if d['_id'] == dto.id:
                    d['_source']['completed'] = "2026-10-26T01:00:00"
        return dto

    def pending(self) -> int:
        with self.lock:
            return sum(1 for d in self.docs if 'completed' not in d['_source'])


class _Tracker(object):
    ''' Counts items in flight per source name, remembers each peak, and holds workers until released. '''
    def __init__(self):
        self.lock = threading.Lock()
        self.current = {}
        self.peak = {}
        self.total_peak = 0
        self.together = set()   # frozenset of (name, count) pairs seen at one instant
        self.release = threading.Event()

    def enter(self, name:str):
        with self.lock:
            self.current[name] = self.current.get(name, 0) + 1
            self.peak[name] = max(self.peak.get(name, 0), self.current[name])
            self.total_peak = max(self.total_peak, sum(self.current.values()))
            self.together.add(tuple(sorted(self.current.items())))

    def leave(self, name:str):
        with self.lock:
            self.current[name] -= 1

    def snapshot(self) -> tuple:
        with self.lock:
            return tuple(sorted(self.current.items()))


class _TestWorker(Worker):
    def __init__(self, id:int, agent:Agent, tracker:_Tracker):
        super().__init__(id, agent)
        self._tracker = tracker

    @property
    def logger(self):
        return logging.getLogger("test.worker")

    def _process(self, item:QueueClientDto):
        name = item.doc.dto()['data']['target_instance']
        self._tracker.enter(name)
        try:
            self._tracker.release.wait(timeout=10)
        finally:
            self._tracker.leave(name)
        self.agent.complete(item, is_error=False)


class _TestWorkerFactory(WorkerFactory):
    def __init__(self, tracker:_Tracker):
        self._tracker = tracker

    def create_worker(self, id:int, agent:Agent, **kwargs) -> Worker:
        return _TestWorker(id, agent, self._tracker)


class AgentWorkerCapTests(unittest.TestCase):
    ''' PBI-131 unit tests: the FOD worker cap per credential and per source.  No Elasticsearch or FOD needed. '''

    def setUp(self):
        self.tmp = tempfile.TemporaryDirectory()
        self.addCleanup(self.tmp.cleanup)
        self.tracker = _Tracker()
        self.addCleanup(self.tracker.release.set)
        self.queue = _FakeQueue()
        self.sources = {}

    def _settings(self, shipped:bool=False, **sync_agent) -> ApplicationSettings:
        ''' Loads a SyncAgent config (the shipped file byte for byte, or the shipped values plus overrides)
        and the sources added with _source. '''
        main = os.path.join(self.tmp.name, "SyncAgent.json")
        if shipped:
            shutil.copyfile(SHIPPED_CONFIG, main)
        else:
            with open(SHIPPED_CONFIG) as f:
                cfg = json.load(f)
            cfg.update(sync_agent)
            with open(main, "w") as f:
                json.dump(cfg, f)
        sdir = os.path.join(self.tmp.name, "Sources")
        os.makedirs(sdir, exist_ok=True)
        files = []
        for name, data in self.sources.items():
            path = os.path.join(sdir, f"{name}.json")
            with open(path, "w") as f:
                json.dump(data, f)
            files.append(path)
        return ApplicationSettings([main], files, keyFile=os.path.join(self.tmp.name, "test.key"))

    def _source(self, name:str, *client_ids, source_type:str="FOD"):
        ''' Adds a source of the given type holding one key per client id (the first as the top-level pair). '''
        data = {"Source": source_type, "SourceName": name, "BaseUrl": "https://fod.test"}
        if client_ids:
            data["ClientId"] = client_ids[0]
            data["ClientSecret"] = f"secret-{client_ids[0]}"
            data["ApiKeys"] = [{"ClientId": c, "ClientSecret": f"secret-{c}"} for c in client_ids[1:]]
        self.sources[name] = data

    def _agent(self, settings:ApplicationSettings) -> Agent:
        args = BuildAgentArgs(settings)
        args.polling_interval_secs = 0.01   # only the poll is sped up; every cap stays as configured
        agent = Agent(_FakeApp(), args, _TestWorkerFactory(self.tracker))
        agent._queue_client = self.queue
        return agent

    def _start(self, agent:Agent) -> threading.Thread:
        t = threading.Thread(target=agent.run, kwargs={"stop_when_empty": True}, daemon=True)
        t.start()
        self.addCleanup(self.tracker.release.set)
        return t

    def _settle(self, limit_secs:float=5.0):
        ''' Waits until the in-flight picture has held steady for 0.5 s, or the limit passes. '''
        deadline = time.monotonic() + limit_secs
        last, since = None, time.monotonic()
        while time.monotonic() < deadline:
            snap = self.tracker.snapshot()
            if snap != last:
                last, since = snap, time.monotonic()
            elif snap and time.monotonic() - since >= 0.5:
                return
            time.sleep(0.02)

    def _finish(self, t:threading.Thread):
        ''' Releases the held workers and waits for the agent to drain the queue. '''
        self.tracker.release.set()
        t.join(timeout=15)
        self.assertFalse(t.is_alive(), "agent did not finish")
        self.assertEqual(self.queue.pending(), 0)

    def _run(self, agent:Agent):
        t = self._start(agent)
        self._settle()
        held = dict(self.tracker.peak)
        self._finish(t)
        return held

    # ---- AC-1 ----------------------------------------------------------------------------
    def test_shipped_config_caps_fod_at_4(self):
        """AC-1: shipped SyncAgent.json, one single-key FOD source, 10 items: at most 4 FOD workers at once."""
        self._source("FOD1", "kA")
        self.queue.add(10, "FOD", "FOD1")
        settings = self._settings(shipped=True)
        self.assertEqual(ResolveSourceNameLimits(settings), {})
        held = self._run(self._agent(settings))
        self.assertLessEqual(max(self.tracker.peak.values()), 4)
        self.assertEqual(held["FOD1"], 4)

    def test_shipped_type_cap_binds_above_worker_count(self):
        """With WorkerCount 8 and nothing else set, the type cap of 4 still binds, not the pool."""
        self._source("FOD1", "kA")
        self.queue.add(10, "FOD", "FOD1")
        self._run(self._agent(self._settings(WorkerCount=8)))
        self.assertEqual(self.tracker.peak["FOD1"], 4)

    # ---- AC-2 ----------------------------------------------------------------------------
    def test_per_credential_three_keys(self):
        """AC-2: 4 per credential, three keys, WorkerCount 12, 20 items: 12 at once and never 13."""
        self._source("FOD1", "kA", "kB", "kC")
        self.queue.add(20, "FOD", "FOD1")
        settings = self._settings(WorkerCount=12, PerCredentialWorkerLimits={"FOD": 4})
        self.assertEqual(ResolveSourceNameLimits(settings), {"FOD1": 12})
        held = self._run(self._agent(settings))
        self.assertEqual(held["FOD1"], 12)
        self.assertEqual(self.tracker.peak["FOD1"], 12)
        self.assertEqual(self.tracker.total_peak, 12)

    # ---- AC-3 ----------------------------------------------------------------------------
    def test_two_sources_own_caps(self):
        """AC-3: two FOD sources, a cap of 2 each: each runs 2 at once, and both at the same time."""
        self._source("FOD1", "kA")
        self._source("FOD2", "kB")
        self.queue.add(6, "FOD", "FOD1")
        self.queue.add(6, "FOD", "FOD2")
        held = self._run(self._agent(self._settings(WorkerCount=8, SourceNameWorkerLimits={"FOD1": 2, "FOD2": 2})))
        self.assertEqual(held, {"FOD1": 2, "FOD2": 2})
        self.assertEqual(self.tracker.peak, {"FOD1": 2, "FOD2": 2})
        self.assertIn((("FOD1", 2), ("FOD2", 2)), self.tracker.together)

    def test_name_cap_replaces_type_cap_for_that_source(self):
        """A source with a name cap is not counted against its type's cap; the other FOD source still is."""
        self._source("FOD1", "kA")
        self._source("FOD2", "kB")
        self.queue.add(6, "FOD", "FOD1")
        self.queue.add(6, "FOD", "FOD2")
        self._run(self._agent(self._settings(WorkerCount=12, SourceNameWorkerLimits={"FOD1": 2})))
        self.assertEqual(self.tracker.peak, {"FOD1": 2, "FOD2": 4})

    # ---- AC-4 ----------------------------------------------------------------------------
    def test_saturation_info_once(self):
        """AC-4: a source held at its cap of 2 with work waiting, across many feed passes, logs one INFO line naming it."""
        self._source("FOD1", "kA")
        self.queue.add(5, "FOD", "FOD1")
        agent = self._agent(self._settings(WorkerCount=4, SourceNameWorkerLimits={"FOD1": 2}))
        with self.assertLogs(level="INFO") as logs:
            t = self._start(agent)
            self._settle()
            time.sleep(0.3)
        lines = [r for r in logs.records if r.levelno == logging.INFO and "at its worker cap" in r.getMessage()]
        self.assertEqual(len(lines), 1)
        self.assertIn("FOD1", lines[0].getMessage())
        self._finish(t)

    def test_name_caps_without_type_caps(self):
        """A name cap applies even when SourceWorkerLimits is empty."""
        self._source("FOD1", "kA")
        self.queue.add(6, "FOD", "FOD1")
        self._run(self._agent(self._settings(WorkerCount=6, SourceWorkerLimits={}, SourceNameWorkerLimits={"FOD1": 2})))
        self.assertEqual(self.tracker.peak["FOD1"], 2)

    # ---- resolver ------------------------------------------------------------------------
    def test_resolver_rules(self):
        """A name cap beats the per-credential cap; a bad key list counts 1 with an ERROR; a shared ClientId warns."""
        self._source("FOD1", "kA", "kB", "kC")
        self._source("FOD2", "kD")
        self.sources["FOD2"]["ApiKeys"] = [{"ClientId": "kE"}]        # malformed entry
        self._source("FOD3")                                          # no credentials at all
        self._source("FOD4", "kA")                                    # repeats FOD1's ClientId
        self._source("SSC1", source_type="SSC")
        settings = self._settings(PerCredentialWorkerLimits={"FOD": 4, "SSC": 3}, SourceNameWorkerLimits={"FOD4": 7})
        with self.assertLogs(level="INFO") as logs:
            caps = ResolveSourceNameLimits(settings)
        self.assertEqual(caps, {"FOD1": 12, "FOD2": 4, "FOD3": 4, "FOD4": 7, "SSC1": 3})
        text = "\n".join(f"{r.levelname} {r.getMessage()}" for r in logs.records)
        self.assertIn("ERROR Cannot count the credentials of source 'FOD2'", text)
        self.assertIn("ERROR Cannot count the credentials of source 'FOD3'", text)
        self.assertIn("WARNING ClientId 'kA' is used by FOD sources FOD1, FOD4", text)
        self.assertIn("INFO Source 'FOD1' worker cap 12 (4 per credential x 3 keys)", text)
        self.assertNotIn("secret-", text)

    def test_bad_limit_values_are_ignored(self):
        self._source("FOD1", "kA")
        settings = self._settings(PerCredentialWorkerLimits={"FOD": "many"}, SourceNameWorkerLimits={"FOD1": -1})
        with self.assertLogs(level="ERROR") as logs:
            self.assertEqual(ResolveSourceNameLimits(settings), {})
        self.assertEqual(len(logs.records), 2)


if __name__ == "__main__":
    unittest.main()
