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

import json
import logging
import datetime

from Core.ElasticClient import ElasticClient
from Core.FodClient import FodClient
from Utility.ProgressLogger import *
from Utility.SyncQueueHelper import *
from elasticsearch import Elasticsearch, NotFoundError, exceptions, ConflictError

class SyncExtractor(object):
    """Extraction of Open FOD Changes"""

    def __init__(self, appSettings, sourceName, logger=None, heartbeat=None):
        '''
        :heartbeat: optional zero-arg callable invoked as work progresses.  Supplied by SyncWorker
        so the agent can tell a slow sync from a defunct worker; None (the default) for standalone runs.
        '''
        if type(appSettings).__name__ != "ApplicationSettings":
            raise TypeError("Type of appSettings must be 'ApplicationSettings'")
        if not sourceName or not sourceName in appSettings.GetSourceNames():
            raise SyncExtractorException(f"Invalid or missing source configuration for source name '{sourceName}'")

        self.__Logger = logger or logging.getLogger(__name__)
        self.__Heartbeat = heartbeat
        self.__Fod = FodClient(appSettings, sourceName, heartbeat=heartbeat)
        self.__Es = appSettings.Application.GetElasticClient()
        self.__SyncQueue = SyncQueueHelper(appSettings, sourceName)
        self.__PreloadReleases = appSettings.GetSource(sourceName, 'SyncPreloadReleases', True)
        self.__ApplicationCache = {}
        self.__CheckAttributes = appSettings.GetSource(sourceName, 'SyncCheckAttributes', False)
        self.__SourceName = sourceName
        self.__SourceNameField = "sourceName"

        self.__Logger.debug("ExtractFOD.init complete.")

    @property
    def SourceName(self):
        return self.__SourceName

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

    def MapESIndices(self, Force):
        
        # map fodreleases elastic table
        self.__Es.MapIndex("fodreleases", Force)

        # map forapplications elastic table
        self.__Es.MapIndex("fodapplications", Force)
        
        # map fodcounts elastic table
        self.__Es.MapIndex("fodcounts", Force)

        # map fodscans elastic table
        self.__Es.MapIndex("fodscans", Force)
        
        # map fodscansummary elastic table
        self.__Es.MapIndex("fodscansummary", Force)

        # map fodrelissues elastic table
        self.__Es.MapIndex("fodrelissues", Force)
        
        # map fodupdatequeue elastic table
        self.__Es.MapIndex("fodupdatequeue", Force)

    def __GetElasticDataByKeyField(self, index, idKey, keyField="releaseId"):
        body = { 
            "query": {
                "bool": {
                    "must": [
                        { "term": { keyField: { "value": idKey }}},
                        { "term": { self.__SourceNameField: { "value": self.__SourceName }}}
                    ]
                }
            }
        }
        lst = self.__Es.Search(index, body, navToData=True)
        return lst[0] if lst else None

    def __GetElasticFodRelease(self, releaseId):
        return self.__GetElasticDataByKeyField('fodreleases', releaseId)

    def __GetElasticFodCounts(self, releaseId):
        return self.__GetElasticDataByKeyField('fodcounts', releaseId)

    def __GetElasticFodApplication(self, appId):
        return self.__GetElasticDataByKeyField('fodapplications', appId, 'applicationId')

    def __GetRelease(self, avid, avList):
        # 1 - return from memory list
        if not avList:
            avList = []
        for itm in avList:
            if str(itm['releaseId']) == str(avid):
                return itm
        # 2 - return from elastic
        rel = self.__GetElasticFodRelease(avid)
        if rel:
            return rel
        # 3 - return from FOD (or None if not found)
        rel = self.__Fod.GetRelease(avid).Content
        rel[self.__SourceNameField] = self.__SourceName
        return rel

    def __ClearRelease(self, appId, relId):
        releaseComponents = [['fodapplications', 'applicationId', appId],['fodreleases', 'releaseId', relId],['fodcounts', 'releaseId', relId],
                                ['fodscans', 'releaseId', relId],['fodscansummary', 'releaseId', relId],['fodrelissues', 'releaseId', relId]]
        for component in releaseComponents:
            try:
                qry = { "query": { "bool": { "must": [  
                    { "term": { self.__SourceNameField: { "value": self.__SourceName } } },
                    { "term": { component[1]: { "value": component[2] } } }
                ] }}}
                self.__Es.DeleteByQuery(component[0], qry)
            except ConflictError as e:
                msg = e.args[0] if e and e.args else "unknown"
                self.__Logger.warning("[SyncExtractor] Conflict Error (409) clearing index %s for id %s: %s", component[0], component[2], msg)
                continue
    
    def CheckDrop(self, safetyOverride=False):
        self.__Logger.info('Compare local data with FOD looking for dropped app/versions.  Loading releases for current source...')
        body = { 
            "sort": [ "releaseId"], 
            "_source": ["releaseId", "releaseName", "applicationId", "applicationName"], 
            "query": { "term": { self.__SourceNameField: { "value": self.__SourceName }}}
        }
        scroller = self.__Es.SearchScroll("fodreleases", body, scrollSize=500, scrollTimeout=None)
        esTotal = scroller.TotalHits
        if esTotal <= 0:
            self.__Logger.warning("No FOD releases found in local data (fodreleases).")
            return
        fscroller = self.__Fod.GetReleases(fields="releaseId", scroller=True)
        releases = []
        if fscroller and fscroller.TotalHits > 0:
            for rel in fscroller.GetAll():
                releases.append(rel['releaseId'])
        else:
            self.__Logger.warning("No releases found in FOD.")
        fscroller = None
        fodTotal = len(releases)
        self.__Logger.info("Totals: Local FOD count: %s, Actual FOD count: %s", esTotal, fodTotal)
        if esTotal > fodTotal and (int(abs(esTotal - fodTotal) / esTotal * 100) > 5):
            if safetyOverride:
                self.__Logger.warning("Local data counts are higher than FOD by more than 5%, safety override means we're cleaning house anyway.")
            else:
                self.__Logger.error("Local counts are higher than SSC by more than 5%, canceling auto-drop of FOD app versions from SaltMiner.  CheckDrop can be called manually with a safety override switch if desired.")
                return

        p = ProgressLogger(self.__Es)
        p.Start("[CheckDrop]", esTotal, "CheckDrop Status")
        p.Progress(0, 'Starting CheckDrop - check for FOD dropped releases')

        iDropCount = 0
        iCount = 0

        while len(scroller.Results):
            for dto in scroller.Results:
                esRelease = dto['_source']
                if not esRelease['releaseId'] in releases:
                    self.__Logger.info('Removing release with ID %s', esRelease['releaseId'])
                    self.__ClearRelease(esRelease['applicationId'], esRelease['releaseId'])
                    qdoc = {
                        'processedDateTime' : datetime.datetime.now().strftime("%Y-%m-%dT%H:%M:%S"),
                        'releaseId': esRelease['releaseId'],
                        'updateType': 'D',
                        'updateReason': 'CheckDrop did not find this app version in FOD',
                        'completedDateTime' : '1900-01-01T00:00:00.000-0000',
                        self.__SourceNameField: self.__SourceName
                    }
                    self.__Es.Index('fodupdatequeue', json.dumps(qdoc))
                    iDropCount += 1
                iCount += 1
                if iCount % 50 == 0 or iCount == esTotal:
                    p.Progress(iCount, 'Processed {} of {}'.format(iCount, esTotal))
            scroller.GetNext()
        self.__Logger.info('Total releases dropped: %s', iDropCount)
        p.Finish(esTotal, "Complete")

    def ReloadSyncQueue(self, clearSyncQueue='none'):
        '''
        Reloads sync extractor queue, optionally clearing data first.

        clearSyncQueue - 'none' means do not clear, 'completed' for completed, 'unlocked' for unlocked only, 'locked' for all locked, 'all' for all.
        '''
        avList = self.__Fod.GetReleases(fields='releaseId', scroller=True).GetAll()

        if not clearSyncQueue in ['none', 'all', 'locked', 'unlocked', 'completed']:
            raise SyncExtractorException("Invalid/unsupported clearSyncqueue value.")
        if clearSyncQueue != 'none':
            completed = clearSyncQueue in ['all', 'completed']
            locked = clearSyncQueue in ['all', 'locked']
            self.__SyncQueue.ClearSyncQueue(completed, locked)

        count = 0
        idList = []
        for itm in avList:
            if count > 0 and count % 200 == 0:
                self.__SyncQueue.InsertQueueBatch(idList)
                idList = []
                self.__Logger.info("Reloading sync queue: processed %s IDs", count)
            idList.append(itm['releaseId'])
            count += 1
        if len(idList) > 0:
            self.__SyncQueue.InsertQueueBatch(idList)
        self.__Logger.info("Sync queue reloaded successfully.")
    
    def ProcessOne(self, avid, forceRefresh=False):
        '''
        Enables sync of a single release (by id), bypassing the queue entirely
        '''
        # Check mappings - ensures indices are created from templates rather than dynamically mapped by first doc write
        self.MapESIndices(False)
        releases = []
        release = self.__GetRelease(avid, releases)
        if not release:
            raise SyncExtractorException(f"Release {avid} could not be found.")
        self.__Logger.info('Syncing FOD to Elastic for release %s', avid)
        self.__ProcessOne(release['_source'], forceRefresh)
        self.__Logger.info('Sync complete.')

    def Process(self, reloadSyncQueue=False):
        '''
        Runs sync for queued releases, optionally reloading the queue before starting.

        reloadSyncQueue - if set, reloads the queue from sscprojects, skipping any existing PVs.
        '''

        # Check mappings
        self.__Logger.debug("Ensuring Mappings are available")
        self.MapESIndices(False)

        # Reload sync queue if configured
        if reloadSyncQueue:
            self.ReloadSyncQueue()

        # Preload releases if configured
        allReleases = []
        if self.__PreloadReleases:
            allReleases = self.__Fod.GetReleases(scroller=True).GetAll()
            for rel in allReleases:
                rel[self.__SourceNameField] = self.__SourceName


        # Main queue loop
        self.__Es.FlushIndex(self.__SyncQueue.Index)
        r = self.__SyncQueue.GetSyncQueueBatch()
        p = ProgressLogger(self.__Es)
        p.Start("ExtractFOD", r[1], "ExtractFOD Status")
        avCount = 0
        iTotal = 0
        bailoutCount = 0
        while r and len(r[0]) > 0:
            queueBatch = r[0]
            iTotal = r[1]
            self.__Logger.debug("Sync queue total: %s", iTotal)
    
            try:
                if not queueBatch or len(queueBatch) == 0:
                    self.__Logger.info("No release queued for sync - nothing to do.")

                for qItem in queueBatch:
                    if bailoutCount == 1000:
                        self.__Logger.warning("[SYNC] Unable to lock queue item(s) after 1000 consecutive attempts, canceling sync.")
                        return
                    avCount = avCount + 1
                    sqdto = self.__SyncQueue.SetInProgress(qItem)
                    if not sqdto:
                        self.__Logger.debug("Skipping sync queue item %s:%s:%s, unable to lock", qItem.SyncQueueDoc.TargetType, qItem.SyncQueueDoc.Instance, qItem.SyncQueueDoc.TargetId)
                        bailoutCount += 1
                        continue
                    bailoutCount = 0

                    release = self.__GetRelease(qItem.SyncQueueDoc.TargetId, allReleases)
                    if not release:
                        self.__Logger.warning("[SYNC] FOD release ID %s not found, cannot sync.", qItem.SyncQueueDoc.TargetId)
                        sqdto = self.__SyncQueue.SetComplete(sqdto)
                        if not sqdto:
                            self.__Logger.warning("Failed to complete sync queue item %s:%s:%s. Earlier log messages may have more details.", qItem.SyncQueueDoc.TargetType, qItem.SyncQueueDoc.Instance, qItem.SyncQueueDoc.TargetId)
                        continue
                    p.Progress(avCount, 'Syncing FOD to Elastic for release {} ({} of {})'.format(qItem.SyncQueueDoc.TargetId, avCount, iTotal), iTotal)
                    self.__ProcessOne(release, qItem.SyncQueueDoc.Force)
                    sqdto = self.__SyncQueue.SetComplete(sqdto)
                    if not sqdto:
                        self.__Logger.warning("Failed to complete sync queue item %s:%s:%s. Earlier log messages may have more details.", qItem.SyncQueueDoc.TargetType, qItem.SyncQueueDoc.Instance, qItem.SyncQueueDoc.TargetId)
                # end for
                r = self.__SyncQueue.GetSyncQueueBatch()

            except:
                raise
            finally:
                try:
                    self.__Logger.debug("Attempting to clear sync queue session.")
                    self.__SyncQueue.ClearSession()
                    self.__Logger.debug("Sync queue session cleared.")
                except:
                    self.__Logger.error("Failed to clear sync queue session - see previous log messages for details.")
        # end 'while r and len(r[0]) > 0'
        if iTotal == 0:
            self.__Logger.info("No sync queue entries found, aborting sync.")
        p.Finish(iTotal, "Complete")

    def __GetApplication(self, applicationId):
        # use a numbered position FIFO cache approach, might reduce duplicate application ID lookups
        # expects dict to be sorted by key, and that we process in order of ascending application ID
        if not applicationId in self.__ApplicationCache.keys():
            app = self.__Fod.GetApplication(applicationId).Content
            app[self.__SourceNameField] = self.__SourceName
            self.__ApplicationCache[applicationId] = app
            if len(self.__ApplicationCache.keys()) > 25:
                try:
                    i = 0
                    first = list(self.__ApplicationCache.keys())[i]
                    if first == applicationId:
                        i += 1
                    self.__ApplicationCache.pop(first)
                except KeyError:
                    self.__Logger.error("[SYNC] Failed to remove application with id %s from application cache", first)
            if not applicationId in self.__ApplicationCache.keys():
                self.__Logger.warning("Application %s not found", applicationId)
                return None
        return self.__ApplicationCache[applicationId]

    def __ProcessOne(self, release, forceRefresh=False):
        self._Beat()
        needsReset = False
        checkStaticDate = True
        checkDynamicDate = True
        checkMobileDate = True
        holdReleaseId = release['releaseId']

        if release['staticScanDate'] != None:
            holdStaticScanDate = json.dumps(release['staticScanDate'])
            checkStaticDate = True
        else:
            holdStaticScanDate = 'null'
            checkStaticDate = False

        if release['dynamicScanDate'] != None:
            holdDynamicScanDate = json.dumps(release['dynamicScanDate'])
            checkDynamicDate = True
        else:
            holdDynamicScanDate = 'null'
            checkDynamicDate = False

        if release['mobileScanDate'] != None:
            holdMobileScanDate = json.dumps(release['mobileScanDate'])
            checkMobileDate = True
        else:
            holdMobileScanDate = 'null'
            checkMobileDate = False

        holdCritical = release['critical']
        holdHigh = release['high']
        holdMedium = release['medium']
        holdLow = release['low']

        self.__Logger.debug(holdReleaseId)

        dto = self.__GetElasticFodRelease(holdReleaseId)
        foundRelease = None if not dto else dto['_source']


        if not foundRelease:
            self.__Logger.debug('not in table - need to reset - get next release')
            needsReset = True
        else:
            self.__Logger.debug('found it in table')
            self.__Logger.debug(json.dumps(foundRelease))

        if not needsReset:
            # Compare names
            if foundRelease['releaseName'] != release['releaseName'] or foundRelease['applicationName'] != release['applicationName']:
                self.__Logger.debug('Release or application name changed, needs reset')
                needsReset = True

        if not needsReset:
                
            # Compare counts
            compareStaticScanDate = json.dumps(foundRelease['staticScanDate'])
            compareDynamicScanDate = json.dumps(foundRelease['dynamicScanDate'])
            compareMobileScanDate = json.dumps(foundRelease['mobileScanDate'])
            compareCritical = foundRelease['critical']
            compareHigh = foundRelease['high']
            compareMedium = foundRelease['medium']
            compareLow = foundRelease['low']
            dateMismatch = False

            # Compare last scan date
            if checkStaticDate == True:

                if holdStaticScanDate != compareStaticScanDate:
                    dateMismatch = True

            if checkDynamicDate == True:

                if holdDynamicScanDate != compareDynamicScanDate:
                    dateMismatch = True

            if checkMobileDate == True:

                if holdMobileScanDate != compareMobileScanDate:
                    dateMismatch = True

            if dateMismatch == True:

                logging.debug ('one or more dates are off - need to reset')
                needsReset = True

            else:

                if ((holdCritical == compareCritical) and (holdHigh == compareHigh) and (holdMedium == compareMedium) and (holdLow == compareLow)):

                    #logging.info ('everything matches - check fixed and suppressed')

                    _summary = {'releaseId': holdReleaseId, 'FixedIssue': 0, 'SuppressedIssues': 0}

                    _summary = self.__Fod.GetSummaryCounts(holdReleaseId)

                    #self.__Logger.info("summary count response: {}".format(_summary))
                    holdFixed = _summary['FixedIssue']
                    holdSuppressed = _summary ['SuppressedIssues']

                    foundRelCounts = self.__GetElasticFodCounts(holdReleaseId)


                    if foundRelCounts and len(foundRelCounts) == 1:
                        #logging.info ('found it in table')
                        compareFixed = foundRelCounts[0]['_source']['FixedIssue']
                        compareSuppressed = foundRelCounts[0]['_source']['SuppressedIssues']

                        if ((holdFixed == compareFixed) and (holdSuppressed == compareSuppressed)):
                            self.__Logger.debug('all counts match - no need to reset')
                            needsReset = False
                        else:
                            self.__Logger.debug('fixed or suppressed is off - need to reset')
                            needsReset = True

                    else:
                        self.__Logger.debug('no fixed or suppressed counts found - need to reset')
                        needsReset = True

                else:

                    self.__Logger.debug('something off in counts - need to reset')
                    needsReset = True

        if not needsReset and self.__CheckAttributes == True:
            # Check attributes
            fAttrApp = self.__GetApplication(release['applicationId'])
            fAttr = [] if not (fAttrApp and 'attributes' in fAttrApp.keys()) else fAttrApp['attributes']
            eAttrApp = self.__GetElasticFodApplication(release['applicationId'])
            eAttr = [] if not (eAttrApp and len(eAttrApp) > 0 and '_source' in eAttrApp[0].keys() and 'attributes' in eAttrApp[0]['_source'].keys()) else eAttrApp[0]['_source']['attributes']
            if len(fAttr) != len(eAttr):
                self.__Logger.debug("Application ID % attributes count doesn't match in release %s, need to reset", release['applicationId'], release['releaseId'])
                needsReset = True
            if not needsReset:
                eAttrList = {}
                for a in eAttr:
                    eAttrList[a['name']] = a['value']
                for a in fAttr:
                    if a['name'] not in eAttrList.keys() or eAttrList[a['name']] != a['value']:
                        needsReset = True
                        self.__Logger.debug("Application ID % attributes don't match in release %s, need to reset", release['applicationId'], release['releaseId'])
                        break

        if needsReset == True or forceRefresh:
            self.__Logger.info('Needs update - sending FOD data to Elastic for %s', holdReleaseId)
            holdApplicationId = release['applicationId']

            # Clear old data
            self._Beat()
            self.__ClearRelease(holdApplicationId, holdReleaseId)

            # Update fodapplications
            jApp = self.__GetApplication(holdApplicationId)
            self.__Es.Index('fodapplications', jApp)

            jRel = json.dumps(release)
            self.__Es.Index('fodreleases', jRel)
   
            _summary = {'releaseId': holdReleaseId, 'FixedIssue': 0, 'SuppressedIssues': 0, self.__SourceNameField: self.__SourceName}
            _summary = self.__Fod.GetSummaryCounts(holdReleaseId)

            self.__Es.Index('fodcounts', _summary)

            releasescans = { 'items': [] }
            rsp = self.__Fod.GetScans(holdReleaseId)
            if rsp and rsp.Content:
                releasescans = rsp.Content

            #self.__Logger.info("scan response: {}".format(releasescans))
            scnCount = 0
                   
            for relScan in releasescans['items']:

                self._Beat()  # a scan summary call per scan
                relScan[self.__SourceNameField] = self.__SourceName
                scnCount = scnCount + 1
                self.__Es.Index('fodscans', relScan)
                    
                #post Release Scan records
                
                if relScan['scanType'] != "OpenSource":

                    holdScan = relScan['scanId']
                    #self.__Logger.info(holdscan)
                    scanSumm = self.__Fod.GetScanSummary(holdScan)
                    #self.__Logger.info(scansumm)
                    holdScanSum = scanSumm.Content
                    if holdScanSum:
                        self.__Es.Index('fodscansummary', holdScanSum)
                    else:
                        self.__Logger.warning("Invalid/empty response when retrieving scan %s for release %s.", holdScan, holdReleaseId)
                    #self.__Logger.info(holdscansum)
                        
            self.__BulkLoadVulns(holdReleaseId)

            queueInfo = {
                'processedDateTime': '',
                'releaseId': holdReleaseId,
                'updateType': 'U',
                'completedDateTime' : '1900-01-01T00:00:00.000-0000',
                'processedDateTime': datetime.datetime.now().strftime("%Y-%m-%dT%H:%M:%S.%f"),
                'sourceName': self.__SourceName
            }
            self.__Es.Index('fodupdatequeue', json.dumps(queueInfo))

    def __BulkLoadVulns(self, id):
        ''' 
        Bulk inserts vulnerabilities for given releaseId into elastic
        original name: getAndLoadFODVulnerabilityBulk or BulkLoadVulnerabilitiesIntoElastic (FodClient)

        :id: release Id for which to retrieve vulnerabilities
        '''
        vuls = { 'items': [] }
        rsp = self.__Fod.GetVulnerabilities(id, True, True, logPrefix=f"FOD Issues for release {id}")
        if rsp and rsp.Content:
            vuls = rsp.Content
        for vuln in vuls['items']:
            self._Beat()
            vuln['lastUpdated'] = datetime.datetime.now().strftime("%Y-%m-%dT%H:%M:%S")
            vuln[self.__SourceNameField] = self.__SourceName
            self.__Es.BulkSendBatch('fodrelissues', vuln, batchSize=1000)
        self.__Es.BulkSendBatch() # send remaining

class SyncExtractorException(Exception):
    pass