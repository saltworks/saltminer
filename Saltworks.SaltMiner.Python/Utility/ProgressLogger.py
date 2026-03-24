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
import sys
import datetime
import time
import logging
import uuid

from Core.ElasticClient import ElasticClient

class ProgressLogger:
    ''' Logs progress messages for a process/job '''
   
    def __init__(self, esClient):
        #if (not esClient or not isinstance(esClient, ElasticClient)):
        #    raise ProgressLoggerException("Missing/invalid Elasticsearch client")
        #self.__Es = esClient
        #self.__CheckIndex()
        self.JobKey = None
        self.DefaultStatus = None
        self.TotalCount = None
        self.Finished = False
        self.Started = False
        self.WriteToElastic = False
        self.StartTime = None
        self.EndTime = None
        logging.debug("ProgressLogger initialized.")

    def Start(self, jobKey, inTotalCount, defaultStatus=None):
        totalCount = inTotalCount
        if totalCount == 0:
            totalCount = 1

        if not self.JobKey:
            if not jobKey:
                raise ProgressLoggerException("Missing/invalid job key")
            self.JobKey = jobKey
        if not self.TotalCount and not totalCount:
                raise ProgressLoggerException("Missing/invalid total count")
        if totalCount:
            self.TotalCount = totalCount
        if defaultStatus:
            self.DefaultStatus = defaultStatus
        if not self.DefaultStatus and not defaultStatus:
            self.DefaultStatus = "Processing something..."
        self.CurrentCount = 0
        self.Id = uuid.uuid4()
        self.StartTime = datetime.datetime.utcnow()
        self.EndTime = None
        status = "{} Starting...".format(self.JobKey)

        try:
            if self.Started:
                raise ProgressLoggerException("Invalid call; job already started.")
            if self.Finished:
                raise ProgressLoggerException("Invalid call; job already finished.")
            #if self.WriteToElastic:
            #    doc = self.__CreateDoc(start=True, status=status)
            #    self.__Es.Index(ProgressLogger.IndexName(), doc)
            self.__WriteConsole(status)
            self.Started = True
        except Exception as ex:
            logging.error("Unable to start a new progress log: {}".format(ex))

    def __WriteConsole(self, status):
        c = self.CurrentCount
        if c == 0:
            c = 1
        if self.CurrentCount == 0 or self.Finished:
            msg = "{}".format(status)
        else:
            msg = "{}, {}% complete".format(status, int(self.CurrentCount / self.TotalCount * 100))
        logging.info("[ProgressLog] " + msg)

    def Progress(self, currentCount, status=None, totalCount=None):
        if totalCount:
            self.TotalCount = totalCount
        self.CurrentCount = currentCount
        if not status:
            status = self.DefaultStatus
        try:
            if not self.Started:
                raise ProgressLoggerException("Invalid call; job not started.")
            if self.Finished:
                raise ProgressLoggerException("Invalid call; job already finished.")
            #if self.WriteToElastic:
            #    doc = self.__CreateDoc(status=status)
            #    self.__Es.Index(ProgressLogger.IndexName(), doc)
            self.__WriteConsole(status)
        except Exception as ex:
            logging.error("Unable to record progress log: {}".format(ex))

    def Finish(self, currentCount=None, status=None):
        
        if currentCount:
            self.CurrentCount = currentCount
        else:
            self.CurrentCount = self.TotalCount
        if self.StartTime and self.EndTime:
            elapsed = "{} elapsed time: {}".format(self.JobKey, self.StartTime - self.EndTime)
        else:
            elapsed = "{} elapsed time unknown".format(self.JobKey)
        if not status:
            status = "{} Complete.".format(self.JobKey)
        try:
            if not self.Started:
                raise ProgressLoggerException("Invalid call; job not started.")
            if self.Finished:
                raise ProgressLoggerException("Invalid call; job already finished.")
            #if self.WriteToElastic:
            #    doc = self.__CreateDoc(False, True, status)
            #    self.__Es.Index(ProgressLogger.IndexName(), doc)
            self.__WriteConsole(status)
            self.__WriteConsole(elapsed)
            self.Finished = True
        except Exception as ex:
            logging.error("Unable to finish a progress log: {}".format(ex))

    #def __CreateDoc(self, start=False, finish=False, status=None):

    #    try:
    #        percentDone = int((self.CurrentCount/self.TotalCount)*100)
    #    except:
    #        percentDone = 0
        
    #    doc = { "id": self.Id, 
    #           "jobkey": self.JobKey, 
    #           "timestamp": datetime.datetime.utcnow().isoformat(), 
    #           "starting": start, 
    #           "finished": finish, 
    #           "current": self.CurrentCount, 
    #           "total": self.TotalCount, 
    #           "percent": percentDone,
    #           "status": status }
    #    return doc

    #def __CheckIndex(self):
    #    mapping = {
    #        "mappings": {
    #            "properties": {
    #                "id": {"type": "text"},
    #                "jobkey": {"type": "keyword"},
    #                "timestamp": {"type": "date"},
    #                "starting": {"type": "boolean"},
    #                "finished": {"type": "boolean"},
    #                "current": {"type": "integer"},
    #                "total": {"type": "integer"},
    #                "percent": {"type": "integer"},
    #                "status": {"type": "text"}
    #            }
    #        }
    #    }
    #    self.__Es.MapIndexWithMapping(ProgressLogger.IndexName(), mapping, False)

    @staticmethod
    def IndexName():
        return "progress-log-{}".format(datetime.date.today())

class ProgressLoggerException(Exception):
    pass
