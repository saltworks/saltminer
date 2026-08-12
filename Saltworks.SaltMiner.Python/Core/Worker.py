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

# Worker class - used to run processing in multi-threaded environment.

import logging
import threading
from abc import ABC, abstractmethod

from .ElasticClient import ElasticClient
from .Agent import Agent
from .QueueClient import QueueClientDto

class WorkerException(Exception):
    '''Custom exception for worker errors.'''
    pass


class WorkerFactory(ABC):
    '''Factory class for creating Worker instances.'''
    @abstractmethod
    def create_worker(self, id:int, agent:Agent, **kwargs) -> 'Worker':
        '''Create and return a new Worker instance.'''
        raise NotImplementedError("Subclasses must implement create_worker() method")


class Worker(ABC):
    '''Worker class for multi-threaded processing of sync/refresh.'''
    def __init__(self, id:int, agent:Agent, **kwargs):
        if not isinstance(id, int) or id < 0:
            raise WorkerException("Invalid worker ID provided.")
        if not isinstance(agent, Agent) or agent is None:
            raise WorkerException("Invalid or null agent provided to worker.")
        self._es = None
        self._logger = None
        self._agent = agent
        self._id = id
        self._err_count = 0
        self._err_threshold = kwargs.get("err_threshold", 5)
        # Set by the agent when it abandons this worker (e.g. defunct/timed out).  A truly hung
        # worker can't observe it mid-item, but if it ever unblocks it stops rather than taking more work.
        self._abandon = threading.Event()

    @property
    def es(self) -> ElasticClient:
        if self._es is None:
            self._es = self._agent.app.GetElasticClient()
        return self._es
    
    @property
    def logger(self) -> logging.Logger:
        if self._logger is None:
            self._logger = self._agent.app.logging_provider.get_thread_logger(self.id)
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
    
    @property
    def error_count(self) -> int:
        return self._err_count
    @error_count.setter
    def error_count(self, value:int):
        self._err_count = value

    @property
    def error_threshold(self) -> int:
        return self._err_threshold
    @error_threshold.setter
    def error_threshold(self, value:int):
        self._err_threshold = value

    @property
    def abandoned(self) -> bool:
        '''True once the agent has abandoned this worker (defunct/timed out).'''
        return self._abandon.is_set()

    def abandon(self):
        '''Signal this worker to stop processing; called by the agent when abandoning it.'''
        self._abandon.set()

    def run(self):
        '''Processes items from the agent queue until a sentinel (None) is received.'''
        self.logger.info("Worker %d started", self.id)
        self.agent._record_beat(self.id, item=None)  # register as alive and idle
        while True:
            if self._abandon.is_set():
                break
            item = self.agent.queue.get()
            if item is None:
                self.agent.queue.task_done()
                break
            if self._abandon.is_set():
                # abandoned while blocked on the queue - release this item and stop
                self.agent.queue.task_done()
                break
            self.agent._record_beat(self.id, item=item)  # now busy on this item
            try:
                if not isinstance(item, QueueClientDto):
                    raise WorkerException(f"Invalid queue item type: expected QueueClientDto, got {type(item)}")
                self.logger.info("Worker %d processing item with ID: %s", self.id, item.id)
                self._process(item)
                self.error_count = 0
            except Exception:
                # getattr: item may not be a QueueClientDto (that's one of the exceptions handled here)
                self.logger.exception("Worker %d failed processing item with ID: %s", self.id, getattr(item, 'id', '[unknown]'))
                self.error_count += 1
                if self.error_count >= self.error_threshold:
                    self.logger.error("Worker %d has reached error threshold with %d consecutive errors, stopping worker", self.id, self.error_count)
                    break
            finally:
                self.agent.queue.task_done()
                if not self._abandon.is_set():
                    self.agent._record_beat(self.id, item=None)  # back to idle
        logging.getLogger(__name__).info("Worker %d stopped", self.id)

    @abstractmethod
    def _process(self, item:QueueClientDto):
        '''
        Process a single queue item. Override or extend as needed.
        self.logger is available for logging, self.es for ElasticClient access, and self.agent for reference to the agent.
        '''
        raise NotImplementedError("Subclasses must implement _process() method")

    

