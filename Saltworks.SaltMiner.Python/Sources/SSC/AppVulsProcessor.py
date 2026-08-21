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

# 10/8/2021 TD
# Originally AppVulsSSC.py and class AppVulsSSC.

import datetime
import json
import logging
import time
from dateutil.parser import parse as dtparse

from Utility.CancelTracker import CancelTracker
from Utility.DImport import DImport
from Utility.ProgressLogger import ProgressLogger
from Utility.SmApiClient import SmApiClient
from Utility.UpdateQueueHelper import UpdateQueueHelper

ISSUE_COUNT_RECHECK_DELAY_SEC = 5

# How hard to chase the sync stage's expected issue count before giving up and letting the pull run
# short (the guard then reports it).  One refresh, then this many counts spaced by the delay.
ISSUE_VISIBILITY_ATTEMPTS = 3
ISSUE_VISIBILITY_DELAY_SEC = 2


class AppVulsProcessor(object):
    """ App vulnerability processor for SSC """

    def __init__(self, appSettings, sourceName, smv3ConfigName="SMv3", mainConfigName="Main", logger=None, heartbeat=None, agent_mode:bool=False):
        '''
        :heartbeat: optional zero-arg callable invoked as work progresses.  Supplied by SyncWorker
        so the agent can tell a slow refresh from a defunct worker; None (the default) for standalone runs.
        :agent_mode: set by SyncWorker.  Marks a run that handles one app version per invocation, so
        per-run costs the batch runners amortise over a whole queue are paid every item instead - see
        SmApiClient's refresh_indices.
        '''
        if type(appSettings).__name__ != "ApplicationSettings":
            raise TypeError("Type of appSettings must be 'ApplicationSettings'")
        if not smv3ConfigName or smv3ConfigName not in appSettings.GetConfigNames():
            raise AppVulsSSCException(f"Invalid or missing configuration for name '{smv3ConfigName}'")
        if not mainConfigName or mainConfigName not in appSettings.GetConfigNames():
            raise AppVulsSSCException(f"Invalid or missing configuration for name '{mainConfigName}'")

        self.__Logger = logger or logging.getLogger(__name__)
        self.__Heartbeat = heartbeat
        self.__Es = appSettings.Application.GetElasticClient()
        self.__Attributes = appSettings.Get(mainConfigName, 'Attributes')
        self.__App = appSettings.Application
        self.__UpdateQHelper = UpdateQueueHelper(appSettings, sourceName)
        self.__LastScanDateField = appSettings.GetSource(sourceName, "LastScanDateField", "lastScanDate")
        self.__BulkDocs = []
        self.__IssueCountMismatch = None  # set by __CheckIssueCounts when a pull came up short
        self.__ExpectedIssueCount = None  # set per run by PopulateVulsOne, from the sync stage
        self.__ExpectedScanCount = None   # ditto - scan history is built from these
        self.__BulkSendBatchSize = appSettings.GetSource(sourceName, "BulkSendBatchSize", 1000)
        self.__SourceName = sourceName

        #
        # SM API Integration
        # Setup API Client enable switch
        # Create SaltMiner API Client class (attempts to connect upon creation)
        #
        self.__SmApiClientEnabled = appSettings.Get(smv3ConfigName, "ApiClientEnabled", False)
        if self.__SmApiClientEnabled:
            self.__SmApiClient = SmApiClient(appSettings, sourceName, smv3ConfigName, refresh_indices=not agent_mode)
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
            self.__Logger.warn("Assessment type map missing from source name '%s'.  This will cause all scans to be considered assessment type 'Unknown'.", sourceName)
               
        self.__Logger.info("AppVulsSSC init complete, connected to Elastic")

    @property
    def SourceName(self):
        return self.__SourceName

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

        self.__Logger.debug('Mapping of indices complete.')

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
        self.__Logger.info('Mapping indices as needed...')
        self.MapAppSecVuls(False)

        # Remove app versions marked for delete
        b = 1
        c = 0
        uq, total = self.__UpdateQHelper.GetUpdateQueueBatch(["D"])
        while uq and len(uq) > 0:
            for qitem in uq:
                if c % 10 == 0:
                    self.__Logger.info("Retiring app version(s), %s of %s (batch %s)", c, total, b)
                qid = qitem['id'] if qitem and 'id' in qitem.keys() else None
                if qid:
                    self.__Logger.debug("Deleting v2 data for project version id %s", qid)
                    self.__DeleteStuff(qid)
                else:
                    self.__Logger.warning("Skipping qitem for deleting v2 data, invalid. qitem: %s", qitem)
                self.__UpdateQHelper.CompleteUpdateQueue(qid, ["D"])
                c += 1
            # next batch
            uq, total = self.__UpdateQHelper.GetUpdateQueueBatch(["D"])
            b += 1
        if c > 0:
            self.__Logger.info("Waiting 5 sec..")
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
                    self.__Logger.info("Project version data outdated, reloading...")
                    sscProjectListPulledAt = datetime.datetime.now(datetime.timezone.utc)
                    sscProjects = self._GetAllSscProjects()

                if qid in updated:
                    retries +=1
                    if retries >= 10:
                        self.__Logger.warning("App version %s recently processed but failed to update after 10 retries, canceling process.", qid)
                        break
                    # We already processed this PVID - might be ahead of the db so let's pause
                    self.__Logger.info('Skipping app version %s, already processed.  Pausing for 5 sec to let the db catch up...', qid)
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
                self.__SmApiClient.finalize_everything()
        # end while

        p.Finish(c, "App version updates complete.")
        if cleanupAfter:
            self.Cleanup()

    def PopulateVulsOne(self, pvid, cleanupAfter=True, race_retry:bool=False, race_retry_delay:int=5, expected_issue_count:int=None, expected_scan_count:int=None):
        '''
        Process one project version (doesn't have to be in the update queue).
        Returns the list of queue scan IDs created (empty when SM API integration is disabled or nothing processed).

        :race_retry: passed through to _GetSscProjectVersion - see there.
        :expected_issue_count: how many issues the sync stage just wrote for this project version
        (SyncResult.expected_issue_count).  None when the caller doesn't know - the sync didn't re-load
        it, or this isn't the agent path - in which case the pull proceeds as before.  When known, it is
        the authoritative number: the pull waits for the index to show it and is checked against it,
        rather than against an index count that is still settling.
        '''

        self.__ExpectedIssueCount = expected_issue_count
        self.__ExpectedScanCount = expected_scan_count
        sscProject = self._GetSscProjectVersion(pvid, race_retry, race_retry_delay)
        if not sscProject:
            self.__Logger.error("Couldn't retrieve project version %s from SSC, skipping this update.", pvid)
            return []

        # Ensure the mappings exist and create them if they don't
        self.__Logger.info('Mapping indices if needed')
        self.MapAppSecVuls(False)

        # Process
        self.__Logger.info('Running PopulateVulsOne for project version %s', pvid)
        self.__ProcessUpdate(pvid, { int(pvid): sscProject })
        #
        # SM API Integration
        # Finalize batch items for issues and complete queue scans - unless the issue pull came up
        # short, in which case the load is incomplete and the queue scans are cancelled instead so
        # the manager never processes partial data.  Cleanup still runs; the raise is the last thing.
        #
        countError = self.__IssueCountMismatch
        queue_scan_ids = []
        if self.__SmApiClientEnabled:
            self._Beat()
            if countError:
                self.__SmApiClient.abort_everything(countError)
            else:
                queue_scan_ids = self.__SmApiClient.finalize_everything()
            self._Beat()

        self.__Logger.info("Complete" if not countError else "Complete (queue load abandoned)")
        if cleanupAfter:
            self.Cleanup()
        if countError:
            raise AppVulsSSCException(countError)
        return queue_scan_ids

    def CancelQueueScan(self, queue_scan_id, lock_id=None):
        '''
        Cancels a single queue scan by ID, for a load the manager didn't carry through.  Pass the lock the
        manager reported holding, or the api rejects the change for a locked scan.  Returns True if
        the status was set.  Never raises - a rejected transition (the manager already set Error, or our
        view of the status is stale) is the caller's to log, not a reason to fail harder.
        '''
        if not self.__SmApiClientEnabled:
            return False
        try:
            self.__SmApiClient.cancel_queue_scan(queue_scan_id, lock_id)
            return True
        except Exception as ex:
            self.__Logger.warning("Could not cancel queue scan %s: [%s] %s", queue_scan_id, type(ex).__name__, ex)
            return False


    def _Beat(self):
        '''
        Signal progress to the caller's heartbeat delegate, if one was supplied.  No-op for
        standalone runs.  Never lets a heartbeat failure break the work in progress.
        '''
        if self.__Heartbeat is not None:
            try:
                self.__Heartbeat()
            except Exception:
                self.__Logger.debug("Heartbeat delegate raised; ignoring.", exc_info=True)

    def Cleanup(self):
        if self.__AppVulsSscCustom.Cleanup:
            self.__AppVulsSscCustom.Cleanup()

    def Nvl(self, obj:dict, prop:str, replace=None):
        return replace if prop not in obj.keys() else obj[prop]

    def __GetAssessmentType(self, engineType):
        if not engineType:
            engineType = ""
        engineType = str(engineType).upper()
        if engineType in self.__AssessmentTypeMap.keys():
            return self.__AssessmentTypeMap[engineType]
        else:
            self.__Logger.warning("Assessment type '%s' not configured in map.", engineType)
            return "Unknown"

    def __ProcessUpdate(self, avid, sscProjects):
        '''Primary method calls the various private methods to update vuls, history and tests'''
        # Cleared per app version, not per pull: __UpdateIssues can return early before it ever counts
        # anything, and SyncWorker reuses this instance across queue items - so a mismatch left over from
        # a previous app version would fail the next one and cancel its queue scans.
        self.__IssueCountMismatch = None

        attributes = {}
        appVerId = int(avid)
        appVer = None
        for i in range(1, 3):
            delay = (i - 1) * 30
            if appVerId not in sscProjects.keys():
                self.__Logger.warning("App version %s not found in SSC extract data, retrying (%s of 3) after %s sec delay...", appVerId, i, delay)
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
            self.__Logger.warning("App version %s not found in SSC extract data and will be skipped", appVerId)
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
            self.__SmApiClient.map_scanless_asset(appVerId, "Fortify", appVer['project']['name'], appVer['name'], appVer['description'], attributes, assessment_types=list(lastScans.keys()))

        #
        # Update the Issues for the project
        #
        self.__UpdateIssues(lastScans, appVerId, sscProjects, attributes)

    @staticmethod
    def __GetDateStr(ds):
        if not ds:
            return None
        i = ds.find(".")
        if i > -1:
            ds = ds[0:i]
        try:
            return dtparse(ds).isoformat()
        except Exception as e:
            raise(ValueError(f"Date string '{ds}' is incorrect ({e})"))

    def __UpdateScanHistory(self, appVerId, sscProjects, attributes):
        
        #
        # It's possible we have a record in the Queue that has since been
        # removed from SSC, in that case we can bail out
        #
        if appVerId not in sscProjects.keys():
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
        # Scan history is built from these, so a scan that isn't visible yet is a history record missing
        # from the final index.  Worse than the issue case: scans are written one document at a time with
        # no bulk barrier at all, so there is nothing to hang a wait_for on - the count is all we have.
        self.__WaitForExpectedCount("sscprojscans", { "query": { "bool": { "must": [
            { "term": { "projectVersionId": appVerId } } ] } } }, appVerId, self.__ExpectedScanCount, "scan")
        projectScans = self.__GetSscProjScansByProjectId(appVerId)

        allDocsToInsert = []

        lastScans = {}
        for scan in projectScans.values():
            self._Beat()
            if 'type' not in scan and 'scanrec' in scan:
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
                    lastScans[assessment_type] = { "lastscan": datetime.datetime(1900, 1, 1).isoformat(), "orgType": scanType, "engineVersion": None }
                if scanDate > lastScans[assessment_type]['lastscan'] and not cancel:
                    # Update last scan date if newer for this assessement type
                    lastScans[assessment_type] = { "lastscan": scanDate, "orgType": scanType, "engineVersion": None }
                if lastScans[assessment_type]['engineVersion'] is None and 'engineVersion' in scan:
                    # Set engine version if not already set and available
                    lastScans[assessment_type]['engineVersion'] = scan['engineVersion']

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
                self.__Logger.error(f"[{type(e).__name__}] {e}", exc_info=1)
                # continue implied since last line of for block

            # end for

        if not self.__DisableSM2Indices:
            #
            # Send any remaining bulk docs
            #
            self.__SendBulkItem()
        self.__Logger.info(f"Inserting {len(allDocsToInsert)} scan history doc(s) for id {projectVersion['id']}")
        return lastScans

    def __WaitForExpectedCount(self, index, query, appVerId, expected, label='issue'):
        '''
        Blocks until the source index shows the number of documents the sync stage said it wrote.

        The sync bulk-loads with refresh='wait_for', but that only refreshes the shards its final
        request wrote to - '{index}' has more than one, so a small final batch can leave another shard's
        documents unsearchable.  Rather than refresh the whole index on every app version, count first
        and only refresh when the count is short: the healthy majority costs one _count, and the
        stragglers cost the refresh they actually need.

        Never raises.  A count that never arrives is left to __CheckIssueCounts to report, with the
        numbers, once the pull has actually happened.
        '''
        if expected is None:
            return
        try:
            count = self.__Es.Count(index, self.__CountSafeQuery(query))
            if count == expected:
                return
            self.__Logger.warning("Sync wrote %s %s(s) for %s but only %s are visible - refreshing '%s' and re-counting...",
                                  expected, label, appVerId, count, index)
            self.__Es.RefreshIndex(index)
            for attempt in range(ISSUE_VISIBILITY_ATTEMPTS):
                count = self.__Es.Count(index, self.__CountSafeQuery(query))
                if count == expected:
                    self.__Logger.info("All %s %s(s) for %s visible after refresh%s.", expected, label, appVerId,
                                       f" and {attempt} recheck(s)" if attempt else "")
                    return
                if attempt < ISSUE_VISIBILITY_ATTEMPTS - 1:
                    time.sleep(ISSUE_VISIBILITY_DELAY_SEC)
            # Still short.  Not a visibility problem any more - the pull runs and the guard reports it.
            self.__Logger.error("Only %s of %s %s(s) for %s are visible after a refresh and %s recheck(s) - the read will be short.",
                                count, expected, label, appVerId, ISSUE_VISIBILITY_ATTEMPTS)
        except Exception as ex:
            self.__Logger.warning("Could not verify %s visibility for %s: [%s] %s", label, appVerId, type(ex).__name__, ex)


    @staticmethod
    def __CountSafeQuery(query):
        '''
        A copy of a search body with the paging keys stripped - _count rejects them.  The scroller adds
        "sort" to the dict it is handed, in place, so a query that has been through it is not count-safe.
        '''
        q = json.loads(json.dumps(query))
        for key in ('sort', 'search_after', '_source', 'size', 'from'):
            q.pop(key, None)
        return q


    def __CheckIssueCounts(self, index, query, appVerId, syncCountBefore, pulledCount):
        '''
        Validates that the issue pull read the whole source set.

        The sync stage bulk-writes these issues immediately before the refresh stage scrolls them, and
        elasticsearch makes writes searchable asynchronously, so a scroll can quietly return a short
        set.  Comparing the count of the source index taken *after* the pull against the number of
        documents the pull actually read catches that: they agree on a complete read and disagree when
        documents became visible mid-pull.  A single {delay} sec recheck confirms the discrepancy is
        real rather than a count taken mid-refresh.

        Records the mismatch on the instance (read by PopulateVulsOne, which cancels the queue scans
        and errors the item) and logs it.  Never raises - the caller decides what to do about it.

        Deliberately does NOT count the final issues index: it holds every app version for the
        source/instance, the manager merges rather than appends, and synthetic zero/noscan records and
        cancel-hook drops all move that number legitimately.  See PBI-0005.
        '''
        countQuery = self.__CountSafeQuery(query)

        # When the sync stage told us what it wrote, that is the number that matters: it is what SHOULD
        # be there, where the index count is only what happens to be visible.  Reading the right number
        # of documents is a complete pull even if the index has since moved.
        expected = self.__ExpectedIssueCount
        if expected is not None and pulledCount == expected:
            self.__Logger.debug("Pulled all %s issue(s) the sync stage wrote for %s.", expected, appVerId)
            return

        syncCountAfter = self.__Es.Count(index, countQuery)
        if syncCountAfter == pulledCount:
            if syncCountBefore != syncCountAfter:
                # Settled before we finished reading, so the pull is still complete - worth knowing.
                self.__Logger.info("Source issue count for %s moved during the pull (%s -> %s) but the pull read all %s.",
                                   appVerId, syncCountBefore, syncCountAfter, pulledCount)
            return

        self.__Logger.warning("Issue count mismatch for %s: pulled %s, '%s' now holds %s (%s at pull start).  Rechecking in %s sec...",
                              appVerId, pulledCount, index, syncCountAfter, syncCountBefore, ISSUE_COUNT_RECHECK_DELAY_SEC)
        time.sleep(ISSUE_COUNT_RECHECK_DELAY_SEC)
        recheckCount = self.__Es.Count(index, countQuery)
        if recheckCount == pulledCount:
            self.__Logger.info("Issue count for %s matched on recheck (%s) - pull was complete.", appVerId, recheckCount)
            return

        moved = "" if syncCountBefore == syncCountAfter else f", and moved during the pull ({syncCountBefore} at start)"
        # Naming the expected count separates the two causes: short of what the sync wrote is a
        # visibility problem, while an index holding MORE than the sync wrote means something else is
        # writing this app version.
        target = "" if expected is None else f"  The sync stage wrote {expected}."
        self.__IssueCountMismatch = (
            f"Incomplete issue pull for {appVerId}: read {pulledCount} issue(s) but '{index}' holds "
            f"{recheckCount} after a {ISSUE_COUNT_RECHECK_DELAY_SEC} sec recheck ({syncCountAfter} on first check){moved}.{target} "
            "The source data was still becoming visible while it was being read - re-run the sync for this ID."
        )
        self.__Logger.error(self.__IssueCountMismatch)


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
        # flushAfter off - this runs per app version (twice), and a flush is a Lucene commit on indices
        # every worker is writing.  Nothing reads these deletes back within the run.
        if not issuesOnly:
            self.__Es.DeleteByQuery("app_scan_history_ssc", DeleteQuery, flushAfter=False)
        if not scansOnly:
            self.__Es.DeleteByQuery("app_vuls_ssc", DeleteQuery, flushAfter=False)

    def __GetAssessmentTypes(self):
        lst = []
        for k in self.__AssessmentTypeMap.keys():
            atype = self.__AssessmentTypeMap[k]
            if atype not in lst:
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
                if ca not in issue.keys():
                    issue[ca] = {}
                for tag in srcIssue[ctv]:
                    if 'keyValue' not in tag.keys() or not tag['keyValue'] or 'name' not in tag['keyValue'].keys() or 'value' not in tag['keyValue'].keys():
                        self.__Logger.debug("Missing/null keyValue in customTagValue for issue %s", srcIssue['id'])
                        continue
                    issue[ca][tag['keyValue']['name']] = tag['keyValue']['value']

    def __UpdateIssues(self, lastScans, appVerId, sscProjects, attributes):
        # It's possible we have a record in the Queue that has been removed from SSC, in that case we can bail out
        if appVerId not in sscProjects.keys():
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
                    assessmentTypeStatuses[assessment_type] = { "lastscan": lastScans[assessment_type]['lastscan'], "present": False, "orgType": lastScans[assessment_type]['orgType'], "engineVersion": lastScans[assessment_type]['engineVersion'] }
                else:
                    self.__Logger.debug("App version %s appears to have no assessments of type '%s'", appVerId, assessment_type)

        #
        # It's possible we have a record in the Queue that has since been
        # removed from SSC, in that case we can bail out
        #
        if appVerId not in sscProjects.keys():
            return

        # Get a list of all the vulnerabilites found by SSC with the matching
        # project ID
        issueQuery = { "query": { "term": { "projectVersionId": appVerId }}}
        p = ProgressLogger(self.__Es)      

        # Race guard - the sync stage wrote these issues moments ago and elasticsearch's refresh is
        # asynchronous, so a scroll started too early silently pulls a short set.  When the sync told us
        # how many it wrote, wait for exactly that many to be visible before starting; otherwise
        # TotalCount below is all we have to go on and __CheckIssueCounts does the checking at the end.
        self.__WaitForExpectedCount("sscprojissues", issueQuery, appVerId, self.__ExpectedIssueCount, "issue")

        with self.__Es.SearchScroll("sscprojissues", queryBody=issueQuery, scrollSize=1000, scrollTimeout=None) as scroller:
            TotalCount = scroller.TotalHits if scroller else 0
            p.Start("PopulateVuls-UpdateIssues", TotalCount, "PopulateVuls-UpdateIssues Status")
            iCount = 0
            while len(scroller.Results):

                # Main issue handling loop
                for IssueContainer in scroller.Results:
                    self._Beat()
                    Issue = IssueContainer['_source']
                    # IssueKey = IssueContainer['_id']
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
                            if Issue['removed'] and not Issue['removedDate']:
                                Issue['removedDate'] = '1876-01-01T00:00:00.000+00:00'

                            # Check to see if the Fortify vulnerability is "active", ie should be shown.
                            if Issue['suppressed'] or Issue['removed'] or Issue['hidden']:
                                IssueActive = False
                            RemovedDate = None if Issue['removed'] else Issue['removedDate']

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
                            engineVersion = None
                            if assessment_type in assessmentTypeStatuses.keys():
                                # Set to last scan date for the assessment type if we know it
                                lastAssessmentDate = assessmentTypeStatuses[assessment_type]['lastscan']
                                # Set engineVersion if available
                                engineVersion = assessmentTypeStatuses[assessment_type]['engineVersion']
                            if Issue['foundDate'] is None:
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
                                    "engine_version": engineVersion,
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
                                self.__SmApiClient.map_everything(_app_vul, issueAssetKeys, issueKeys, self.__HistoryV3Enable)

                            #
                            # Bulk insert the array of documents.
                            #
                            # if not self.__DisableSM2Indices and not cancel:
                            #     bulkDocument = {
                            #         '_index': 'app_vuls_ssc',
                            #         '_id': 'SSC1-{}'.format(IssueKey),
                            #         '_source': _app_vul        
                            #         }
                                # self.__SendBulkItem(bulkDocument)

                            if (iCount % 1000 == 0):
                                if self.__DisableSM2Indices:
                                    p.Progress(iCount, 'Processing issues')
                                else:
                                    p.Progress(iCount, f'App/ver {appVerId}: added {iCount} docs to bulk queue')

                    #except KeyError as ex:
                    #    msg = f"Unknown scan type found: [{type(ex).__name__}] {ex}"
                    #    print(msg)
                    #    self.__Logger.warning(msg)
                    except Exception as ex:
                        msg = f"[{type(ex).__name__}] {ex}"
                        print(msg)
                        self.__Logger.error(msg)
                        raise(ex)

                    iCount = iCount + 1
                # End main issue handling loop

                scroller.GetNext()
            # end while len(scroller.Results)

        # TotalCount is scroller.TotalHits, captured before the first page was consumed.  It is an exact
        # count - the scroller back-fills a _count of its own whenever elasticsearch reports a capped
        # 'gte' total - and it has to be read there rather than here: this pull uses search_after, which
        # re-evaluates the total on every page.
        self.__CheckIssueCounts("sscprojissues", issueQuery, appVerId, TotalCount, iCount)

        # Zero records handling - if any scan assessment type is not present in issues, add zero record
        for assessment_type in assessmentTypeStatuses.keys():
            if assessmentTypeStatuses[assessment_type]['present']:
                continue

            self._Beat()
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
                    "engine_version": "",
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

            self.__Logger.info("Adding zero record for app version %s and assessment type %s", appVerId, assessment_type)
            # IssueKey = f"{appVerId}-{assessment_type}-0"
            #
            # SM API Integration
            # Submit queue issue to SM API
            #
            if self.__SmApiClientEnabled and _app_vul:
                self.__SmApiClient.map_everything(_app_vul, issueAssetKeys, issueKeys)

            # if not self.__DisableSM2Indices:
                #
                # Bulk insert the array of documents.
                #
                # bulkDocument = {
                #     '_index': 'app_vuls_ssc',
                #     '_id': 'SSC1-{}'.format(IssueKey),
                #     '_source': _app_vul                
                #     }
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
                self.__Logger.info("Bulk queue empty, nothing to send.")
            else:
                self.__Logger.info("Bulk queue send (%s items)", len(self.__BulkDocs))
                self._Beat()
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
                    self.__Logger.info("Loading ssc project versions, %s of %s", c, scroller.TotalHits)
            scroller.GetNext()
        scroller.Clear()
        return lst

    def _GetSscProjectVersion(self, id, race_retry:bool=False, race_retry_delay:int=5):
        '''
        Return a single project version by project version ID.

        :race_retry: when the project version isn't found, wait race_retry_delay seconds and look
        once more.  Callers that read straight after the sync stage wrote the doc (the sync worker)
        set this so elasticsearch's near-real-time refresh gap doesn't look like a missing PV.
        '''
        query = { "query": { "term": { "id": id } } }
        res = self.__Es.Search('sscprojects', query)
        if race_retry and not res:
            self.__Logger.info("Project version %s not found, retrying in %s sec in case this is a race condition...", id, race_retry_delay)
            time.sleep(race_retry_delay)
            res = self.__Es.Search('sscprojects', query)
            if res:
                self.__Logger.info("Found project version %s after waiting for the index to catch up.", id)
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