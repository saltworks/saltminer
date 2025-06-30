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

from Core.ElasticClient import ElasticClient
from Utility.ProgressLogger import *
from Sources.SSC.SscEsUtils import SscEsUtils


def initBlankQueueObject():
    _attrInfo = {
        'processedDateTime' : '',
        'projectVersionId': 0,
        'updateType': '',
        'completedDateTime' : ''
    }

    return _attrInfo

class RefreshSSC(object):
    """Refresh of Active SSC Records"""

    def __init__(self, appSettings, sourceName):
        if type(appSettings).__name__ != "ApplicationSettings":
            raise TypeError("Type of appSettings must be 'ApplicationSettings'")
        self.__Es = appSettings.Application.GetElasticClient()
        self.__SscEs = SscEsUtils(appSettings)

        logging.debug("ExtractSSC.init complete.")
    
    def __ForceRefreshOne(self, pvid):
        logging.info(f"Updating sscupdatequeue record for refresh of pvid {pvid}")
        query = { "query": { "term": { "projectVersionId": pvid } } }
        self.__Es.DeleteByQuery('sscupdatequeue', query)
        queueInfo = initBlankQueueObject()
        queueInfo['processedDateTime'] = datetime.datetime.now().strftime("%Y-%m-%dT%H:%M:%S.%f")
        queueInfo['projectVersionId'] = pvid
        queueInfo['updateType'] = 'U'
        queueInfo['completedDateTime'] = '1900-01-01T00:00:00.000-0000'
        self.__Es.Index('sscupdatequeue', json.dumps(queueInfo))

    def ForceRefresh(self, pvid=None):
        '''
        Force the refresh of all app_vuls data for all project versions, or just one if pvid passed.
        '''

        # If just one ID requested, update it and quit
        if pvid:
            self.__ForceRefreshOne(pvid)
            return

        # Clear out all sscupdateQueue records to do refresh
        logging.info('Flush sscupdatequeue records for refresh')

        #Ensure the sscupdatequeue index exists
        self.__Es.MapIndex("sscupdatequeue", True)
    
        logging.info('Getting ProjectVersions')

        self.__SscEs.getAllESSSCProjects()

        iTotal = len(self.__SscEs.AllSscProjects)
        logging.info(f"{iTotal} total ProjectVersions")

        p = ProgressLogger(self.__Es)
        p.Start("RefreshSSC", iTotal, "RefreshSSC Status")
        p.Progress(0, 'Starting ForceRefreshSSC - create update records for all active SSC Records')


        pvCount = 0
        for sscProj in self.__SscEs.AllSscProjects:
            projid = sscProj['id']

            pvCount = pvCount + 1
                
            logging.info(pvCount)

            p.Progress(pvCount, 'Creating UpdateQueue Records for SSC ProjectVersion {} of {}'.format(pvCount, iTotal))
    
            queueInfo = initBlankQueueObject()
            holdnow = datetime.datetime.now()
            #formatnow = holdnow.strftime("%Y-%m-%dT%H:%M:%S")
            formatnow = holdnow.strftime("%Y-%m-%dT%H:%M:%S.%f")
        
            #logging.info(holdnow)
            #logging.info(formatnow)
            queueInfo['processedDateTime'] = formatnow
            #logging.info(queueInfo['processedDateTime'])

            queueInfo['projectVersionId'] = projid
            queueInfo['updateType'] = 'U'
            queueInfo['completedDateTime'] = '1900-01-01T00:00:00.000-0000'
            logging.info(queueInfo)
            self.__Es.Index('sscupdatequeue', json.dumps(queueInfo))
                

        p.Finish(iTotal, "Complete")
        logging.info("Attempting to flush index sscupdatequeue")
        self.__Es.FlushIndex('sscupdatequeue')
