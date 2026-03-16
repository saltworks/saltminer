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
from operator import index
import uuid
import json

from Utility.GeneralUtility import GeneralUtility
from .Application import Application

# Use cases:
# 1. Search for and lock a "bite" for a particular index pattern
# def Search(body:dict, size:int, lock_id:str) -> list
# body can include custom query as well as custom sort, but better not mess with the schema
# 2. Queue up new stuff
# 3. Status updates (for my locked item, provide a status/stage/reason update)
# 4. Complete - status = complete or error, also accept stage / reason / error info


class QueueClient(object):
    def __init__(self, app:Application, idx_pattern:str):
        '''
        Setup the class

        Params
        :app: Application
        :idx_pattern: Index pattern to be used with this instance of client.  Ex: "sm_queue_sync", or "sync" both will become "sm_queue_sync*"
        '''

        self.app = app 
        logging.debug("QueueClient init")

        self.__target_type = None
        self.__target_instance = None
        # self.__index = 'async_queue'
        # self.__priority_index = 'async_queue_priority'
        # self.__id_field = 'target_id'
        self.__es = app.GetElasticClient()
        # self.__batch_size = app.GetSource("AsyncQueue", "AsyncQueueBatchSize", 500)
        # self.__days_old = app.GetSource("AsyncQueue", "AsyncQueueRetentionDays", 1)
        # self.__lock_days_old = app.GetSource("AsyncQueue", "AsyncQueueLockRetentionDays", 1)
        # self.__es.MapIndex(self.__index, False)  # will map if doesn't exist
        # self.__es.MapIndex(self.__priority_index, False)  # will map if doesn't exist
        self.__load_exclusions = []
        self.__priority_reservations = {}
        self.__session_id = uuid.uuid4()
        self.__default_priority = 5
        logging.debug("QueueClient init complete.")

    @property
    def batch_size(self):
        return self.__batch_size

    @batch_size.setter
    def batch_size(self, value):
        self.__batch_size = value

    @property
    def session_id(self):
        return self.__session_id

    @property
    def index(self):
        return self.__index
    
    @index.setter
    def index(self, value):
        self.__index = value

    @property
    def priority_index(self):
        return self.__priority_index
    
    @priority_index.setter
    def priority_index(self, value):
        self.__priority_index = value

    @property
    def target_type(self):
        return self.__target_type

    @target_type.setter
    def target_type(self, value):
        self.__target_type = value

    @property
    def target_instance(self):
        return self.__target_instance

    @target_instance.setter
    def target_instance(self, value):
        self.__target_instance = value

    def clear_priority_reservations(self, priority_index:str ):
        '''
        Clears all priority reservations for current type and instance
        '''
        body = {
          "query": {
            "bool": {
              "must": [
                { "term": { "target_type": { "value": self.__target_type } } },
                { "term": { "target_instance": { "value": self.__target_instance } } }
              ]
            }
          }
        }
        self.__es.DeleteByQuery(priority_index, body, ignoreMissingIndex=True)
        logging.info("Cleared async priority reservations for target type '%s' and instance '%s'.", self.__target_type, self.__target_instance)

    def clear_async_queue(self, index:str, completed=True, locked=False):
        '''
        Clears all sync items from queue, optionally including those that are completed or locked
        '''
        body = {
          "query": {
            "bool": {
              "must": [
                { "term": { "target_type": { "value": self.__target_type } } },
                { "term": { "target_instance": { "value": self.__target_instance } } }
              ],
              "must_not": []
            }
          }
        }
        if completed == False:
            body['query']['bool']['must_not'].append({ "exists": { "field": "completed" } })
        if not locked == True:
            body['query']['bool']['must_not'].append({ "exists": { "field": "lock_id" } })
        self.__es.DeleteByQuery(index, body, ignoreMissingIndex=True)
        logging.debug("Cleared async queue for target type '%s' and instance '%s'.", self.__target_type, self.__target_instance)

    def get_async_queue_current(self, id_field:str):
        '''
        Returns all target IDs currently in the queue where completed not set, including any that are locked.
        This can be used as an exclusion list to avoid duplicates in the queue.
        '''
        if not self.__es.IndexExists(self.__index):
            raise QueueClientException("Unable to return sync queue items, index '%s' does not exist.", self.__index)
        body = {
          "query": {
            "bool": {
              "must": [
                { "term": { "target_type": { "value": self.__target_type } } },
                { "term": { "target_instance": { "value": self.__target_instance } } }
              ],
              "must_not": [
                { "exists": { "field": "completed" } }
              ]
            }
          },
          "_source": [id_field],
          "sort": [ { id_field: "asc" } ]
        }
        lst = []
        sc = self.__es.SearchScroll(self.__index, body, self.__batch_size, scrollTimeout=None)
        while sc and sc.Results:
            for dto in sc.Results:
                lst.append(dto['_source'][self.__IdField])
            sc.GetNext()
        logging.debug("%s incomplete target ID(s) in the queue for target type '%s'.", len(lst), self.__target_type)
        return lst

    def get_async_queue_batch(self, size, id_field:str):
        '''
        Returns a tuple containing a single batch of update queue docs (only 1 per ID), and a count of the total hits (if over 10k, then 10k will be returned).
        We assume these will be completed using CompleteSyncQueue() before getting the next batch.
        '''
        if not self.__es.IndexExists(self.__index):
            raise QueueClientException("Unable to return sync queue items, index '%s' does not exist.", self.__index)
        body = {
          "aggs": {
            "total_count": { "value_count": { "field": id_field } }
          },
          "query": {
            "bool": {
              "must": [
                { "term": { "target_type": { "value": self.__target_type } } },
                { "term": { "target_instance": { "value": self.__target_instance } } }
              ],
              "must_not": [
                { "exists": { "field": "completed" } }
              ],
              "should": [
                { "bool": { "must_not": [ { "exists": { "field": "lock_id" } } ] } },
                { "range": { "locked": { "lt": "now-1d/d" } } }
              ],
              "minimum_should_match": 1
            }
          },
          "sort": [ "priority", "created", "target_id" ]
        }
        logging.debug("Getting next async queue batch (qty %s)", size)
        self.__load_priority_reservations(self.__priority_index)
        r = self.__es.Search(self.__index, body, size, False, True)
        if not r or not "aggregations" in r.keys():
            return None, None
        ret = []
        for item in r['hits']['hits']:
            dto = QueueClientDto(item)
            ret.append(dto)
        return ret, r['aggregations']['total_count']['value']

    def __get_async_queue_dto(self, dto):
        sqdto = dto
        if isinstance(sqdto, dict):
            if '_source' in sqdto.keys():
                sqdto = QueueClientDto(dto)
        if not isinstance(sqdto, QueueClientDto):
            raise QueueClientException("Invalid value for dto, expected AsyncQueueDto, or a dict that can be turned into a AsyncQueueDto.")
        return sqdto

    def __load_priority_reservations(self, lazy=True):
        if lazy and len(self.__priority_reservations) > 0:
            return
        body = {
            "query": {
                "bool": {
                    "must": [
                        { "term": { "target_type": { "value": self.__target_type } } },
                        { "term": { "target_instance": { "value": self.__target_instance } } }
                    ]
                }
            },
            "sort": [ "target_id" ]
        }
        scroller = self.__es.SearchScroll(self.__priority_index, body, scrollSize=500, scrollTimeout=None)
        while scroller and len(scroller.Results):
            for dto in scroller.Results:
                itm = dto['_source']
                self.__priority_reservations[itm['target_id']] = itm['priority']
            scroller.GetNext()

    def set_in_progress(self, dto):
        sqdto = self.__get_async_queue_dto(dto)
        doc = sqdto.QueueClientDoc
        if doc.LockId and doc.LockId != self.__SessionId and GeneralUtility.ParseDate(doc.Locked, True) > (datetime.datetime.utcnow() - datetime.timedelta(days=self.__LockDaysOld)) and not doc.Completed:
            logging.info("Async queue doc for target %s:%s:%s not eligible for lock (lock id: '%s', locked: '%s').", self.__target_type, self.__TargetInstance, doc.TargetId, doc.LockId, doc.Locked)
            return None
        doc.Locked = datetime.datetime.utcnow().isoformat()
        doc.LockId = self.__SessionId
        rsp = self.__Es.UpdateWithLocking(self.__Index, doc.Dto(), sqdto.Id, sqdto.SequenceNumber, sqdto.PrimaryTerm)
        if rsp['result'] == "updated":
            sqdto.UpdateLockingInfo(rsp)
            return sqdto
        else:
            return None

    def set_complete(self, dto):
        sqdto = self.__get_async_queue_dto(dto)
        doc = sqdto.QueueClientDoc
        if not doc.LockId or doc.LockId != self.__SessionId or doc.Completed:
            logging.info("Async queue doc for target %s:%s:%s not eligible for completion with lock id '%s'.", self.__target_type, self.__TargetInstance, doc.TargetId, doc.LockId)
            return None
        doc.LockId = None
        doc.Completed = datetime.datetime.utcnow().isoformat()
        rsp = self.__es.UpdateWithLocking(self.__index, doc.Dto(), sqdto.Id, sqdto.SequenceNumber, sqdto.PrimaryTerm)
        if rsp['result'] == "updated":
            sqdto.UpdateLockingInfo(rsp)
            return sqdto
        else:
            return None

    def clear_session(self):
        body = {
            "query": { "term": { "lock_id": { "value": self.__SessionId } } },
            "script": {
                "source": f"ctx._source.lock_id = null;",
                "lang": "painless"
            }
        }
        logging.debug("Clearing locks for current session...")
        try:
            self.__es.UpdateByQuery(self.__index, body, ignoreConflicts=True)
            logging.debug("Session locks cleared.")
        except Exception as e:
            logging.exception(f"Clear session unexpected error: {e}")

    def search_and_lock(self, size, id_field:str):
        pass 

    

    def insert_queue_batch(self, idList, priority=None, skipExisting=True, permanent=False, force=False):
        '''
        Insert provided ID list into the queue, optionally setting priority and other parameters.

        :idList: list (array) of string IDs to add to the queue.
        :priority: set priority 1-9, where 1 is highest and 9 lowest priority - defaults to 5 if None.
        :skipExisting: if set, will only load IDs not already waiting in queue.
        :permanent: add this ID list and priority to a helper index so that the IDs will always be added to the queue with this priority.
        :force: use the force...to indicate that sync should be run even if not needed. Defaults to False.

        Must specify priority with permanent=True.
        If an ID is already present in the queue, will either skip update entirely (no priority update) or append a copy of the same queue ID (possibly with different priority), depending on skipExisting setting.
        If an ID is already present in the helper index that priority will be updated.
        No internal batching, don't pass a huge id list all at once.
        Force will only apply to current sync addition if permanent=True, not all future ones.
        '''
        if permanent == True and not priority:
            raise ValueError("If permanent set, must include priority")
        if skipExisting:
            self.__LoadExclusions = self.get_async_queue_current()
        self.__load_priority_reservations()
        dt = datetime.datetime.now(datetime.UTC).isoformat()
        prmList = []
        wrkList = []
        for i in idList:
            sid = str(i)
            curPriority = priority if priority else self.__DefaultPriority
            if permanent:
                doc = QueueClientPriorityDoc.new(sid, self.__target_type, self.__TargetInstance, priority, dt)
                self.__priority_reservations[sid] = curPriority
                prmList.append(self.__es.BulkInsertDocument(self.__priority_index, doc.dto(), doc.key))
            else:
                if sid in self.__priority_reservations.keys():
                    curPriority = self.__priority_reservations[sid] if not priority else curPriority
            if sid in self.__LoadExclusions:
                logging.debug("Target ID %s already exists in queue for target type '%s', skipping", i, self.__target_type)
                continue
            doc = QueueClientDoc.new(sid, self.__target_type, self.__TargetInstance, curPriority, dt, force=force).Dto()
            wrkList.append(self.__es.BulkInsertDocument(self.__index, doc, None, "create"))
        if len(prmList):
            rsp = self.__es.BulkInsert(prmList, raiseErrors=False)
            logging.info("Bulk inserted %s priority reservation entries", len(prmList))
        if len(wrkList):
            rsp = self.__es.BulkInsert(wrkList, raiseErrors=False)
            logging.info("Bulk inserted %s queue entries, %s succeeded, %s already present (or failed)", len(wrkList), rsp[0], rsp[1])

    def cleanup_queue_history(self, daysOld=None):
        if not daysOld:
            daysOld = self.__DaysOld
        days = f"now-{daysOld}d"
        body = {
          "query": {
            "bool": {
              "must": [
                { "range": { "completed": { "lte": days, "gt": "0" } } }
              ]
            }
          }
        }
        logging.debug("Removing queue history older than % day(s)", daysOld)
        self.__es.DeleteByQuery(self.__index, body, wait = False)


class QueueClientPriorityDoc(object):
    def __init__(self, dictObj=None):
        self.__targetId = None
        self.__targetType = None
        self.__priority = None
        self.__created = None
        self.__instance = None
        if dictObj:
            self.map(dictObj)

    @staticmethod
    def new(targetId, targetType, instance, priority, created):
        return QueueClientPriorityDoc({
            "target_id": targetId,
            "target_type": targetType,
            "target_instance": instance,
            "priority": priority,
            "created": created
        })

    @staticmethod
    def get_key(targetId, targetType, targetInstance):
        return f"{targetId}|{targetType}|{targetInstance}"

    def __map_field(self, dictObj, field):
        if not isinstance(dictObj, dict):
            return None
        if not field in dictObj.keys():
            return None
        return dictObj[field]

    def map(self, dto):
        self.target_id = self.__map_field(dto, "target_id")
        self.target_type = self.__map_field(dto, "target_type")
        self.instance = self.__map_field(dto, "target_instance")
        self.priority = self.__map_field(dto, "priority")
        self.created = self.__map_field(dto, "created")

    def dto(self):
        return {
            "target_id": self.target_id,
            "target_type": self.target_type,
            "target_instance": self.instance,
            "priority": self.priority,
            "created": self.created
        }

    @property
    def key(self):
        return QueueClientPriorityDoc.get_key(self.__targetId, self.__targetType, self.__instance)

    @property
    def target_id(self):
        return self.__targetId

    @target_id.setter
    def target_id(self, value):
        self.__targetId = value

    @property
    def target_type(self):
        return self.__targetType

    @target_type.setter
    def target_type(self, value):
        self.__targetType = value

    @property
    def instance(self):
        return self.__instance

    @instance.setter
    def instance(self, value):
        self.__instance = value

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
    def __init__(self, dict_obj=None):
        self.__target_id = None
        self.__target_type = None
        self.__priority = None
        self.__force = None
        self.__created = None
        self.__completed = None
        self.__lock_id = None
        self.__locked = None
        self.__instance = None
        if dict_obj:
            self.Map(dict_obj)

    @staticmethod
    def new(targetId, targetType, instance, priority, created, completed=None, locked=None, lockId=None, force=False):
        return QueueClientDoc({
            "target_id": targetId,
            "target_type": targetType,
            "target_instance": instance,
            "priority": priority,
            "force": force,
            "created": created,
            "completed": completed,
            "locked": locked,
            "lock_id": lockId
        })

    def __MapField(self, dictObj, field):
        if not isinstance(dictObj, dict):
            return None
        if not field in dictObj.keys():
            return None
        return dictObj[field]

    def Map(self, dto):
        self.TargetId = self.__MapField(dto, "target_id")
        self.TargetType = self.__MapField(dto, "target_type")
        self.Instance = self.__MapField(dto, "target_instance")
        self.Priority = self.__MapField(dto, "priority")
        self.Force = self.__MapField(dto, "force")
        self.Created = self.__MapField(dto, "created")
        self.Completed = self.__MapField(dto, "completed")
        self.Locked = self.__MapField(dto, "locked")
        self.LockId = self.__MapField(dto, "lock_id")

    def Dto(self):
        return {
            "target_id": self.TargetId,
            "target_type": self.TargetType,
            "target_instance": self.Instance,
            "priority": self.Priority,
            "force": self.Force,
            "created": self.Created,
            "completed": self.Completed,
            "locked": self.Locked,
            "lock_id": self.LockId
        }

    @property
    def TargetId(self):
        return self.__target_id

    @TargetId.setter
    def TargetId(self, value):
        self.__target_id = value

    @property
    def TargetType(self):
        return self.__target_type

    @TargetType.setter
    def TargetType(self, value):
        self.__target_type = value

    @property
    def Instance(self):
        return self.__instance

    @Instance.setter
    def Instance(self, value):
        self.__instance = value

    @property
    def Priority(self):
        return self.__priority

    @Priority.setter
    def Priority(self, value):
        self.__priority = value

    @property
    def Force(self):
        return self.__force

    @Force.setter
    def Force(self, value):
        self.__force = True if value else False

    @property
    def Created(self):
        return self.__created

    @Created.setter
    def Created(self, value):
        self.__created = value

    @property
    def Completed(self):
        return self.__completed

    @Completed.setter
    def Completed(self, value):
        self.__completed = value

    @property
    def Locked(self):
        return self.__locked

    @Locked.setter
    def Locked(self, value):
        self.__locked = value

    @property
    def LockId(self):
        return self.__lock_id

    @LockId.setter
    def LockId(self, value):
        self.__lock_id = value

class QueueClientDto(object):
    def __init__(self, dto=None):
        self.__doc = None
        self.__seq = None
        self.__pri = None
        self.__id = None
        if dto:
            self.QueueClientDoc = QueueClientDoc(dto['_source'])
            self.SequenceNumber = dto['_seq_no']
            self.PrimaryTerm = dto['_primary_term']
            self.Id = dto['_id']

    def Dto(self):
        return {
            '_source': self.QueueClientDoc.Dto(),
            '_seq_no': self.SequenceNumber,
            '_primary_term': self.PrimaryTerm
        }

    def UpdateLockingInfo(self, response):
        if response and '_seq_no' in response.keys() and '_primary_term' in response.keys():
            self.SequenceNumber = response['_seq_no']
            self.PrimaryTerm = response['_primary_term']
            logging.debug("Locking information updated")
            return True
        else:
            logging.debug("Locking information not found in response")
            return False

    @property
    def Id(self):
        return self.__id

    @Id.setter
    def Id(self, value):
        self.__id = value

    @property
    def QueueClientDoc(self):
        return self.__doc

    @QueueClientDoc.setter
    def QueueClientDoc(self, value: QueueClientDoc):
        self.__doc = value

    @property
    def SequenceNumber(self):
        return self.__seq

    @SequenceNumber.setter
    def SequenceNumber(self, value):
        self.__seq = value

    @property
    def PrimaryTerm(self):
        return self.__pri

    @PrimaryTerm.setter
    def PrimaryTerm(self, value):
        self.__pri = value

class QueueClientException(Exception):
    pass
