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
Worker class - used to run processing in multi-threaded environment.
'''
import logging
from abc import ABC, abstractmethod

from Core.ElasticClient import ElasticClient
from .Agent import Agent, AgentQueueItem

class WorkerException(Exception):
    '''Custom exception for worker errors.'''
    pass

class Worker(ABC):
    '''Worker class for multi-threaded processing of sync/refresh.'''
    def __init__(self):
        self._es = None
        self._logger = None
        self._agent = None
        self._id = None

    @property
    def es(self) -> ElasticClient:
        if self._es is None:
            self._es = self._agent.app.GetElasticClient()
        return self._es
    
    @property
    def logger(self) -> logging.Logger:
        if self._logger is None:
            self._logger = logging.getLogger(f"{__name__}-{self.id}")
        return self._logger
    
    @property
    def agent(self) -> Agent:
        if self._agent is None:
            raise WorkerException("Agent is not set")
        return self._agent
    
    @property
    def id(self) -> int:
        if self._id is None:
            raise WorkerException("Worker ID is not set")
        return self._id
    
    def initialize(self, id:int, agent:Agent):
        '''Initialize the worker with an ID and reference to the agent. Must be called before run().'''
        self._id = id
        self._agent = agent

    def run(self):
        '''Processes items from the agent queue until a sentinel (None) is received.'''
        self.logger.info("Worker %d started", self.id)
        while True:
            item = self.agent.queue.get()
            if item is None:
                self.agent.queue.task_done()
                break
            try:
                if not isinstance(item, AgentQueueItem):
                    raise WorkerException(f"Invalid queue item type: expected AgentQueueItem, got {type(item)}")
                self._process(item)
            except Exception:
                self.logger.exception("Worker %d failed processing item", self.id)
            finally:
                self.agent.queue.task_done()
        logging.getLogger(__name__).info("Worker %d stopped", self.id)

    @abstractmethod
    def _process(self, item:dict):
        '''
        Process a single queue item. Override or extend as needed.
        self._logger is available for logging, self._es for ElasticClient access, and self._agent for reference to the agent.
        '''
        raise NotImplementedError("Subclasses must implement _process() method")

    

