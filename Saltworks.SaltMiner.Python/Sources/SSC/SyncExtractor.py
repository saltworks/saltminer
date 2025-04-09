# Copyright (C) Saltworks Security, LLC - All Rights Reserved
# Unauthorized copying of this file, via any medium is strictly prohibited
# Proprietary and confidential
# Written by Saltworks Security, LLC  (www.saltworks.io) , 2024

# 10/26/21 TD
# Originally SyncSSC.py

import json
import logging
import datetime
import time

from Core.SscClient import SscClient409ConflictException
from Utility.ProgressLogger import *
from .SscUtilities import SscUtilities
from .SscEsUtils import SscEsUtils
from Utility.SyncQueueHelper import *


def initBlankAttrObject():
    return {
        'projectVersionId': 0,
        'attributeId': 0,
        'attributeName': '',
        'attributeValue': ''
    }

def initZeroIssueObject():
    return {
        'projectVersionId': 0,
        'issueName': 'Zero',
        'engineType': '',
        'friority': 'Zero',
        'engineCategory': '',
        'hidden': False,
        'removed': False,
        'suppressed': False,
        'lastUpdated': '',
        'bugURL': '',
        'folderGuid': '',
        'lastScanId': 0,
        'issueStatus': 'Zero',
        'analyzer': '',
        'primaryLocation': '',
        'reviewed': '',
        'id': 0,
        'hasAttachments': False,
        'projectVersionName': 'Zero',
        'severity': 0,
        '_href': '',
        'displayEngineType': '',
        'foundDate': '',
        'removedDate': None,
        'confidence': 0,
        'impact': 0,
        'primaryRuleGuid': '',
        'scanStatus': '',
        'audited': False,
        'kingdom': 'Zero',
        'folderId': 0,
        'revision': 0,
        'likelihood': 0,
        'issueInstanceId': '',
        'hasCorrelatedIssues': False,
        'primaryTag': '',
        'lineNumber': 0,
        'projectName': '',
        'fullFileName': '',
        'primaryTagValueAutoApplied': False
    }

def initBlankQueueObject():
    return {
        'processedDateTime' : '',
        'projectVersionId': 0,
        'updateType': '',
        'completedDateTime' : ''
    }

class SyncExtractor(object):
    """Extraction of Open SSC Changes"""

    def __init__(self, appSettings, sourceName, mainConfigName="Main"):

        if type(appSettings).__name__ != "ApplicationSettings":
            raise SyncExtractorException("Type of appSettings must be 'ApplicationSettings'")
        if not sourceName or not sourceName in appSettings.GetSourceNames():
            raise SyncExtractorException(f"Invalid or missing source configuration for source name '{sourceName}'")
        if not mainConfigName or not mainConfigName in appSettings.GetConfigNames():
            raise SyncExtractorException(f"Invalid or missing configuration for name '{mainConfigName}'")

        self.__App = appSettings.Application
        self.__RulePacks = []
        self.__SscUtils = SscUtilities(appSettings, sourceName)
        self.__ElasticClient = appSettings.Application.GetElasticClient()
        self.__SscEsUtils = SscEsUtils(appSettings)
        self.__Attributes = appSettings.Get(mainConfigName, 'Attributes', {})
        self.__IssueDetailsEndpoint = appSettings.GetSource(sourceName, 'UseIssueDetailsEndpoint', False)
        self.__AssessmentTypeMap = appSettings.GetSource(sourceName, 'AssessmentTypeMap', {})
        self.__SidecarAttributesMapped = False
        self.__SidecarBulkDocs = []
        self.__EnableTagCounts = appSettings.GetSource(sourceName, 'EnableSyncTagCounts', False)
        self.__SidecarAttributesEnrichPolicy = appSettings.GetSource(sourceName, 'AttributesEnrichmentPolicy', '')
        self.__SscDiagEnabled = False # set this to true for troubleshooting counts
        self.__ImportPurgedScans = appSettings.GetSource(sourceName, 'ImportPurgedScans', True)
        self.__SyncQueue = SyncQueueHelper(appSettings, sourceName)
        self.__PreloadProjectVersions = appSettings.GetSource(sourceName, 'SyncPreloadProjectVersions', True)

        if not len(self.__AssessmentTypeMap.keys()):
            logging.warn("Assessment type map missing from source name '%s'.  This will cause all scans to be considered assessment type 'Unknown'.", sourceName)
        logging.debug("ExtractSSC.init complete.")

    def Cleanup(self):
        self.__SscUtils.Cleanup()

    def MapESIndices(self, Force):
        
        # map sscprojects elastic table
        self.__ElasticClient.MapIndex("sscprojects", Force)

        # map sscprojcounts elastic table
        self.__ElasticClient.MapIndex("sscprojcounts", Force)
        
        # map sscprojattrs elastic table
        self.__ElasticClient.MapIndex("sscprojattrs", Force)

        # map sscprojattr2 elastic table
        self.__ElasticClient.MapIndex("sscprojattr2", Force)
        
        # map sscprojscans elastic table
        self.__ElasticClient.MapIndex("sscprojscans", Force)

        # map sscprojissues elastic table
        self.__ElasticClient.MapIndex("sscprojissues", Force)
        
        # map sscupdatequeue elastic table
        self.__ElasticClient.MapIndex("sscupdatequeue", Force)
     
    def __CleanMappingFieldName(self, fieldname):
        return fieldname.replace(" ", "")
        
    def __SidecarBulk(self, attribsDoc):
        if len(self.__SidecarBulkDocs) >= 200 or (not attribsDoc and len(self.__SidecarBulkDocs) > 0):
            logging.info("Bulk inserting %s sidecar docs...", len(self.__SidecarBulkDocs))
            self.__ElasticClient.BulkInsert(self.__SidecarBulkDocs)
            self.__SidecarBulkDocs = []
            logging.info("Sidecar bulk operation complete")
        if attribsDoc:
            self.__SidecarBulkDocs.append(attribsDoc)
        
    def __SidecarMapping(self):
        if self.__SidecarAttributesMapped:
            return
        m = {   
            "mappings": {
                "properties": {
                    "projectVersionId": { "type": "keyword" }
                }
            }
        }
        for a in self.__Attributes:
            m['mappings']['properties'][self.__CleanMappingFieldName(a)] = { "type": "keyword" }
        self.__ElasticClient.MapIndexWithMapping("sidecar_ssc_attributes", m, False)
        self.__SidecarAttributesMapped = True

    def UpdateSidecarAttributes(self, attribs=None):
        if not attribs or not len(attribs):
            self.__SidecarBulk(None)
            return
        self.__SidecarMapping()
        pvid = attribs[0]['_source']['projectVersionId'] if '_source' in attribs[0].keys() else attribs[0]['projectVersionId']
        doc = { "projectVersionId": pvid }
        for attr in attribs:
            a = attr['_source'] if '_source' in attr.keys() else attr
            afld = self.__CleanMappingFieldName(a['attributeName'])
            # if multi value, send as array (else send first one as a value, or None if no values at all)
            doc[afld] = a['attributeValue'] if len(a['attributeValue']) > 1 else (a['attributeValue'][0] if len(a['attributeValue']) else None)
        self.__SidecarBulk(self.__ElasticClient.BulkInsertDocument("sidecar_ssc_attributes", doc, pvid))

    def SynchronizeSidecarAttributes(self):
        if not self.__SidecarAttributesEnrichPolicy:
            return
        es = self.__ElasticClient
        avScroller = es.SearchScroll("sscprojects", { "_source": ["id"] }, 200)
        while avScroller.Results:
            for item in avScroller.Results:
                avid = item['_source']['id']
                sscAttr = es.Search("sscprojattr2", { "query": { "term": { "projectVersionId": { "value": avid } } } }, 1000, True)
                if sscAttr and len(sscAttr) > 0:
                    self.UpdateSidecarAttributes(sscAttr)
            avScroller.GetNext()
        self.UpdateSidecarAttributes()
        # if enrichment policy configured, execute it
        if self.__SidecarAttributesEnrichPolicy:
            self.__ElasticClient.ExecuteEnrichPolicy(self.__SidecarAttributesEnrichPolicy)

    def __AddRulepackLanguage(self, rulepacks:list):
        try:
            if not self.__RulePacks:
                self.__RulePacks = self.__SscUtils.SscClient.GetRulePacks()
            for rulepack in rulepacks:
                g = rulepack['guid']
                if g in self.__RulePacks:
                    rulepack['language'] = self.__RulePacks[g]['language']
        except Exception as ex:
            logging.error("Failed when adding rulepack language: %s", ex, exc_info=ex)
        return rulepacks
    
    def __GetAssessmentType(self, engineType):
        if not engineType:
            engineType = ""
        engineType = str(engineType).upper()
        if engineType in self.__AssessmentTypeMap.keys():
            return self.__AssessmentTypeMap[engineType]
        else:
            logging.warning("Scan type '%s' not found in configuration", engineType)
            return "Unknown"

    def __WriteZeroIssue(self, projid, atype, category):
        formatnow = datetime.datetime.now().strftime("%Y-%m-%dT%H:%M:%S.%f")
        issueInfo = initZeroIssueObject()
        issueInfo['projectVersionId'] = projid
        issueInfo['engineType'] = atype
        issueInfo['engineCategory'] = category
        issueInfo['foundDate'] = formatnow
        issueInfo['lastUpdated'] = formatnow
        self.__ElasticClient.Index('sscprojissues', json.dumps(issueInfo))
        
    def __GetSSCProjectByProjectId(self, projid):
        # declare a filter query dict object
        match = {
            "size": 1000,
            "query": {
                "bool" : {
                    "must": [{"term": {"id": projid}}]
                    }
                }
            }

        return self.__ElasticClient.SearchWithCursor('id','sscprojects', match)

    def __GetSSCProjCountsByProjectId(self, projid):
        # declare a filter query dict object
        match = {
            "size": 1000,
            "query": {
                "bool" : {
                    "must": [{"term": {"projectVersionId": projid}}]
                    }
                }
            }

        return self.__ElasticClient.SearchWithCursor('projectVersionId','sscprojcounts', match)

    def __GetSscProjIssueCountsByIdAndTag(self, projid):
        # declare a filter query dict object
        sscprojectissuestagstots = {
            "aggs": {
                "primaryTag": {
                  "terms": {
                    "field": "primaryTag",
                    "order": {
                      "_count": "desc"
                    },
                    "missing": "Not Set",
                    "size": 5000
                  }
                }
              },
              "size": 0,
              "query": {
                "term": {
                  "projectVersionId": projid
                }
              }
            }

        result = self.__ElasticClient.Search('sscprojissues', sscprojectissuestagstots, navToData=False)

        return result['aggregations']['primaryTag']['buckets']

    def __GetSSCProjectAttr2ByProjectId(self, projid, attrid):
        # declare a filter query dict object
        match = {
            "query": {
                "bool": {
                    "must": [{"term": {"projectVersionId": projid,}},
                            {"term": {"attributeId": attrid}}]
                }
            }
        }

        return self.__ElasticClient.SearchWithCursor('projectVersionId', 'sscprojattr2', match)

    def __DeleteById(self, index, field, id):
        DeleteQuery = { "query": { "term": { field: { "value": id } } } }
        if self.__ElasticClient.IndexExists(index):
            self.__ElasticClient.DeleteByQuery(index, DeleteQuery, flushAfter=False, wait=False)
        else:
            logging.warn("[DeleteById] Index %s doesn't exist, no delete performed", index)

    def __ClearProject(self, projid):
        self.__DeleteById('sscprojects', 'id', projid)
        self.__DeleteById('sscprojcounts', 'projectVersionId', projid)
        self.__DeleteById('sscprojattrs', 'projectVersionId', projid)
        self.__DeleteById('sscprojattr2', 'projectVersionId', projid)
        self.__DeleteById('sscprojscans', 'projectVersionId', projid)
        self.__DeleteById('sscprojissues', 'projectVersionId', projid)

    def __initBlankQueueObject(self):
        return {
		    'processedDateTime' : '',
		    'projectVersionId' : 0,
		    'updateType' : '',
		    'completedDateTime' : ''
	    }

    def Nvl(self, obj, prop, replace=None):
        return replace if not prop in obj.keys() else obj[prop]

    def __GetProjectVersion(self, pvid, pvList, allowLocal=False):
        '''
        Look in passed list first, then elastic (sscprojects) if enabled, then SSC

        :pvid: project version ID
        :pvList: existing project version list
        :allowLocal: if enabled, look in elasticsearch (sscprojects) before looking at SSC
        '''
        if not pvList:
            pvList = [] 
        for itm in pvList:
            if str(itm['id']) == str(pvid):
                return itm
        if allowLocal:
            rsp = self.__ElasticClient.Search("sscprojects", { "query": { "term": { "id": str(pvid) } } }, navToData=True)
            if rsp and len(rsp) > 0:
                return rsp[0]['_source']
        rsp = self.__SscUtils.SscClient.GetProjectVersion(pvid, True)
        if rsp:
            return rsp
        return None

    # this one is used outside the class as well in an upgrade utility
    def ArtifactToScans(self, artifact, pvid, prjName, verName):
        list = []
        if not artifact or not '_embed' in artifact.keys() or not 'scans' in artifact['_embed'].keys() or not artifact['_embed']['scans']:
            return list
        if artifact['purged'] and not self.__ImportPurgedScans:
            return list
        if artifact['status'] in ['PROCESSING', 'ERROR_PROCESSING']:
            return list # invalid status on artifact for import
        for artScan in artifact['_embed']['scans']:
            rulepacks = []
            if not 'uploadDate' in artScan.keys():
                logging.warning("Invalid scan, upload date missing for app version %s, scan id %s, certification '%s'.  Skipping this scan.", pvid, artScan['id'], artScan['certification'])
                continue
            if "rulepacks" in artScan.keys():
                rulepacks = self.__AddRulepackLanguage(artScan['rulepacks'])
            scan = {
                'projectVersionId': pvid,
                'projectName': prjName,
                'versionName': verName,
                'lastUpdated': datetime.datetime.now().strftime("%Y-%m-%dT%H:%M:%S"),
                'artifactType': artifact['artifactType'],
                'fileName': artifact['fileName'],
                'approvalDate': self.Nvl(artScan, 'approvalDate'),
                'messageCount': self.Nvl(artifact, 'messageCount'),
                'id': artScan['id'],
                'guid': self.Nvl(artScan, 'guid'),
                'uploadDate': artScan['uploadDate'],
                'type': artScan['type'],
                'certification': self.Nvl(artScan, 'certification'),
                'hostname': self.Nvl(artScan, 'hostname'),
                'engineVersion': artScan['engineVersion'],
                'artifactId': artScan['artifactId'],
                'buildId': self.Nvl(artScan, 'buildId'),
                'buildLabel': self.Nvl(artScan, 'buildLabel'),
                'noOfFiles': self.Nvl(artScan, 'noOfFiles'),
                'totalLOC': self.Nvl(artScan, 'totalLOC'),
                'execLOC': self.Nvl(artScan, 'execLOC'),
                'elapsedTime': self.Nvl(artScan, 'elapsedTime'),
                'fortifyAnnotationsLOC': self.Nvl(artScan, 'fortifyAnnotationsLOC'),
                'scanErrorsCount': self.Nvl(artifact, 'scanErrorsCount'),
                'uploadIP': self.Nvl(artifact, 'uploadIP'),
                'allowApprove': self.Nvl(artifact, 'allowApprove'),
                'allowPurge': self.Nvl(artifact, 'allowPurge'),
                'lastScanDate': artifact['lastScanDate'],
                'fileURL': self.Nvl(artifact, 'fileURL'),
                'purged': self.Nvl(artifact, 'purged'),
                'webInspectStatus': self.Nvl(artifact, 'webInspectStatus'),
                'inModifyingStatus': self.Nvl(artifact, 'inModifyingStatus'),
                'originalFileName': self.Nvl(artifact, 'originalFileName'),
                'allowDelete': self.Nvl(artifact, 'allowDelete'),
                '_href': artifact['_href'],
                'scaStatus': self.Nvl(artifact, 'scaStatus'),
                'indexed': self.Nvl(artifact, 'indexed'),
                'runtimeStatus': self.Nvl(artifact, 'runtimeStatus'),
                'userName': self.Nvl(artifact, 'userName'),
                'versionNumber': self.Nvl(artifact, 'versionNumber'),
                'otherStatus': self.Nvl(artifact, 'otherStatus'),
                'artifactUploadDate': artifact['uploadDate'],
                'approvalComment': self.Nvl(artifact, 'approvalComment'),
                'approvalUsername': self.Nvl(artifact, 'approvalUsername'),
                'fileSize': self.Nvl(artifact, 'fileSize'),
                'auditUpdated': self.Nvl(artifact, 'auditUpdated'),
                'messages': self.Nvl(artifact, 'messages'),
                'status': self.Nvl(artifact, 'status'),
                'rulepacks': rulepacks
            }
            mlen = 0 if not scan['messages'] else len(scan['messages'])
            if mlen > 5000:
                scan['messages'] = str(scan['messages'])[:4989] + "[TRUNCATED]"
                logging.debug(f"Project scan messages field for PV ID {pvid} was truncated to 5000 chars (from {mlen}).")
            list.append(scan)
        return list

    def RemoveSyncOrphans(self, safetyOverride=False):
        logging.info("Comparing sync project versions with SSC to find orphans.  Getting PV IDs from local store...")
        scroller = self.__ElasticClient.SearchScroll("sscprojects", { "_source": ["id"] }, scrollSize=1000, scrollTimeout=None)
        lstLocal = []
        while scroller.Results:
            for dto in scroller.Results:
                lstLocal.append(dto['_source']['id'])
            scroller.GetNext()

        esTotal = len(lstLocal)
        if esTotal <= 0:
            logging.error("No SSC project versions found in elasticsearch (sscprojects).")
            return
        sscTotal = self.__SscUtils.SscClient.GetProjectVersionCount()
        logging.info("Totals: Elastic SSC count: %s, SSC count: %s", esTotal, sscTotal)
        if esTotal > sscTotal and (int(abs(esTotal - sscTotal) / esTotal * 100) > 5):
            if safetyOverride:
                logging.warning("Elastic counts are higher than SSC by more than 5%, safety override means we're cleaning house anyway.")
            else:
                logging.error("Elastic counts are higher than SSC by more than 5%, canceling auto-drop of SSC app versions from SaltMiner.  CheckSscDropProjects can be called manually with a safety override switch if desired.")
                return

        logging.info("Getting PV IDs from SSC...")
        lstSsc = []
        dtoSsc = self.__SscUtils.SscClient.GetProjectVersions("id", forceRefresh=True)
        for dto in dtoSsc:
            lstSsc.append(dto['id'])

        count = 0
        dropcount = 0
        for lid in lstLocal:
            count +=1
            if count % 1000 == 0:
                logging.info("Processed %s of %s", count, len(lstLocal))
            if lid not in lstSsc:
                logging.info("Orphan sync project version %s found, removing...")
                self.__ClearProject(lid)
                sscQ = {
                    'processedDateTime': datetime.datetime.now(datetime.UTC).isoformat(),
                    'updateReason': 'Dropping orphan project version',
                    'projectVersionId': lid,
                    'updateType': 'D',
                    'completedDateTime': '1900-01-01T00:00:00.000-0000'
                }
                self.__ElasticClient.Index('sscupdatequeue', sscQ)
                dropcount += 1
        logging.info("Process complete.  %s project version orphans removed.", dropcount)
                

    def CheckSscDropProjects(self, safetyOverride=False):
        logging.info('Compare Elastic SSC information with SSC looking for dropped app/versions')
        logging.info('Getting ProjectVersions')

        self.__SscEsUtils.getAllESSSCProjects()
        esTotal = len(self.__SscEsUtils.AllSscProjects)
        if esTotal <= 0:
            logging.error("No SSC project versions found in elasticsearch (sscprojects).")
            return
        sscTotal = self.__SscUtils.SscClient.GetProjectVersionCount()
        logging.info("Totals: Elastic SSC count: %s, SSC count: %s", esTotal, sscTotal)
        if esTotal > sscTotal and (int(abs(esTotal - sscTotal) / esTotal * 100) > 5):
            if safetyOverride:
                logging.warning("Elastic counts are higher than SSC by more than 5%, safety override means we're cleaning house anyway.")
            else:
                logging.error("Elastic counts are higher than SSC by more than 5%, canceling auto-drop of SSC app versions from SaltMiner.  CheckSscDropProjects can be called manually with a safety override switch if desired.")
                return

        projectVersions = self.__SscUtils.SscClient.GetProjectVersions("id", forceRefresh=True)
        p = ProgressLogger(self.__ElasticClient)
        p.Start("CheckSSCDropProjects", esTotal, "CheckSscDropProjects Status")
        p.Progress(0, 'Starting CheckSscDropProjects - check for SSC dropped projectversions')

        counttodrop = 0
        iCount = 0

        for sscProj in self.__SscEsUtils.AllSscProjects:
            holdprojectId = sscProj['id']
            bfoundincurrent = False
            iCount = iCount + 1

            if iCount % 50 == 0 or iCount == esTotal:
                p.Progress(iCount, 'Checking Elastic app/version is in SSC {} of {}'.format(iCount, esTotal))
        
            for projectVersion in projectVersions:
                projid = projectVersion['id']
                if holdprojectId == projid:
                    bfoundincurrent = True
    
            if bfoundincurrent == False:
                p.Progress(iCount, 'Dropping Elastic app/version {} of {}'.format(iCount, esTotal))
                logging.info('Removing Elastic app/version ID {}'.format(holdprojectId))
                projid = holdprojectId
                self.__ClearProject(projid)

                queueInfo = self.__initBlankQueueObject()
                holdnow = datetime.datetime.now()
                formatnow = holdnow.strftime("%Y-%m-%dT%H:%M:%S")
                queueInfo['processedDateTime'] = formatnow
                queueInfo['updateReason'] = 'CheckDrop did not find this app version in SSC'
                queueInfo['projectVersionId'] = projid
                queueInfo['updateType'] = 'D'
                queueInfo['completedDateTime'] = '1900-01-01T00:00:00.000-0000'
                logging.info(queueInfo)
                self.__ElasticClient.Index('sscupdatequeue', json.dumps(queueInfo))

                counttodrop = counttodrop + 1

        logging.info('Total Elastic app/versions dropped: {}'.format(counttodrop))
        p.Finish(esTotal, "Complete")

    def ReloadSyncQueue(self, clearSyncQueue='none'):
        '''
        Reloads sync extractor queue, optionally clearing data first.

        clearSyncQueue - 'none' means do not clear, 'completed' for completed, 'unlocked' for unlocked only, 'locked' for all locked, 'all' for all.
        '''
        pvList = self.__SscUtils.SscClient.GetProjectVersions(fields='id', forceRefresh=True)

        if not clearSyncQueue in ['none', 'all', 'locked', 'unlocked', 'completed']:
            raise SyncExtractorException("Invalid/unsupported clearSyncqueue value.")
        if clearSyncQueue != 'none':
            completed = clearSyncQueue in ['all', 'completed']
            locked = clearSyncQueue in ['all', 'locked']
            self.__SyncQueue.ClearSyncQueue(completed, locked)

        count = 0
        idList = []
        for itm in pvList:
            if count > 0 and count % 200 == 0:
                self.__SyncQueue.InsertQueueBatch(idList)
                idList = []
                logging.info("Reloading sync queue: processed %s IDs", count)
            idList.append(itm['id'])
            count += 1
        if len(idList) > 0:
            self.__SyncQueue.InsertQueueBatch(idList)
        logging.info("Sync queue reloaded successfully.")
    
    def ProcessOne(self, pvid, forceSync=False):
        '''
        Enables sync of a single project version (by id), bypassing the queue entirely
        '''
        projectAttrDefs = self.__SscUtils.SscClient.GetProjectVersionAttributeDefinitions()
        seenIdList = []
        projectVersions = []
        projectVersion = self.__GetProjectVersion(pvid, projectVersions)
        if not projectVersion:
            raise SyncExtractorException(f"Project version {pvid} could not be found.")
        logging.info('Syncing SSC to Elastic for project version %s', pvid)
        self.__ProcessOne(projectVersion, projectAttrDefs, seenIdList, f"PVID: {pvid}", forceSync)
        logging.info('Sync complete.')

    def Process(self, cleanupAfter=True, reloadSyncQueue=False):
        '''
        Runs sync for queued project versions, optionally reloading the queue before starting.

        cleanupAfter - use this to clean up SSC client and temp files when complete.
        includeDropCheck - look for and remove project versions from elasticsearch that are no longer in SSC.
        reloadSyncQueue - if set, reloads the queue from sscprojects, skipping any existing PVs.
        '''

        # Check mappings
        logging.info("Ensuring Mappings are available")
        self.MapESIndices(False)

        projectAttrDefs = self.__SscUtils.SscClient.GetProjectVersionAttributeDefinitions()

        # Reload sync queue if configured
        if reloadSyncQueue:
            self.ReloadSyncQueue()

        # Preload project versions if configured
        seenIdList = []
        if self.__PreloadProjectVersions != False:
            logging.info('Getting ProjectVersions')
            projectVersions = self.__SscUtils.SscClient.GetProjectVersions(forceRefresh=True)
            logging.info('ProjectVersions loaded: %s', len(projectVersions))
        else:
            projectVersions = []

        # Main queue loop
        r = self.__SyncQueue.GetSyncQueueBatch()
        p = ProgressLogger(self.__ElasticClient)
        p.Start("ExtractSSC", r[1], "ExtractSSC Status")
        pvCount = 0
        iTotal = 0
        bailoutCount = 0
        while r and len(r[0]) > 0:
            queueBatch = r[0]
            iTotal = r[1]
            logging.debug("Sync queue total: %s", iTotal)
            
            try:
                if not queueBatch or len(queueBatch) == 0:
                    logging.info("No project version queued for sync - nothing to do.")
                for qItem in queueBatch:
                    if bailoutCount == 1000:
                        logging.warning("[SYNC] Unable to lock queue item(s) after 1000 consecutive attempts, canceling sync.")
                        return
                    pvCount += 1
                    sqdto = self.__SyncQueue.SetInProgress(qItem)
                    if not sqdto:
                        logging.debug("Skipping sync queue item %s:%s:%s, unable to lock", qItem.SyncQueueDoc.TargetType, qItem.SyncQueueDoc.Instance, qItem.SyncQueueDoc.TargetId)
                        bailoutCount += 1
                        continue
                    bailoutCount = 0

                    projectVersion = self.__GetProjectVersion(qItem.SyncQueueDoc.TargetId, projectVersions)
                    if not projectVersion:
                        logging.warning("[SYNC] SSC app/version ID %s not found, cannot sync.", qItem.SyncQueueDoc.TargetId)
                        sqdto = self.__SyncQueue.SetComplete(sqdto)
                        if not sqdto:
                            logging.warning("Failed to complete sync queue item %s:%s:%s. Earlier log messages may have more details.", qItem.SyncQueueDoc.TargetType, qItem.SyncQueueDoc.Instance, qItem.SyncQueueDoc.TargetId)
                        continue
                    pvid = projectVersion['id']
                    pvMessage = f"PVID: {pvid}"
                    p.Progress(pvCount, f'Comparing SSC to Elastic - {pvid}, {pvCount} processed, {iTotal-pvCount} remain')
                    self.__ProcessOne(projectVersion, projectAttrDefs, seenIdList, pvMessage, qItem.SyncQueueDoc.Force)
                    sqdto = self.__SyncQueue.SetComplete(sqdto)
                    if not sqdto:
                        logging.warning("Failed to complete sync queue item %s:%s:%s. Earlier log messages may have more details.", qItem.SyncQueueDoc.TargetType, qItem.SyncQueueDoc.Instance, qItem.SyncQueueDoc.TargetId)

                    if self.__SscDiagEnabled: # enable to log if counts get out of sync
                        eCount = self.__ElasticClient.Count("sscprojects")
                        if eCount < pvCount - 1:
                            logging.warning("Elastic count (%s) is less than current processing count (%s).  Something might be wrong.", eCount, pvCount)
                
                    if self.__SscDiagEnabled: # enable this block to log whether an app version made it to elastic
                        r = self.__ElasticClient.Search("sscprojects", { "query": { "term": { "id": { "value": pvid } } } })
                        if not r or not len(r) == 1:
                            time.sleep(2)
                            r = self.__ElasticClient.Search("sscprojects", { "query": { "term": { "id": { "value": pvid } } } })
                        if r and len(r) == 1:
                            logging.info("Found projid %s in elastic", pvid)
                        else:
                            logging.error("Did not find projid %s in elastic after sync", pvid)
                # end 'for qItem in queueBatch'
            except Exception:
                logging.warning("Exception raised, attempting to clear sync queue session...")
                raise
            finally:
                try:
                    logging.debug("Attempting to clear sync queue session.")
                    self.__SyncQueue.ClearSession()
                    logging.debug("Sync queue session cleared.")
                except:
                    logging.error("Failed to clear sync queue session - see previous log messages for details.")
            r = self.__SyncQueue.GetSyncQueueBatch()
        # end 'while r and len(r[0]) > 0'

        self.UpdateSidecarAttributes() # make sure all remaining bulk docs are sent
        # if enrichment policy configured, execute it
        if self.__SidecarAttributesEnrichPolicy:
            self.__ElasticClient.ExecuteEnrichPolicy(self.__SidecarAttributesEnrichPolicy)

        if cleanupAfter:
            self.__SscUtils.Cleanup()
        p.Finish(iTotal, "Complete")

    def __ProcessOne(self, projectVersion, projectAttrDefs, seenIdList, pvMessage, forceSync=False):
        needsReset = False
        needsAttrReset = False
        updateReason = ""
    
        projid = projectVersion['id']

        if self.__SscUtils.IsIncompleteProjectVersion(projid):
            logging.warning(f"ProjectVersion {projid} appears to not be setup correctly in SSC and will be skipped.")
            return

        projectFilterSet = self.__SscUtils.getProjectVersionFilterSet(projid)
        if not projectFilterSet:
            logging.error("Invalid/missing filterset from SSC API for project version %s.  Skipping...")
            return
        for projectFilter in projectFilterSet['data']:
            if projectFilter['defaultFilterSet'] == True:
                projectDefFilter = projectFilter['guid']
                #logging.info(projectDefFilter)

        #print(projectFilterSet)
        #sys.exit()

        holdlastFPR = json.dumps(projectVersion['currentState']['lastFprUploadDate'])
        holdAttnRequired = projectVersion['currentState']['attentionRequired']
        holdname = projectVersion['name']
        holdprojectname = projectVersion['project']['name']

        logging.debug(f"{pvMessage}, Checking to see if we need to refresh")

        foundproject = self.__GetSSCProjectByProjectId(projid)
        #logging.info(foundproject)
    
        if len(foundproject) == 1:
            lastFPRdate = json.dumps(foundproject[projid]['currentState']['lastFprUploadDate'])
            lastAttnRequired = foundproject[projid]['currentState']['attentionRequired']
            compname = foundproject[projid]['name']
            compprojectname = foundproject[projid]['project']['name'] 
            #logging.info (lastFPRdate)

            if lastFPRdate == holdlastFPR and lastAttnRequired == holdAttnRequired:
                #match found - see if counts have changed
                issues_count = self.__SscUtils.getProjectVersionIssueCounts(projid, projectDefFilter)

                jprojcounts = json.dumps(issues_count)
                holdcritical = issues_count['critical']
                holdhigh = issues_count['high']
                holdmedium = issues_count['medium']
                holdlow = issues_count['low']
                #holdhidden = issues_count['hiddenCount']
                holdsuppressed = issues_count['suppressedCount']
                holdremoved = issues_count['removedCount']

                foundprojcounts = self.__GetSSCProjCountsByProjectId(projid)

                if len(foundprojcounts) == 1:
                    comparecritical = foundprojcounts[projid]['critical']
                    comparehigh = foundprojcounts[projid]['high']
                    comparemedium = foundprojcounts[projid]['medium']
                    comparelow = foundprojcounts[projid]['low']
                    comparesuppressed = foundprojcounts[projid]['suppressedCount']
                    compareremoved = foundprojcounts[projid]['removedCount']

                    if ((holdcritical == comparecritical) and (holdhigh == comparehigh) and (holdmedium == comparemedium) and (holdlow == comparelow)
                            and (holdsuppressed == comparesuppressed) and (holdremoved == compareremoved)):
                        logging.info (f"{pvMessage}, found project & counts and they all match")
                    else:
                        logging.info (f"{pvMessage}, found counts and they are different")
                        needsReset = True
                        updateReason = f"{updateReason}, found counts and they are different"
                else:
                    logging.info(f"{pvMessage}, did not find counts")
                    needsReset = True
                    updateReason = f"{updateReason}, did not find counts"

            else:
                if lastFPRdate == holdlastFPR:
                    logging.info(f"{pvMessage}, found project but currentState.attentionRequired has changed")
                    updateReason = f"{updateReason}, found project but currentState.attentionRequired has changed"
                else:
                    logging.info(f"{pvMessage}, found project but different lastFPRUploadDate")
                    updateReason = f"{updateReason}, found project but different lastFPRUploadDate"
                needsReset = True
        else:
            logging.info(f"{pvMessage}, did not find project at all")
            needsReset = True
            updateReason = f"{updateReason}, did not find project at all"

        if (len(foundproject) == 1 and (holdname != compname or holdprojectname != compprojectname)):
            logging.info (f"{pvMessage}, name changed - force refresh")
            needsReset = True
            updateReason = f"{updateReason}, name changed - force refresh"

        # Check issue tag counts
        if needsReset == False and self.__EnableTagCounts:

            # get counts for ssctags in sscprojissues:
            foundtagcounts = self.__GetSscProjIssueCountsByIdAndTag(projid)

            # check to see if tags have changed on release
            ssctagcounts = self.__SscUtils.SscClient.GetProjectVersionIssueTagCounts(projid, projectDefFilter)
            ssctagslist = ssctagcounts['data']

            for ssctags in ssctagslist:
                holdname = ssctags['cleanName']
                holdssccount = ssctags['totalCount']
                foundmatch = False
 
                for tagcounts in foundtagcounts:
                    holdkey = tagcounts['key']
                    holdcount = tagcounts['doc_count']

                    if holdkey == holdname:
                        foundmatch = True
                        if holdssccount != holdcount:
                            logging.info (f"{pvMessage}, tag count different for {holdname}")
                            logging.info ('ssc {} vs elastic {}'.format(holdssccount, holdcount))
                            needsReset = True
                            updateReason = f"{updateReason},ssctags ssc {holdssccount} vs elastic {holdcount}"

                if foundmatch == False:
                    logging.info (f"{pvMessage}, ssctags didnt find match for {holdname}")
                    needsReset = True
                    updateReason = f"{updateReason}, ssctags didnt find match for {holdname}"

            #sys.exit()

        # Check to see if attributes need to be updated
        if needsReset == False:

            paDefs = {}
            for pad in projectAttrDefs['data']:
                if pad['id'] != None and pad['inUse'] == True:
                    paDefs[pad['id']] = pad

            projectAttrs = self.__SscUtils.getProjectVersionAttributes(projid)
            
            for projectAttr in projectAttrs['data']:
                #print(projectAttr)
                projectAttrDef = paDefs[projectAttr['attributeDefinitionId']]
                holdsscattrvalue = []

                if projectAttrDef['name'] in self.__Attributes:
                    holdattrid = projectAttr['attributeDefinitionId']
                    if projectAttr['values'] == None:
                        if projectAttr['value'] != None:
                            #holdsscattrvalue = projectAttr['value']
                            if projectAttrDef['type']  == 'DATE':
                                tempsscattrvalue = projectAttr['value']
                                #logging.info(tempsscattrvalue)
                                holdsscattrvalue = format(tempsscattrvalue).replace(" 00:00:00.0","T00:00:00.0")
                                #logging.info(holdsscattrvalue)
                            elif projectAttrDef['type']  == 'BOOLEAN':
                                holdsscattrvalue = projectAttr['value']
                            else:
                                holdsscattrvalue.append(projectAttr['value'])
                    else:
                        for val in projectAttr['values']:
                            holdsscattrvalue.append(val['name'])
        
                    #if exists see if it is in elastic table
            
                    if len(holdsscattrvalue) > 0:
                                
                        elattribute = self.__GetSSCProjectAttr2ByProjectId(projid,holdattrid)
                        #logging.info(elattribute)
                        if len(elattribute) > 0:
                            compareattrvalue = elattribute[projid]['attributeValue']
                            if projectAttrDef['type']  == 'DATE':
                                datecomparevalue = json.dumps(compareattrvalue)
                                missingt = datecomparevalue.find(" 00:00:00.0")
                                if missingt > 0:
                                    needsAttrReset = True
                    
                            if holdsscattrvalue != compareattrvalue:
                                logging.info(f"{pvMessage}, no matched attribute value: {projectAttrDef['name']}" )
                                #logging.info('ssc attribute value {}'.format(holdsscattrvalue))
                                #logging.info('elastic table value {}'.format(compareattrvalue))
                                #logging.info('no matched value')
                                needsAttrReset = True
                        else:
                            logging.info(f"{pvMessage}, no matching es attribute: {projectAttrDef['name']}" )
                            #logging.info('ssc attribute value {}'.format(holdsscattrvalue))
                            needsAttrReset = True
                else: # projectAttrDef['name'] not in self.__Attributes
                    logging.info(f"{pvMessage}, no matching es attribute: {projectAttrDef['name']}" )
                    needsAttrReset = True

               
            if needsAttrReset == True:
                logging.info('%s, syncing SSC attributes', pvMessage)
                # Clear out records to do refresh
                self.__DeleteById('sscprojattrs', 'projectVersionId', projid)
                self.__DeleteById('sscprojattr2', 'projectVersionId', projid)

                attCount = 0
                for projectAttr in projectAttrs['data']:

                    attCount = attCount + 1

                    #post Project Attribute records
                    holddata = {'projectVersionId': projid,
                                'attributerec': projectAttr}
                    self.__ElasticClient.Index('sscprojattrs', json.dumps(holddata))
                    

                for projectAttr in projectAttrs['data']:
                    for projectAttrDef in projectAttrDefs['data']:
                        if projectAttr['attributeDefinitionId'] == projectAttrDef['id']:
                            holdsscattrvalue = []
                            if projectAttr['values'] == None:
                                if projectAttr['value'] != None:
                                    if projectAttrDef['type']  == 'DATE':
                                        tempsscattrvalue = projectAttr['value']
                                        holdsscattrvalue = format(tempsscattrvalue).replace(" 00:00:00.0","T00:00:00.0")
                                    elif projectAttrDef['type']  == 'BOOLEAN':
                                        holdsscattrvalue = projectAttr['value']
                                    else:
                                        holdsscattrvalue.append(projectAttr['value'])
                            else:

                                holdsscattrvalue = []
                                for val in projectAttr['values']:
                                    holdsscattrvalue.append(val['name'])

                            attrInfo = initBlankAttrObject()
                            attrInfo['projectVersionId'] = projid
                            attrInfo['attributeId'] = projectAttr['attributeDefinitionId']
                            attrInfo['attributeName'] = projectAttrDef['name']
                            attrInfo['attributeValue'] = holdsscattrvalue
                            self.__ElasticClient.Index('sscprojattr2', json.dumps(attrInfo))

                queueInfo = initBlankQueueObject()
                holdnow = datetime.datetime.now()
                formatnow = holdnow.strftime("%Y-%m-%dT%H:%M:%S.%f")
                queueInfo['processedDateTime'] = formatnow
                queueInfo['projectVersionId'] = projid
                queueInfo['updateType'] = 'A'
                queueInfo['updateReason'] = updateReason
                queueInfo['completedDateTime'] = '1900-01-01T00:00:00.000-0000'
                logging.info(f"{pvMessage}, Creating Queue record")
                logging.info(queueInfo)
                self.__ElasticClient.Index('sscupdatequeue', json.dumps(queueInfo))

        if needsReset == False and forceSync:
            logging.info("%s, force sync requested", pvMessage)
            updateReason = "Force sync requested"
            needsReset = True                    
        
        # MAIN refresh processing
        #needsReset = False
        if needsReset == True:

            logging.info('%s, syncing SSC project version', pvMessage)

            # STEP 1 - Clear out records to do refresh
            self.__ClearProject(projid)

            # STEP 2 - Refresh project version (application and release)
            jproject = json.dumps(projectVersion)
            #logging.info(jproject)
            if projectVersion['id'] != projid:
                logging.error("Project version ID %s doesn't match expected projid %s", projectVersion['id'], projid)
            if projectVersion['id'] in seenIdList:
                logging.warning("Duplicate app version id %s detected when indexing sscproject (current projid: %s)", projectVersion['id'], projid)
            else:
                seenIdList.append(projid)
            self.__ElasticClient.Index('sscprojects', jproject)
                
            # STEP 3 - Refresh project version attributes
            projectAttrs = self.__SscUtils.getProjectVersionAttributes(projid)
            attCount = 0
            for projectAttr in projectAttrs['data']:
                attCount += 1

                #post Project Attribute records
                holddata = {'projectVersionId': projid,
                            'attributerec': projectAttr}
                self.__ElasticClient.Index('sscprojattrs', json.dumps(holddata))

            pvAttrs = []    
            for projectAttr in projectAttrs['data']:
                #print(projectAttr)
                for projectAttrDef in projectAttrDefs['data']:
                    if projectAttr['attributeDefinitionId'] == projectAttrDef['id']:
                        holdsscattrvalue = []
                        if projectAttr['values'] == None:
                            if projectAttr['value'] != None:
                                #holdsscattrvalue = projectAttr['value']
                                if projectAttrDef['type']  == 'DATE':
                                    tempsscattrvalue = projectAttr['value']
                                    logging.debug(tempsscattrvalue)
                                    holdsscattrvalue = format(tempsscattrvalue).replace(" 00:00:00.0","T00:00:00.0")
                                    logging.debug(holdsscattrvalue)
                                elif projectAttrDef['type']  == 'BOOLEAN':
                                    holdsscattrvalue = projectAttr['value']
                                else:
                                    holdsscattrvalue.append(projectAttr['value'])
                                                              
                        else:

                            holdsscattrvalue = []
                            for val in projectAttr['values']:
                                holdsscattrvalue.append(val['name'])

                        attrInfo = initBlankAttrObject()
                        attrInfo['projectVersionId'] = projid
                        attrInfo['attributeId'] = projectAttr['attributeDefinitionId']
                        attrInfo['attributeName'] = projectAttrDef['name']
                        attrInfo['attributeValue'] = holdsscattrvalue
                        pvAttrs.append(attrInfo)
                        self.__ElasticClient.Index('sscprojattr2', json.dumps(attrInfo))
            self.UpdateSidecarAttributes(pvAttrs) # adds to bulk queue, sending when full
                                
            # STEP 4 - Refresh project scans
            projectScans = self.__SscUtils.SscClient.GetProjectVersionScans(projid)

            scnCount = 0
            pvAssessmentTypes = {}
                
            logging.info("Adding scans for PV ID %s to sscprojscans", projid)
            for artifact in projectScans:

                # artifacts can have multiple scans. map and return as list
                scans = self.ArtifactToScans(artifact, projid, holdprojectname, holdname)

                for scan in scans:
                    # set a dict entry to indicate presence of this scan type (and its configured assessment type)
                    atype = self.__GetAssessmentType(scan['type'])
                    if not scan['type'] in pvAssessmentTypes.keys():
                        pvAssessmentTypes[scan['type']] = atype

                    # write the scan to Elastic
                    self.__ElasticClient.Index('sscprojscans', json.dumps(scan))
                    scnCount = scnCount + 1
                    
            # STEP 5 - Refresh issues 
            try:
                if self.__IssueDetailsEndpoint:
                    self.__SscUtils.BulkIssuesLoadFromDetails(projid, projectDefFilter)
                else:
                    self.__SscUtils.BulkIssuesLoad(projid, projectDefFilter)
            except (SscClient409ConflictException) as e:
                # Skip this project version if this specific error occurs
                if e.startswith("Audit session out of date."):
                    logging.error("SSC API audit session error, issue import for project version %s stopped ('[SscClient409ConflictException] %s')", projid, e.message)
                    logging.warning("[DATA WARNING] SSC project version %s may have no or incorrect issue counts", projid)
                    return
                else:
                    # Raise any other flavor of 409 conflict exception
                    raise

            # STEP 6 - Update summary counts for matching
            novuls = False
            issues_count = self.__SscUtils.getProjectVersionIssueCounts(projid, projectDefFilter)

            #logging.info(issues_count)

            jprojcounts = json.dumps(issues_count)
            self.__ElasticClient.Index('sscprojcounts', jprojcounts)
                
            if issues_count['count'] == 0:
                novuls = True

            # STEP 7 - Write zero records for those with 0 issues based on scan types found
            formatnow = datetime.datetime.now().strftime("%Y-%m-%dT%H:%M:%S.%f")
            if novuls:
                for key in pvAssessmentTypes.keys():
                    self.__WriteZeroIssue(projid, key, pvAssessmentTypes[key])

            # STEP 8 - Add to update queue
            queueInfo = initBlankQueueObject()
            queueInfo['processedDateTime'] = formatnow
            queueInfo['projectVersionId'] = projid
            queueInfo['updateType'] = 'U'
            queueInfo['updateReason'] = updateReason
            queueInfo['completedDateTime'] = '1900-01-01T00:00:00.000-0000'
            logging.info("%s, creating queue record for project version", pvMessage)
            #logging.info(queueInfo)
            self.__ElasticClient.Index('sscupdatequeue', json.dumps(queueInfo))

class SyncExtractorException(Exception):
    pass