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

    def __init__(self, appSettings, sourceName):
        
        if type(appSettings).__name__ != "ApplicationSettings":
            raise TypeError("Type of appSettings must be 'ApplicationSettings'")
        if not sourceName or not sourceName in appSettings.GetSourceNames():
            raise SyncExtractorException(f"Invalid or missing source configuration for source name '{sourceName}'")

        self.__Fod = FodClient(appSettings, sourceName)
        self.__Es = appSettings.Application.GetElasticClient()
        self.__SyncQueue = SyncQueueHelper(appSettings, sourceName)
        self.__PreloadReleases = appSettings.GetSource(sourceName, 'SyncPreloadReleases', True)
        self.__ApplicationCache = {}
        self.__CheckAttributes = appSettings.GetSource(sourceName, 'SyncCheckAttributes', False)
        self.__SourceName = sourceName
        self.__SourceNameField = "sourceName"

        logging.debug("ExtractFOD.init complete.")

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
                logging.warning("[SyncExtractor] Conflict Error (409) clearing index %s for id %s: %s", component[0], component[2], msg)
                continue
    
    def CheckDrop(self, safetyOverride=False):
        logging.info('Compare local data with FOD looking for dropped app/versions.  Loading releases for current source...')
        body = { 
            "sort": [ "releaseId"], 
            "_source": ["releaseId", "releaseName", "applicationId", "applicationName"], 
            "query": { "term": { self.__SourceNameField: { "value": self.__SourceName }}}
        }
        scroller = self.__Es.SearchScroll("fodreleases", body, scrollSize=500, scrollTimeout=None)
        esTotal = scroller.TotalHits
        if esTotal <= 0:
            logging.warning("No FOD releases found in local data (fodreleases).")
            return
        fscroller = self.__Fod.GetReleases(fields="releaseId", scroller=True)
        releases = []
        if fscroller and fscroller.TotalHits > 0:
            for rel in fscroller.GetAll():
                releases.append(rel['releaseId'])
        else:
            logging.warning("No releases found in FOD.")
        fscroller = None
        fodTotal = len(releases)
        logging.info("Totals: Local FOD count: %s, Actual FOD count: %s", esTotal, fodTotal)
        if esTotal > fodTotal and (int(abs(esTotal - fodTotal) / esTotal * 100) > 5):
            if safetyOverride:
                logging.warning("Local data counts are higher than FOD by more than 5%, safety override means we're cleaning house anyway.")
            else:
                logging.error("Local counts are higher than SSC by more than 5%, canceling auto-drop of FOD app versions from SaltMiner.  CheckDrop can be called manually with a safety override switch if desired.")
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
                    logging.info('Removing release with ID %s', esRelease['releaseId'])
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
        logging.info('Total releases dropped: %s', iDropCount)
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
                logging.info("Reloading sync queue: processed %s IDs", count)
            idList.append(itm['releaseId'])
            count += 1
        if len(idList) > 0:
            self.__SyncQueue.InsertQueueBatch(idList)
        logging.info("Sync queue reloaded successfully.")
    
    def ProcessOne(self, avid, forceRefresh=False):
        '''
        Enables sync of a single release (by id), bypassing the queue entirely
        '''
        releases = []
        release = self.__GetRelease(avid, releases)
        if not release:
            raise SyncExtractorException(f"Release {avid} could not be found.")
        logging.info('Syncing FOD to Elastic for release %s', avid)
        self.__ProcessOne(release['_source'], forceRefresh)
        logging.info('Sync complete.')

    def Process(self, reloadSyncQueue=False):
        '''
        Runs sync for queued releases, optionally reloading the queue before starting.

        reloadSyncQueue - if set, reloads the queue from sscprojects, skipping any existing PVs.
        '''

        # Check mappings
        logging.debug("Ensuring Mappings are available")
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
            logging.debug("Sync queue total: %s", iTotal)
    
            try:
                if not queueBatch or len(queueBatch) == 0:
                    logging.info("No release queued for sync - nothing to do.")

                for qItem in queueBatch:
                    if bailoutCount == 1000:
                        logging.warning("[SYNC] Unable to lock queue item(s) after 1000 consecutive attempts, canceling sync.")
                        return
                    avCount = avCount + 1
                    sqdto = self.__SyncQueue.SetInProgress(qItem)
                    if not sqdto:
                        logging.debug("Skipping sync queue item %s:%s:%s, unable to lock", qItem.SyncQueueDoc.TargetType, qItem.SyncQueueDoc.Instance, qItem.SyncQueueDoc.TargetId)
                        bailoutCount += 1
                        continue
                    bailoutCount = 0

                    release = self.__GetRelease(qItem.SyncQueueDoc.TargetId, allReleases)
                    if not release:
                        logging.warning("[SYNC] FOD release ID %s not found, cannot sync.", qItem.SyncQueueDoc.TargetId)
                        sqdto = self.__SyncQueue.SetComplete(sqdto)
                        if not sqdto:
                            logging.warning("Failed to complete sync queue item %s:%s:%s. Earlier log messages may have more details.", qItem.SyncQueueDoc.TargetType, qItem.SyncQueueDoc.Instance, qItem.SyncQueueDoc.TargetId)
                        continue
                    p.Progress(avCount, 'Syncing FOD to Elastic for release {} ({} of {})'.format(qItem.SyncQueueDoc.TargetId, avCount, iTotal), iTotal)
                    self.__ProcessOne(release, qItem.SyncQueueDoc.Force)
                    sqdto = self.__SyncQueue.SetComplete(sqdto)
                    if not sqdto:
                        logging.warning("Failed to complete sync queue item %s:%s:%s. Earlier log messages may have more details.", qItem.SyncQueueDoc.TargetType, qItem.SyncQueueDoc.Instance, qItem.SyncQueueDoc.TargetId)
                # end for
                r = self.__SyncQueue.GetSyncQueueBatch()

            except:
                raise
            finally:
                try:
                    logging.debug("Attempting to clear sync queue session.")
                    self.__SyncQueue.ClearSession()
                    logging.debug("Sync queue session cleared.")
                except:
                    logging.error("Failed to clear sync queue session - see previous log messages for details.")
        # end 'while r and len(r[0]) > 0'
        if iTotal == 0:
            logging.info("No sync queue entries found, aborting sync.")
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
                    logging.error("[SYNC] Failed to remove application with id %s from application cache", first)
            if not applicationId in self.__ApplicationCache.keys():
                logging.warning("Application %s not found", applicationId)
                return None
        return self.__ApplicationCache[applicationId]

    def __ProcessOne(self, release, forceRefresh=False):
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

        logging.debug(holdReleaseId)

        dto = self.__GetElasticFodRelease(holdReleaseId)
        foundRelease = None if not dto else dto['_source']


        if not foundRelease:
            logging.debug('not in table - need to reset - get next release')
            needsReset = True
        else:
            logging.debug('found it in table')
            logging.debug(json.dumps(foundRelease))

        if not needsReset:
            # Compare names
            if foundRelease['releaseName'] != release['releaseName'] or foundRelease['applicationName'] != release['applicationName']:
                logging.debug('Release or application name changed, needs reset')
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

                    #logging.info("summary count response: {}".format(_summary))
                    holdFixed = _summary['FixedIssue']
                    holdSuppressed = _summary ['SuppressedIssues']

                    foundRelCounts = self.__GetElasticFodCounts(holdReleaseId)


                    if foundRelCounts and len(foundRelCounts) == 1:
                        #logging.info ('found it in table')
                        compareFixed = foundRelCounts[0]['_source']['FixedIssue']
                        compareSuppressed = foundRelCounts[0]['_source']['SuppressedIssues']

                        if ((holdFixed == compareFixed) and (holdSuppressed == compareSuppressed)):
                            logging.debug('all counts match - no need to reset')
                            needsReset = False
                        else:
                            logging.debug('fixed or suppressed is off - need to reset')
                            needsReset = True

                    else:
                        logging.debug('no fixed or suppressed counts found - need to reset')
                        needsReset = True

                else:

                    logging.debug('something off in counts - need to reset')
                    needsReset = True

        if not needsReset and self.__CheckAttributes == True:
            # Check attributes
            fAttrApp = self.__GetApplication(release['applicationId'])
            fAttr = [] if not (fAttrApp and 'attributes' in fAttrApp.keys()) else fAttrApp['attributes']
            eAttrApp = self.__GetElasticFodApplication(release['applicationId'])
            eAttr = [] if not (eAttrApp and len(eAttrApp) > 0 and '_source' in eAttrApp[0].keys() and 'attributes' in eAttrApp[0]['_source'].keys()) else eAttrApp[0]['_source']['attributes']
            if len(fAttr) != len(eAttr):
                logging.debug("Application ID % attributes count doesn't match in release %s, need to reset", release['applicationId'], release['releaseId'])
                needsReset = True
            if not needsReset:
                eAttrList = {}
                for a in eAttr:
                    eAttrList[a['name']] = a['value']
                for a in fAttr:
                    if a['name'] not in eAttrList.keys() or eAttrList[a['name']] != a['value']:
                        needsReset = True
                        logging.debug("Application ID % attributes don't match in release %s, need to reset", release['applicationId'], release['releaseId'])
                        break

        if needsReset == True or forceRefresh:
            logging.info('Needs update - sending FOD data to Elastic for %s', holdReleaseId)
            holdApplicationId = release['applicationId']

            # Clear old data
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

            #logging.info("scan response: {}".format(releasescans))
            scnCount = 0
                   
            for relScan in releasescans['items']:

                relScan[self.__SourceNameField] = self.__SourceName
                scnCount = scnCount + 1
                self.__Es.Index('fodscans', relScan)
                    
                #post Release Scan records
                
                if relScan['scanType'] != "OpenSource":

                    holdScan = relScan['scanId']
                    #logging.info(holdscan)
                    scanSumm = self.__Fod.GetScanSummary(holdScan)
                    #logging.info(scansumm)
                    holdScanSum = scanSumm.Content
                    if holdScanSum:
                        self.__Es.Index('fodscansummary', holdScanSum)
                    else:
                        logging.warning("Invalid/empty response when retrieving scan %s for release %s.", holdScan, holdReleaseId)
                    #logging.info(holdscansum)
                        
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
            vuln['lastUpdated'] = datetime.datetime.now().strftime("%Y-%m-%dT%H:%M:%S")
            vuln[self.__SourceNameField] = self.__SourceName
            self.__Es.BulkSendBatch('fodrelissues', vuln, batchSize=1000)
        self.__Es.BulkSendBatch() # send remaining

class SyncExtractorException(Exception):
    pass