''' --[auto-generated, do not modify this block]--
*
* Copyright (c) 2026 Saltworks Security, LLC
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

import datetime
import logging
import uuid
from collections.abc import Iterable

from .Application import Application

# Requirements/use cases:
# 1. Search for and lock a batch of queue items
# 2. Queue up new items
# 3. Status/stage updates for (my) locked items)
# 4. Complete - status = complete or error
# 5. QueueClient accepts an "index pattern tag" which is used to build the index names used for the queue, ex: "sync" would use indexes like "sm_queue_sync_20240601".  This allows for multiple different queues to be accessed using the same class.


SM_QUEUE = "sm_queue"
INVALID_QUEUE_CLIENT_DTO = "Invalid argument, expected QueueClientDto."
MAX_HITS = 10000
PRIORITY_ENABLED = False

class QueueClientStatus():
    NEW = "New"
    IN_PROGRESS = "In Progress"
    COMPLETE = "Complete"
    ERROR = "Error"
    
    @staticmethod
    def is_valid(status:str):
        return status in [QueueClientStatus.NEW, QueueClientStatus.IN_PROGRESS, QueueClientStatus.COMPLETE, QueueClientStatus.ERROR]


class QueueClientPriorityDoc(object):
    def __init__(self, dto=None):
        self._priority = None
        self._created = None
        self._key = None
        if dto:
            self.map(dto)

    @staticmethod
    def new(priority, created):
        return QueueClientPriorityDoc({
            "priority": priority,
            "created": created
        })

    def map(self, dto):
        self.key = dto.get("key")
        self.priority = dto.get("priority")
        self.created = dto.get("created")

    def dto(self):
        return {
            "key": self.key,
            "priority": self.priority,
            "created": self.created
        }

    @property
    def key(self):
        return self._key

    @key.setter
    def key(self, value):
        self._key = value

    @property
    def priority(self):
        return self.__priority

    @priority.setter
    def priority(self, value):
        self.__priority = value

    @property
    def created(self):
        return self.__created

    @created.setter
    def created(self, value):
        self.__created = value


class QueueClientDoc(object):
    '''
    Queue client document, represents an item in the queue with all relevant information and metadata.
    '''
    def __init__(self, dto:dict=None):
        self._source = None
        self._key = None
        self._status = None
        self._stage = None
        self._status_reason = None
        self._data = None
        self._created = None
        self._completed = None
        self._locked = None
        self._lock_id = None
        self._priority = None
        self._change_reason = None
        self._change_trigger = None
        if dto:
            self.map(dto)

    @staticmethod
    def new(source, key, **kwargs):
        '''
        Create new QueueClientDoc

        :source: string (keyword) indicating the source of the queue entry, ex: "SSC Webhook", "Queue Load", etc.
        :key: string (keyword) indicating the unique key for the queue entry
        :status: optional status value, defaults to QueueClientStatus.NEW
        :stage: optional stage value to help show progress for multi-step processing.
        :status_reason: optional string to provide additional context for the status, such as error details if status is set to QueueClientStatus.ERROR
        :data: optional dict to hold any relevant data for the queue entry
        :created: optional datetime for when the queue entry was created, defaults to now if not provided
        :completed: optional datetime for when the queue entry was completed, should be set when status is set to QueueClientStatus.COMPLETE or QueueClientStatus.ERROR
        :locked: optional datetime for when the queue entry was locked for processing, should be set when lock_id is set
        :lock_id: optional string (keyword) to indicate the ID of the lock on the queue entry, should be set when locked is set
        :priority: optional integer priority for the queue entry, defaults to 5 if not provided.  Lower numbers indicate higher priority.
        :change_reason: optional text indicating the reason for queueing the changes.
        :change_trigger: optional string (keyword) indicating what triggered the change.
        '''
        status = kwargs.get("status", QueueClientStatus.NEW)
        if status is not None and not QueueClientStatus.is_valid(status):
            raise QueueClientException(f"Invalid status value '{status}' provided.")
        created = kwargs.get("created", datetime.datetime.now(datetime.UTC).isoformat())
        
        return QueueClientDoc({
            "source": source,
            "key": key,
            "status": status,
            "stage": kwargs.get("stage"),
            "status_reason": kwargs.get("status_reason"),
            "data": kwargs.get("data"),
            "created": created,
            "completed": kwargs.get("completed"),
            "locked": kwargs.get("locked"),
            "lock_id": kwargs.get("lock_id"),
            "priority": kwargs.get("priority"),
            "change_reason": kwargs.get("change_reason"),
            "change_trigger": kwargs.get("change_trigger")
        })

    def map(self, dto):
        if not isinstance(dto, dict):
            return
        self.source = dto.get("source")
        self.key = dto.get("key")
        self.status = dto.get("status")
        self.stage = dto.get("stage")
        self.status_reason = dto.get("status_reason")
        self.data = dto.get("data")
        self.created = dto.get("created")
        self.completed = dto.get("completed")
        self.locked = dto.get("locked")
        self.lock_id = dto.get("lock_id")
        self.priority = dto.get("priority")
        self.change_reason = dto.get("change_reason")
        self.change_trigger = dto.get("change_trigger")

    def dto(self) -> dict:
        return {
            "source": self.source,
            "key": self.key,
            "status": self.status,
            "stage": self.stage,
            "status_reason": self.status_reason,
            "data": self.data,
            "created": self.created,
            "completed": self.completed,
            "locked": self.locked,
            "lock_id": self.lock_id,
            "priority": self.priority,
            "change_reason": self.change_reason,
            "change_trigger": self.change_trigger
        }

    @property
    def source(self) -> str:
        return self._source

    @source.setter
    def source(self, value: str):
        self._source = value

    @property
    def key(self) -> str:
        return self._key

    @key.setter
    def key(self, value: str):
        self._key = value

    @property
    def status(self) -> str:
        return self._status

    @status.setter
    def status(self, value: str):
        self._status = value

    @property
    def stage(self) -> str:
        return self._stage

    @stage.setter
    def stage(self, value: str):
        self._stage = value

    @property
    def status_reason(self) -> str:
        return self._status_reason

    @status_reason.setter
    def status_reason(self, value: str):
        self._status_reason = value

    @property
    def data(self) -> dict:
        return self._data

    @data.setter
    def data(self, value: dict):
        self._data = value

    @property
    def created(self) -> datetime.datetime:
        return self._created
    
    @created.setter
    def created(self, value: datetime.datetime):
        self._created = value

    @property
    def completed(self) -> datetime.datetime:
        return self._completed

    @completed.setter
    def completed(self, value: datetime.datetime):
        self._completed = value

    @property
    def locked(self) -> datetime.datetime:
        return self._locked

    @locked.setter
    def locked(self, value: datetime.datetime):
        self._locked = value

    @property
    def lock_id(self) -> str:
        return self._lock_id

    @lock_id.setter
    def lock_id(self, value: str):
        self._lock_id = value

    @property
    def priority(self) -> int:
        return self._priority

    @priority.setter
    def priority(self, value: int):
        self._priority = value

    @property
    def change_reason(self) -> str:
        return self._change_reason

    @change_reason.setter
    def change_reason(self, value: str):
        self._change_reason = value

    @property
    def change_trigger(self) -> str:
        return self._change_trigger

    @change_trigger.setter
    def change_trigger(self, value: str):
        self._change_trigger = value


class QueueClientDto(object):
    def __init__(self, dto=None):
        self._doc = None
        self._seq = None
        self._pri = None
        self._id = None
        self._index = None
        if dto:
            self.doc = QueueClientDoc(dto.get('_source'))
            self.sequence_number = dto.get('_seq_no')
            self.primary_term = dto.get('_primary_term')
            self.id = dto.get('_id')
            self.index = dto.get('_index')

    def Dto(self):
        return {
            '_source': self.doc.Dto(),
            '_seq_no': self.sequence_number,
            '_primary_term': self.primary_term,
            '_index': self.index,
        }

    def UpdateLockingInfo(self, response):
        if response and '_seq_no' in response.keys() and '_primary_term' in response.keys():
            self.sequence_number = response['_seq_no']
            self.primary_term = response['_primary_term']
            logging.debug("Locking information updated")
            return True
        else:
            logging.debug("Locking information not found in response")
            return False

    @property
    def index(self) -> str:
        return self._index
    
    @index.setter
    def index(self, value: str):
        self._index = value
    
    @property
    def id(self) -> str:
        return self._id

    @id.setter
    def id(self, value: str):
        self._id = value

    @property
    def doc(self) -> QueueClientDoc:
        return self._doc

    @doc.setter
    def doc(self, value:QueueClientDoc):
        self._doc = value

    @property
    def sequence_number(self):
        return self._seq

    @sequence_number.setter
    def sequence_number(self, value):
        self._seq = value

    @property
    def primary_term(self):
        return self._pri

    @primary_term.setter
    def primary_term(self, value):
        self._pri = value


class QueueClientException(Exception):
    pass


class QueueClient(object):
    '''
    Client for managing a queue of items to be processed, with support for locking and prioritization.
    '''
    def __init__(self, app:Application, idx_pattern_tag:str, **kwargs):
        '''
        Setup the class

        Params
        :app: Application
        :idx_pattern_tag: Index pattern tag to be used with this instance of client.  Ex: "sm_queue_sync", or "sync" both will become "sm_queue_sync*"
        :batch_size: optional batch size for processing search results, defaults to 500
        '''

        logging.debug("QueueClient init starting.")
        self.app = app 

        if not idx_pattern_tag or len(idx_pattern_tag) == 0:
            raise QueueClientException("Index pattern must be provided for QueueClient.")
        if idx_pattern_tag.startswith(SM_QUEUE):
            idx_pattern_tag = idx_pattern_tag.replace(SM_QUEUE, "")
        idx_pattern_tag = idx_pattern_tag.replace("*", "").strip("_")
        self._index_pattern_tag = idx_pattern_tag

        self._batch_size = app.Settings.Get("Main", "DefaultQueueBatchSize", 500)
        if self._batch_size <= 0 or self._batch_size > MAX_HITS:
            raise QueueClientException(f"DefaultQueueBatchSize setting must be a positive integer no greater than {MAX_HITS}.")

        self.__priority_index = f'{SM_QUEUE}_{idx_pattern_tag}_priority'
        self._es = app.GetElasticClient()
        self._load_exclusions = []
        self._priority_reservations = {}
        self._session_id = uuid.uuid4()
        self._default_priority = 5
        self._process_args(kwargs)

        logging.debug("QueueClient init complete.")


    def _process_args(self, args:dict):
        '''
        Process additional arguments passed to the constructor, such as batch size. (Expect more of these...)
        '''
        if not isinstance(args, dict):
            raise QueueClientException("Args must be a dict.")
        try:
            known_args = ["batch_size"]
            for key in args.keys():
                if key not in known_args:
                    logging.warning(f"Unknown argument '{key}' provided.")
                    continue
                if key == "batch_size":
                    self.batch_size = int(args[key])
        except Exception as e:
            logging.warning(f"Error processing arguments: {e}")

    #region Properties

    @property
    def batch_size(self):
        return self._batch_size

    @batch_size.setter
    def batch_size(self, value):
        if not isinstance(value, int) or value <= 0:
            raise QueueClientException("Batch size must be a positive integer.")
        if value > MAX_HITS:
            logging.warning(f"Batch size of {value} exceeds maximum of {MAX_HITS}, setting to {MAX_HITS}.")
            value = MAX_HITS
        self._batch_size = value

    @property
    def session_id(self):
        return self._session_id

    @property
    def index(self):
        return f'{SM_QUEUE}_{self._index_pattern_tag}_{datetime.datetime.now(datetime.UTC).strftime("%Y%m%d")}'
    
    @property
    def index_pattern(self):
        return f'{SM_QUEUE}_{self._index_pattern_tag}_*'
    
    @property
    def priority_index(self):
        return self.__priority_index
    
    #endregion


    def get_search_body(self, exclude_completed:bool=False, exclude_locked:bool=False) -> dict:
        '''
        Returns body suitable for searching the queue, with optional exclusions for completed and/or locked items.
        Example:
        {
          "query": {
            "bool": {
              "must": [],
              "must_not": [],
              "should": []
            }
          },
          "sort": [ "priority", "created" ]
        }

        :exclude_completed: exclude/include any items where 'completed' field exists
        :exclude_locked: exclude/include any items where 'lock_id' field exists
        '''
        body = {
          "query": {
            "bool": {
              "must": [],
              "must_not": [],
              "should": []
            }
          },
          "sort": [ "priority", "created" ]
        }
        if exclude_completed:
            body['query']['bool']['must_not'].append({ "exists": { "field": "completed" } })
        if exclude_locked:
            body['query']['bool']['must_not'].append({ "exists": { "field": "lock_id" } })
        return body
    

    def get_search(self, size:int=None, custom_body:dict=None, exclude_completed:bool=False, exclude_locked:bool=False) -> Iterable[QueueClientDto]:
        '''
        Searches the queue with optional filters for completed and/or locked items, and returns an iterable of QueueClientDto objects.

        :size: number of items to return, defaults to batch_size property
        :custom_body: optional custom query body to override the default (use get_search_body() to get the default query body).
        :exclude_completed: exclude/include any items where 'completed' field exists
        :exclude_locked: exclude/include any items where 'lock_id' field exists
        '''
        if size is None:
            size = self.batch_size
        if size <= 0 or size > MAX_HITS:
            raise QueueClientException(f"Size must be a positive integer no greater than {MAX_HITS}.")
        if custom_body and not isinstance(custom_body, dict):
            raise QueueClientException("Custom body must be a dict.")
        if custom_body and "sort" not in custom_body.keys():
            raise QueueClientException("Custom body missing 'sort' section, use get_search_body() to get the default query body.")

        body = custom_body if custom_body else self.get_search_body(exclude_completed, exclude_locked)
        scroller = self._es.SearchScroll(self.index_pattern, body, size, scrollTimeout=None)
        if not scroller or not scroller.Results:
            return None
        while scroller.Results:
            for item in scroller.Results:
                dto = QueueClientDto(item)
                yield dto
            scroller.GetNext()


    def get_next_queue_batch_body(self):
        '''
        Returns query body for searching for the next batch of queue items to process, with appropriate sorting and exclusions.
        '''
        body = {
          "aggs": {
            "total_count": { "value_count": { "field": "key" } }
          },
          "query": {
            "bool": {
              "must": [],
              "must_not": [
                { "exists": { "field": "completed" } },
                { "exists": { "field": "lock_id" } }
              ],
              "should": []
            }
          },
          "sort": [ "priority", "created" ]
        }
        return body
    

    def get_next_queue_batch(self, stage:str=None, size:int=None, custom_body:dict=None) -> tuple[list[QueueClientDto], int]:
        '''
        Returns a tuple containing a single batch of available queue docs and a count of the total hits.
        We assume these will be completed using complete_queue_batch() before getting the next batch.

        :stage: optional stage value to set when locking the queue items which can help show progress for multi-step processing.
        :size: batch size, must be a positive integer, recommend no more than double worker count. Defaults to batch_size property if not set.
        :custom_body: optional custom query body to override the default (use get_next_queue_batch_body() to get the default query body).

        Note: It's possible to get fewer items than requested if some fail to lock.  Only one search is performed per request.
        '''
        if size is None:
            size = self.batch_size
        if size <= 0 or size > MAX_HITS:
            raise QueueClientException(f"Size must be a positive integer no greater than {MAX_HITS}.")
        if custom_body and not isinstance(custom_body, dict):
            raise QueueClientException("Custom body must be a dict.")
        if custom_body and "aggs" not in custom_body:
            raise QueueClientException("Custom body missing 'aggs' section, use get_next_queue_batch_body() to get the default query body.")
        body = self.get_next_queue_batch_body() if not custom_body else custom_body

        logging.debug("Getting next async queue batch (qty %s)", size)
        # priority system not yet complete in new implementation
        # self._load_priority_reservations(self.__priority_index)
        r = self._es.Search(self.index_pattern, body, size, False, True)
        if not r or "aggregations" not in r.keys():
            return None, None
        ret = []
        for item in r['hits']['hits']:
            dto = QueueClientDto(item)
            dto = self.set_start(dto, stage)
            if dto:
                ret.append(dto)
        return ret, r['aggregations']['total_count']['value']


    def set_start(self, qdto:QueueClientDto, stage:str=None, data:dict=None) -> QueueClientDto|None:
        if not isinstance(qdto, QueueClientDto):
            raise ValueError(INVALID_QUEUE_CLIENT_DTO)
        doc = qdto.doc
        if (doc.lock_id and doc.lock_id != self.session_id) or doc.completed:
            logging.info("Queue doc for key %s not eligible for lock (lock id: '%s', locked: '%s').", doc.key, doc.lock_id, doc.locked)
            return None
        doc.locked = datetime.datetime.now(datetime.UTC).isoformat()
        doc.lock_id = self.session_id
        doc.status = QueueClientStatus.IN_PROGRESS
        if stage is not None:
            doc.stage = stage
        if data is not None:
            doc.data = data
        rsp = self._es.UpdateWithLocking(qdto.index, doc.dto(), qdto.id, qdto.sequence_number, qdto.primary_term)
        if rsp.get('result') == "updated":
            qdto.UpdateLockingInfo(rsp)
            return qdto
        else:
            return None

    def set_progress(self, qdto:QueueClientDto, stage:str=None, data:dict=None) -> QueueClientDto|None:
        if not isinstance(qdto, QueueClientDto):
            raise ValueError(INVALID_QUEUE_CLIENT_DTO)
        doc = qdto.doc
        if not doc.lock_id or doc.lock_id != self.session_id or doc.completed:
            logging.info("Queue doc for key %s not eligible for progress update with lock id '%s'.", doc.key, doc.lock_id)
            return None
        if stage is not None:
            doc.stage = stage
        if data is not None:
            doc.data = data
        rsp = self._es.UpdateWithLocking(qdto.index, doc.dto(), qdto.id, qdto.sequence_number, qdto.primary_term)
        if rsp.get('result') == "updated":
            qdto.UpdateLockingInfo(rsp)
            return qdto
        else:
            logging.warning("Failed to update progress for queue doc with key %s.", doc.key)
            return None

    def set_complete(self, qdto:QueueClientDto, stage:str=None, data:dict=None, is_error:bool=False, reason:str=None) -> QueueClientDto|None:
        if not isinstance(qdto, QueueClientDto):
            raise ValueError(INVALID_QUEUE_CLIENT_DTO)
        doc = qdto.doc
        if not doc.lock_id or doc.lock_id != self.session_id or doc.completed:
            logging.info("Queue doc for key %s not eligible for completion with lock id '%s'.", doc.key, doc.lock_id)
            return None
        doc.lock_id = None
        if stage is not None:
            doc.stage = stage
        if data is not None:
            doc.data = data
        doc.status = QueueClientStatus.ERROR if is_error else QueueClientStatus.COMPLETE
        if reason is not None:
            doc.status_reason = reason
        doc.completed = datetime.datetime.now(datetime.UTC).isoformat()
        rsp = self._es.UpdateWithLocking(qdto.index, doc.dto(), qdto.id, qdto.sequence_number, qdto.primary_term)
        if rsp.get('result') == "updated":
            qdto.UpdateLockingInfo(rsp)
            return qdto
        else:
            logging.warning("Failed to complete queue doc with key %s.", doc.key)
            return None


    def clear_session(self):
        body = {
            "query": { "term": { "lock_id": { "value": self.session_id } } },
            "script": {
                "source": "ctx._source.lock_id = null",
                "lang": "painless"
            }
        }
        logging.debug("Clearing locks for current session...")
        try:
            self._es.UpdateByQuery(self.index_pattern, body, ignoreConflicts=True)
            logging.debug("Session locks cleared.")
        except Exception as e:
            logging.exception(f"Clear session unexpected error: {e}")


    def _search_existing(self, keys:list[str]) -> list[tuple[str, str, str, int]]:
        '''
        Search the queue for existing items matching the provided keys, returning a list of found indexes, ids, keys, and their priority.
        '''
        body = {
          "query": {
            "bool": {
              "must": [
                { "terms": { "key": keys } },
                { "terms": { "status": [QueueClientStatus.IN_PROGRESS, QueueClientStatus.NEW] } },
              ]
            }
          },
          "_source": [ "key", "priority" ]
        }
        rsp = self._es.Search(self.index_pattern, body, 10000, True)
        found = []
        if not rsp:
            return found
        for item in rsp:
            found.append((item['_index'], item['_id'], item['_source'].get('key'), item['_source'].get('priority', self._default_priority)))
        return found


    def insert_queue(self, source:str, data:dict, stage:str=None, **kwargs):
        '''
        Insert provided key list into the queue, optionally setting stage, custom data, and other parameters.
    
        :source: string indicating the source of the queue entries.
        :data: required dictionary containing queue item key and additional data for the queue entries ({"key": {data} }}).
        :stage: optional string (keyword) indicating the stage of the queue entries.
        :priority: optional integer priority for the queue entries, defaults to 5 if not provided.  Lower numbers indicate higher priority.
        :change_reason: optional text indicating the reason for queueing the changes.
        :change_trigger: optional string (keyword) indicating the trigger for queueing the changes.

        If a key is already present in the queue:
          (1) If in progress, no update will occur for the key.
          (2) If not in progress but present, priority (only) will be updated if different.

        If an ID is already present in the helper index that priority will be updated.
        No internal batching, don't pass a huge key list all at once.
        All queue items will have the same metadata (stage, priority, etc).
        '''
        # validation/setup
        for arg in kwargs.keys():
            if arg not in ["priority", "change_reason", "change_trigger"]:
                raise QueueClientException(f"Unknown argument '{arg}' provided.")
        priority = int(kwargs.get("priority", self._default_priority))
        change_reason = kwargs.get("change_reason")
        change_trigger = kwargs.get("change_trigger")
        if PRIORITY_ENABLED:
            self._load_priority_reservations()
        if not isinstance(data, dict):
            raise QueueClientException("Data must be a dict containing a key/data pairs for each new queue item.")
        dt = datetime.datetime.now(datetime.UTC).isoformat()
        wrk = {}
        bdocs = []
        
        # Convert input into QueueClientDocs
        for key, data in data.items():
            key = str(key)
            curPriority = priority if priority else self._default_priority
            if PRIORITY_ENABLED and key in self._priority_reservations.keys():
                curPriority = self._priority_reservations[key] if not priority else curPriority
            doc = QueueClientDoc.new(source, key, data=data, stage=stage, created=dt, priority=curPriority, 
                                     change_reason=change_reason, change_trigger=change_trigger)
            wrk[key] = doc
        if len(wrk) == 0:
            logging.warning("No queue items provided to insert.")
            return
        
        # Handle existing queue items ("Gatekeeper" logic)
        found_list = self._search_existing(list(wrk.keys()))
        for idx, id, key, pri in found_list:
            if key not in wrk:
                logging.error(f"[insert_queue] Existing queue item with key '{key}' found but not in current insert list, skipping update.")
                continue
            if pri != priority:
                logging.info(f"Updating priority for existing queue item with key '{key}' from {pri} to {priority}.")
                bdocs.append(self._es.BulkInsertDocument(idx, {"priority": priority}, id, "update"))
            else:
                logging.info(f"Queue item with key '{key}' already exists.")
            wrk.pop(key)

        # Insert new items, process bulk updates
        for doc in wrk.values():
            bdocs.append(self._es.BulkInsertDocument(self.index, doc.dto(), None, "index"))
        rsp = self._es.BulkInsert(bdocs, raiseErrors=False)
        if rsp[1] and rsp[1] > 0:
            logging.error("Bulk insert had %s errors, check response for details.", rsp[1])
        logging.info("Bulk inserted %s queue entries, %s succeeded, %s failed", len(wrk), rsp[0], rsp[1])

    #region Priority system (not fully implemented yet)

    def clear_priority_reservations(self, priority_index:str ):
        '''
        Clears all priority reservations
        '''
        if not PRIORITY_ENABLED:
            logging.warning("Priority system not yet complete, clear_priority_reservations() has no effect.")
            return
        body = {
          "query": {
              "exists": { "field": "id" }
          }
        }
        self._es.DeleteByQuery(priority_index, body, ignoreMissingIndex=True)
        logging.info("Cleared async priority reservations for target type '%s' and instance '%s'.", self._target_type, self._target_instance)

    def _load_priority_reservations(self, lazy=True):
        if not PRIORITY_ENABLED:
            logging.warning("Priority system not yet complete, _load_priority_reservations() has no effect.")
            return
        if lazy and len(self._priority_reservations) > 0:
            return
        body = {
            "query": {
                "bool": {
                    "must": []
                }
            },
            "sort": []
        }
        scroller = self._es.SearchScroll(self.__priority_index, body, scrollSize=500, scrollTimeout=None)
        while scroller and len(scroller.Results):
            for dto in scroller.Results:
                itm = dto['_source']
                self._priority_reservations[itm['target_id']] = itm['priority']
            scroller.GetNext()

    #endregion
