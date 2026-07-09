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

from Core.Agent import Agent
from Core.Worker import WorkerFactory, Worker, WorkerException
from Core.QueueClient import QueueClientDto
from .SSC.SyncExtractor import SyncExtractor as SscSync
from .SSC.AppVulsProcessor import AppVulsProcessor as SscRefresh
from .FOD.SyncExtractor import SyncExtractor as FodSync
from .FOD.AppVulsProcessor import AppVulsProcessor as FodRefresh


class SyncQueueType():
    SSC = "SSC"
    FOD = "FOD"
    @staticmethod
    def is_valid(value:str) -> bool:
        return value in [SyncQueueType.SSC, SyncQueueType.FOD]


class SyncQueueStage():
    REFRESH = "Refresh"
    SYNC = "Sync"
    MANAGER = "Manager"


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


class SyncQueueDto(QueueClientDto):
    """Data transfer object for sync queue items."""
    def __init__(self, dto:dict=None):
        """
        Initialize SyncQueueDto with required and optional parameters.

        :dto: required dictionary containing the data for the sync queue item, including the base QueueClientDto fields and the SyncQueueData fields
        """
        super().__init__(dto)
        self._sync_data = None if not dto else SyncQueueData(self.doc.data)


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

    def _get_ssc_sync(self, src_name:str) -> SscSync:
        if self._ssc_sync is None or self._ssc_sync.SourceName != src_name:
            if self._ssc_sync is not None:
                self._ssc_sync.Cleanup()
            self._ssc_sync = SscSync(self.agent.app.Settings, src_name, logger=self.logger)
        return self._ssc_sync


    def _get_ssc_refresh(self, src_name:str) -> SscRefresh:
        if self._ssc_refresh is None or self._ssc_refresh.SourceName != src_name:
            self._ssc_refresh = SscRefresh(self.agent.app.Settings, src_name, logger=self.logger)
        return self._ssc_refresh


    def _get_fod_sync(self, src_name:str) -> FodSync:
        if self._fod_sync is None or self._fod_sync.SourceName != src_name:
            self._fod_sync = FodSync(self.agent.app.Settings, src_name, logger=self.logger)
        return self._fod_sync


    def _get_fod_refresh(self, src_name:str) -> FodRefresh:
        if self._fod_refresh is None or self._fod_refresh.SourceName != src_name:
            self._fod_refresh = FodRefresh(self.agent.app.Settings, src_name, logger=self.logger)
        return self._fod_refresh


    def _process(self, item:QueueClientDto):
        """Process a single queue item - exceptions handled by Worker.run()"""
        sync_item = SyncQueueDto(item.dto())
        if sync_item._sync_data.target_type == SyncQueueType.SSC:
            self._process_ssc(sync_item)
        elif sync_item._sync_data.target_type == SyncQueueType.FOD:
            self._process_fod(sync_item)
        else:
            self.error_count += 1
            raise WorkerException(f"Invalid sync queue item target type: {sync_item._sync_data.target_type}")


    def _process_ssc(self, item:SyncQueueDto):
        try:
            sync = self._get_ssc_sync(item._sync_data.target_instance)
            sync.ProcessOne(item._sync_data.target_id, item._sync_data.force)
            self.agent.update(item, SyncQueueStage.REFRESH, item._sync_data.to_dto())
            refresh = self._get_ssc_refresh(item._sync_data.target_instance)
            refresh.PopulateVulsOne(item._sync_data.target_id)
            self.agent.complete(item, is_error=False)
        except Exception as ex:
            self.logger.error("Error processing SSC ID '%s' ('%s'), stage %s: %s", item._sync_data.target_id, item._sync_data.target_instance, item.doc.stage, str(ex))
            try:
                self.agent.complete(item, is_error=True, reason=str(ex))
            except Exception as ex2:
                self.logger.error("Error setting error for SSC ID '%s' ('%s'), stage %s: %s", item._sync_data.target_id, item._sync_data.target_instance, item.doc.stage, str(ex2))
            raise WorkerException(f"Error processing SSC ID '{item._sync_data.target_id}' ('{item._sync_data.target_instance}'), stage {item.doc.stage}") from ex


    def _process_fod(self, item:SyncQueueDto):
        try:
            sync = self._get_fod_sync(item._sync_data.target_instance)
            sync.ProcessOne(item._sync_data.target_id, item._sync_data.force)
            self.agent.update(item, SyncQueueStage.REFRESH, item._sync_data.to_dto())
            refresh = self._get_fod_refresh(item._sync_data.target_instance)
            refresh.PopulateVulsOne(item._sync_data.target_id)
            self.agent.complete(item, is_error=False)
        except Exception as ex:
            self.logger.error("Error processing FOD ID '%s' ('%s'), stage %s: %s", item._sync_data.target_id, item._sync_data.target_instance, item.doc.stage, str(ex))
            try:
                self.agent.complete(item, is_error=True, reason=str(ex))
            except Exception as ex2:
                self.logger.error("Error setting error for FOD ID '%s' ('%s'), stage %s: %s", item._sync_data.target_id, item._sync_data.target_instance, item.doc.stage, str(ex2))
            raise WorkerException(f"Error processing FOD ID '{item._sync_data.target_id}' ('{item._sync_data.target_instance}'), stage {item.doc.stage}") from ex
