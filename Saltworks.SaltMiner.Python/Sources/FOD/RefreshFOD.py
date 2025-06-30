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

import sys
import json
import os.path
import os
import logging
import time
import datetime

from Utility.ProgressLogger import *


class RefreshFOD(object):
    """Refresh of Active FOD Records"""

    def __init__(self, appSettings, sourceName):

        self.__Es = appSettings.Application.GetElasticClient()
        self.__Prog = os.path.splitext(os.path.basename(__file__))[0]
        self.__SourceName = sourceName
        self.__SourceNameField = "sourceName"
        logging.debug(f"{self.__Prog}.init complete.")

    
    def ForceRefresh(self):

        # Clear out all fodupdateQueue records to do refresh
        logging.info('Flush fodupdatequeue records for refresh')
        body = { "query": { "term": { self.__SourceNameField: { "value": self.__SourceName } } } }
        self.__Es.DeleteByQuery('fodupdatequeue', body)
        logging.info('Getting All FOD Releases')
        body['_source'] = ['releaseId']
        body['sort'] = ['releaseId']
        scroller = self.__Es.SearchScroll('fodreleases', body, scrollTimeout=None)

        iTotal = scroller.TotalHits
        logging.info(iTotal)

        p = ProgressLogger(self.__Es)
        p.Start(self.__Prog, iTotal, f"{self.__Prog} Status")
        p.Progress(0, f"{self.__Prog} - create update records for all active FOD Records")

        relCount = 0
        for dto in scroller.Generator():
            fodRel = dto['_source']['releaseId']
            relCount = relCount + 1
                
            logging.info(relCount)

            p.Progress(relCount, 'Creating UpdateQueue Records for FOD ProjectVersion {} of {}'.format(relCount, iTotal))
    
            queueInfo = {
                'processedDateTime' : datetime.datetime.now(datetime.timezone.utc).strftime("%Y-%m-%dT%H:%M:%S.%f"),
                'releaseId': fodRel,
                'updateType': 'U',
                'completedDateTime' : '1900-01-01T00:00:00.000-0000',
                self.__SourceNameField : self.__SourceName
            }
            self.__Es.BulkSendBatch('fodupdatequeue', queueInfo)
        self.__Es.BulkSendBatch() # send remaining

        p.Finish(iTotal, "Complete")
        self.__Es.FlushIndex('fodupdatequeue')
