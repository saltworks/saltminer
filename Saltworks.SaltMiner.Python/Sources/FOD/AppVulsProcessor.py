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

# 10/8/2021 TD
# Originally AppVulsFOD.py and class AppVulsFOD.

import datetime
import logging
import time
from dateutil.parser import parse as dtparse

from Utility.CancelTracker import CancelTracker
from Utility.DImport import DImport
from Utility.ProgressLogger import *
from Utility.SmApiClient import SmApiClient
from Utility.UpdateQueueHelper import UpdateQueueHelper
from Core.FodClient import FodClient

class AppVulsProcessor(object):
    """ App vulnerability processor for FOD """

    def __init__(self, appSettings, sourceName, smv3ConfigName="SMv3", mainConfigName="Main"):

        if type(appSettings).__name__ != "ApplicationSettings":
            raise TypeError("Type of appSettings must be 'ApplicationSettings'")
        if not smv3ConfigName or not smv3ConfigName in appSettings.GetConfigNames():
            raise AppVulsFODException(f"Invalid or missing configuration for name '{smv3ConfigName}'")
        if not mainConfigName or not mainConfigName in appSettings.GetConfigNames():
            raise AppVulsFODException(f"Invalid or missing configuration for name '{mainConfigName}'")

        self.__Es = appSettings.Application.GetElasticClient()
        self.__Fod = FodClient(appSettings, sourceName)
        self.__Attributes = appSettings.Get(mainConfigName, 'Attributes')
        self.__UpdateQHelper = UpdateQueueHelper(appSettings, sourceName)
        self.__NullUnsetAttributes = appSettings.GetSource(sourceName, "NullUnsetAttributes", True)
        self.__SourceName = sourceName
        self.__SourceNameField = "sourceName"
        logging.info(f"Unset attributes will {'be' if self.__NullUnsetAttributes else 'not be'} set to null (control with NullUnsetAttributes setting in source config).")
        self.FOD_UNSET_VALUE = "(Not Set)"

        #
        # SM API Integration
        # Setup API Client enable switch
        # Create SaltMiner API Client class (attempts to connect upon creation)
        #
        self.__SmApiClientEnabled = appSettings.Get(smv3ConfigName, "ApiClientEnabled", False)
        if self.__SmApiClientEnabled:
            self.__SmApiClient = SmApiClient(appSettings, sourceName, smv3ConfigName)
        self.__DisableSM2Indices = appSettings.Get(sourceName, "DisableSM2Indices", False)
        
        clientCode = appSettings.Get(mainConfigName, 'CustomerCode', 'SW')
        
        appVulsFodCustomFactory = DImport.Import(f"AppVulsCustom.AppVulsFodCustom{clientCode}", "AppVulsFodCustom", "AppVulsCustom")
        self.__AppVulsFodCustom = appVulsFodCustomFactory(appSettings, sourceName)
        
        appVulsCustomFactory = DImport.Import(f"AppVulsCustom.AppVulsCustom{clientCode}", "AppVulsCustom", "AppVulsCustom")
        self.__AppVulsCustom = appVulsCustomFactory(appSettings, sourceName)

        self.__AssessmentTypeMap = appSettings.GetSource(sourceName, 'AssessmentTypeMap', {})

        if not len(self.__AssessmentTypeMap.keys()):
            logging.warn("Assessment type map missing from source name '%s'.  This will cause all scans to be considered assessment type 'Unknown'.", sourceName)

        logging.info("AppVulsFOD init complete, connected to Elastic")
    
       
    def MapAppSecVuls(self, Force):
        '''Create the Indices whith proper mappings if they don't already exist.'''

        '''
            Note that we are using fields and following the Elastic ECS format
                https://www.elastic.co/guide/en/elasticsearch/reference/7.6/multi-fields.html

                https://www.elastic.co/guide/en/ecs/current/ecs-conventions.html

            Canonical field: myfield is keyword
            Multi-field: myfield.text is text

        '''

        #
        # Map the app_vuls_fod index but don't overwrite it if it already exists.
        #
        _mapping = self.__Es.GetMapping('app_vuls')

        #
        # Now add the custom attributes to the mapping so they are of the correct data type.
        #
        #_attributes = self.__Attributes
        #for MapKey in _attributes:
        #    mapKeyUnderline = MapKey.replace(' ', '_')
        #    _mapping['mappings']['properties'][mapKeyUnderline] = _attributes[MapKey]
       
        self.__Es.MapIndexWithMapping("app_vuls_fod", _mapping, Force)

        
        activeFilter = {"filter" : { "term" : { "active": True } }}
        self.__Es.PutAlias('app_vuls_fod', 'app_vuls_active_fod', activeFilter, Force)
        self.__Es.PutAlias('app_vuls_fod', 'app_vuls_all_fod', '', Force)
         

        # Now mapp the app_scan_history_fod index
        _mapping = self.__Es.GetMapping('app_scan_history')
        #for MapKey in _attributes:
        #    mapKeyUnderline = MapKey.replace(' ', '_')
        #    _mapping['mappings']['properties'][mapKeyUnderline] = _attributes[MapKey]
       
        self.__Es.MapIndexWithMapping("app_scan_history_fod", _mapping, Force)

        logging.debug('Mapping of indices complete.')
 

    def PopulateVuls(self, cleanupAfter=True):
        '''
            Main function that will drive the population of vulnerabilites.

            Overall Process:

            FODReleases = ...Get all Releases into memory, filtered by current source name

            Get list of docs from fodupdatequeue index where sourceName == current source name, ordered by processedDate
            foreach doc
                Delete any existing records
                Flush ElasticSearch
                get the issues from fodrelissues
                populate the app_vuls table

        '''
        # Get all Releases into memory
        fodReleases = self._GetAllFodReleases()

        #Ensure the mappings exist and create them if they don't
        logging.info('Mapping indices as needed...')
        self.MapAppSecVuls(False)

        # Remove app versions marked for delete
        b = 1
        c = 0
        uq, total = self.__UpdateQHelper.GetUpdateQueueBatch(["D"], sourceName=self.__SourceName)
        while uq and len(uq) > 0:
            for qitem in uq:
                if c % 10 == 0:
                    logging.info("Retiring app version(s), %s of %s (batch %s)", c, total, b)
                qid = qitem['id'] if qitem and 'id' in qitem.keys() else None
                if qid:
                    logging.debug("Deleting v2 data for app version id %s", qid)
                    self.__DeleteStuff(qid)
                else:
                    logging.warning("Skipping qitem for deleting v2 data, invalid. qitem: %s", qitem)
                self.__UpdateQHelper.CompleteUpdateQueue(qid, ["D"], sourceName=self.__SourceName)
                c += 1
            # next batch
            uq, total = self.__UpdateQHelper.GetUpdateQueueBatch(["D"], sourceName=self.__SourceName)
            b += 1
        if c > 0:
            logging.info("Waiting 5 sec..")
            time.sleep(5)

        # Create an object so we can remember if we have run an import already.
        # If we get duplicates it would be because we got ahead of the updates while in process, 
        # and we can wait a few sec for it to catch up. 
        updated = []

        # Priming read
        updateBatch, total = self.__UpdateQHelper.GetUpdateQueueBatch(["U", "A"], sourceName=self.__SourceName)
        if not total:
            total = 0
        #
        # Setup the progress logger
        #
        p = ProgressLogger(self.__Es)
        p.Start("PopulateVuls", total, "PopulateVuls Status")
        p.Progress(0, 'Starting PopulateVuls')
        c = 1
        batch = 1
        while updateBatch and len(updateBatch) > 0:
            #
            # Run through the update queue and update the main app_vuls_ssc indices
            #
            for qitem in updateBatch:
                qid = qitem['id']
                p.Progress(c, f"Updating app version {qid}, {c} of {total} (batch {b})")

                if qid in updated:
                    # We already processed this PVID - might be ahead of the db so let's pause
                    logging.debug('Skipping %s, already processed.  Pausing for 5 sec to let the db catch up...', qid)
                    time.sleep(5)
                else:
                    self.__ProcessUpdate(qid, fodReleases)

                self.__UpdateQHelper.CompleteUpdateQueue(qid, ["U", "A"], sourceName=self.__SourceName)
                updated.append(qid)
                c += 1
            # end for

            #
            # Get next batch (throw away the total, don't need)
            #
            updateBatch = self.__UpdateQHelper.GetUpdateQueueBatch(["U", "A"], True, self.__SourceName)[0]
            batch += 1
            #
            # SM API Integration
            # Finalize batch items for issues and complete queue scans
            #
            if self.__SmApiClientEnabled:
                self.__SmApiClient.FinalizeEverything()
        # end while

        p.Finish(c, "App version updates complete.")
        if cleanupAfter:
            self.Cleanup()


    def PopulateVulsOne(self, avid, cleanupAfter=True):
        '''
        Process one project version (doesn't have to be in the update queue)
        '''
        
        fodRelease = self._GetFodRelease(avid)
        if not fodRelease:
            logging.error("Couldn't retrieve release %s from FOD", avid)
            return

        # Ensure the mappings exist and create them if they don't
        logging.info('Mapping indices if needed')
        self.MapAppSecVuls(False)

        # Process
        logging.info('Running PopulateVulsOne for release %s', avid)
        self.__ProcessUpdate(avid, [ fodRelease ])
        #
        # SM API Integration
        # Finalize batch items for issues and complete queue scans
        #
        if self.__SmApiClientEnabled:
            self.__SmApiClient.FinalizeEverything()

        logging.info("Complete")
        if cleanupAfter:
            self.Cleanup()

    def Cleanup(self):
        pass
          
    def __GetAssessmentType(self, engineType):
        if not engineType:
            engineType = ""
        engineType = str(engineType).upper()
        if engineType in self.__AssessmentTypeMap.keys():
            return self.__AssessmentTypeMap[engineType]
        else:
            return "Unknown"

    def __ProcessUpdate(self, avid, fodReleases):
        '''Primary method calls the various private methods to update vuls, history and tests'''
        
        attributes = {}
        # It's possible we have a record in the Queue that has been removed from FOD, in that case we can bail out
        find = [ rid for rid in fodReleases if str(rid['_source']['releaseId']) == avid ]
        if not find:
            logging.debug("Unable to process updates for release %s, not found in extract data.  Could be orphan queue record.")
            return
        appVer = find[0]['_source']
        if appVer:
            appVerId = str(appVer['releaseId'])
            cancelTrk = CancelTracker(False)
            self.__AppVulsCustom.CustomUpdateAppVersion(appVer, "FOD", cancelTrk)
            if not cancelTrk.Cancel:
                self.__AppVulsFodCustom.CustomUpdateAppVersion(appVer, cancelTrk)
            cancel = cancelTrk.Cancel
            if cancel:
                # If cancel, remove it
                self.__DeleteStuff(appVerId)
            else:
                #
                # Get the attributes that will be used to populate
                # all reporting records.
                #
                attributes = self.__GetAttributes(appVerId, appVer)
                if len(attributes.keys()) == 1 and 'cancel' in attributes.keys():
                    # If cancel, remove it
                    self.__DeleteStuff(appVerId)
        else:
            logging.warning("App version %s not found in FOD extract data and will be skipped", appVerId)
            # Remove if not found in ssc data
            self.__DeleteStuff(appVerId)       
        #
        # Append to the scan history
        #
        lastScans = self.__UpdateScanHistory(appVerId, fodReleases, attributes)

        #
        # If expected assessment types missing, add "noscan" queue data
        #
        if appVer:
            self.__SmApiClient.MapScanlessAsset(appVerId, "FOD", appVer['applicationName'], appVer['releaseName'], appVer['releaseDescription'], attributes, assessmentTypes=list(lastScans.keys()))

        #
        # Update the Issues for the project
        #
        self.__UpdateIssues(lastScans, appVerId, fodReleases, attributes)

    @staticmethod
    def __GetDateStr(ds):
        if not ds: return None
        i = ds.find(".")
        if i > -1:
            ds = ds[0:i]
        try:
            return dtparse(ds).isoformat()
        except:
            raise(ValueError(f"Date string '{ds}' is incorrect"))
      
    def __UpdateScanHistory(self, appVerId, fodReleases, attributes):
        
        #
        # It's possible we have a record in the Queue that has since been
        # removed from FOD, in that case we can bail out
        #
        find = [ rid for rid in fodReleases if str(rid['_source']['releaseId']) == appVerId ]
        if not find:
            return
        release = find[0]['_source']
        isDelete = False

        # Run custom hook before loading scans
        self.__AppVulsCustom.CustomBeforeScanUpdates(release, attributes, 'FOD', isDelete)
        self.__AppVulsFodCustom.CustomBeforeScanUpdates(release, attributes, isDelete)

        # Delete all FOD scan history (for this app version)
        self.__DeleteStuff(appVerId, False, True)

        #
        # Get a list of FOD Project Scans
        #
        allDocsToInsert = []

        lastScans = {}
        # generator used to retrieve scans in batches in background
        for dto in self.__GetFodRelScansByReleaseId(appVerId):
            scan = dto['_source']
            scanType = scan['scanType']
            assessment_type = self.__GetAssessmentType(scanType)
            scanDate = self.__GetDateStr(scan['completedDateTime'])
            if not scanDate:
                logging.info("Scan with ID '%s' for release '%s' will be skipped, as it has no completed date.", scan['scanId'], release['releaseId'])
                continue

            try:
                scanRec = {
                    "timestamp": datetime.datetime.now(datetime.timezone.utc).isoformat(),
                    "ScanType": scanType,
                    "application_id": release['applicationId'],
                    "application_name": release['applicationName'],
                    "application_description": release['releaseDescription'],
                    "application_version_id": release['releaseId'],
                    "application_version_name": release['releaseName'],
                    "scan_date": scan['completedDateTime'],
                    "scan_id": scan['scanId'],
                    "assessment_type": assessment_type
                }

                #
                # Add the custom attributes to the scan object
                #
                self.__AddCustomAttributesToDocument(attributes, scanRec)
                #
                # Customizations if present
                #
                cancelTrk = CancelTracker(False)
                self.__AppVulsCustom.CustomUpdateScan(release, attributes, scanRec, 'FOD', cancelTrk)
                if not cancelTrk.Cancel:
                    self.__AppVulsFodCustom.CustomUpdateScan(release, attributes, scanRec, cancelTrk)
                cancel = cancelTrk.Cancel

                # Track last scans by assessment for ease of use with zero issues later
                if not assessment_type in lastScans.keys():
                    lastScans[assessment_type] = datetime.datetime(1900, 1, 1).isoformat()
                if scanDate > lastScans[assessment_type] and not cancel:
                    lastScans[assessment_type] = scanDate

                if not self.__DisableSM2Indices and not cancel:
                    #
                    # Bulk insert the array of documents.
                    #
                    bulkDocument = {
                        '_index': 'app_scan_history_fod',
                        '_id': 'FOD1-{}'.format(scanRec['scan_id']),
                        '_source': scanRec                
                        }
                    allDocsToInsert.append(bulkDocument)

            except Exception as e:
                logging.error(f"[{type(e).__name__}] {e}", exc_info=1)
                # continue implied since last line of for block

            # end for

        if not self.__DisableSM2Indices:
            #
            # Now insert the entire batch in a single shot, much faster.
            #
            logging.info(f"Inserting {len(allDocsToInsert)} scan history doc(s) for id {appVerId}")
            self.__Es.BulkInsert(allDocsToInsert)
        return lastScans

    def __DeleteStuff(self, appVersionId, issuesOnly=False, scansOnly=False):
        if issuesOnly and scansOnly:
            raise AppVulsFODException("Cannot set both issuesOnly and scansOnly")
        if self.__DisableSM2Indices:
            return
        DeleteQuery = {
          "query": {
            "term": { "application_version_id": { "value": appVersionId } }
          }
        }
        if not issuesOnly:
            self.__Es.DeleteByQuery("app_scan_history_fod", DeleteQuery)
        if not scansOnly:
            self.__Es.DeleteByQuery("app_vuls_fod", DeleteQuery)

    def __GetAssessmentTypes(self):
        lst = []
        for k in self.__AssessmentTypeMap.keys():
            atype = self.__AssessmentTypeMap[k]
            if not atype in lst:
                lst.append(atype)
        return lst

    def __UpdateIssues(self, lastScans, appVerId, fodReleases, releaseAttributes):
        # It's possible we have a record in the Queue that has been removed from FOD, in that case we can bail out
        find = [ rid for rid in fodReleases if str(rid['_source']['releaseId']) == appVerId ]
        if not find:
            return
        release = find[0]['_source']
        self.__DeleteStuff(appVerId, True)

        # We need to know the configured assessment types for tracking when to create zero recs.
        # lastScans is a dict containing { assessment type : last scan date } entries
        assessmentTypeStatuses = {}
        for assessment_type in self.__GetAssessmentTypes():
            if assessment_type not in assessmentTypeStatuses.keys():
                if assessment_type in lastScans.keys():
                    assessmentTypeStatuses[assessment_type] = { "lastscan": lastScans[assessment_type], "present": False }
                else:
                    logging.debug("App version %s appears to have no assessments of type '%s'", appVerId, assessment_type)

        # Get a list of all the vulnerabilites found by SSC with the matching
        # project ID
        body = { 
            "query": {
                "bool": {
                    "must": [
                        { "term": { "releaseId": { "value": appVerId }}},
                        { "term": { self.__SourceNameField: { "value": self.__SourceName }}}
                    ]
                }
            } # id field is present, so no need to add explicit sort
        }
        p = ProgressLogger(self.__Es)
        allDocsToInsert = []

        with self.__Es.SearchScroll("fodrelissues", queryBody=body, scrollSize=1000, scrollTimeout=None) as scroller:
            TotalCount = scroller.TotalHits if scroller else 0
            p.Start("PopulateVuls-UpdateIssues", TotalCount, "PopulateVuls-UpdateIssues Status")
            iCount = 0
            while len(scroller.Results):
                # Main issue handling loop
                for IssueContainer in scroller.Results:
                    Issue = IssueContainer['_source']
                    IssueKey = IssueContainer['_id']
                    IssueActive = True
            
                    # Default to Static if it's missing
                    #scanType = 'Static' if not Issue or 'engineType' not in Issue.keys() else Issue['engineType']
                    scanType = 'Static' if not Issue or 'scantype' not in Issue.keys() else Issue['scantype']
                    assessment_type = self.__GetAssessmentType(scanType)

                    cancelTrk = CancelTracker(False)
                    self.__AppVulsCustom.CustomBeforeIssueUpdate(release, releaseAttributes, assessment_type, 'FOD', cancelTrk)
                    if not cancelTrk.Cancel:
                        self.__AppVulsFodCustom.CustomBeforeIssueUpdate(release, releaseAttributes, assessment_type, cancelTrk)
                    cancel = cancelTrk.Cancel

                    try:
                        if not cancel:

                            # Check to see if the Fortify vulnerability is "active", ie should be shown.
                            if Issue['isSuppressed'] == True or Issue['status'] == 'Fix Validated':
                                IssueActive = False
                                
                            # Need to remember if the issue is Critical, High, etc.
                            Critical = 0
                            High = 0
                            Medium = 0
                            Low = 0
                                
                            is_vulnerability = False
                            if Issue["severityString"] == "Critical":
                                Critical = 1
                                is_vulnerability = True
                            elif Issue["severityString"] == "High":
                                High = 1
                                is_vulnerability = True
                            elif Issue["severityString"] == "Medium":
                                Medium = 1
                                is_vulnerability = True
                            elif Issue["severityString"] == "Low":
                                Low = 1
                                is_vulnerability = True
                            else:
                                pass

                            lastAssessmentDate = None
                            if assessment_type in assessmentTypeStatuses.keys():
                                # Set to last scan date for the assessment type if we know it
                                lastAssessmentDate = assessmentTypeStatuses[assessment_type]['lastscan']

                            # Set Removed Indicator and Date based on Issue Status since no Removed Date in FOD
                            if Issue["status"] == "Fix Validated":
                                # default to last scan date for this assessment type if the scan has been removed
                                holdremoveddate = lastAssessmentDate
                            else:
                                holdremoveddate = None

                            holdaudited = len(Issue["auditorStatus"]) > 0
                    
                            _app_vul = {
                                    "timestamp": datetime.datetime.now(datetime.timezone.utc).isoformat(),
                                    "active": IssueActive,
                                    "labels": "",
                                    "message": "",
                                    "tags": Issue["auditorStatus"],
                                    "category": Issue["category"],
                                    "classification": "",
                                    "description": "",
                                    "enumeration": "",
                                    "id": Issue['id'],
                                    "reference": "",
                                    "report_id"	:Issue["scanId"],
                                    "scanner_vendor":"Fortify on Demand",
                                    "score_base":"0",
                                    "score_environmental": "0",
                                    "score_temporal":"0",
                                    "score_version":"2.0",
                                    "severity": Issue["severityString"],
                                    "sor_url": "",
                                    "name": Issue["category"],
                                    "hidden": False,
                                    "engine_type": Issue["scantype"],
                                    "engine_category": "",
                                    "issue_status": Issue['status'],
                                    "location": Issue["primaryLocation"],
                                    "analyzer": Issue['analysisType'],
                                    "reviewed": False,
                                    "scanner_id": Issue['id'],
                                    "suppressed": Issue["isSuppressed"],
                                    "removed_date": holdremoveddate,
                                    "found_date"	: Issue['introducedDate'],
                                    "confidence"	: "0",
                                    "impact"	: "0",
                                    "scan_status": Issue['status'],
                                    "audited"	: holdaudited,
                                    "kingdom": Issue['kingdom'],
                                    "likelihood": "0",
                                    "removed": True if holdremoveddate else False,
                                    "location_full": Issue["primaryLocationFull"],
                                    "application_id": release['applicationId'],
                                    "application_name": release['applicationName'],
                                    "application_description": release['releaseDescription'],
                                    "application_version_id": release['releaseId'],
                                    "application_version_name": release["releaseName"],
                                    "saltminer.is_vulnerability": is_vulnerability,
                                    "assessment_type": assessment_type,
                                    "last_scan_date": lastAssessmentDate,
                                    "issue_instance_id": Issue['instanceId'],
                                    "Critical": Critical,
                                    "High": High,
                                    "Medium": Medium,
                                    "Low": Low
                                    }
                
                            #
                            # SM API Integration
                            # Track all keys before asset attribute addition/manipulation
                            #
                            issueAssetKeys = []
                            if self.__SmApiClientEnabled:
                                for k in _app_vul.keys():
                                    issueAssetKeys.append(k)

                            #
                            # Add the custom attributes to the vulnerability object
                            #
                            self.__AddCustomAttributesToDocument(releaseAttributes, _app_vul)
            
                            # Track all keys after adding asset attributes
                            issueKeys = []
                            if self.__SmApiClientEnabled:
                                for k in _app_vul.keys():
                                    issueKeys.append(k)

                            #
                            # Now apply custom logic for customer specific needs
                            #
                            try:
                                cancelTrk = CancelTracker(False)
                                self.__AppVulsCustom.CustomUpdateIssue(release, releaseAttributes, assessment_type, Issue, _app_vul, 'FOD', cancelTrk)
                                if not cancelTrk.Cancel:
                                    self.__AppVulsFodCustom.CustomUpdateIssue(release, releaseAttributes, assessment_type, Issue, _app_vul, cancelTrk)
                                cancel = cancelTrk.Cancel
                            except Exception as e:
                                raise AppVulsFODException("Error in an AppVulsCustom customization") from e
            
                            # Add issue attribute keys to asset key list (this prevents duplication of issue attributes in asset attributes)
                            if self.__SmApiClientEnabled:
                                for k in _app_vul.keys():
                                    if k not in issueKeys:
                                        issueAssetKeys.append(k)

                            # Mark this assessment type as present (no zero rec needed)
                            if assessment_type in assessmentTypeStatuses.keys() and not cancel:
                                assessmentTypeStatuses[assessment_type]['present'] = True

                            #
                            # SM API Integration
                            # Submit queue issue to SM API
                            #
                            if self.__SmApiClientEnabled and _app_vul and not cancel:
                                self.__SmApiClient.MapEverything(_app_vul, issueAssetKeys, issueKeys)


                            if not self.__DisableSM2Indices and not cancel:
                                #
                                # Bulk insert the array of documents.
                                #
                                bulkDocument = {
                                    '_index': 'app_vuls_fod',
                                    '_id': 'FOD1-{}'.format(IssueKey),
                                    '_source': _app_vul                
                                    }

                                allDocsToInsert.append(bulkDocument)

                            if (iCount % 1000 == 0):
                                if self.__DisableSM2Indices:
                                    p.Progress(iCount, 'Processing issues')
                                else:
                                    p.Progress(iCount, 'Adding docs to bulk queue')

                    except KeyError as ex:
                        msg = f"Unknown scan type found for issue: [{type(ex).__name__}] {ex}"
                        print(msg)
                        logging.warning(msg)
                    except Exception as ex:
                        msg = f"[{type(ex).__name__}] {ex}"
                        print(msg)
                        logging.error(msg)
                        raise(ex)

                    iCount = iCount + 1
                # end for
                scroller.GetNext()
            # end while

        # Zero records handling
        for assessment_type in assessmentTypeStatuses.keys():
            if assessmentTypeStatuses[assessment_type]['present']:
                continue

            cancelTrk = CancelTracker(False)
            self.__AppVulsCustom.CustomBeforeIssueUpdate(release, releaseAttributes, assessment_type, 'FOD', cancelTrk)
            if not cancelTrk.Cancel:
                self.__AppVulsFodCustom.CustomBeforeIssueUpdate(release, releaseAttributes, assessment_type, cancelTrk)
            cancel = cancelTrk.Cancel
            if cancel:
                continue

            _app_vul = {
                        "timestamp": datetime.datetime.now(datetime.timezone.utc).isoformat(),
                        "active": True,
                        "labels": "",
                        "message": "",
                        "tags": "",
                        "category": "Zero",
                        "classification": "",
                        "description": "No Issues Found",
                        "enumeration": "",
                        "id": 0,
                        "reference": "",
                        "report_id"	: 0,
                        "scanner_vendor":"Fortify on Demand",
                        "score_base":"0",
                        "score_environmental": "0",
                        "score_temporal":"0",
                        "score_version":"2.0",
                        "severity": "Zero",
                        "sor_url": "",
                        "name": "Zero",
                        "hidden": False,
                        "engine_type": assessment_type,
                        "engine_category": "",
                        "issue_status": "Zero",
                        "location": "",
                        "analyzer": "",
                        "reviewed": False,
                        "scanner_id": "",
                        "suppressed": False,
                        "removed_date": None,
                        "found_date"	: assessmentTypeStatuses[assessment_type]['lastscan'],
                        "confidence"	: "0",
                        "impact"	: "0",
                        "scan_status": "Existing",
                        "audited"	: False,
                        "kingdom": "Zero",
                        "likelihood": "0",
                        "removed": False ,
                        "location_full": "",
                        "application_id": release['applicationId'],
                        "application_name": release['applicationName'],
                        "application_description": release['releaseDescription'],
                        "application_version_id": release['releaseId'],
                        "application_version_name": release["releaseName"],
                        "assessment_type": assessment_type,
                        "last_scan_date": assessmentTypeStatuses[assessment_type]['lastscan'],
                        "issue_instance_id": "",
                        "Critical": 0,
                        "High": 0,
                        "Medium": 0,
                        "Low": 0
                        }

            #
            # SM API Integration
            # Track all keys before attribute addition/manipulation
            #
            issueAssetKeys = []
            if self.__SmApiClientEnabled:
                for k in _app_vul.keys():
                    issueAssetKeys.append(k)

            #
            # Add the custom attributes to the vulnerability object
            #
            self.__AddCustomAttributesToDocument(releaseAttributes, _app_vul)
            
            # Track all keys after adding asset attributes
            issueKeys = []
            if self.__SmApiClientEnabled:
                for k in _app_vul.keys():
                    issueKeys.append(k)

            cancelTrk = CancelTracker(False)
            self.__AppVulsCustom.CustomUpdateIssue(release, releaseAttributes, assessment_type, None, _app_vul, 'FOD', cancelTrk)
            if not cancelTrk.Cancel:
                self.__AppVulsFodCustom.CustomUpdateIssue(release, releaseAttributes, assessment_type, None, _app_vul, cancelTrk)
            if cancelTrk.Cancel:
                continue

            # Add issue attribute keys to asset key list (this prevents duplication of issue attributes in asset attributes)
            if self.__SmApiClientEnabled:
                for k in _app_vul.keys():
                    if k not in issueKeys:
                        issueAssetKeys.append(k)

            logging.info("Adding zero record for app version %s and assessment type %s", appVerId, assessment_type)
            IssueKey = f"{appVerId}-{assessment_type}-0"
            #
            # SM API Integration
            # Submit queue issue to SM API
            #
            if self.__SmApiClientEnabled and _app_vul:
                self.__SmApiClient.MapEverything(_app_vul, issueAssetKeys, issueKeys)

            if not self.__DisableSM2Indices:
                #
                # Bulk insert the array of documents.
                #
                bulkDocument = {
                    '_index': 'app_vuls_fod',
                    '_id': 'FOD1-{}'.format(IssueKey),
                    '_source': _app_vul                
                    }

                allDocsToInsert.append(bulkDocument)

        if not self.__DisableSM2Indices:
            p.Progress(iCount, 'Starting Bulk Insert')
            print("ReleaseID:{} Count:{}".format(appVerId, iCount))
            self.__Es.BulkInsert(allDocsToInsert)
            p.Finish(iCount, 'Insert complete.')

    def __AddCustomAttributesToDocument(self, ReleaseAttributes, _app_doc):
        '''Adds common app_version attributes to the document for searching'''
        #
        # This takes the custom attributes and attaches them to the passed in _app_doc and is used by app_vuls_ssc, app_scan_history_ssc and app_
        # scans
        #
        for RelAttribute in ReleaseAttributes:
            
            ESAttributeName = RelAttribute.replace(' ', '_')

            if isinstance(ReleaseAttributes[RelAttribute], list):
                _app_doc[ESAttributeName] = ', '.join(str(x) for x in ReleaseAttributes[RelAttribute])
            else:
                _app_doc[ESAttributeName] = '{}'.format(ReleaseAttributes[RelAttribute]).strip()
        

    def __GetAttribute(self, source, key, default=None):
        if key in source.keys():
            if source[key] == self.FOD_UNSET_VALUE and not self.__NullUnsetAttributes:
                return str(source[key])
            if source[key] and len(str(source[key])):
                return str(source[key])
        return default
        

    def __GetAttributes(self, appVerId, release):

        RelAttributes = {}
        if not release:
            return RelAttributes
        appId = release['applicationId']

         # declare a filter query dict object
        query = {
          "size": 10000,
          "query": { 
            "term": { "applicationId": { "value": appVerId } } 
          }
        }
 
        appResult = self.__Es.Search('fodapplications', { "query": { "term": { "applicationId": appId }}})
        relResult = self.__Es.Search('fodreleases', { "query": { "term": { "releaseId": appVerId }}})

        # should only have one application returned
        if not appResult or not len(appResult) == 1:
            logging.critical("[DATA] FOD Application id %s not found in fodapplications, skipping release %s", appId, appVerId)
            return {}
        if not relResult or not len(relResult) == 1:
            logging.critical("[DATA] FOD Release id %s not found in fodreleases, skipping release", appVerId)
            return {}

        FODApplication = appResult[0]['_source']
        FODRelease = relResult[0]['_source']
           
        FODAttributes =  FODApplication['attributes']
        if not FODAttributes:
            FODAttributes = []

        # Set "star" rating and a couple others from the application and release.  If a custom attribute exists with these names then it will overwrite.
        RelAttributes['business_criticality'] = self.__GetAttribute(FODApplication, 'businessCriticalityType')
        RelAttributes['application_type'] = self.__GetAttribute(FODApplication, 'applicationType')
        RelAttributes['sdlc_status'] = self.__GetAttribute(FODRelease, 'sdlcStatusType')
        RelAttributes['rating'] = self.__GetAttribute(FODRelease, 'rating')
        RelAttributes['passing'] = self.__GetAttribute(FODRelease, 'isPassed')
        RelAttributes['fod_application_id'] = self.__GetAttribute(FODRelease, 'applicationId')
        for FODAttribute in FODAttributes:
            if FODAttribute['value'] == self.FOD_UNSET_VALUE and self.__NullUnsetAttributes:
                continue
            attrName = FODAttribute['name'].lower().replace(' ', '_')
            RelAttributes[attrName] = FODAttribute['value']
        
        # next line does nothing so is commented for now
        # self.__AddCustomAttributesToDocument(releaseAttributes, _app_vul)
        cancelTrk = CancelTracker(False)
        self.__AppVulsCustom.CustomUpdateAttributes(release, RelAttributes, "FOD", cancelTrk)
        if not cancelTrk.Cancel:
            self.__AppVulsFodCustom.CustomUpdateAttributes(release, RelAttributes, cancelTrk)
        cancel = cancelTrk.Cancel
        if cancel: 
            return { "cancel": True }
            
        return RelAttributes

    def _GetAllFodReleases(self):
        '''Get a list of all FOD Releases (and current source name)'''

        body = { "query": { "term": { self.__SourceNameField: { "value": self.__SourceName } } }, "sort": [ "releaseId" ] }
        sc = self.__Es.SearchScroll('fodreleases', body, 200, scrollTimeout=None)
        return sc.GetAll() if sc else []

    def _GetFodRelease(self, releaseId):
        '''Return a single release by release ID (and current source name)'''
        body = { 
            "query": {
                "bool": {
                    "must": [
                        { "term": { "releaseId": { "value": releaseId } } },
                        { "term": { self.__SourceNameField: { "value": self.__SourceName } } }
                    ]
                }
            }
        }
        res = self.__Es.Search('fodreleases', body)
        if res and len(res) > 0:
            if ("_source" in res[0].keys()):
                return res[0]['_source']
        return None

    def __GetFodRelScansByReleaseId(self, releaseId):
        body = { 
            "query": {
                "bool": {
                    "must": [
                        { "term": { "releaseId": { "value": releaseId } } },
                        { "term": { self.__SourceNameField: { "value": self.__SourceName } } }
                    ]
                }
            },
            "sort": ["scanId"]
        }
        sc = self.__Es.SearchScroll('fodscans', body, 200, scrollTimeout=None)
        return sc.Generator() if sc else []

    def ResetQueue(self):
        _post = {
            "sort": [{"completedDateTime": {"order": "desc"}}],
            "query": {
                "bool": {
                    "filter": {
                        "range": {
                            "completedDateTime": {
                                "gt": 0
                            }
                        }
                    }
                }
            }
        }

        QueueList = self.__Es.SearchWithCursor('_id', 'fodupdatequeue', _post)

        for QueueKey in QueueList:
            QueueDoc = QueueList[QueueKey]
            QueueDoc['completedDateTime'] = "1900-01-01T00:00:00"
            self.__Es.IndexWithId('fodupdatequeue', QueueKey, QueueDoc)


class AppVulsFODException(Exception):
    pass