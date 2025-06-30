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

class SscSnapshotHelper(object):

    def __init__(self, appSettings):
        '''
        Setup the class
        Ensure the mappings are correct
        '''
        if type(appSettings).__name__ != "ApplicationSettings":
            raise TypeError("Type of appSettings must be 'ApplicationSettings'")

        self.app = appSettings.Application
        logging.info("SscSnapshotHelper init")
        
        self.__TmpSnapshotIndex = 'app_version_snapshot_purple'
        self.__SnapshotIndex = 'snapshots_app_monthly_historical'
        self.__SnapshotIndexMapping = "AppVersionSnapshots"

        self.__Es = self.app.GetElasticClient()

        # Create a container to hold the AppVersionIssue records
        self.__AppVersionIssues = {}
        self.__TotalIssuesProcessed = 0

    def RebuildAllRecords(self, startDate):
        '''
        Main loop to create all records from the startDate forward
        Records are created as of the last milisecond of the month, EST
        '''
        try:
            # Maps the temporary index before we move it with a clone which is FAST
            self.__Es.MapIndexWithMapping(self.__TmpSnapshotIndex, self.__Es.GetMapping(self.__SnapshotIndexMapping), True)

            # Convert to utc datetime
            startDateTimeString = f"{startDate}T00:00:00.000000"
            currentMonth = datetime.datetime.strptime(startDateTimeString, "%Y-%m-%dT%H:%M:%S.%f")
            currentMonth = currentMonth.replace(tzinfo = datetime.timezone.utc)
            minDataDate = self.__GetFirstMonthWithData()
            if minDataDate:
                minDataDate = minDataDate.replace(tzinfo = datetime.timezone.utc)
            else:
                minDataDate = currentMonth
                
            tempCurrDate = self.__MyUtcNow()
            #backupTwoMonths = tempCurrDate + datetime.timedelta(days=-60)
            #3-21-24 taking out 2 month backup until V3 is fixed
            backupTwoMonths = tempCurrDate
            
            #while currentMonth < self.__MyUtcNow() backed up by 2 months (60 days):
            while currentMonth < backupTwoMonths:
                logging.info("Rebuilding month %s", currentMonth)
                self.__RebuildMonth(currentMonth, minDataDate)
                days_in_month = calendar.monthrange(currentMonth.year, currentMonth.month)[1]
                currentMonth = currentMonth + datetime.timedelta(days=days_in_month)

            totalSnapshotCount = 0
            for counter in self.__AppVersionIssues.values():
                totalSnapshotCount = totalSnapshotCount + counter['totalSnapshotCount']

            logging.info("Total snapshot count: %s", totalSnapshotCount)
        except Exception as e:
            logging.exception("Error, not moving snapshot data to final index")
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

        #Find the end of the month
        dtNow = self.__MyUtcNow()
        days_in_month = calendar.monthrange(startDateTime.year, startDateTime.month)[1]
        endDateTime = startDateTime + datetime.timedelta(days=days_in_month)
        endDateTime = endDateTime + datetime.timedelta(0,0,-1)
        snapDateTime = endDateTime + datetime.timedelta(0,0,-1)
        skipcurrent = False

        # If the endDateTime is later than now we need to set it to now for the visualizaton to work and show the data
        if endDateTime > dtNow:
            endDateTime = dtNow
            snapDateTime = dtNow
            skipcurrent = False
            #3-21-24 - changed to False to get current month to load
        else:
            if days_in_month == 31:
              snapDateTime = endDateTime + datetime.timedelta(days=-16)
            elif days_in_month == 30:
              snapDateTime = endDateTime + datetime.timedelta(days=-15)
            elif days_in_month == 29:
              snapDateTime = endDateTime + datetime.timedelta(days=-14)
            else:
              snapDateTime = endDateTime + datetime.timedelta(days=-13)
              
        # Reset the counters
        iCount = 0
        iTotal = 0
        for counter in self.__AppVersionIssues.values():
            counter['openedCount'] = 0
            counter['removedCount'] = 0
            counter['saltminer']['snapshot_date'] = snapDateTime
            counter['Last_Updated_Date'] = startDateTime
            iCount = iCount + 1
            iTotal = iTotal + counter['totalSnapshotCount']
            #Leave the total count alone, we use it over and over again.

        types = ["opened", "removed"]
        if skipcurrent == True:
            types = []
        for type in types:
            if minDataDate >= endDateTime:
                continue
            scroller = self.__GetVulsByDateRange(startDateTime, endDateTime, type)
            while len(scroller.Results):
                for dto in scroller.Results:
                    vul = dto['_source']
                    self.__TotalIssuesProcessed = self.__TotalIssuesProcessed + 1
                    key = f"{vul['saltminer']['asset']['id']}-{vul['vulnerability']['name']}-{vul['vulnerability']['scanner']['assessment_type']}-{vul['vulnerability']['severity']}"
                
                    if key in self.__AppVersionIssues.keys():
                        counter = self.__AppVersionIssues[key]
                    else:
                        #Didn't find it, add the object
                        counter = vul
                        '''try:
                            del counter['saltminer']['asset']['id']
                        except:
                            pass
                        try:
                            del counter['saltminer']['asset']['is_saltminer_source']
                        except:
                            pass'''
                        try:
                            del counter['saltminer']['asset']['scan_count']
                        except:
                            pass
                        try:
                            del counter['saltminer']['attributes']['RMYearMonth']
                        except:
                            pass
                        '''try:
                            del counter['saltminer']['asset']['source_id']
                        except:
                            pass
                        try:
                            del counter['saltminer']['asset_type']
                        except:
                            pass
                        try:
                            del counter['saltminer']['attributes']
                        except:
                            pass
                        try:
                            del counter['saltminer']['engagement']
                        except:
                            pass
                        try:
                            del counter['saltminer']['is_histoical']
                        except:
                            pass
                        try:
                            del counter['saltminer']['is_vulnerability']
                        except:
                            pass'''
                        try:
                            del counter['saltminer']['scan_id']
                        except:
                            pass
                        try:
                            del counter['saltminer']['scan']['id']
                        except:
                            pass
                        try:
                            del counter['saltminer']['scan']['scan_date']
                        except:
                            pass
                        try:
                            del counter['saltminer']['scan']['report_id']
                        except:
                            pass
                        '''try:
                            del counter['saltminer']['scan']
                        except:
                            pass
                        try:
                            del counter['saltminer']['source']['confidence']
                        except:
                            pass
                        try:
                            del counter['saltminer']['source']['impact']
                        except:
                            pass
                        try:
                            del counter['saltminer']['source']['issue_status']
                        except:
                            pass
                        try:
                            del counter['saltminer']['source']['likelihood']
                        except:
                            pass
                        try:
                            del counter['saltminer']['source_id']
                        except:
                            pass
                        try:
                            del counter['saltminer']['tags']
                        except:
                            pass'''
                        try:
                            del counter['vulnerability']['audit']
                        except:
                            pass
                        try:
                            del counter['vulnerability']['description']
                        except:
                            pass
                        try:
                            del counter['vulnerability']['enumeration']
                        except:
                            pass
                        try:
                            del counter['vulnerability']['found_date']
                        except:
                            pass
                        try:
                            del counter['vulnerability']['is_active']
                        except:
                            pass
                        try:
                            del counter['vulnerability']['is_filtered']
                        except:
                            pass
                        try:
                            del counter['vulnerability']['location']
                        except:
                            pass
                        try:
                            del counter['vulnerability']['location_full']
                        except:
                            pass
                        try:
                            del counter['vulnerability']['reference']
                        except:
                            pass
                        try:
                            del counter['vulnerability']['removed_date']
                        except:
                            pass
                        try:
                            del counter['vulnerability']['report_id']
                        except:
                            pass
                        try:
                            del counter['vulnerability']['scanner']['api_url']
                        except:
                            pass
                        try:
                            del counter['vulnerability']['scanner']['gui_url']
                        except:
                            pass
                        try:
                            del counter['vulnerability']['scanner']['id']
                        except:
                            pass
                    
                        counter['saltminer']['critical'] = 0
                        counter['saltminer']['high'] = 0
                        counter['saltminer']['medium'] = 0
                        counter['saltminer']['low'] = 0
                        counter['openedCount'] = 0
                        counter['removedCount'] = 0
                        counter['totalSnapshotCount'] = 0
                        counter['saltminer']['snapshot_date'] = snapDateTime
                        counter['saltminer']['scan']['assessment_type'] = counter['vulnerability']['scanner']['assessment_type']
                        counter['saltminer']['scan']['product'] = counter['vulnerability']['scanner']['product']
                        counter['saltminer']['scan']['product_type'] = counter['vulnerability']['scanner']['product']
                        counter['saltminer']['scan']['vendor'] = counter['vulnerability']['scanner']['vendor']
                        counter['Last_Updated_Date'] = startDateTime
                        self.__AppVersionIssues[key] = counter
                
                    #These are in the opened so need to increment the opened.
                    if type == "opened":
                    
                        if vul['vulnerability']['severity'] == 'Critical':
                            counter['saltminer']['critical'] = counter['saltminer']['critical'] + 1
                        if vul['vulnerability']['severity'] == 'High':
                            counter['saltminer']['high'] = counter['saltminer']['high'] + 1
                        if vul['vulnerability']['severity'] == 'Medium':
                            counter['saltminer']['medium'] = counter['saltminer']['medium'] + 1
                        if vul['vulnerability']['severity'] == 'Low':
                            counter['saltminer']['low'] = counter['saltminer']['low'] + 1

                        counter['openedCount'] = counter['openedCount'] + 1
                        counter['totalSnapshotCount'] = counter['totalSnapshotCount'] + 1
                        counter['Last_Updated_Date'] = startDateTime
                    elif type== "removed":
                        if (vul['vulnerability']['is_suppressed'] == True and vul['vulnerability']['is_removed'] == False):
                            pass
                        else:
                            if vul['vulnerability']['severity'] == 'Critical':
                                counter['saltminer']['critical'] = counter['saltminer']['critical'] - 1
                            if vul['vulnerability']['severity'] == 'High':
                                counter['saltminer']['high'] = counter['saltminer']['high'] - 1
                            if vul['vulnerability']['severity'] == 'Medium':
                                counter['saltminer']['medium'] = counter['saltminer']['medium'] - 1
                            if vul['vulnerability']['severity'] == 'Low':
                                counter['saltminer']['low'] = counter['saltminer']['low'] - 1 
                            counter['removedCount'] = counter['removedCount'] + 1
                            counter['totalSnapshotCount'] = counter['totalSnapshotCount'] - 1
                            counter['Last_Updated_Date'] = startDateTime
                    else:
                        raise Exception(f'Unknown type passed in {type}')
                    
                # end for
                scroller.GetNext()
        logging.info(f"Current types:{iCount}, current total snapshot count:{iTotal}, processed:{self.__TotalIssuesProcessed}")

        allDocsToInsert = []
        for summaryIssue in self.__AppVersionIssues.values():
            #Write the records for the current month, for all vulnerability types 
            # that had somethign opened, removed or there is a non-zero total.
            if summaryIssue['openedCount'] > 0 or summaryIssue['removedCount'] > 0 or summaryIssue['totalSnapshotCount'] != 0 and skipcurrent == False:
                #write the record to the index.
                
                

                bulkDocument = {
                    '_index': self.__TmpSnapshotIndex,
                    '_id': uuid.uuid4(),
                    '_source': summaryIssue
                    }
                try:
                    allDocsToInsert.append(bulkDocument)
                except Exception:
                    logging.exception('Bulk doc append failure')

        self.__Es.BulkInsert(allDocsToInsert)


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

    def __GetVulsByDateRange(self, startDateTime, endDateTime, type):
        
        # Dates are expected as utc

        openQuery = {
          "query": {
            "bool": {
              "must": [
                { "term": { "vulnerability.is_filtered": { "value": False } } },
                { "range": { "vulnerability.found_date": { 
                  "gte": startDateTime.isoformat(),
                  "lt": endDateTime.isoformat()
                  } }
                }
              ]
            }
          }
        }
        
        removedQuery = {
          "query": {
            "bool": {
              "must": [
                { "term": { "vulnerability.is_filtered": { "value": False } } },
                { "term": { "vulnerability.is_removed": { "value": True } } },
                { "range": { "vulnerability.removed_date": { 
                  "gte": startDateTime.isoformat(),
                  "lt": endDateTime.isoformat()
                  } }
                }
              ]
            }
          }
        }

        try:

            if type == "opened":
                return self.__Es.SearchScroll("issues_app_saltworks*", openQuery, 1000, None)
            elif type == "removed":
                return self.__Es.SearchScroll("issues_app_saltworks*", removedQuery, 1000, None)
            else:
                raise Exception(f'Unknown type passed in {type}')
        except:
             raise
        
