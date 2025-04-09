''' --[auto-generated, do not modify this block]--
*
* Copyright (c) 2025 Saltworks Security, LLC
*
* Use of this software is governed by the Business Source License included
* in the LICENSE file.
*
* Change Date: 2029-04-09
*
* On the date above, in accordance with the Business Source License, use
* of this software will be governed by version 2 or later of the General
* Public License.
*
* ----
'''

import datetime
import logging

class UpdateQueueHelper(object):
    def __init__(self, appSettings, sourceName):
        '''
        Setup the class
        '''
        if type(appSettings).__name__ != "ApplicationSettings":
            raise TypeError("Type of appSettings must be 'ApplicationSettings'")

        app = appSettings.Application
        logging.debug("UpdateQueueHelper init, source name '%s'", sourceName)
        
        self.__Source = appSettings.GetSource(sourceName, "Source")
        if not self.__Source in ['SSC', 'FOD']:
            raise UpdateQueueHelperException(f"Invalid source '{self.__Source}' in config with source name '{sourceName}'.  Should be SSC or FOD.")
        self.__UpdateIndex = 'sscupdatequeue' if self.__Source == 'SSC' else 'fodupdatequeue'
        self.__IdField = 'projectVersionId' if self.__Source == 'SSC' else 'releaseId'
        self.__Es = app.GetElasticClient()
        self.__BatchSize = appSettings.GetSource(sourceName, "UpdateQueueBatchSize", 100)
        self.__DaysOld = appSettings.GetSource(sourceName, "UpdateQueueRetentionDays", 7)
        logging.debug("UpdateQueueHelper init complete.")

    def GetUpdateQueueBatch(self, queueTypes=[], refreshFirst=False, sourceName=None):
        '''
        Returns a single batch of update queue docs (only 1 per ID).
        We assume these will be completed using CompleteUpdateQueue() before getting the next batch.
        '''
        if len(queueTypes) == 0:
            raise UpdateQueueHelperException("At least one queue type must be specified.")
        for qt in queueTypes:
            if qt not in ['U', 'D', 'A']:
                raise UpdateQueueHelperException(f"Invalid update type '{qt}'.")
        body = {
          "aggs": {
            "id": {
              "terms": { "field": self.__IdField, "size": self.__BatchSize },
              "aggs": { "max_processed": { "max": { "field": "processedDateTime" } } }
            },
            "total_count": { "value_count": { "field": self.__IdField } }
          }, 
          "query": {
            "bool": {
              "must": [
                { "range": { "completedDateTime": { "lt": "0" } } },
                { "terms": { "updateType": queueTypes } }
              ]
            }
          },
          "size": 0
        }
        if sourceName:
            body['query']['bool']['must'].append({ "term": { "sourceName": { "value": sourceName }}})
        if refreshFirst:
            logging.debug("Flushing index '%s'", self.__UpdateIndex)
            self.__Es.RefreshIndex(self.__UpdateIndex)
        logging.debug("Getting update queue batch for queue type(s) '%s'", queueTypes)
        r = self.__Es.Search(self.__UpdateIndex, body, 0, False)
        if not r or not "aggregations" in r.keys():
            return None, None
        ret = []
        for b in r['aggregations']['id']['buckets']:
            ret.append({ "id": b['key'], "max_processed_date": b['max_processed']['value_as_string'] })
        return ret, r['aggregations']['total_count']['value']

    def CompleteUpdateQueue(self, id, queueTypes=[], sourceName=None):
        '''
        Marks update queue doc(s) as complete for a given ID and queue type.
        '''
        if len(queueTypes) == 0:
            raise UpdateQueueHelperException("At least one queue type must be specified.")
        dtNow = datetime.datetime.utcnow().isoformat()
        body = {
          "query": {
            "bool": {
              "must": [
                { "range": { "completedDateTime": { "lte": 0 } } },
                { "terms": { "updateType": queueTypes } },
                { "term": { self.__IdField: { "value": str(id) } } }
              ]
            }
          },
          "script": {
            "source": "ctx._source.completedDateTime = params.dt",
            "lang": "painless",
            "params": {
                "dt": f'{dtNow}'
            }
          }
        }
        if sourceName:
            body['query']['bool']['must'].append({ "term": { "sourceName": { "value": sourceName }}})
        logging.debug("Marking queue complete for id %s and queue types '%s'", id, queueTypes)
        self.__Es.UpdateByQuery(self.__UpdateIndex, body, False, False, True)

    def CleanupQueueHistory(self, daysOld=None):
        if not daysOld:
            daysOld = self.__DaysOld
        days = f"now-{daysOld}d"
        body = {
          "query": {
            "bool": {
              "must": [
                { "range": { "completedDateTime": { "lte": days, "gt": "0" } } }
              ]
            }
          }
        }
        logging.debug("Removing queue history older than % day(s)", daysOld)
        self.__Es.DeleteByQuery(self.__UpdateIndex, body, False, False, True)


class UpdateQueueHelperException(Exception):
    pass