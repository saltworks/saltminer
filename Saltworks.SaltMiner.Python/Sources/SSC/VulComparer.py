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

# 10/8/2021 TD
# Originally CheckSSCVulsDrop.py and class CheckSSCVulsDrop
# Please confirm function - it appears this one is a dependency for CompareSSCVuls

import json
import time
import datetime
import sys
import logging

from Core.ElasticClient import ElasticClient
from .SscUtilities import SscUtilities
from Utility.ProgressLogger import *

def initBlankQueueObject():
	_attrIfno = {
		'processedDateTime' : '',
		'projectVersionId' : 0,
		'updateType' : '',
		'completedDateTime' : ''
	}

	return _attrIfno

class VulComparer(object):
    """description of class"""

    def __init__(self, appSettings, sourceName):

        if type(appSettings).__name__ != "ApplicationSettings":
            raise VulComparerException("Type of appSettings must be 'ApplicationSettings'")
        if not sourceName or not sourceName in appSettings.GetSourceNames():
            raise VulComparerException(f"Invalid or missing source configuration for source name '{sourceName}'")

        self.__ElasticClient = appSettings.Application.GetElasticClient()
        self.__SscUtils = SscUtilities(appSettings)
        self.__CreateSscUpdateRecords = appSettings.Get(sourceName, 'CreateUpdateRecords', False)
        logging.debug("VulComparer initialization complete")
    
    def CompareAppVuls(self):

        print('Starting CompareAppVuls')

        es = self.__SscUtils.getElasticUtil()
                        
        # Get all Open SSC App Vuls into memory
        
        SSCOpenAppVulsList = self.__GetAllSSCAppVerCounts()
        
        iCount = 0
        iTotal = len(SSCOpenAppVulsList)

        p = ProgressLogger(self.__ElasticClient)
        p.Start("CompareAppVuls", iTotal, "CompareAppVuls Status")
        p.Progress(0, 'Starting CompareAppVuls')

        updateList = {}
               
        for AppVulRecord in SSCOpenAppVulsList:
        
            iCount = iCount + 1

            p.Progress(iCount, 'Comparing AppVulSSC record {} of {}'.format(iCount, iTotal))
            #print("Checking {} of {}".format(iCount, iTotal))
                        
            holdapplicationversionid = AppVulRecord['key']
            holdcriticalcount = AppVulRecord['Critical']['value']
            holdhighcount = AppVulRecord['High']['value']
            holdmediumcount = AppVulRecord['Medium']['value']
            holdlowcount = AppVulRecord['Low']['value']
            
            projid = holdapplicationversionid
            projectFilterSet = self.__SscUtils.getProjectVersionFilterSet(projid)
            for projectFilter in projectFilterSet['data']:
                if projectFilter['defaultFilterSet'] == True:
                    projectDefFilter = projectFilter['guid']
                    logging.info(projectDefFilter)

            issues_count = self.__SscUtils.getProjectVersionIssueCounts(projid,projectDefFilter)
            
            jprojcounts = json.dumps(issues_count)
            holdcritical = issues_count['critical']
            holdhigh = issues_count['high']
            holdmedium = issues_count['medium']
            holdlow = issues_count['low']
            matched = True

            if ((holdcritical == holdcriticalcount) and (holdhigh == holdhighcount) and (holdmedium == holdmediumcount) and (holdlow == holdlowcount)):
                #print ("Counts Match for applid {} - we are good".format(holdapplicationversionid))
                matched = True
            else:
                # found at least one difference 
                #Get Application Name and Version Name for report from app_vuls_ssc record - has to be at least one
                AppInfo = self.__GetAppVulRecord(holdapplicationversionid)
                holdappname = AppInfo['application_name']
                holdappvername = AppInfo['application_version_name']
                critiss = ''
                showcrit = ''
                highiss = ''
                showhigh = ''
                mediss = ''
                showmed = ''
                lowiss = ''
                showlow = ''

                if holdcritical != holdcriticalcount:
                    critiss = 'C'
                    showcrit = str(holdcriticalcount) + ' vs ' + str(holdcritical)
                if holdhigh != holdhighcount:
                    highiss = 'H'
                    showhigh = str(holdhighcount) + ' vs '  + str(holdhigh)
                if holdmedium != holdmediumcount:
                    mediss = 'M'
                    showmed = str(holdmediumcount) + ' vs ' + str(holdmedium)
                if holdlow != holdlowcount:
                    lowiss = 'L'
                    showlow = str(holdlowcount) + ' vs '  + str(holdlow)
                sep = ';'
               
                print ('{} {} {} {} {} {} {} {} {} {} {} {} {} {} {} {} {} {} {}'.format(holdappname,sep,holdappvername,sep,critiss,sep,showcrit,sep,highiss,sep,showhigh,sep,mediss,sep,showmed,sep,lowiss,sep,showlow))
                

                queueInfo = initBlankQueueObject()
                holdnow = datetime.datetime.now()
                formatnow = holdnow.strftime("%Y-%m-%dT%H:%M:%S")
                queueInfo['processedDateTime'] = datetime.datetime.utcnow().isoformat(), 
        
                queueInfo['projectVersionId'] = holdapplicationversionid
                queueInfo['updateType'] = 'U'
                queueInfo['completedDateTime'] = '1900-01-01T00:00:00.000-0000'
                updateList[holdapplicationversionid] = queueInfo

                if self.__CreateSscUpdateRecords == True:
                    holdqueuecreate = es.postSSCUpdateQueue(queueInfo)
                

                #print('Counts are off - need to check')
                #print ("Elastic applid {} = C {}, H {}, M {}, L {}".format(holdapplicationversionid, holdcriticalcount, holdhighcount, holdmediumcount, holdlowcount))
                #print ("SSC projid {} = C {}, H {}, M {}, L {}".format(projid, holdcritical, holdhigh, holdmedium, holdlow))
        
        print(updateList)
        p.Finish(iTotal, "Complete")
        return True

    def __GetAllSSCAppVerCounts(self):

        # declare a filter query dict object
        sscappvuls_all = {
                  "size": 1,
                  "query": {"match_all": {}},
                  "aggs": {
                    "application_version_id": {
                      "terms": {
                        "field": "application_version_id",
                        "order": {"_key": "asc"},
                        "size": 5000
                        },
                      "aggs": {
                         "Critical": {
                          "sum": {
                            "field": "Critical"
                          }
                        },
                        "High": {
                          "sum": {
                            "field": "High"
                          }
                        },
                        "Medium": {
                          "sum": {
                            "field": "Medium"
                          }
                        },
                        "Low": {
                          "sum": {
                            "field": "Low"
                          }
                        }
                      }
                      }
                    }
                }

        
        result = self.__ElasticClient.RunQuery('app_vuls_active_ssc', sscappvuls_all)

        return result['aggregations']['application_version_id']['buckets']

    
    def __GetAppVulRecord(self, applverid):


        # declare a filter query dict object
        match = {
            "size": 1,
            "query": {
                "bool" : {
                    "must": [{"term": {"application_version_id": applverid}}]
                    }
                }
            }
                
        result = self.__ElasticClient.RunQuery('app_vuls_ssc', match)

        return result['hits']['hits'][0]['_source']

    
class VulComparerException(Exception):
    pass