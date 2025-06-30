''' --[auto-generated, do not modify this block]--
*
* Copyright (c) 2025 Saltworks Security, LLC
*
* Use of this software is governed by the Business Source License included
* in the LICENSE file.
*
* Change Date: 2029-06-30
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

from Utility.GeneralUtility import GeneralUtility

class SyncQueueHelper(object):
    def __init__(self, appSettings, sourceName):
        '''
        Setup the class
        '''
        if type(appSettings).__name__ != "ApplicationSettings":
            raise TypeError("Type of appSettings must be 'ApplicationSettings'")

        app = appSettings.Application
        logging.debug("SyncQueueHelper init, source name '%s'", sourceName)
        
        self.__TargetType = appSettings.GetSource(sourceName, "Source")
        self.__TargetInstance = sourceName
        if not self.__TargetType in ['SSC', 'FOD']:
            raise SyncQueueHelperException(f"Invalid source '{self.__TargetType}' in config with source name '{sourceName}'.  Should be SSC or FOD.")
        self.__Index = 'syncqueue'
        self.__PriorityIndex = 'syncqueue_priority'
        self.__IdField = 'target_id'
        self.__Es = app.GetElasticClient()
        self.__BatchSize = appSettings.GetSource(sourceName, "SyncQueueBatchSize", 500)
        self.__DaysOld = appSettings.GetSource(sourceName, "SyncQueueRetentionDays", 1)
        self.__LockDaysOld = appSettings.GetSource(sourceName, "SyncQueueLockRetentionDays", 1)
        self.__Es.MapIndex(self.__Index, False)  # will map if doesn't exist
        self.__Es.MapIndex(self.__PriorityIndex, False)  # will map if doesn't exist
        self.__LoadExclusions = []
        self.__PriorityReservations = {}
        self.__SessionId = uuid.uuid4()
        self.__DefaultPriority = 5
        logging.debug("SyncQueueHelper init complete.")

    @property
    def BatchSize(self):
        return self.__BatchSize

    @BatchSize.setter
    def BatchSize(self, value):
        self.__BatchSize = value

    @property
    def SessionId(self):
        return self.__SessionId

    @property
    def Index(self):
        return self.__Index

    def ClearPriorityReservations(self):
        '''
        Clears all priority reservations for current type and instance
        '''
        body = {
          "query": {
            "bool": {
              "must": [
                { "term": { "target_type": { "value": self.__TargetType } } },
                { "term": { "target_instance": { "value": self.__TargetInstance } } }
              ]
            }
          }
        }
        self.__Es.DeleteByQuery(self.__PriorityIndex, body, ignoreMissingIndex=True)
        logging.info("Cleared sync priority reservations for target type '%s' and instance '%s'.", self.__TargetType, self.__TargetInstance)

    def ClearSyncQueue(self, completed=True, locked=False):
        '''
        Clears all sync items from queue, optionally including those that are completed or locked
        '''
        body = {
          "query": {
            "bool": {
              "must": [
                { "term": { "target_type": { "value": self.__TargetType } } },
                { "term": { "target_instance": { "value": self.__TargetInstance } } }
              ],
              "must_not": []
            }
          }
        }
        if completed == False:
            body['query']['bool']['must_not'].append({ "exists": { "field": "completed" } })
        if not locked == True:
            body['query']['bool']['must_not'].append({ "exists": { "field": "lock_id" } })
        self.__Es.DeleteByQuery(self.__Index, body, ignoreMissingIndex=True)
        logging.debug("Cleared sync queue for target type '%s' and instance '%s'.", self.__TargetType, self.__TargetInstance)

    def GetSyncQueueCurrent(self):
        '''
        Returns all target IDs currently in the queue where completed not set, including any that are locked.
        This can be used as an exclusion list to avoid duplicates in the queue.
        '''
        if not self.__Es.IndexExists(self.__Index):
            raise SyncQueueHelperException("Unable to return sync queue items, index '%s' does not exist.", self.__Index)
        body = {
          "query": {
            "bool": {
              "must": [
                { "term": { "target_type": { "value": self.__TargetType } } },
                { "term": { "target_instance": { "value": self.__TargetInstance } } }
              ],
              "must_not": [
                { "exists": { "field": "completed" } }
              ]
            }
          },
          "_source": [self.__IdField],
          "sort": [ { self.__IdField: "asc" } ]
        }
        lst = []
        sc = self.__Es.SearchScroll(self.__Index, body, self.__BatchSize, scrollTimeout=None)
        while sc and sc.Results:
            for dto in sc.Results:
                lst.append(dto['_source'][self.__IdField])
            sc.GetNext()
        logging.debug("%s incomplete target ID(s) in the queue for target type '%s'.", len(lst), self.__TargetType)
        return lst
        
    def GetSyncQueueBatch(self):
        '''
        Returns a tuple containing a single batch of update queue docs (only 1 per ID), and a count of the total hits (if over 10k, then 10k will be returned).
        We assume these will be completed using CompleteSyncQueue() before getting the next batch.
        '''
        if not self.__Es.IndexExists(self.__Index):
            raise SyncQueueHelperException("Unable to return sync queue items, index '%s' does not exist.", self.__Index)
        body = {
          "aggs": {
            "total_count": { "value_count": { "field": self.__IdField } }
          }, 
          "query": {
            "bool": {
              "must": [
                { "term": { "target_type": { "value": self.__TargetType } } },
                { "term": { "target_instance": { "value": self.__TargetInstance } } }
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
          "sort": [ "priority", "created" ]
        }
        logging.debug("Getting next sync queue batch (qty %s)", self.__BatchSize)
        self.__LoadPriorityReservations()
        r = self.__Es.Search(self.__Index, body, self.__BatchSize, False, True)
        if not r or not "aggregations" in r.keys():
            return None, None
        ret = []
        for item in r['hits']['hits']:
            dto = SyncQueueDto(item)
            ret.append(dto)
        return ret, r['aggregations']['total_count']['value']

    def __GetSyncQueueDto(self, dto):
        sqdto = dto
        if isinstance(sqdto, dict):
            if '_source' in sqdto.keys():
                sqdto = SyncQueueDto(dto)
        if not isinstance(sqdto, SyncQueueDto):
            raise SyncQueueHelperException("Invalid value for dto, expected SyncQueueDto, or a dict that can be turned into a SyncQueueDto.")
        return sqdto

    def __LoadPriorityReservations(self, lazy=True):
        if lazy and len(self.__PriorityReservations) > 0:
            return
        body = {
            "query": {
                "bool": {
                    "must": [
                        { "term": { "target_type": { "value": self.__TargetType } } },
                        { "term": { "target_instance": { "value": self.__TargetInstance } } }
                    ]
                }
            },
            "sort": [ "target_id" ]
        }
        scroller = self.__Es.SearchScroll(self.__PriorityIndex, body, scrollSize=500, scrollTimeout=None)
        while scroller and len(scroller.Results):
            for dto in scroller.Results:
                itm = dto['_source']
                self.__PriorityReservations[itm['target_id']] = itm['priority']
            scroller.GetNext()
    
    def SetInProgress(self, dto):
        sqdto = self.__GetSyncQueueDto(dto)
        doc = sqdto.SyncQueueDoc
        if doc.LockId and doc.LockId != self.__SessionId and GeneralUtility.ParseDate(doc.Locked, True) > (datetime.datetime.utcnow() - datetime.timedelta(days=self.__LockDaysOld)) and not doc.Completed:
            logging.info("Sync queue doc for target %s:%s:%s not eligible for lock (lock id: '%s', locked: '%s').", self.__TargetType, self.__TargetInstance, doc.TargetId, doc.LockId, doc.Locked)
            return None
        doc.Locked = datetime.datetime.utcnow().isoformat()
        doc.LockId = self.__SessionId
        rsp = self.__Es.UpdateWithLocking(self.__Index, doc.Dto(), sqdto.Id, sqdto.SequenceNumber, sqdto.PrimaryTerm)
        if rsp['result'] == "updated":
            sqdto.UpdateLockingInfo(rsp)
            return sqdto
        else:
            return None

    def SetComplete(self, dto):
        sqdto = self.__GetSyncQueueDto(dto)
        doc = sqdto.SyncQueueDoc
        if not doc.LockId or doc.LockId != self.__SessionId or doc.Completed:
            logging.info("Sync queue doc for target %s:%s:%s not eligible for completion with lock id '%s'.", self.__TargetType, self.__TargetInstance, doc.TargetId, doc.LockId)
            return None
        doc.LockId = None
        doc.Completed = datetime.datetime.utcnow().isoformat()
        rsp = self.__Es.UpdateWithLocking(self.__Index, doc.Dto(), sqdto.Id, sqdto.SequenceNumber, sqdto.PrimaryTerm)
        if rsp['result'] == "updated":
            sqdto.UpdateLockingInfo(rsp)
            return sqdto
        else:
            return None

    def ClearSession(self):
        body = {
            "query": { "term": { "lock_id": { "value": self.__SessionId } } },
            "script": {
                "source": f"ctx._source.lock_id = null;",
                "lang": "painless"
            }
        }
        logging.debug("Clearing locks for current session...")
        try:
            self.__Es.UpdateByQuery(self.__Index, body, ignoreConflicts=True)
            logging.debug("Session locks cleared.")
        except Exception as e:
            logging.exception(f"Clear session unexpected error: {e}")

    def InsertQueueBatch(self, idList, priority=None, skipExisting=True, permanent=False, force=False):
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
            self.__LoadExclusions = self.GetSyncQueueCurrent()
        self.__LoadPriorityReservations()
        dt = datetime.datetime.utcnow().isoformat()
        prmList = []
        wrkList = []
        for i in idList:
            sid = str(i)
            curPriority = priority if priority else self.__DefaultPriority
            if permanent:
                doc = SyncQueuePriorityDoc.New(sid, self.__TargetType, self.__TargetInstance, priority, dt)
                self.__PriorityReservations[sid] = curPriority
                prmList.append(self.__Es.BulkInsertDocument(self.__PriorityIndex, doc.Dto(), doc.Key))
            else:
                if sid in self.__PriorityReservations.keys():
                    curPriority = self.__PriorityReservations[sid] if not priority else curPriority
            if sid in self.__LoadExclusions:
                logging.debug("Target ID %s already exists in queue for target type '%s', skipping", i, self.__TargetType)
                continue
            doc = SyncQueueDoc.New(sid, self.__TargetType, self.__TargetInstance, curPriority, dt, force=force).Dto()
            wrkList.append(self.__Es.BulkInsertDocument(self.__Index, doc, None, "create"))
        if len(prmList):
            rsp = self.__Es.BulkInsert(prmList, raiseErrors=False)
            logging.info("Bulk inserted %s priority reservation entries", len(prmList))
        if len(wrkList):
            rsp = self.__Es.BulkInsert(wrkList, raiseErrors=False)
            logging.info("Bulk inserted %s queue entries, %s succeeded, %s already present (or failed)", len(wrkList), rsp[0], rsp[1])

    def CleanupQueueHistory(self, daysOld=None):
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
        self.__Es.DeleteByQuery(self.__Index, body, wait = False)


class SyncQueuePriorityDoc(object):
    def __init__(self, dictObj=None):
        self.__targetId = None
        self.__targetType = None
        self.__priority = None
        self.__created = None
        self.__instance = None
        if dictObj:
            self.Map(dictObj)

    @staticmethod
    def New(targetId, targetType, instance, priority, created):
        return SyncQueuePriorityDoc({
            "target_id": targetId,
            "target_type": targetType,
            "target_instance": instance,
            "priority": priority,
            "created": created
        })

    @staticmethod
    def GenKey(targetId, targetType, targetInstance):
        return f"{targetId}|{targetType}|{targetInstance}"

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
        self.Created = self.__MapField(dto, "created")

    def Dto(self):
        return {
            "target_id": self.TargetId,
            "target_type": self.TargetType,
            "target_instance": self.Instance,
            "priority": self.Priority,
            "created": self.Created
        }
            
    @property
    def Key(self):
        return SyncQueuePriorityDoc.GenKey(self.__targetId, self.__targetType, self.__instance)
    
    @property
    def TargetId(self):
        return self.__targetId

    @TargetId.setter
    def TargetId(self, value):
        self.__targetId = value

    @property
    def TargetType(self):
        return self.__targetType

    @TargetType.setter
    def TargetType(self, value):
        self.__targetType = value

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
    def Created(self):
        return self.__created

    @Created.setter
    def Created(self, value):
        self.__created = value

class SyncQueueDoc(object):
    def __init__(self, dictObj=None):
        self.__targetId = None
        self.__targetType = None
        self.__priority = None
        self.__force = None
        self.__created = None
        self.__completed = None
        self.__lockId = None
        self.__locked = None
        self.__instance = None
        if dictObj:
            self.Map(dictObj)

    @staticmethod
    def New(targetId, targetType, instance, priority, created, completed=None, locked=None, lockId=None, force=False):
        return SyncQueueDoc({
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
        return self.__targetId

    @TargetId.setter
    def TargetId(self, value):
        self.__targetId = value

    @property
    def TargetType(self):
        return self.__targetType

    @TargetType.setter
    def TargetType(self, value):
        self.__targetType = value

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
        return self.__lockId

    @LockId.setter
    def LockId(self, value):
        self.__lockId = value

class SyncQueueDto(object):
    def __init__(self, dto=None):
        self.__doc = None
        self.__seq = None
        self.__pri = None
        self.__id = None
        if dto:
            self.SyncQueueDoc = SyncQueueDoc(dto['_source'])
            self.SequenceNumber = dto['_seq_no']
            self.PrimaryTerm = dto['_primary_term']
            self.Id = dto['_id']

    def Dto(self):
        return {
            '_source': self.SyncQueueDoc.Dto(),
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
    def SyncQueueDoc(self):
        return self.__doc

    @SyncQueueDoc.setter
    def SyncQueueDoc(self, value: SyncQueueDoc):
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

class SyncQueueHelperException(Exception):
    pass
