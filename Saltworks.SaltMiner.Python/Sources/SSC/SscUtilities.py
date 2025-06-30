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
# Originally RWSSC_Utils.py and class sscUtils.
        
import json
import sys
import os
from datetime import datetime
import logging

from Core.RestClient import RestClient
from Core.SscClient import SscClient, SscClient409ConflictException
from Utility.ProgressLogger import *
from Utility.GeneralUtility import GeneralUtility

'''
    SSC Utilities
'''

class SscUtilities:

    def __init__(self, appSettings, sourceName):

        self.__App = appSettings.Application
        self.__ProjectVersions = {'data': [], 'count': 0}
        self.__AllIssues = {'data': [], 'Critical': 0, 'High': 0, 'Medium': 0, 'Low':0, 'count': 0}
        self.__GroupingTypeId = appSettings.GetSource(sourceName, 'GroupingTypeId')
        self.__FiltersetId = appSettings.GetSource(sourceName, 'FiltersetId')
        self.__DepWarnList = []
        self.__CallList = {}
        self.__SscBatchSize = appSettings.GetSource(sourceName, "IssueBatchSize", 500)
        self.__SscIssueDetailBatchSize = appSettings.GetSource(sourceName, "IssueDetailsBatchSize", 200)
        self.__IssueDetailsUseBulkApi = appSettings.GetSource(sourceName, "IssueDetailsUseBulkApi", False)
        self.__ElasticBatchSize = appSettings.GetSource(sourceName, "ElasticBatchSize", self.__SscBatchSize)
        self.__Prog = os.path.splitext(os.path.basename(__file__))[0]
        self.__CustomTagDefinitions = None
        
        self.__ElasticClient = appSettings.Application.GetElasticClient()

        self.__UseSscClient = not appSettings.FlagSet("RWSSC_Utils-Disable-SscClient")
        #self.__UseSscClient = False
        
        if self.__UseSscClient :
            self.__Ssc = SscClient(appSettings, sourceName)
            logging.debug(f"{self.__Prog}.init complete - using SSC client.")
            if appSettings.GetSource(sourceName, 'UseIssueDetailsEndpoint', False):
                self.__CustomTagDefinitions = self.__Ssc.GetCustomTagDefinitions()
        else:
            _headers = {'Accept':'application/json',
                'Content-Type':'application/json;charset=UTF-8'
                }
            self.restClient = RestClient(appSettings.GetSource(sourceName, 'BaseUrl'), appSettings.GetSource(sourceName, 'Username'), appSettings.GetSource(sourceName, 'Password'), appSettings.GetSource(sourceName, 'SslVerify'), _headers)
        
            #
            # Make the token expire in a day
            #
            add_days = datetime.datetime.now() + datetime.timedelta(days=1)
            #add_days = datetime.datetime.now() + datetime.timedelta(seconds=30)
            utcTomorrow = add_days.strftime("%Y-%m-%dT%H:%M:%S.%f")

            typeToken = {
                    "type": "UnifiedLoginToken",
                    "terminalDate": utcTomorrow
                }
        
            response = self.restClient.post('/api/v1/tokens', json=typeToken)
            authToken = json.loads(response.text)['data']['token']
            print(authToken)

            _headers = {'Accept':'application/json',
                'Content-Type':'application/json;charset=UTF-8',
                'Authorization': 'FortifyToken {}'.format(authToken)
                }

            self.restClient = RestClient(appSettings.GetSource(sourceName, 'BaseUrl'), sslVerify=appSettings.GetSource(sourceName, 'SslVerify'), defaultHeaders= _headers)

            logging.debug("{self.__Prog}.init complete.  RestClient params - url: '{}', username: '{}', verify: {}".format(appSettings.GetSource(sourceName, 'BaseUrl', '[Unknown]'), appSettings.GetSource(sourceName, 'Username', '[Unknown]'), appSettings.GetSource(sourceName, 'SslVerify', '[Unknown]')))
    
    def Cleanup(self):
        if self.__UseSscClient :
            self.__Ssc.Cleanup()
        self.callReport()

    @property
    def SscClient(self):
        return self.__Ssc
        
    def depWarn(self, mname):
        if mname in self.__DepWarnList:
            return
        msg = f"[DEPRECATED] {self.__Prog} method '{mname}' has been deprecated - please use SscClient version instead."
        logging.warning(msg)
        print(msg)
        self.__DepWarnList.append(mname)

    def addCall(self, mname):
        if not mname in self.__CallList.keys():
            self.__CallList[mname] = 0
        self.__CallList[mname] += 1

    def callReport(self):
        logging.info(f"{self.__Prog} method call report (only applies to methods tagged with addCall()...)")
        for c in self.__CallList.keys():
            logging.info(f"{c}: {self.__CallList[c]} call(s)")

    def getElasticUtil(self):
        return self.__ElasticClient

    def testConnection(self):
        
        if self.__UseSscClient:
            # no need to test, connection tested when initialized
            return

        self.depWarn("testConnection")
        _url = "/api/v1/projects"
        try:
            response = self.restClient.get(_url)
            if response.status_code != 200:
                logging.error("SSC api call failed with status {} and text {}".format(response.status_code, response.text))
                return False
            data = json.loads(response.text)
            if data is None or not 'count' in data:
                logging.error("SSC connection failure, response null or missing expected fields")
                return False
            logging.info("SSC test call result - status code: {}, project count: {}".format(response.status_code, data['count']))
            return True
        except Exception as error:
            logging.error("SSC test call failed - exception: " + str(error))
            return False

    # GregLook - all these SSC operations have been replaced with an SSC client call (when the switch is set).  
    # Deprecation warnings will fire a few times for anything that has been replaced, even when the SSC client is in use.
    def getProjectVersionIssueCounts(self, id, projDefFilter):
        
        # New Ssc client
        if self.__UseSscClient:
            return self.__Ssc.GetProjectVersionIssueCounts(id, projDefFilter)

        self.depWarn("getProjectVersionIssueCounts")
        self.addCall("getProjectVersionIssueCounts")

        _issueCounts = {
            'projectVersionId': id,
            'critical': 0, 'high': 0, 'medium': 0, 'low': 0, 'count': 0,
            'hiddenCount': 0, 'suppressedCount': 0, 'removedCount': 0
        }

        #_url = '/api/v1/projectVersions/{}/issueGroups?groupingtype=11111111-1111-1111-1111-111111111150&filterset=a243b195-0a59-3f8b-1403-d55b7a7d78e6&filter=FOLDER:b968f72f-cc12-03b5-976e-ad4c13920c21&qm=issues&showhidden=false&showremoved=false&showshortfileNames=true&showsuppressed=false'.format(id)
        _url = '/api/v1/projectVersions/{}/issueGroups?groupingtype={}&filterset={}&qm=issues&showhidden=false&showremoved=false&showshortfileNames=true&showsuppressed=false'.format(id, self.__GroupingTypeId, projDefFilter)
        response = self.restClient.get(_url)
        
        if response.status_code != 200:
            print('here')
        
        counts = json.loads(response.text)

        try:
            for count in counts['data']:
                if count['cleanName'] == "Critical":
                    _issueCounts['critical'] = count['visibleCount']

                elif count['cleanName'] == "High":
                    _issueCounts['high'] = count['visibleCount']
                elif count['cleanName'] == "Medium":
                    _issueCounts['medium'] = count['visibleCount']
                elif count['cleanName'] == "Low":
                    _issueCounts['low'] = count['visibleCount']
                else:
                    logging.info('odd: {}'.format(count['cleanName']))

            _issueCounts['count'] = _issueCounts['critical'] + _issueCounts['high'] + _issueCounts['medium'] + _issueCounts['low']    
        
        except KeyError:
            _issueCounts['critical'] = 0
            _issueCounts['high'] = 0
            _issueCounts['medium'] = 0
            _issueCounts['low'] = 0
            logging.info('error getting count totals - force recalc')

        
        _summaryHidden = self.getProjectVersionSummaryCounts(id, projDefFilter)

        try:

            #_issueCounts['hiddenCount'] = _summaryHidden['data'][0]['hiddenCount']
            _issueCounts['suppressedCount'] = _summaryHidden['data'][0]['suppressedCount']
            _issueCounts['removedCount'] = _summaryHidden['data'][0]['removedCount']

        except KeyError:

            _issueCounts['suppressedCount'] = 0
            _issueCounts['removedCount'] = 0
            logging.info('error getting count totals - force recalc')


        return _issueCounts

    def getProjectVersionIssueCountsHidden(self, id):
        # New Ssc client
        if self.__UseSscClient:
            return self.__Ssc.GetProjectVersionIssueCountsHidden(id)

        self.depWarn("getProjectVersionIssueCountsHidden")
        self.addCall("getProjectVersionIssueCountsHidden")
        
        _issueCountsHidden = {
            'projectVersionId': id,
            'hiddenCount': 0
        }
        _summaryHidden = self.getProjectVersionSummaryCounts(id)

        try:

            _issueCountsHidden['hiddenCount'] = _summaryHidden['data'][0]['hiddenCount']
            
        except KeyError:

            _issueCountsHidden['hiddenCount'] = 0
            logging.info('error getting count totals - force recalc')

        return _issueCountsHidden

    def IsIncompleteProjectVersion(self, id):
        # New Ssc client
        if self.__UseSscClient:
            return self.__Ssc.IsIncompleteProjectVersion(id)

        self.depWarn("IsIncompleteProjectVersion")
        self.addCall("IsIncompleteProjectVersion")
        
        r = self.restClient.get(f"/api/v1/projectVersions/{id}/filterSets")
        return r.status_code == 400

    def GetProjectVersion(self, id):
        # New Ssc client
        if self.__UseSscClient:
            return self.__Ssc.GetProjectVersion(id, False)
        
        
        self.depWarn("GetProjectVersion")
        self.addCall("GetProjectVersion")
        
        url = f'/api/v1/projectVersions/{id}'
        r = self.restClient.get(url)
        return json.loads(r.text)

    def getProjectVersionLOCCounts(self, id):
        # New Ssc client
        if self.__UseSscClient:
            return self.__Ssc.GetProjectVersionLocCounts(id)

        self.depWarn("getProjectVersionLOCCounts")
        self.addCall("getProjectVersionLOCCounts")
        
        _url = '/api/v1/projectVersions/{}/artifacts?embed=scans&start=0&limit=1000'.format(id)
        response = self.restClient.get(_url)
        LOCcounts = json.loads(response.text)
        return LOCcounts

    def getProjectVersionFilterSet(self, id):
        # New Ssc client
        if self.__UseSscClient:
            return self.__Ssc.GetProjectVersionFilterset(id)

        self.depWarn("getProjectVersionFilterSet")
        self.addCall("getProjectVersionFilterSet")
        
        _url = '/api/v1/projectVersions/{}/filterSets?&start=0&limit=200'.format(id)
        response = self.restClient.get(_url)
        PVfiltersets = json.loads(response.text)
        return PVfiltersets

    def getProjectVersionScans(self, id):
        # New Ssc client
        if self.__UseSscClient:
            return self.__Ssc.GetProjectVersionScans(id)

        self.depWarn("getProjectVersionScans")
        self.addCall("getProjectVersionScans")
        
        _url = '/api/v1/projectVersions/{}/artifacts?embed=scans&start=0&limit=1000'.format(id)
        response = self.restClient.get(_url)
        PVscans = json.loads(response.text)
        return PVscans


    def getProjectVersionAttributes(self, id):
        # New Ssc client
        if self.__UseSscClient:
            return self.__Ssc.GetProjectVersionAttributes(id)

        self.depWarn("getProjectVersionAttributes")
        self.addCall("getProjectVersionAttributes")
        
        _url = '/api/v1/projectVersions/{}/attributes'.format(id)
        response = self.restClient.get(_url)
        PVattrs = json.loads(response.text)

        return PVattrs

    def getProjectVersionAttributeDefinitions(self, limit=200):
        # New Ssc client
        if self.__UseSscClient:
            return self.__Ssc.GetProjectVersionAttributeDefinitions(limit)

        self.depWarn("getProjectVersionAttributeDefinitions")
        self.addCall("getProjectVersionAttributeDefinitions")
        
        _url = '/api/v1/attributeDefinitions?start=0&limit={}'.format(limit)
        response = self.restClient.get(_url)
        PVattrdef = json.loads(response.text)

        return PVattrdef

    def getCloudscanJob(self):
        # New Ssc client
        if self.__UseSscClient:
            return self.__Ssc.GetCloudJobs()

        self.depWarn("getCloudscanJob")
        self.addCall("getCloudscanJob")
        
        _url = '/api/v1/cloudjobs?start=-1&limit=-1'
        response = self.restClient.get(_url)
        CloudScanJobs = json.loads(response.text)
        return CloudScanJobs


    def getProjectVersionSummaryCounts(self, id, projDefFilter):
        # New Ssc client
        if self.__UseSscClient:
            return self.__Ssc.GetProjectVersionSummaryCounts(id, projDefFilter)

        self.depWarn("getProjectVersionSummaryCounts")
        self.addCall("getProjectVersionSummaryCounts")
        
        #_url = '/api/v1/projectVersions/{}/issueStatistics?filterset={}'.format(id, self.__FiltersetId)
        _url = '/api/v1/projectVersions/{}/issueStatistics?filterset={}'.format(id, projDefFilter)
        response = self.restClient.get(_url)
        _hiddens = json.loads(response.text)
        return _hiddens


    def getProjectVersionIssueDetail(self, projid, issueid):
        # New Ssc client
        if self.__UseSscClient:
            return self.__Ssc.GetProjectVersionIssue(projid, issueid)

        self.depWarn("getProjectVersionIssueDetail")
        self.addCall("getProjectVersionIssueDetail")
        
        _url = '/api/v1/projectVersions/{}/issues/{}'.format(projid, issueid)
        response = self.restClient.get(_url)
        _detail = json.loads(response.text)
        return _detail

    def getProjectVersionIssueTagCounts(self, id, projDefFilter):
        # New Ssc client
        if self.__UseSscClient:
            return self.__Ssc.GetProjectVersionIssueTagCounts(id, projDefFilter)

        self.depWarn("getProjectVersionIssueTagCounts")
        self.addCall("getProjectVersionIssueTagCounts")
        
        #taggrouping = '87f2364f-dcd4-49e6-861d-f8d3f351686b'
        _url = '/api/v1/projectVersions/{}/issueGroups?filterset={}&start=0&limit=200&groupingtype=87f2364f-dcd4-49e6-861d-f8d3f351686b&qm=issues&showhidden=false&showremoved=true&showshortfileNames=true&showsuppressed=true'.format(id, projDefFilter)
        response = self.restClient.get(_url)
        tagcounts = json.loads(response.text)

        return tagcounts

    def getProjectVersionsUsers(self, projectVersionId):
        # New Ssc client
        if self.__UseSscClient:
            return self.__Ssc.GetProjectVersionUsers(id, False)

        self.depWarn("getProjectVersionsUsers")
        self.addCall("getProjectVersionsUsers")
        
        _url = f"/api/v1/projectVersions/{projectVersionId}/authEntities"
        response = self.restClient.get(_url)
        authEntities = json.loads(response.text)
        return authEntities
    
    # GregLook: modified to include batchSize when calling __Ssc.GetProjectVersions, and to log when the special shorten lists flag is set
    def getProjectVersions(self):
        batchSize = 200
        debug = self.__App.Settings.FlagSet("Debug-Shorten-Lists")
        if debug:
            batchSize = 10
            logging.info('Shorten list flag in effect ("Debug-Shorten-Lists")')
        
        # New Ssc client
        if self.__UseSscClient:
            self.__ProjectVersions['data'] = self.__Ssc.GetProjectVersions('project,id,issueTemplateId,currentState,name', batchSize=batchSize)
            self.__ProjectVersions['count'] = len(self.__ProjectVersions['data'])
            return self.__ProjectVersions

        self.depWarn("getProjectVersions")
        self.addCall("getProjectVersions")
        
        _url = '/api/v1/projectVersions?start=0&limit=200&fulltextsearch=false&includeInactive=false&fields=project,id,issueTemplateId,currentState,name'
        _moreRecords = True

        while _moreRecords:
            response = self.restClient.get(_url)
            projectVersions = json.loads(response.text)

            if self.__ProjectVersions['count'] == 0:
                self.__ProjectVersions['count'] = projectVersions['count']
                logging.info('Downloading for {} project versions'.format(self.__ProjectVersions['count']))
            else:       
                logging.info('Downloading {} of {} total records'.format(len(projectVersions['data']), self.__ProjectVersions['count']))

            for projectVersion in projectVersions['data']:
                self.__ProjectVersions['data'].append(projectVersion)

            if debug:
                _moreRecords = False
                print('[DEBUG] Shorten list flag in effect')
                continue

            try:
                _url = projectVersions['links']['next']['href']
            except KeyError:
                _moreRecords = False
            except:
                logging.error('Unexpected error:{}'.format(sys.exc_info()[0]))
                _moreRecords = False

        logging.info('Downloaded total of {} project versions'.format(len(self.__ProjectVersions['data'])))
        return self.__ProjectVersions


    # elastic & SSC stuff

    def Nvl(self, doc, field, repl=None):
        return repl if not doc or field not in doc.keys() else doc[field]

    def __AddCustomTagKeyValue(self, ctv):
        '''
        Adds keyValue to a customTagValue object
        '''
        if not ctv:
            logging.debug("Passed ctv missing or None")
            return
        guid = ctv['customTagGuid']
        defn = None
        for d in self.__CustomTagDefinitions:
            if d['guid'] == guid:
                defn = d
                break
        if not defn:
            logging.debug("Couldn't find custom tag definition for guid '%s'", guid)
            ctv['keyValue'] = None
            return
        if 'dateValue' in ctv.keys():
            ctv['keyValue'] = { "name": defn['name'], "value": GeneralUtility.GetFormattedDateString(ctv['dateValue']) } 
        if 'decimalValue' in ctv.keys():
            ctv['keyValue'] = { "name": defn['name'], "value": str(ctv['decimalValue']) } 
        if 'textValue' in ctv.keys():
            ctv['keyValue'] = { "name": defn['name'], "value": ctv['textValue'] } 
        if 'newCustomTagIndex' in ctv.keys():
            if not 'valueList' in defn.keys():
                logging.debug("Expected value list not present in custom tag definition for guid '%s', name '%s'", guid, defn['name'])
                return
            for v in defn['valueList']:
                if v['lookupIndex'] == ctv['newCustomTagIndex']:
                    ctv['keyValue'] = { "name": defn['name'], "value": v['lookupValue'] }
                    break
            if not 'keyValue' in ctv.keys():
                logging.debug("Custom tag lookup index %s not found for '%s' (%s) custom tag", ctv['newCustomTagIndex'], defn['name'], guid)

    def __BulkIssuesLoadFromDetailsMapIssueFromDetail(self, dtl, reviewed, baseUrl, pvid):
        url = self.Nvl(dtl, 'url')
        ctvList = self.Nvl(dtl, 'customTagValues')
        for ctv in ctvList:
            self.__AddCustomTagKeyValue(ctv)
        return {
            'bugUrl': None, # not used and not available from this endpoint
            'hasComments': False, # not used and not available from this endpoint
            'hidden': self.Nvl(dtl, 'hidden'),
            'issueName': self.Nvl(dtl, 'issueName'),
            'folderGuid': '', # not used and not available from this endpoint
            'lastScanId': self.Nvl(dtl, 'lastScanId'),
            'engineType': self.Nvl(dtl, 'engineType'),
            'externalBugId': None, # not used and not available from this endpoint
            'issueStatus': self.Nvl(dtl, 'issueStatus'),
            'friority': self.Nvl(dtl, 'friority'),
            'analyzer': self.Nvl(dtl, 'analyzer'),
            'primaryLocation': self.Nvl(dtl, 'shortFileName'),
            'reviewed': reviewed,
            'id': self.Nvl(dtl, 'id'),
            'suppressed': self.Nvl(dtl, 'suppressed'),
            'hasAttachments': False, # not used and not available from this endpoint
            'engineCategory': self.Nvl(dtl, 'engineCategory'),
            'projectVersionName': None, # not used and not available from this endpoint
            'removedDate': self.Nvl(dtl, 'removedDate'),
            'severity': self.Nvl(dtl, 'severity'),
            '_href': f"{baseUrl}api/v1/projectVersions/{pvid}/issues/{self.Nvl(dtl, 'id')}",
            'displayEngineType': self.Nvl(dtl, 'displayEngineType'),
            'foundDate': self.Nvl(dtl, 'foundDate'),
            'confidence': self.Nvl(dtl, 'confidence'),
            'impact': self.Nvl(dtl, 'impact'),
            'primaryRuleGuid': self.Nvl(dtl, 'primaryRuleGuid'),
            'projectVersionId': pvid,
            'scanStatus': self.Nvl(dtl, 'scanStatus'),
            'audited': self.Nvl(dtl, 'audited'),
            'kingdom': self.Nvl(dtl, 'kingdom'),
            'folderId': 0, # not used and not available from this endpoint
            'revision': self.Nvl(dtl, 'revision'),
            'likelihood': self.Nvl(dtl, 'likelihood'),
            'removed': False if not self.Nvl(dtl, 'removedDate') else True,
            'issueInstanceId': self.Nvl(dtl, 'issueInstanceId'),
            'hasCorrelatedIssues': False, # not used and not available from this endpoint
            'primaryTag': self.Nvl(self.Nvl(dtl, 'primaryTag'), 'tagValue'),
            'lineNumber': self.Nvl(dtl, 'lineNumber'),
            'projectName': None, # not used and not available from this endpoint
            'fullFileName': self.Nvl(dtl, 'fullFileName') if not url else url,
            'primaryTagValueAutoApplied': False, # not used and not available from this endpoint,
            'customTagValues': ctvList, # detail endpoint only, this is why we do this
            'customAttributes': self.Nvl(dtl, 'customAttributes') # detail endpoint only, this is why we do this
        }
    
    def __BulkIssuesLoadDetails(self, issueBatch, pvid):
        '''
        Takes a batch of issues from the issues endpoint and returns completed issues with detail additions in them (no SSC bulk API usage).
        '''
        ssc = self.__Ssc
        url = ssc.BaseUrl + "api/v1/issueDetails/"
        issues = []
        counter = 0
        for iss in issueBatch:
            rsp = ssc.GetProjectVersionIssueDetail(iss['id'])['data']
            issues.append(self.__BulkIssuesLoadFromDetailsMapIssueFromDetail(rsp, iss['reviewed'], self.__Ssc.BaseUrl, pvid))
            counter += 1
            if counter % self.__SscIssueDetailBatchSize == 0:
                logging.info("Bulk issue detail loading %i-%i of a batch of %i issues", counter - self.__SscIssueDetailBatchSize + 1, counter, len(issueBatch))
        if len(issueBatch) != len(issues):
            logging.warning("BulkIssuesLoadDetails - counts don't match (expected %s, found %s)", len(issueBatch), len(issues))
        return issues
    
    def __BulkIssuesLoadFromDetailsBatchBulk(self, requests, issues, pvid):
        '''
        Loads a batch of issues from the issue details endpoint given a list of requests, appending them to the passed issues list in progress

        This "batch" is a set of issue details requests, not to be confused with the batch of issues from the issues endpoint that is a level higher.  
        Batch of a batch if you will.
        '''
        lst = []
        for r in requests:
            lst.append(r['req'])
        for rsp in self.__Ssc.Bulk(lst):
            for req in requests:
                if rsp['id'] == req['id']:
                    issues.append(self.__BulkIssuesLoadFromDetailsMapIssueFromDetail(rsp, req['reviewed'], self.__Ssc.BaseUrl, pvid))
                    break
    
    def __BulkIssuesLoadFromDetailsBatch(self, issueBatch, pvid):
        '''
        Takes a batch of issues from the issues endpoint and using the Bulk API and further division of the batch into detailBatchSize chunks, 
        returns completed issues with detail additions in them.
        '''
        ssc = self.__Ssc
        url = ssc.BaseUrl + "api/v1/issueDetails/"
        requests = []
        issues = []
        counter = 0
        for iss in issueBatch:
            requests.append({ 'id': iss['id'], 'reviewed': iss['reviewed'], 'req': ssc.BulkRequest(f"{url}/{iss['id']}") })
            counter += 1
            if len(requests) >= self.__SscIssueDetailBatchSize:
                logging.info("Bulk issue detail loading %i-%i of a batch of %i issues", counter - self.__SscIssueDetailBatchSize + 1, counter, len(issueBatch))
                self.__BulkIssuesLoadFromDetailsBatchBulk(requests, issues, pvid)
                requests = []
        if len(requests) > 0:
            logging.info("Bulk issue detail loading remaining %i issues", len(requests))
            self.__BulkIssuesLoadFromDetailsBatchBulk(requests, issues, pvid)
        if len(issueBatch) != len(issues):
            logging.warning("BulkIssuesLoadFromDetailsBatch - counts don't match (expected %s, found %s)", len(issueBatch), len(issues))
        return issues
    
    def BulkIssuesLoadFromDetails(self, projectVersionId, projDefFilter):
        ''' 
        Same bulk issue loader as BulkIssuesLoad(), but uses the IssueDetails endpoint for the issue data, 
        which allows us to bring in more information than is available in just the Issues endpoint.
        '''
        more = True
        count = 0
        bulkDocs = []
        eBatchSize = self.__ElasticBatchSize
        p = ProgressLogger(self.__ElasticClient)
        while more:
            try:
                r = self.__Ssc.GetProjectVersionIssuesV2(projectVersionId, projDefFilter, "id,reviewed", self.__SscBatchSize, False, restartScroll = (count == 0))
            except SscClient409ConflictException as e:
                logging.error("SSC conflict error (409) while updating issues, aborting with a partial update for PV ID %s.  Message: %s", projectVersionId, e.message)
                r = None
            if not r or not r['data']:
                more = False
            else:
                if not p.Started:
                    p.Start(f"{self.__Prog}.BulkIssuesLoad", r['count'])
                if self.__IssueDetailsUseBulkApi:
                    batch = self.__BulkIssuesLoadFromDetailsBatch(r['data'], projectVersionId)
                else:
                    batch = self.__BulkIssuesLoadDetails(r['data'], projectVersionId)
                for doc in batch:
                    doc['lastUpdated'] = datetime.datetime.now().strftime("%Y-%m-%dT%H:%M:%S")
                    bulkDocs.append(self.__ElasticClient.BulkInsertDocument('sscprojissues', doc))
                    count += 1
                    if len(bulkDocs) == eBatchSize:
                        p.Progress(count, f"Bulk inserting {eBatchSize} documents into SaltMiner")
                        self.__ElasticClient.BulkInsert(bulkDocs)
                        bulkDocs = []

        if len(bulkDocs) > 0:
            p.Progress(count, f"Bulk inserting {len(bulkDocs)} documents into SaltMiner")
            self.__ElasticClient.BulkInsert(bulkDocs)
        if p.Started:
            p.Finish(None, "Completed loading this batch of issues from SSC to SaltMiner.")

    # Simplified bulk issues loader using SscClient and ElasticClient
    def BulkIssuesLoad(self, projectVersionId, projDefFilter):
        _moreRecords = True
        iCurrentCount = 0
        bulkDocs = []
        sBatchSize = self.__SscBatchSize
        eBatchSize = self.__ElasticBatchSize
        p = ProgressLogger(self.__ElasticClient)
        while _moreRecords:
            response = self.__Ssc.GetProjectVersionIssues(projectVersionId, projDefFilter, sBatchSize, False)
            if response == None:
                if p.Started:
                    p.Finish(None, "Completed loading issues from SSC to SaltMiner.")
                _moreRecords = False
            else:
                if iCurrentCount == 0:
                    p.Start(f"{self.__Prog}.BulkIssuesLoad", response['count'])
                for doc in response['data']:
                    doc['lastUpdated'] = datetime.datetime.now().strftime("%Y-%m-%dT%H:%M:%S")
                    bulkDocs.append(self.__ElasticClient.BulkInsertDocument('sscprojissues', doc))
                    iCurrentCount += 1
                    if len(bulkDocs) == eBatchSize:
                        p.Progress(iCurrentCount, f"Bulk inserting {eBatchSize} documents into SaltMiner")
                        self.__ElasticClient.BulkInsert(bulkDocs)
                        bulkDocs = []

                if len(bulkDocs) > 0:
                    p.Progress(iCurrentCount, f"Bulk inserting {len(bulkDocs)} documents into SaltMiner")
                    self.__ElasticClient.BulkInsert(bulkDocs)
    
    # GregLook - deprecating and disabling this method.  Only reference to it is in RWExtractSSCHidden.py (line 94), which seems to not be in use.
    def getAndLoadProjectVersionIssuesHidden(self, id, elasticUtility):
        '''
        WARNING - this method appears to not be currently in use in current code, and is therefore deprecated.  Don't use it as-is, it needs to be rewritten first.
        '''
        raise(Exception("Deprecated code is disabled"))

        _issues = {'data': [], 'count': 0}
        # TODO: this doesn't quite fit into the SscClient.GetProjectVersions() method as is - let's either modify that method to be more flexible, or add
        #       one that is made to serve this use case.
        _url = '/api/v1/projectVersions/{}/issues?start=0&limit=500&showhidden=true&showremoved=true&showsuppressed=true&showshortfilenames=true'.format(id)
        _moreRecords = True
        iCurrentRecord = 0

        while _moreRecords:

            response = self.restClient.get(_url)
            issues = json.loads(response.text)
            
            if _issues['count'] == 0:
                _issues['count'] = issues['count']
                logging.info('Downloading for {} issues'.format(_issues['count']))
            else:
                logging.info('Downloading at {} - {} of {} total records'.format(iCurrentRecord, len(issues['data']), _issues['count']))

            for issue in issues['data']:
                iCurrentRecord = iCurrentRecord + 1

                #logging.info(issue)

                if (issue['hidden'] == True):
                    
                    elasticUtility.postSSCProjIssuesHidden(json.dumps(issue))
                #_issues['data'].append(issue)
                                   
            try:
                _url = issues['links']['next']['href']
        
            except KeyError:
                _moreRecords = False
                logging.info('no more records to download')
            except:
                _moreRecords = False
                logging.info('something else happened trying to get next href')
                '''print('In getProjectVersionIssues - Unexpected error:{}'.format(sys.exc_info()[0]))
                '''
        
        return True

    # GregLook - deprecating and disabling this method.  No references.
    def getAndLoadProjectVersionIssuesHold(self, id):
        '''
        WARNING - this method appears to not be currently in use in current code, and is therefore deprecated.  Don't use it as-is, it needs to be rewritten first.
        '''
        raise(Exception("Deprecated code is disabled"))

        es = self.getElasticUtil()

        _issues = {'data': [], 'count': 0}
        
        # TODO: this doesn't quite fit into the SscClient.GetProjectVersions() method as is - let's either modify that method to be more flexible, or add
        #       one that is made to serve this use case.
        _url = '/api/v1/projectVersions/{}/issues?start=0&limit=500&showhidden=true&showremoved=true&showsuppressed=true&showshortfilenames=true'.format(id)

        _moreRecords = True

        iCurrentRecord = 0

        while _moreRecords:

            response = self.restClient.get(_url)
            issues = json.loads(response.text)

            if _issues['count'] == 0:
                _issues['count'] = issues['count']
                logging.info('Downloading for {} issues'.format(_issues['count']))
            else:
                logging.info('Downloading at {} - {} of {} total records'.format(iCurrentRecord, len(issues['data']), _issues['count']))

            for issue in issues['data']:
                iCurrentRecord = iCurrentRecord + 1
                es.postSSCProjIssues(json.dumps(issue))
                #_issues['data'].append(issue)
                                   
            try:
                _url = issues['links']['next']['href']
        
            except KeyError:
                _moreRecords = False
                logging.info('no more records to download')
            except:
                _moreRecords = False
                logging.info('something else happened trying to get next href')
                '''print('In getProjectVersionIssues - Unexpected error:{}'.format(sys.exc_info()[0]))
                '''

        
        return True



    def fixComma(self,instr):
        sT = '{}'.format(instr).replace(',', ' ')
        return sT

    # GregLook - deprecated and disabled this method.  No references to it.
    def exportSSCSummaryStats(self):
        '''
        WARNING - this method appears to not be currently in use in current code, and is therefore deprecated.  Review before using.
        '''
        ofile = open('summaryExport.csv', 'w+')

        ofile.write("ProjectID, ProjectName, VersionID, VersionName, lastFprUploadDate, issueTemplateId, Critical, High, Medium, Low\n") 
        for projectVersion in self.__ProjectVersions['data']:

            Critical = 0
            High = 0
            Medium = 0
            Low = 0

            for issue in self.__AllIssues['data']:
                if issue['projectVersionID'] == projectVersion['id']:
                    Critical = issue['Critical']
                    High = issue['High']
                    Medium = issue['Medium']
                    Low = issue['Low']
                
            ofile.write("{}, {}, {}, {}, {}, {}, {}, {}, {}, {}\n".format(
                projectVersion['project']['id'],
                self.fixComma(projectVersion['project']['name']),
                projectVersion['id'],
                self.fixComma(projectVersion['name']),
                projectVersion['currentState']['lastFprUploadDate'],
                projectVersion['issueTemplateId'],
                Critical, High, Medium, Low))
        ofile.close()

        logging.info('Data export complete.')


 
class SscUtilitiesException(Exception):
    pass