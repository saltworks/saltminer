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
        # Refresh, not flush - the refresh stage reads these records back, so what is needed is
        # searchability, not a Lucene commit.
        logging.info("Refreshing index sscupdatequeue")
        self.__Es.RefreshIndex('sscupdatequeue')
