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
# Originally AppVulsSSC.py and class AppVulsSSC.

import datetime
import logging
import time
from queue import Queue
from dateutil.parser import parse as dtparse

from Utility.CancelTracker import CancelTracker
from Utility.DImport import DImport
from Utility.ProgressLogger import *
from Utility.SmApiClient import SmApiClient
from Utility.UpdateQueueHelper import UpdateQueueHelper

class AppVulsProcessor(object):
    """ App vulnerability processor for SSC """

    def __init__(self, appSettings, sourceName, smv3ConfigName="SMv3", mainConfigName="Main"):

        if type(appSettings).__name__ != "ApplicationSettings":
            raise TypeError("Type of appSettings must be 'ApplicationSettings'")
        if not smv3ConfigName or not smv3ConfigName in appSettings.GetConfigNames():
            raise AppVulsSSCException(f"Invalid or missing configuration for name '{smv3ConfigName}'")
        if not mainConfigName or not mainConfigName in appSettings.GetConfigNames():
            raise AppVulsSSCException(f"Invalid or missing configuration for name '{mainConfigName}'")
        
        self.__Es = appSettings.Application.GetElasticClient()
        self.__Attributes = appSettings.Get(mainConfigName, 'Attributes')
        self.__App = appSettings.Application
        self.__UpdateQHelper = UpdateQueueHelper(appSettings, sourceName)
        self.__LastScanDateField = appSettings.GetSource(sourceName, "LastScanDateField", "lastScanDate")
        self.__BulkDocs = []
        self.__BulkSendBatchSize = appSettings.GetSource(sourceName, "BulkSendBatchSize", 1000)

        #
        # SM API Integration
        # Setup API Client enable switch
        # Create SaltMiner API Client class (attempts to connect upon creation)
        #
        self.__SmApiClientEnabled = appSettings.Get(smv3ConfigName, "ApiClientEnabled", False)
        if self.__SmApiClientEnabled:
            self.__SmApiClient = SmApiClient(appSettings, sourceName, smv3ConfigName)
            self.__HistoryV3Enable =  appSettings.GetSource(sourceName, "EnableHistoryImportToV3", False)
        self.__DisableSM2Indices = appSettings.GetSource(sourceName, "DisableSM2Indices", False)
        
        clientCode = appSettings.Get(mainConfigName, 'CustomerCode', 'SW')
        self.__IssueCustomTagsToCustomAttributes = appSettings.GetSource(sourceName, "IssueCustomTagsToCustomAttributes", True)

        appVulsSscCustomFactory = DImport.Import(f"AppVulsCustom.AppVulsSscCustom{clientCode}", "AppVulsSscCustom", "AppVulsCustom")
        self.__AppVulsSscCustom = appVulsSscCustomFactory(appSettings, sourceName)
        
        appVulsCustomFactory = DImport.Import(f"AppVulsCustom.AppVulsCustom{clientCode}", "AppVulsCustom", "AppVulsCustom")
        self.__AppVulsCustom = appVulsCustomFactory(appSettings, sourceName)

        self.__AssessmentTypeMap = appSettings.GetSource(sourceName, 'AssessmentTypeMap', {})

        if not len(self.__AssessmentTypeMap.keys()):
            logging.warn("Assessment type map missing from source name '%s'.  This will cause all scans to be considered assessment type 'Unknown'.", sourceName)
               
        logging.info("AppVulsSSC init complete, connected to Elastic")
       
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
        # Map the app_vuls_ssc index but don't overwrite it if it already
        # exists.
        #
        _mapping = self.__Es.GetMapping('app_vuls')

        #
        # Now add the custom attributes to the mapping so they are of the
        # correct data type.
        #
        
        #_attributes = self.__Attributes
        #for MapKey in _attributes:
        #    mapKeyUnderline = MapKey.replace(' ', '_')
        #    _mapping['mappings']['properties'][mapKeyUnderline] = _attributes[MapKey]
       
        self.__Es.MapIndexWithMapping("app_vuls_ssc", _mapping, Force)

        
        activeFilter = {"filter" : { "term" : { "active": True } }}
        allFilter = {"filter": {"match_phrase": {"saltminer.is_vulnerability": True}}}
        self.__Es.PutAlias('app_vuls_ssc', 'app_vuls_active_ssc', activeFilter, Force)
        self.__Es.PutAlias('app_vuls_ssc', 'app_vuls_all_ssc', allFilter, Force)
         

        # Now mapp the app_scan_history_ssc index
        _mapping = self.__Es.GetMapping('app_scan_history')
        #for MapKey in _attributes:
        #    mapKeyUnderline = MapKey.replace(' ', '_')
        #    _mapping['mappings']['properties'][mapKeyUnderline] = _attributes[MapKey]
       
        self.__Es.MapIndexWithMapping("app_scan_history_ssc", _mapping, Force)

        logging.debug('Mapping of indices complete.')

    def PopulateVuls(self, cleanupAfter=True):
        '''Main function that will drive the population of vulnerabilites.'''
        
        '''
        Overall Process:

        SSCProjectVersions = ...Get all Projectversion into memory
            should be a dictionary
            https://www.w3schools.com/python/python_dictionaries.asp

        Get list of docs from sscupdatequeue index ordered by processedDate
        foreach doc
            Delete any existing records
            Flush ElasticSearch
            get the issues from sscprojissues
            populate the app_vuls table

        '''
        # Get all Projectversion into memory
        sscProjectListPulledAt = datetime.datetime.now(datetime.timezone.utc)
        sscProjects = self._GetAllSscProjects()

        # Ensure the mappings exist and create them if they don't
        logging.info('Mapping indices as needed...')
        self.MapAppSecVuls(False)

        # Remove app versions marked for delete
        b = 1
        c = 0
        uq, total = self.__UpdateQHelper.GetUpdateQueueBatch(["D"])
        while uq and len(uq) > 0:
            for qitem in uq:
                if c % 10 == 0:
                    logging.info("Retiring app version(s), %s of %s (batch %s)", c, total, b)
                qid = qitem['id'] if qitem and 'id' in qitem.keys() else None
                if qid:
                    logging.debug("Deleting v2 data for project version id %s", qid)
                    self.__DeleteStuff(qid)
                else:
                    logging.warning("Skipping qitem for deleting v2 data, invalid. qitem: %s", qitem)
                self.__UpdateQHelper.CompleteUpdateQueue(qid, ["D"])
                c += 1
            # next batch
            uq, total = self.__UpdateQHelper.GetUpdateQueueBatch(["D"])
            b += 1
        if c > 0:
            logging.info("Waiting 5 sec..")
            time.sleep(5)

        # Create an object so we can remember if we have run an import already.
        # If we get duplicates it would be because we got ahead of the updates while in process, 
        # and we can wait a few sec for it to catch up. 
        updated = []

        # Priming read
        updateBatch, total = self.__UpdateQHelper.GetUpdateQueueBatch(["U", "A"])
        if not total:
            total = 0
        #
        # Setup the progress logger
        #
        p = ProgressLogger(self.__Es)
        p.Start("PopulateVuls", total, "PopulateVuls Status")
        p.Progress(0, f'Starting PopulateVuls, {total} app/version(s) to process')
        c = 1
        batch = 1
        retries = 0
        while updateBatch and len(updateBatch) > 0 and retries < 10:
            #
            # Run through the update queue and update the main app_vuls_ssc indices
            #
            for qitem in updateBatch:
                qid = qitem['id']
                processedDate = dtparse(qitem['max_processed_date'])
                if processedDate > sscProjectListPulledAt:
                    logging.info("Project version data outdated, reloading...")
                    sscProjectListPulledAt = datetime.datetime.now(datetime.timezone.utc)
                    sscProjects = self._GetAllSscProjects()

                if qid in updated:
                    retries +=1
                    if retries >= 10:
                        logging.warning("App version %s recently processed but failed to update after 10 retries, canceling process.", qid)
                        break
                    # We already processed this PVID - might be ahead of the db so let's pause
                    logging.info('Skipping app version %s, already processed.  Pausing for 5 sec to let the db catch up...', qid)
                    time.sleep(5)
                    break
                else:
                    retries = 0
                    p.Progress(c, f"Updating app version {qid}, {c} of {total} (batch {b})")
                    self.__ProcessUpdate(qid, sscProjects)

                self.__UpdateQHelper.CompleteUpdateQueue(qid, ["U", "A"])
                updated.append(qid)
                c += 1
            # end for

            #
            # Get next batch (throw away the total, don't need)
            #
            updateBatch, whocares = self.__UpdateQHelper.GetUpdateQueueBatch(["U", "A"], True)
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


    def PopulateVulsOne(self, pvid, cleanupAfter=True):
        '''
        Process one project version (doesn't have to be in the update queue)
        '''
        
        sscProject = self._GetSscProjectVersion(pvid)
        if not sscProject:
            logging.error("Couldn't retrieve project version %s from SSC, skipping this update.", pvid)
            return

        # Ensure the mappings exist and create them if they don't
        logging.info('Mapping indices if needed')
        self.MapAppSecVuls(False)

        # Process
        logging.info('Running PopulateVulsOne for project version %s', pvid)
        self.__ProcessUpdate(pvid, { int(pvid): sscProject })
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
        if self.__AppVulsSscCustom.Cleanup:
            self.__AppVulsSscCustom.Cleanup()

    def __GetAssessmentType(self, engineType):
        if not engineType:
            engineType = ""
        engineType = str(engineType).upper()
        if engineType in self.__AssessmentTypeMap.keys():
            return self.__AssessmentTypeMap[engineType]
        else:
            logging.warning("Assessment type '%s' not configured in map.", engineType)
            return "Unknown"

    def __ProcessUpdate(self, avid, sscProjects):
        '''Primary method calls the various private methods to update vuls, history and tests'''

        attributes = {}
        appVerId = int(avid)
        appVer = None
        for i in range(1, 3):
            delay = (i - 1) * 30
            if appVerId not in sscProjects.keys():
                logging.warning("App version %s not found in SSC extract data, retrying (%s of 3) after %s sec delay...", appVerId, i, delay)
                time.sleep(delay)
                rsp = self._GetSscProjectVersion(avid)
                if rsp:
                    sscProjects[appVerId] = rsp
            else:
                appVer = sscProjects[appVerId]
                break

        if appVer:
            cancelTrk = CancelTracker(False)
            self.__AppVulsCustom.CustomUpdateAppVersion(appVer, "SSC", cancelTrk)
            if not cancelTrk.Cancel:
                self.__AppVulsSscCustom.CustomUpdateAppVersion(appVer, cancelTrk)
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
            logging.warning("App version %s not found in SSC extract data and will be skipped", appVerId)
            # Remove if not found in ssc data
            self.__DeleteStuff(appVerId)

        #
        # Append to the scan history
        #
        lastScans = self.__UpdateScanHistory(appVerId, sscProjects, attributes)
        
        #
        # If expected assessment types missing, add "noscan" queue data
        #
        if appVer:
            self.__SmApiClient.MapScanlessAsset(appVerId, "Fortify", appVer['project']['name'], appVer['name'], appVer['description'], attributes, assessmentTypes=list(lastScans.keys()))

        #
        # Update the Issues for the project
        #
        self.__UpdateIssues(lastScans, appVerId, sscProjects, attributes)

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

    def __UpdateScanHistory(self, appVerId, sscProjects, attributes):
        
        #
        # It's possible we have a record in the Queue that has since been
        # removed from SSC, in that case we can bail out
        #
        if not appVerId in sscProjects.keys():
            return
        projectVersion = sscProjects[appVerId]
        isDelete = False

        # Run custom hook before loading scans
        self.__AppVulsCustom.CustomBeforeScanUpdates(projectVersion, attributes, 'SSC', isDelete)
        self.__AppVulsSscCustom.CustomBeforeScanUpdates(projectVersion, attributes, isDelete)

        self.__DeleteStuff(appVerId, False, True)

        #
        # Get a list of SSC Project Scans
        #
        projectScans = self.__GetSscProjScansByProjectId(appVerId)

        allDocsToInsert = []

        lastScans = {}
        for scan in projectScans.values():
            if not 'type' in scan and 'scanrec' in scan:
                raise AppVulsSSCException("sscprojscans index is incompatible and must be upgraded to the latest version.  See Upgrade/RunSscScansUpgrade for more details.")
            scanType = scan['type']
            assessment_type = self.__GetAssessmentType(scanType)
            scanDate = self.__GetDateStr(scan[self.__LastScanDateField])

            try:
                scanId = f"{scan['artifactId']}-{scan['id']}"
                scanRec = {
                    "timestamp": datetime.datetime.now(datetime.timezone.utc).isoformat(),
                    "ScanType": scanType,
                    "application_id": projectVersion['project']['id'],
                    "application_name": projectVersion['project']['name'],
                    "application_description": projectVersion['project']['description'],
                    "application_version_id": projectVersion['id'],
                    "application_version_name": projectVersion["name"],
                    "scan_date": scanDate,
                    "scan_id": scanId,
                    "assessment_type": assessment_type,
                    "rulepacks": []
                }
                key = 'rulepacks'
                if key in scan:
                    for rp in scan[key]:
                        scanRec['rulepacks'].append({ 
                            'id': rp['guid'] if 'guid' in rp else '', 
                            'name': rp['name'] if 'name' in rp else '', 
                            'version': rp['version'] if 'version' in rp else '',
                            'language': rp['language'] if 'language' in rp else ''
                        })
            
                #
                # Add the custom attributes to the scan object
                #
                self.__AddCustomAttributesToDocument(attributes, scanRec)
                #
                # Customizations if present
                #
                cancelTrk = CancelTracker(False)
                self.__AppVulsCustom.CustomUpdateScan(projectVersion, attributes, scanRec, 'SSC', cancelTrk)
                if not cancelTrk.Cancel:
                    self.__AppVulsSscCustom.CustomUpdateScan(projectVersion, attributes, scanRec, cancelTrk)
                cancel = cancelTrk.Cancel

                # Track last scans by assessment for ease of use with zero issues later
                # NOTE: if there are ever multiple SSC scan types that map to the same assessment type then the last scan information here may be inaccurate
                if assessment_type not in lastScans:
                    # Haven't seen this assessment type yet, so create it in the list
                    lastScans[assessment_type] = { "lastscan": datetime.datetime(1900, 1, 1).isoformat(), "orgType": scanType }
                if scanDate > lastScans[assessment_type]['lastscan'] and not cancel:
                    # Update last scan date if newer for this assessement type
                    lastScans[assessment_type] = { "lastscan": scanDate, "orgType": scanType }

                if not self.__DisableSM2Indices and not cancel:
                    #
                    # Bulk insert the array of documents.
                    #
                    bulkDocument = {
                        '_index': 'app_scan_history_ssc',
                        '_id': 'SSC1-{}'.format(scanId),
                        '_source': scanRec                
                        }
                    self.__SendBulkItem(bulkDocument)

            except Exception as e:
                logging.error(f"[{type(e).__name__}] {e}", exc_info=1)
                # continue implied since last line of for block

            # end for

        if not self.__DisableSM2Indices:
            #
            # Send any remaining bulk docs
            #
            self.__SendBulkItem()
        logging.info(f"Inserting {len(allDocsToInsert)} scan history doc(s) for id {projectVersion['id']}")
        return lastScans

    def __DeleteStuff(self, appVersionId, issuesOnly=False, scansOnly=False):
        if issuesOnly and scansOnly:
            raise AppVulsSSCException("Cannot set both issuesOnly and scansOnly")
        if self.__DisableSM2Indices:
            return
        DeleteQuery = {
          "query": {
            "term": { "application_version_id": { "value": appVersionId } }
          }
        }
        if not issuesOnly:
            self.__Es.DeleteByQuery("app_scan_history_ssc", DeleteQuery)
        if not scansOnly:
            self.__Es.DeleteByQuery("app_vuls_ssc", DeleteQuery)

    def __GetAssessmentTypes(self):
        lst = []
        for k in self.__AssessmentTypeMap.keys():
            atype = self.__AssessmentTypeMap[k]
            if not atype in lst:
                lst.append(atype)
        return lst

    def __AddIssueDetailsData(self, srcIssue, issue):
        key = "customAttributes"
        if key in srcIssue.keys():
            issue[key] = srcIssue[key]
            # remove any null (None) custom attributes
            flds = []
            for k1 in issue[key].keys():
                flds.append(k1)
            for k2 in flds:
                if issue[key][k2] is None:
                    issue[key].pop(k2, '')
        if self.__IssueCustomTagsToCustomAttributes:
            ctv = "customTagValues"
            ca = "customAttributes"
            if ctv in srcIssue.keys():
                if not ca in issue.keys():
                    issue[ca] = {}
                for tag in srcIssue[ctv]:
                    if 'keyValue' not in tag.keys() or not tag['keyValue'] or not 'name' in tag['keyValue'].keys() or not 'value' in tag['keyValue'].keys():
                        logging.debug("Missing/null keyValue in customTagValue for issue %s", srcIssue['id'])
                        continue
                    issue[ca][tag['keyValue']['name']] = tag['keyValue']['value']

    def __UpdateIssues(self, lastScans, appVerId, sscProjects, attributes):
        # It's possible we have a record in the Queue that has been removed from SSC, in that case we can bail out
        if not appVerId in sscProjects.keys():
            return
        self.__DeleteStuff(appVerId, True)
        projectVersion = sscProjects[appVerId]

        # We need to know the configured assessment types for tracking when to create zero recs.
        # lastScans is a dict containing { assessment type : { "lastscan": last scan date, "orgType": engine type } entries
        assessmentTypeStatuses = {}
        for assessment_type in self.__GetAssessmentTypes():
            if assessment_type not in assessmentTypeStatuses.keys():
                if assessment_type in lastScans.keys():
                    # Start each assessment type as not present, then set present when encountered while processing issues
                    assessmentTypeStatuses[assessment_type] = { "lastscan": lastScans[assessment_type]['lastscan'], "present": False, "orgType": lastScans[assessment_type]['orgType'] }
                else:
                    logging.debug("App version %s appears to have no assessments of type '%s'", appVerId, assessment_type)

        #
        # It's possible we have a record in the Queue that has since been
        # removed from SSC, in that case we can bail out
        #
        if not appVerId in sscProjects.keys():
            return

        # Get a list of all the vulnerabilites found by SSC with the matching
        # project ID
        issueQuery = { "query": { "term": { "projectVersionId": appVerId }}}
        p = ProgressLogger(self.__Es)      
        allDocsToInsert = []

        with self.__Es.SearchScroll("sscprojissues", queryBody=issueQuery, scrollSize=1000, scrollTimeout=None) as scroller:
            TotalCount = scroller.TotalHits if scroller else 0
            p.Start("PopulateVuls-UpdateIssues", TotalCount, "PopulateVuls-UpdateIssues Status")
            iCount = 0
            while len(scroller.Results):

                # Main issue handling loop
                for IssueContainer in scroller.Results:
                    Issue = IssueContainer['_source']
                    IssueKey = IssueContainer['_id']
                    IssueActive = True

                    # SSC randomly does not have the scan type so default to SSC if it's missing
                    scanType = 'SCA' if not Issue or 'engineType' not in Issue.keys() else Issue['engineType']
                    assessment_type = self.__GetAssessmentType(scanType)

                    cancelTrk = CancelTracker(False)
                    self.__AppVulsCustom.CustomBeforeIssueUpdate(projectVersion, attributes, assessment_type, 'SSC', cancelTrk)
                    if not cancelTrk.Cancel:
                        self.__AppVulsSscCustom.CustomBeforeIssueUpdate(projectVersion, attributes, assessment_type, cancelTrk)
                    cancel = cancelTrk.Cancel

                    try:
                        if not cancel:
                            # 2/8/24
                            # Remediation for SSC bug that sometimes sets removed = True with no removedDate
                            if Issue['removed'] == True and not Issue['removedDate']:
                                Issue['removedDate'] = '1876-01-01T00:00:00.000+00:00'

                            # Check to see if the Fortify vulnerability is "active", ie should be shown.
                            if Issue['suppressed'] == True or Issue['removed'] == True or Issue['hidden'] == True:
                                IssueActive = False
                            RemovedDate = None if Issue['removed'] == False else Issue['removedDate']

                            # Need to remember if the issue is Critical, High, etc.
                            Critical = 0
                            High = 0
                            Medium = 0
                            Low = 0

                            is_vulnerability = False
                            if Issue["friority"] == "Critical":
                                Critical = 1
                                is_vulnerability = True
                            elif Issue["friority"] == "High":
                                High = 1
                                is_vulnerability = True
                            elif Issue["friority"] == "Medium":
                                Medium = 1
                                is_vulnerability = True
                            elif Issue["friority"] == "Low":
                                Low = 1
                                is_vulnerability = True
                            else:
                                pass
                    
                            lastAssessmentDate = projectVersion['currentState']['lastFprUploadDate']
                            if assessment_type in assessmentTypeStatuses.keys():
                                # Set to last scan date for the assessment type if we know it
                                lastAssessmentDate = assessmentTypeStatuses[assessment_type]['lastscan']
                            if Issue['foundDate'] == None:
                                Issue['foundDate'] = lastAssessmentDate
                            _app_vul = {
                                    "timestamp": datetime.datetime.now(datetime.timezone.utc).isoformat(),
                                    "active": IssueActive,
                                    "labels": "",
                                    "message": "",
                                    "tags": Issue["primaryTag"],
                                    "category":"Application",
                                    "classification": "",
                                    "description": "keyword",
                                    "enumeration": "",
                                    "id": Issue['id'],
                                    "reference": "",
                                    "report_id"	:Issue["lastScanId"],
                                    "scanner_vendor":"Fortify",
                                    "score_base":"0",
                                    "score_environmental": "0",
                                    "score_temporal":"0",
                                    "score_version":"2.0",
                                    "severity": Issue["friority"],
                                    "sor_url": Issue['_href'],
                                    "name": Issue["issueName"],
                                    "hidden": Issue["hidden"],
                                    "engine_type": scanType,
                                    "engine_category": Issue['engineCategory'],
                                    "issue_status": Issue['issueStatus'],
                                    "location": Issue["primaryLocation"],
                                    "analyzer": Issue['analyzer'],
                                    "reviewed": Issue['reviewed'],
                                    "scanner_id": Issue['id'],
                                    "suppressed": Issue["suppressed"],
                                    "removed_date": RemovedDate,
                                    "found_date"	: Issue['foundDate'],
                                    "confidence"	: Issue['confidence'],
                                    "impact"	: Issue['impact'],
                                    "scan_status": Issue['scanStatus'],
                                    "audited"	: Issue['audited'],
                                    "kingdom": Issue['kingdom'],
                                    "likelihood": Issue['likelihood'] ,
                                    "removed": Issue["removed"],
                                    "location_full": Issue["fullFileName"],
                                    "application_id": projectVersion['project']['id'],
                                    "application_name": projectVersion['project']['name'],
                                    "application_description": projectVersion['project']['description'],
                                    "application_version_id": projectVersion['id'],
                                    "application_version_name": projectVersion["name"],
                                    "vulnerability.application.name": projectVersion['project']['name'],
                                    "vulnerability.application.version.name": projectVersion["name"],
                                    "vulnerability.application.version.id": projectVersion['id'],
                                    "saltminer.is_vulnerability": is_vulnerability,
                                    "assessment_type": assessment_type,
                                    "last_scan_date": lastAssessmentDate,
                                    "primary_rule_guid": Issue['primaryRuleGuid'],
                                    "issue_instance_id": Issue['issueInstanceId'],
                                    "Critical": Critical,
                                    "High": High,
                                    "Medium": Medium,
                                    "Low": Low
                                    }
                            self.__AddIssueDetailsData(Issue, _app_vul)
                
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
                            self.__AddCustomAttributesToDocument(attributes, _app_vul)

                            # Track all keys again after asset attributes are added
                            issueKeys = []
                            if self.__SmApiClientEnabled:
                                for k in _app_vul.keys():
                                    issueKeys.append(k)
            
                            #
                            # Now apply custom logic for customer specific needs
                            #
                            try:
                                cancelTrk = CancelTracker(False)
                                self.__AppVulsCustom.CustomUpdateIssue(projectVersion, attributes, assessment_type, Issue, _app_vul, 'SSC', cancelTrk)
                                if not cancelTrk.Cancel:
                                    self.__AppVulsSscCustom.CustomUpdateIssue(projectVersion, attributes, assessment_type, Issue, _app_vul, cancelTrk)
                                cancel = cancelTrk.Cancel
                            except Exception as e:
                                raise AppVulsSSCException("Error in an AppVulsCustom customization") from e
            
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
                                self.__SmApiClient.MapEverything(_app_vul, issueAssetKeys, issueKeys, self.__HistoryV3Enable)

                            #
                            # Bulk insert the array of documents.
                            #
                            if not self.__DisableSM2Indices and not cancel:
                                bulkDocument = {
                                    '_index': 'app_vuls_ssc',
                                    '_id': 'SSC1-{}'.format(IssueKey),
                                    '_source': _app_vul        
                                    }
                                # self.__SendBulkItem(bulkDocument)

                            if (iCount % 1000 == 0):
                                if self.__DisableSM2Indices:
                                    p.Progress(iCount, 'Processing issues')
                                else:
                                    p.Progress(iCount, f'App/ver {appVerId}: added {iCount} docs to bulk queue')

                    #except KeyError as ex:
                    #    msg = f"Unknown scan type found: [{type(ex).__name__}] {ex}"
                    #    print(msg)
                    #    logging.warning(msg)
                    except Exception as ex:
                        msg = f"[{type(ex).__name__}] {ex}"
                        print(msg)
                        logging.error(msg)
                        raise(ex)

                    iCount = iCount + 1
                # End main issue handling loop

                scroller.GetNext()
            # end while len(scroller.Results)

        # Zero records handling - if any scan assessment type is not present in issues, add zero record
        for assessment_type in assessmentTypeStatuses.keys():
            if assessmentTypeStatuses[assessment_type]['present']:
                continue

            cancelTrk = CancelTracker(False)
            self.__AppVulsCustom.CustomBeforeIssueUpdate(projectVersion, attributes, assessment_type, 'SSC', cancelTrk)
            if not cancelTrk.Cancel:
                self.__AppVulsSscCustom.CustomBeforeIssueUpdate(projectVersion, attributes, assessment_type, cancelTrk)
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
                    "scanner_vendor":"Fortify",
                    "score_base":"0",
                    "score_environmental": "0",
                    "score_temporal":"0",
                    "score_version":"2.0",
                    "severity": "Zero",
                    "sor_url": "",
                    "name": "Zero",
                    "hidden": False,
                    "engine_type": assessmentTypeStatuses[assessment_type]['orgType'],
                    "engine_category": "",
                    "issue_status": "Zero",
                    "location": "",
                    "analyzer": "",
                    "reviewed": False,
                    "scanner_id": "",
                    "suppressed": False,
                    "removed_date": None,
                    "found_date"	: assessmentTypeStatuses[assessment_type]['lastscan'],
                    "confidence"	: 0,
                    "impact"	: 0,
                    "scan_status": "Existing",
                    "audited"	: False,
                    "kingdom": "Zero",
                    "likelihood": 0 ,
                    "removed": False,
                    "location_full": "",
                    "application_id": projectVersion['project']['id'],
                    "application_name": projectVersion['project']['name'],
                    "application_description": projectVersion['project']['description'],
                    "application_version_id": projectVersion['id'],
                    "application_version_name": projectVersion["name"],
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
            self.__AddCustomAttributesToDocument(attributes, _app_vul)

            # Track all keys again after asset attributes are added
            issueKeys = []
            if self.__SmApiClientEnabled:
                for k in _app_vul.keys():
                    issueKeys.append(k)

            cancelTrk = CancelTracker(False)
            self.__AppVulsCustom.CustomUpdateIssue(projectVersion, attributes, assessment_type, None, _app_vul, 'SSC', cancelTrk)
            if not cancelTrk.Cancel:
                self.__AppVulsSscCustom.CustomUpdateIssue(projectVersion, attributes, assessment_type, None, _app_vul, cancelTrk)
            cancel = cancelTrk.Cancel
            if cancel:
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
                    '_index': 'app_vuls_ssc',
                    '_id': 'SSC1-{}'.format(IssueKey),
                    '_source': _app_vul                
                    }
                #self.__SendBulkItem(bulkDocument)
            
        if not self.__DisableSM2Indices:
            self.__SendBulkItem()
            p.Finish(iCount, 'Insert complete.')
            
    def __SendBulkItem(self, doc=None):
        if doc:
            finishUp = False
            self.__BulkDocs.append(doc)
        else:
            finishUp = True
        if len(self.__BulkDocs) >= self.__BulkSendBatchSize or finishUp:
            if len(self.__BulkDocs) == 0:
                logging.info("Bulk queue empty, nothing to send.")
            else:
                logging.info("Bulk queue send (%s items)", len(self.__BulkDocs))
                self.__Es.BulkInsert(self.__BulkDocs)
                self.__BulkDocs = []

    def __AddCustomAttributesToDocument(self, attributes, _app_doc):
        '''Addes common app_version attributes to the document for searching'''
        #
        # This takes the custom attributes and attaches them to the passed in
        # _app_doc and is used by app_vuls_ssc, app_scan_history_ssc and app_
        # scans
        #
        for SSCAttribute in attributes:

            ESAttributeName = SSCAttribute.replace(' ', '_')

            if isinstance(attributes[SSCAttribute], list):
                _app_doc[ESAttributeName] = ', '.join(str(x) for x in attributes[SSCAttribute])
            else:
                _app_doc[ESAttributeName] = '{}'.format(attributes[SSCAttribute]).strip()
        

    def __GetAttributes(self, appVerId, holdProjectVersion):

        attributes = {}

         # declare a filter query dict object
        match_all = {
          "size": 10000,
          "query": { 
            "term": { "projectVersionId": { "value": appVerId } } 
          }
        }

        SSCAttributes = self.__Es.SearchWithCursor('_id', 'sscprojattr2', match_all)

        for SCCAttribute in SSCAttributes.values():
            attribName = SCCAttribute['attributeName'].lower().replace(' ', '_')
            attributes[attribName] = SCCAttribute['attributeValue']
       
        cancelTrk = CancelTracker(False)
        self.__AppVulsCustom.CustomUpdateAttributes(holdProjectVersion, attributes, "SSC", cancelTrk)
        if not cancelTrk.Cancel:
            self.__AppVulsSscCustom.CustomUpdateAttributes(holdProjectVersion, attributes, cancelTrk)
        cancel = cancelTrk.Cancel
        if cancel:
            return { 'cancel': True }
        
        return attributes

    def _GetAllSscProjects(self):
        scroller = self.__Es.SearchScroll("sscprojects", None, 500, None)
        lst = {}
        c = 0
        while scroller.Results:
            for dto in scroller.Results:
                lst[dto['_source']['id']] = dto['_source']
                c += 1
                if c % 500 == 0:
                    logging.info("Loading ssc project versions, %s of %s", c, scroller.TotalHits)
            scroller.GetNext()
        scroller.Clear()
        return lst

    def _GetSscProjectVersion(self, id):
        '''Return a single project version by project version ID'''
        res = self.__Es.Search('sscprojects', { "query": { "term": { "id": id } } })
        if res and len(res) > 0:
            if ("_source" in res[0].keys()):
                return res[0]['_source']
        return None
   
    def __GetSscProjScansByProjectId(self, projectVersionId):
        # declare a filter query dict object
        match = {
            "size": 1000,
            "query": {
                "bool" : {
                    "must": [{"term": {"projectVersionId": projectVersionId}}]
                    }
                }
            }

        return self.__Es.SearchWithCursor('_id', 'sscprojscans', match)
        
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


        QueueList = self.__Es.SearchWithCursor('_id', 'sscupdatequeue', _post)

        for QueueKey in QueueList:
            QueueDoc = QueueList[QueueKey]
            QueueDoc['completedDateTime'] = "1900-01-01T00:00:00"
            self.__Es.IndexWithId('sscupdatequeue', QueueKey, QueueDoc)

        
class AppVulsSSCException(Exception):
    pass