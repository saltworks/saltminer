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
'''

# SyncWorker class - used to run sync processing in multi-threaded environment.

import subprocess
import threading
import time
from collections import deque

from Core.Agent import Agent
from Core.Heartbeat import Heartbeat
from Core.Worker import WorkerFactory, Worker, WorkerException
from Core.QueueClient import QueueClientDto
from .SSC.SyncExtractor import SyncExtractor as SscSync
from .SSC.AppVulsProcessor import AppVulsProcessor as SscRefresh
from .FOD.SyncExtractor import SyncExtractor as FodSync
from .FOD.AppVulsProcessor import AppVulsProcessor as FodRefresh


# Relative to the agent's working directory (the python app dir) - the manager binaries sit
# alongside it in the services container.
MANAGER_DLL_DEFAULT = "./manager/Saltworks.SaltMiner.Manager.dll"

# Once ManagerTimeoutSec is up, manager is only killed if it has also been silent this long.  Big
# queue scans legitimately run past the timeout, and a killed manager leaves its queue scan stranded.
MANAGER_ACTIVITY_WINDOW_SEC = 30


class SyncQueueType():
    SSC = "SSC"
    FOD = "FOD"
    @staticmethod
    def is_valid(value:str) -> bool:
        return value in [SyncQueueType.SSC, SyncQueueType.FOD]


class SyncQueueStage():
    REFRESH = "Refresh"
    SYNC = "Sync"
    FINALIZE = "Finalize"


class SyncQueueData():
    """Data object for sync queue items."""
    def __init__(self, dto:dict=None):
        """
        Initialize SyncQueueData with required and optional parameters.

        :dto: required dictionary containing the data for the sync queue data
        """
        if dto:
            self._target_id = dto.get("target_id")
            self._target_type = dto.get("target_type")
            self._target_instance = dto.get("target_instance")
            self._force = dto.get("force", False)
    
    @property
    def target_id(self) -> str:
        if not hasattr(self, "_target_id"):
            self._target_id = None
        return self._target_id
    @target_id.setter
    def target_id(self, value:str):
        self._target_id = value

    @property
    def target_type(self) -> str:
        if not hasattr(self, "_target_type"):
            self._target_type = None
        return self._target_type
    @target_type.setter
    def target_type(self, value:str):
        self._target_type = value

    @property
    def target_instance(self) -> str:
        if not hasattr(self, "_target_instance"):
            self._target_instance = None
        return self._target_instance
    @target_instance.setter
    def target_instance(self, value:str):
        self._target_instance = value

    @property
    def force(self) -> bool:
        if not hasattr(self, "_force"):
            self._force = False
        return self._force
    @force.setter
    def force(self, value:bool):
        self._force = value

    def to_dto(self) -> dict:
        """Convert SyncQueueData to a dictionary."""
        return {
            "target_id": self._target_id,
            "target_type": self._target_type,
            "target_instance": self._target_instance,
            "force": self._force
        }


class SyncWorkerFactory(WorkerFactory):
    """Factory class for creating SyncWorker instances."""
    def create_worker(self, id:int, agent:Agent, **kwargs) -> Worker:
        """Create and return a new SyncWorker instance."""
        return SyncWorker(id, agent, **kwargs)

class SyncWorker(Worker):
    """Worker class for multi-threaded processing of sync/refresh."""
    def __init__(self, id:int, agent:Agent, **kwargs):
        super().__init__(id, agent, **kwargs)
        self._ssc_sync = None
        self._ssc_refresh = None
        self._fod_sync = None
        self._fod_refresh = None
        # A sync or refresh of one project version/release can run far longer than the agent's
        # defunct_worker_timeout_secs without returning, so the extractors and processors get a
        # throttled delegate they can fire as they make progress.  One per worker; the extractor
        # instances below are cached per worker too, so it's bound once at construction.
        self._heartbeat = Heartbeat(self.heartbeat, min_interval_secs=kwargs.get("heartbeat_interval_secs", 5))

    def _get_ssc_sync(self, src_name:str) -> SscSync:
        if self._ssc_sync is None or self._ssc_sync.SourceName != src_name:
            if self._ssc_sync is not None:
                self._ssc_sync.Cleanup()
            self._ssc_sync = SscSync(self.agent.app.Settings, src_name, logger=self.logger, heartbeat=self._heartbeat)
        return self._ssc_sync


    def _get_ssc_refresh(self, src_name:str) -> SscRefresh:
        if self._ssc_refresh is None or self._ssc_refresh.SourceName != src_name:
            self._ssc_refresh = SscRefresh(self.agent.app.Settings, src_name, logger=self.logger, heartbeat=self._heartbeat)
        return self._ssc_refresh


    def _get_fod_sync(self, src_name:str) -> FodSync:
        if self._fod_sync is None or self._fod_sync.SourceName != src_name:
            self._fod_sync = FodSync(self.agent.app.Settings, src_name, logger=self.logger, heartbeat=self._heartbeat)
        return self._fod_sync


    def _get_fod_refresh(self, src_name:str) -> FodRefresh:
        if self._fod_refresh is None or self._fod_refresh.SourceName != src_name:
            self._fod_refresh = FodRefresh(self.agent.app.Settings, src_name, logger=self.logger, heartbeat=self._heartbeat)
        return self._fod_refresh


    def _run_manager(self, queue_scan_ids:list, target_desc:str):
        """
        Runs the manager's queue processor once per queue scan ID created by the refresh stage, so
        the data lands in assets/issues now instead of waiting for the next cron pass.

        Manager is a run-once CLI sharing the services container with us, so it's launched the same
        way svc mgr's CommandJob does: `dotnet <ManagerDll> queue --queue-scan-id <id>`.  It picks up
        its own config from the SALTMINER_CONFIG_PATH already in our environment, which the child
        inherits.  Set ManagerDll to "" to turn the hand-off off and leave queue scans for the
        manager cron.  Raises on failure - the caller turns that into an errored item.
        """
        if not queue_scan_ids:
            self.logger.info("No queue scans created for %s, nothing for manager to process.", target_desc)
            return
        settings = self.agent.app.Settings
        manager_dll = settings.Get("SyncAgent", "ManagerDll", MANAGER_DLL_DEFAULT)
        if not manager_dll:
            self.logger.warning("SyncAgent.ManagerDll is empty - skipping manager processing for %s (%s queue scan(s) left for the manager cron).", target_desc, len(queue_scan_ids))
            return
        dotnet = settings.Get("SyncAgent", "ManagerDotNetPath", "dotnet")
        timeout_sec = settings.Get("SyncAgent", "ManagerTimeoutSec", 600)

        for qsid in queue_scan_ids:
            cmd = [dotnet, manager_dll, "queue", "--queue-scan-id", str(qsid)]
            self.logger.info("Running manager for queue scan ID %s (%s)", qsid, target_desc)
            self._run_manager_command(cmd, timeout_sec, qsid)


    def _run_manager_command(self, cmd:list, timeout_sec:int, qsid:str):
        """
        Runs one manager process, beating while it works so a slow queue load doesn't get this
        worker reaped as defunct.  Raises WorkerException on non-zero exit or timeout.

        The timeout is a liveness check, not a wall-clock budget.  A big queue scan can legitimately
        take longer than ManagerTimeoutSec, and killing a manager that is still loading issues leaves
        the queue scan stranded mid-flight.  So when the timeout is reached we look at when the child
        last wrote to us: still talking within MANAGER_ACTIVITY_WINDOW_SEC means it's working, and it
        gets another full ManagerTimeoutSec.  Silent for that long means wedged, and it's killed.
        """
        try:
            proc = subprocess.Popen(cmd, stdout=subprocess.PIPE, stderr=subprocess.STDOUT, text=True)
        except OSError as ex:
            raise WorkerException(f"Unable to start manager for queue scan ID {qsid} ('{cmd[0]} {cmd[1]}'): {ex}") from ex

        # Drain the pipe on its own thread.  Manager is chatty, and reading only after wait()
        # returns would deadlock it the moment it filled the pipe buffer.  Keep just the tail -
        # manager writes its own full log; this is so a failure is diagnosable from our log alone.
        # The drain also doubles as the liveness signal: last_output is the proof of life below.
        tail = deque(maxlen=5)
        last_output = [time.monotonic()]
        def drain():
            try:
                for line in proc.stdout:
                    last_output[0] = time.monotonic()
                    tail.append(line.rstrip())
            except Exception:
                pass
            finally:
                proc.stdout.close()
        reader = threading.Thread(target=drain, daemon=True)
        reader.start()

        started = time.monotonic()
        deadline = started + timeout_sec if timeout_sec else 0
        silent_for = 0
        timed_out = False
        while True:
            try:
                proc.wait(timeout=5)
                break
            except subprocess.TimeoutExpired:
                self.heartbeat()
                now = time.monotonic()
                if not deadline or now < deadline:
                    continue
                silent_for = now - last_output[0]
                if silent_for > MANAGER_ACTIVITY_WINDOW_SEC:
                    timed_out = True
                    proc.kill()
                    proc.wait()
                    break
                deadline = now + timeout_sec
                self.logger.warning(
                    "Manager has been running %.0f sec on queue scan ID %s, past the %s sec timeout, but wrote output %.0f sec ago - still working, extending by another %s sec.",
                    now - started, qsid, timeout_sec, silent_for, timeout_sec)
        reader.join(timeout=5)

        if timed_out:
            raise WorkerException(
                f"Manager killed after {time.monotonic() - started:.0f} sec processing queue scan ID {qsid} - "
                f"no output for {silent_for:.0f} sec (timeout {timeout_sec} sec, liveness window {MANAGER_ACTIVITY_WINDOW_SEC} sec). "
                "Last output:\n" + "\n".join(tail))
        if proc.returncode != 0:
            raise WorkerException(f"Manager exited with code {proc.returncode} processing queue scan ID {qsid}. Last output:\n" + "\n".join(tail))
        self.logger.info("Manager completed queue scan ID %s in %.0f sec", qsid, time.monotonic() - started)


    def _process(self, item:QueueClientDto):
        """Process a single queue item - exceptions handled by Worker.run()"""
        # Parse the payload alongside the DTO, never into a copy of it.  Every successful write
        # refreshes the DTO's _seq_no/_primary_term, so a second DTO over the same document goes
        # stale the moment either one writes: the loser's next UpdateWithLocking is a version
        # conflict, which returns {"result": "noop"} instead of raising, and the queue document is
        # silently left locked forever - invisible to the queue and never retried.  The agent holds
        # *this* instance in its liveness map to release the item if we go defunct, so it has to be
        # the same object we update and complete.
        data = SyncQueueData(item.doc.data)
        if data.target_type == SyncQueueType.SSC:
            self._process_ssc(item, data)
        elif data.target_type == SyncQueueType.FOD:
            self._process_fod(item, data)
        else:
            # Worker.run() increments error_count for any raised exception - don't double count here
            raise WorkerException(f"Invalid sync queue item target type: {data.target_type}")


    def _process_ssc(self, item:QueueClientDto, data:SyncQueueData):
        try:
            sync = self._get_ssc_sync(data.target_instance)
            # queueRefresh off - we run the refresh for this ID ourselves below, so an
            # sscupdatequeue record would only queue the same work a second time.
            sync.ProcessOne(data.target_id, data.force, queueRefresh=False)
            self.agent.update(item, SyncQueueStage.REFRESH, data.to_dto())
            refresh = self._get_ssc_refresh(data.target_instance)
            # race_retry on - the sync stage above just wrote this doc, and elasticsearch's
            # near-real-time refresh can leave it unsearchable for a moment.  Without the retry
            # PopulateVulsOne reads back nothing and returns early, skipping the whole refresh
            # (including the "noscan" queue data for missing expected assessment types).
            queue_scan_ids = refresh.PopulateVulsOne(data.target_id, race_retry=True)
            self.agent.update(item, SyncQueueStage.FINALIZE, data.to_dto())
            self._run_manager(queue_scan_ids, f"SSC project version {data.target_id} ('{data.target_instance}')")
            self.agent.complete(item, stage="", is_error=False)
        except Exception as ex:
            self.logger.error("Error processing SSC ID '%s' ('%s'), stage %s: %s", data.target_id, data.target_instance, item.doc.stage, str(ex))
            try:
                self.agent.complete(item, is_error=True, reason=str(ex))
            except Exception as ex2:
                self.logger.error("Error setting error for SSC ID '%s' ('%s'), stage %s: %s", data.target_id, data.target_instance, item.doc.stage, str(ex2))
            raise WorkerException(f"Error processing SSC ID '{data.target_id}' ('{data.target_instance}'), stage {item.doc.stage}") from ex


    def _process_fod(self, item:QueueClientDto, data:SyncQueueData):
        try:
            sync = self._get_fod_sync(data.target_instance)
            # queueRefresh off - we run the refresh for this ID ourselves below, so a
            # fodupdatequeue record would only queue the same work a second time.
            sync.ProcessOne(data.target_id, data.force, queueRefresh=False)
            self.agent.update(item, SyncQueueStage.REFRESH, data.to_dto())
            refresh = self._get_fod_refresh(data.target_instance)
            # race_retry on - the sync stage above just wrote this doc, and elasticsearch's
            # near-real-time refresh can leave it unsearchable for a moment.  Without the retry
            # PopulateVulsOne reads back nothing and returns early, skipping the whole refresh
            # (including the "noscan" queue data for missing expected assessment types).
            queue_scan_ids = refresh.PopulateVulsOne(data.target_id, race_retry=True)
            self.agent.update(item, SyncQueueStage.FINALIZE, data.to_dto())
            self._run_manager(queue_scan_ids, f"FOD release {data.target_id} ('{data.target_instance}')")
            self.agent.complete(item, stage="", is_error=False)
        except Exception as ex:
            self.logger.error("Error processing FOD ID '%s' ('%s'), stage %s: %s", data.target_id, data.target_instance, item.doc.stage, str(ex))
            try:
                self.agent.complete(item, is_error=True, reason=str(ex))
            except Exception as ex2:
                self.logger.error("Error setting error for FOD ID '%s' ('%s'), stage %s: %s", data.target_id, data.target_instance, item.doc.stage, str(ex2))
            raise WorkerException(f"Error processing FOD ID '{data.target_id}' ('{data.target_instance}'), stage {item.doc.stage}") from ex
