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

'''
SyncWorker class - used to run sync processing in multi-threaded environment.
'''
import logging

from Core.Application import Application
from Core.ElasticClient import ElasticClient
from .SSC import SyncExtractor as SscSync, AppVulsProcessor as SscRefresh
from .FOD import SyncExtractor as FodSync, AppVulsProcessor as FodRefresh
from Core.SscClient import SscClient
from Core.FodClient import FodClient
from Core.Agent import Agent, AgentQueueItem
from Core.Worker import Worker

class SyncWorker(Worker):
    """Worker class for multi-threaded processing of sync/refresh."""
    def __init__(self):
        super().__init__()
        self._ssc_sync = None
        self._ssc_refresh = None
        self._fod_sync = None
        self._fod_refresh = None
        self._ssc = None
        self._fod = None
    
    def _get_ssc(self, src_name:str) -> SscClient:
        if self._ssc is None or self._ssc.SourceName != src_name:
            if self._ssc is not None:
                self._ssc.Cleanup()
            self._ssc = self.agent.app.GetSscClient(src_name)
        return self._ssc

    def _process(self, item:AgentQueueItem):
        
        pass

    def _process_ssc(self, item:AgentQueueItem):
        pass

    def _process_fod(self, item:AgentQueueItem):
        pass

