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

import logging
import calendar
import datetime
import uuid

class SscScanSnapshotHelper(object):
    
    def __init__(self, appSettings):
        '''
        Setup the class
        Ensure the mappings are correct
        '''
        if type(appSettings).__name__ != "ApplicationSettings":
            raise TypeError("Type of appSettings must be 'ApplicationSettings'")

        app = appSettings.Application
        logging.info("SscScanSnapshotHelper init")
        
        self.__TmpSnapshotIndex = 'app_version_snapshot_violet'
        self.__SnapshotIndex = 'scan_snapshots_app_historical'

        self.__Es = app.GetElasticClient()

        #Create a container to hold the AppVersionIssue records
        self.__AppVersionScans = {}
        self.__TotalIssuesProcessed = 0

        #Create an object to hold app_
        self.__Previous = {}

        
    def RebuildScanHistory(self, startDate):
        '''
        Main loop to create all records from the startDate forward
        Records are created as of the last millisecond of the month, EST
        '''

        try:
            #Maps the temporary index before we move it with a clone which is FAST
            self.__Es.MapIndexWithMapping(self.__TmpSnapshotIndex, self.__Es.GetMapping("sscprojscans"), True)

            # Convert to datetime
            startDateTimeString = f"{startDate}T00:00:00.000000"
            currentMonth = datetime.datetime.strptime(startDateTimeString, "%Y-%m-%dT%H:%M:%S.%f").replace(tzinfo = datetime.timezone.utc)
            minDataDate = self.__GetFirstMonthWithData()
            if not minDataDate:
                minDataDate = currentMonth
    
            while currentMonth < self.__MyUtcNow():
                logging.info("Rebuilding month: %s", currentMonth)
                self.__RebuildMonth(currentMonth, minDataDate)
                days_in_month = calendar.monthrange(currentMonth.year, currentMonth.month)[1]
                currentMonth = currentMonth + datetime.timedelta(days=days_in_month)

            totalSnapshotCount = 0
            for counter in self.__AppVersionScans.values():
                totalSnapshotCount = totalSnapshotCount + counter['totalSnapshotCount']

            logging.info("Total snapshot count: %s", totalSnapshotCount)
        except Exception as e:
            logging.exception("Error, not moving scan history data to final index")
            return
            
        _settings = {"index": {"blocks": {"write": True}}}
        self.__Es.PutSettings(self.__TmpSnapshotIndex, _settings)
        self.__Es.DeleteIndex(self.__SnapshotIndex)
        self.__Es.CloneIndex(self.__TmpSnapshotIndex, self.__SnapshotIndex)
        _settings = {"index": {"blocks": {"write": False}}}
        self.__Es.PutSettings(self.__TmpSnapshotIndex, _settings)
        

    def __MyUtcNow(self):
        return datetime.datetime.utcnow().replace(tzinfo = datetime.timezone.utc)

    def __RebuildMonth(self, startDateTime, minDataDate):
        '''
        Loop to create summary records for the current month.
        Initially we reset the openedCount and removedCount to reset everything.
        Finally, at the end of the loop we write all the summary records.
        '''

        # Find the end of the month
        days_in_month = calendar.monthrange(startDateTime.year, startDateTime.month)[1]
        endDateTime = startDateTime + datetime.timedelta(days=days_in_month)
        endDateTime = endDateTime + datetime.timedelta(0,0,-1)
        snapDateTime = endDateTime + datetime.timedelta(0,0,-1)
        dtNow = self.__MyUtcNow()

        # If the endDateTime is later than now we need to set it to now for the visualizaton to work and show the data
        if endDateTime > dtNow:
            endDateTime = dtNow
            snapDateTime = dtNow
        else:
            if days_in_month == 31:
              snapDateTime = endDateTime + datetime.timedelta(days=-16)
            elif days_in_month ==30:
              snapDateTime = endDateTime + datetime.timedelta(days=-15)
            else:
              snapDateTime = endDateTime + datetime.timedelta(days=-13)
              
        #print(startDateTime)
        #print(endDateTime)
        # Reset the counters
        iCount = 0
        iTotal = 0
        for counter in self.__AppVersionScans.values():
            counter['snapshot_date'] = snapDateTime
            counter['Last_Updated_Date'] = startDateTime
            iCount = iCount + 1
            iTotal = iTotal + counter['totalSnapshotCount']
            counter['accumLOC'] = 0
            counter['totalLOC'] = 0
            counter['totalSnapshotCount'] = 0

            # Leave the total count alone, we use it over and over again.

        types = ["completed"]

        for type in types:
            if minDataDate >= endDateTime:
                continue
            scroller = self.__GetScansByDateRange(startDateTime, endDateTime, type)
            while len(scroller.Results):
                for dto in scroller.Results:
                    scan = dto['_source']
                    self.__TotalIssuesProcessed = self.__TotalIssuesProcessed + 1
                
                    #Find the aggregator in self.appVersionVul = {}
                    key = f"{scan['projectVersionId']}-{scan['type']}"
                
                    if key in self.__AppVersionScans.keys():
                        counter = self.__AppVersionScans[key]
                    else:
                        #Didn't find it, add the object
                        counter = scan
                        counter['accumLOC'] = 0
                        counter['totalSnapshotCount'] = 0
                        counter['snapshot_date'] = snapDateTime
                        counter['Last_Updated_Date'] = startDateTime
                        self.__AppVersionScans[key] = counter

                    #These are in the opened so need to increment the opened.
                    if type == "completed":
                        #print(scan['totalLOC'])
                        counter['accumLOC'] = counter['accumLOC'] + scan['totalLOC']
                        counter['totalLOC'] = counter['totalLOC'] + scan['totalLOC']                   
                        counter['totalSnapshotCount'] = counter['totalSnapshotCount'] + 1
                        counter['Last_Updated_Date'] = startDateTime
                    else:
                        raise Exception(f'Unknown type passed in {type}')
                # end for                
                #if vul['application_version_id'] == 10001:
                #    print(f"{startDateTime} - {endDateTime} - "
                scroller.GetNext()

        logging.info(f"Current types:{iCount}, current total scan snapshot count:{iTotal}, processed:{self.__TotalIssuesProcessed}")

        allDocsToInsert = []
        for summaryIssue in self.__AppVersionScans.values():
            #Write the records for the current month, for all vulnerability types 
            # that had somethign opened, removed or there is a non-zero total.

            summaryIssue['Total'] = 0 # in case no data found in next step

            # This pulls counts using an aggregate query and adds them to "summaryIssue"
            self.__GetVulCountsByIdAndDateRange(startDateTime, endDateTime, summaryIssue)

            # TO BE REMOVED SOONISH, REPLACED BY THE LINE ABOVE...
            #holdaccumcritical = 0
            #vulscritical = self.__GetVulCountsByDateRange(startDateTime, endDateTime, summaryIssue['projectVersionId'], 'Critical')
            #for vulcrit in vulscritical.values():
            #    #print('found critical')
            #    holdaccumcritical = holdaccumcritical + vulcrit['saltminer']['critical']
            #summaryIssue['Critical'] = holdaccumcritical

            #holdaccumhigh = 0
            #vulshigh = self.__GetVulCountsByDateRange(startDateTime, endDateTime, summaryIssue['projectVersionId'], 'High')
            #for vulhigh in vulshigh.values():
            #    #print('found high')
            #    holdaccumhigh = holdaccumhigh + vulhigh['saltminer']['high']
            #summaryIssue['High'] = holdaccumhigh  

            #holdaccummedium = 0
            #vulsmedium = self.__GetVulCountsByDateRange(startDateTime, endDateTime, summaryIssue['projectVersionId'], 'Medium')
            #for vulmed in vulsmedium.values():
            #    #print('found medium')
            #    holdaccummedium = holdaccummedium + vulmed['saltminer']['medium']
            #summaryIssue['Medium'] = holdaccummedium

            #holdaccumlow = 0
            #vulslow = self.__GetVulCountsByDateRange(startDateTime, endDateTime, summaryIssue['projectVersionId'], 'Low')
            #for vullow in vulslow.values():
            #    #print('found low')
            #    holdaccumlow = holdaccumlow + vullow['saltminer']['low']
            #summaryIssue['Low'] = holdaccumlow

            #holdaccumtotal = holdaccumcritical + holdaccumhigh + holdaccummedium + holdaccumlow
            #summaryIssue['Total'] = holdaccumtotal

            if summaryIssue['totalSnapshotCount'] != 0:
                summaryIssue['averageLOC'] = summaryIssue['accumLOC'] / summaryIssue['totalSnapshotCount']
                prevkey = summaryIssue['projectVersionId']
                self.__Previous[prevkey] = summaryIssue['averageLOC']

            else:
                prevkey = summaryIssue['projectVersionId']
                try:
                    summaryIssue['averageLOC'] = self.__Previous[prevkey]
                except KeyError:
                    summaryIssue['averageLOC'] = 0

            if summaryIssue['averageLOC'] != 0:
                summaryIssue['density1k'] = (summaryIssue['Total'] / summaryIssue['averageLOC']) * 1000
                summaryIssue['density10k'] = (summaryIssue['Total'] / summaryIssue['averageLOC']) * 10000
                summaryIssue['density100k'] = (summaryIssue['Total'] / summaryIssue['averageLOC']) * 100000    
            else:
                summaryIssue['density1k'] = 0 
                summaryIssue['density10k'] = 0 
                summaryIssue['density100k'] = 0 


            #if summaryIssue['openedCount'] > 0 or summaryIssue['removedCount'] > 0 or summaryIssue['totalSnapshotCount'] != 0:
                #write the record to the index.

            bulkDocument = {
                '_index': self.__TmpSnapshotIndex,
                '_id': uuid.uuid4(),
                '_source': summaryIssue
                }
            try:
                allDocsToInsert.append(bulkDocument)
            except Exception:
                logging.exception('Bulk list append failed')

        self.__Es.BulkInsert(allDocsToInsert)


    def __GetScansByDateRange(self, startDateTime, endDateTime, type):
        '''
        Return completed scans for given date range
        Assume dates passed are UTC
        '''
        completedQuery = {
          "_source": ["projectVersionId", "type", "totalLOC"],
          "query": {
            "range": {
              "lastScanDate": {
                "gte": startDateTime.isoformat(),
                "lte": endDateTime.isoformat()
              }
            }
          }
        }

        if type == "completed":
            return self.__Es.SearchScroll("sscprojscans*", completedQuery, 1000, None)
        else:
            raise Exception(f'Unknown type passed in {type}')


    def __GetFirstMonthWithData(self):
        qry = {
          "aggs": {
            "found": { "min": { "field": "vulnerability.found_date" } }, 
            "removed": { "min": { "field": "vulnerability.removed_date" } }
          }, "size": 0
        }
        r = self.__Es.Search("issues_app_saltworks*", queryBody=qry, size=0, navToData=False)
        if r and 'aggregations' in r.keys():
            found = None
            removed = None
            try:
                found = datetime.datetime.strptime(r['aggregations']['found']['value_as_string'], "%Y-%m-%dT%H:%M:%S.%f%z")
            except Exception as e:
                logging.debug("Failed to retrieve min found date.", exc_info = e)
            try:
                removed = datetime.datetime.strptime(r['aggregations']['removed']['value_as_string'], "%Y-%m-%dT%H:%M:%S.%f%z")
            except Exception as e:
                logging.debug("Failed to retrieve min removed date.", exc_info = e)
            if not found and not removed:
                return None
            if not found:
                return removed
            if not removed:
                return found
            return found if removed > found else removed


    def __GetVulCountsByIdAndDateRange(self, startDateTime, endDateTime, pvDoc):
        #holdaccumcritical = 0
        #vulscritical = self.__GetVulCountsByDateRange(startDateTime, endDateTime, summaryIssue['projectVersionId'], 'Critical')
        #for vulcrit in vulscritical.values():
        #    #print('found critical')
        #    holdaccumcritical = holdaccumcritical + vulcrit['saltminer']['critical']
        #summaryIssue['Critical'] = holdaccumcritical
        query = {
          "aggs": {
            "severity": {
              "terms": {
                "field": "vulnerability.source_severity.keyword",
                "size": 100
              }
            }
          },
          "query": {
            "bool": {
              "must": [
                { "term": { "saltminer.asset.version_id": { "value": pvDoc['projectVersionId'] } } },
                { "range": { "saltminer.snapshot_date": { 
                  "gte": startDateTime.isoformat(),
                  "lt": endDateTime.isoformat()
                  } }
                }
              ]
            }
          },
          "size": 0
        }
        r = self.__Es.Search("snapshots_app_monthly_historical*", query, 0, navToData=False)
        buckets = self.__GetPathValue(r, "aggregations.severity.buckets")
        if not buckets:
            return None
        t = 0
        for b in buckets:
            pvDoc[b['key']] = b['doc_count']
            t += b['doc_count']
        pvDoc['Total'] = t
        return pvDoc

    def __GetPathValue(self, obj, path, nullVal=None):
        tmp = obj
        for pth in path.split("."):
            if pth not in tmp.keys():
                return nullVal
            tmp = tmp[pth]
        return tmp

    # REPLACED BY __GetVulCountsByIdAndDateRange ABOVE, TO BE REMOVED SOONISH...
    #def __GetVulCountsByDateRange(self, startDateTime, endDateTime, assetid, severity):
    #    vulsQuery = {
    #      "query": {
    #        "bool": {
    #          "must": [
    #            { "term": { "saltminer.asset.version_id": { "value": assetid } } },
    #            { "term": { "vulnerability.source_severity": { "value": severity } } },
    #            { "range": { "saltminer.snapshot_date": { 
    #              "gte": startDateTime.isoformat(),
    #              "lt": endDateTime.isoformat()
    #              } }
    #            }
    #          ]
    #        }
    #      }
    #    }
    #    return self.__Es.SearchScroll("snapshots_app_monthly_historical*", vulsQuery, 1000, None)
