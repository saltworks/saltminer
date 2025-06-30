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

import logging
import calendar
import datetime
import uuid

#NeedToAdd
from Utility.ProgressLogger import *
from Core.Application import Application

# 10/12/21 DH
# Top level program designed to populate issue history for app_vuls*
# Originally PopulateAppIssueHistory.py, class PopulateAppIssueHistory

# 1/26/22 TD
# Refactor complete, not tested

class PopulateAppIssueHistory(object):
    def __init__(self, appSettings):
        '''
        Setup the class
        Ensure the mappings are correct
        '''
        self.app = Application()
        logging.info("Populate AppIssueHistory starting")
        
        self.snapshotIndex = 'app_version_snapshot_blue'

        self.__Es = appSettings.Application.GetElasticClient()

        #NeedToAdd
        self.progressLogger = ProgressLogger(self.__Es)
        self.progressLogger.Start("PopulateAppIssueHistory",0)

        
        #print("sleeping")
        #time.sleep(10)

        #Create a container to hold the AppVersionIssue records
        self.appVersionIssues = {}
        self.totalIssuesProcessed = 0

        #Create an object to hold app_
        self.appVersions = {}
 
        
    def rebuildAllRecords(self, startDate):
        '''
        Main loop to create all records from the startDate forward
        Records are created as of the last milisecond of the month, EST
        '''
        
        try:
            #Maps the temporary index before we move it with a clone which is FAST
            self.__Es.MapIndexWithMapping(self.snapshotIndex, self.__Es.GetMapping("app_version_snapshots"), True)

            # Convert to datetime
            startDateTimeString = f"{startDate}T00:00:00.000000"
            currentMonth = datetime.datetime.strptime(startDateTimeString, "%Y-%m-%dT%H:%M:%S.%f")
    
            while currentMonth < datetime.datetime.now():
                print(currentMonth)
                self.rebuildMonth(currentMonth)
                days_in_month = calendar.monthrange(currentMonth.year, currentMonth.month)[1]
                currentMonth = currentMonth + datetime.timedelta(days=days_in_month)

            totalSnapshotCount = 0
            for counter in self.appVersionIssues.values():
                totalSnapshotCount = totalSnapshotCount + counter['totalSnapshotCount']

            print(totalSnapshotCount)
        except Exception as e:
            print("Error, not moving data to production")
            print(e)
            return
            
        _settings = {"index": {"blocks": {"write": True}}}
        self.__Es.PutSettings(self.snapshotIndex, _settings)
        self.__Es.DeleteIndex('app_version_snapshots')
        self.__Es.CloneIndex(self.snapshotIndex, 'app_version_snapshots')
        _settings = {"index": {"blocks": {"write": False}}}
        self.__Es.PutSettings(self.snapshotIndex, _settings)
        

    def rebuildMonth(self, startDateTime):
        '''
        Loop to create summary records for the current month.
        Initially we reset the openedCount and removedCount to reset everything.
        Finally, at the end of the loop we write all the summary records.
        '''

        #Find the end of the month
        days_in_month = calendar.monthrange(startDateTime.year, startDateTime.month)[1]
        endDateTime = startDateTime + datetime.timedelta(days=days_in_month)
        endDateTime = endDateTime + datetime.timedelta(0,0,-1)

        # If the endDateTime is later than now we need to set it to now for the visualizaton to work and show the data
        if endDateTime > datetime.datetime.now():
            endDateTime = datetime.datetime.now()


        #Rest the counters
        iCount = 0
        iTotal = 0
        for counter in self.appVersionIssues.values():
            counter['openedCount'] = 0
            counter['removedCount'] = 0
            counter['snapshot_date'] = endDateTime
            iCount = iCount + 1
            iTotal = iTotal + counter['totalSnapshotCount']
            #Leave the total count alone, we use it over and over again.



        types = ["opened", "removed"]

        for type in types:

            vuls = self.GetVulsByDateRange(startDateTime, endDateTime, type)

            for vul in vuls.values():
                self.totalIssuesProcessed = self.totalIssuesProcessed + 1
                
                #Find the aggrigator in self.appVersionVul = {}
                key = f"{vul['application_version_id']}-{vul['name']}"
                #print(key)
                try:
                    counter = self.appVersionIssues[key]
                    #print(f"{key} - {counter['totalCount']}")
                    
                except KeyError:
                    #Didn't find it, add the object
                    counter = vul

                    del counter['location']

                    counter['openedCount'] = 0
                    counter['removedCount'] = 0
                    counter['totalSnapshotCount'] = 0
                    counter['snapshot_date'] = endDateTime
                    self.appVersionIssues[key] = counter
                #These are in the opened so need to increment the opened.
                if type == "opened": 
                    counter['openedCount'] = counter['openedCount'] + 1
                    counter['totalSnapshotCount'] = counter['totalSnapshotCount'] + 1
                elif type== "removed":
                    counter['removedCount'] = counter['removedCount'] + 1
                    counter['totalSnapshotCount'] = counter['totalSnapshotCount'] - 1
                else:
                    raise Exception(f'Unknown type passed in {type}')
                

                #if vul['application_version_id'] == 10001:
                #    print(f"{startDateTime} - {endDateTime} - "

        print(f"Current types:{iCount}, current total snapshot count:{iTotal}, processed:{self.totalIssuesProcessed}")

        allDocsToInsert = []
        for summaryIssue in self.appVersionIssues.values():
            #Write the records for the current month, for all vulnerability types 
            # that had somethign opened, removed or there is a non-zero total.
            if summaryIssue['openedCount'] > 0 or summaryIssue['removedCount'] > 0 or summaryIssue['totalSnapshotCount'] != 0:
                #write the record to the index.

                bulkDocument = {
                    '_index': self.snapshotIndex,
                    '_type': '_doc',
                    '_id': uuid.uuid4(),
                    '_source': summaryIssue
                    }
                try:
                    allDocsToInsert.append(bulkDocument)
                except exception:
                    print('Oops')

            #self.__Es.Index("fulton_app_vul_history", summary)

        self.__Es.BulkInsert(allDocsToInsert)


    def GetVulsByDateRange(self, startDateTime, endDateTime, type):
        
        #First get the open ones

        openQuery = {
          "query": {
            "bool": {
              "must": [],
              "filter": [
                {
                  "match_all": {}
                },
                {
                  "match_phrase": {
                    "hidden": False
                  }
                }, 
                {
                  "match_phrase": {
                    "suppressed": False
                  }
                },
                {
                  "match_phrase": {
                    "saltminer.is_vulnerability": True
                  }
                },
                {
                  "range": {
                    "found_date": {
                      "gte": startDateTime,
                      "lt": endDateTime
                    }
                  }
                }
              ],
              "should": [],
              "must_not": []
            }
          }
        }

        removedQuery = {
          "query": {
            "bool": {
              "must": [],
              "filter": [
                {
                  "match_all": {}
                },
                {
                  "match_phrase": {
                    "hidden": False
                  }
                },
                {
                  "match_phrase": {
                    "suppressed": False
                  }
                },
                {
                  "match_phrase": {
                    "saltminer.is_vulnerability": True
                  }
                },
                {
                  "match_phrase": {
                    "removed": True
                  }
                },

                {
                  "range": {
                    "removed_date": {
                      "gte": startDateTime,
                      "lt": endDateTime
                    }
                  }
                }
              ],
              "should": [],
              "must_not": []
            }
          }
        }

        if type == "opened":

            issues = self.__Es.SearchWithCursor("_id", "app_vuls*", openQuery, 1000)
        elif type == "removed":
            issues = self.__Es.SearchWithCursor("_id", "app_vuls*", removedQuery, 1000)
        else:
            raise Exception(f'Unknown type passed in {type}')


        return issues

#init the Populate class
populate = PopulateAppIssueHistory()
populate.rebuildAllRecords('2017-01-01')
