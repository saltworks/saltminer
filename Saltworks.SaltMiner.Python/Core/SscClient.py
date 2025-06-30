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

import json
import sys
import time
import datetime
import logging
import uuid
import os

import requests
from requests.exceptions import ConnectionError as RequestsConnectionError, ReadTimeout as RequestsReadTimeout

from .RestClient import RestClient

class SscClient(object):

    def __init__(self, appSettings, sourceName):
        '''
        Initializes the class.

        appSettings: Settings instance containing application settings
        sourceName: SourceName appearing in a config file in Config\\Sources
        '''
        if type(appSettings).__name__ != "ApplicationSettings":
            raise SscClientConfigurationException("Type of appSettings must be 'ApplicationSettings'")
        if not sourceName or not sourceName in appSettings.GetSourceNames():
            raise SscClientConfigurationException(f"Invalid or missing source configuration for source name '{sourceName}'")
        sourceType = appSettings.GetSource(sourceName, "Source", "")
        if not sourceType == "SSC":
            raise SscClientConfigurationException(f"Invalid source type '{sourceType}', should be 'SSC'. (in source config '{sourceName}', property 'Source')")
        self.__App = appSettings.Application
        self.__Client = None
        self.__SourceName = sourceName
        self.__GroupingTypeId = appSettings.GetSource(sourceName, 'GroupingTypeId')
        self.__FiltersetId = appSettings.GetSource(sourceName, 'FiltersetId')
        self.__CacheKeys = []
        self.__Id = uuid.uuid4()
        self.__DateForFile = datetime.datetime.now().strftime('%Y.%m.%d')
        self.__GetAuthToken(appSettings, sourceName) # this initializes the internal RestClient (self.__Client)
        self.__BatchUrls = {}
        self.__CleanedUp = False
        self.__RetrySec = appSettings.GetSource(sourceName, 'ServerErrorRetrySeconds', 60)
        self.__MaxRetries = appSettings.GetSource(sourceName, 'ServerErrorMaxRetries', 3)
        self.__RetryCount = 0
        self.__RequestStatsReportEnabled = appSettings.GetSource(sourceName, 'RequestStatsReportEnabled', False)
        self.__LoggingFolder = appSettings.Get('Logging', 'Folder')
        
        # Sample auth response
        # {"data":{"remainingUsages":-1,"terminalDate":"2020-06-19T20:36:42.697+0000","description":null,"id":1963,"creationDate":"2020-06-18T20:36:42.697+0000","type":"UnifiedLoginToken","token":"MGY4NDBkMzItZTlkYS00MjczLWI4OWQtNzEyZjVhMmQ2YTZj","username":"dennis.hurst"},"responseCode":201}
        self.__App.LogDebug("SscClient initialization complete.  RestClient params - url: '{}', username: '{}', verify: {}".format(appSettings.GetSource(sourceName, 'BaseUrl'), appSettings.GetSource(sourceName, 'Username'), appSettings.GetSource(sourceName, 'SslVerify')))

    @property
    def BaseUrl(self):
        return self.__Client.BaseUrl

    def __TokenApiCall(self, appSettings, sourceName, isDelete=False, isRetry=False):
        tokenDescription = appSettings.GetSource(sourceName, 'AuthTokenDescription', 'SaltMiner')
        headers = {
            'Accept':'application/json',
            'Content-Type':'application/json;charset=UTF-8'
        }
        if not isDelete:
            # Make the token expire in a day
            body = {
                "type": "UnifiedLoginToken",
                "terminalDate": (datetime.datetime.now() + datetime.timedelta(days=1)).strftime("%Y-%m-%dT%H:%M:%S.%f"),
                "description": tokenDescription
            }
        auth = RestClient.basicAuth(appSettings.GetSource(sourceName, "Username"), appSettings.GetSource(sourceName, "Password"))
        verify = (appSettings.GetSource(sourceName, 'SslVerify')=='True')
        if isDelete:
            url = f"{appSettings.GetSource(sourceName, 'BaseUrl')}/api/v1/tokens/{self.__AuthTokenId}"
            return requests.delete(url, verify=verify, headers=headers, auth=auth)
        else:
            try:
                url = appSettings.GetSource(sourceName, 'BaseUrl') + '/api/v1/tokens'
                return requests.post(url, json=body, verify=verify, headers=headers, auth=auth)
            except ConnectionError as e:
                msg = f"Error when requesting a login token: {e}"
                logging.error(msg)
                if isRetry:
                    raise SscClientServerErrorException(msg) from e
                logging.info("Login retry in 60 sec...")
                time.sleep(10)
                return self.__TokenApiCall(appSettings, sourceName, False, True)

        
    def __GetAuthToken(self, appSettings, sourceName):
        RestClient.disableRequestWarnings()
        response = self.__TokenApiCall(appSettings, sourceName)
        r = json.loads(response.text)
        if not response.ok:
            raise SscClientAuthenticationException(f"({response.status_code}) {r['message']}")
        self.__AuthToken = r['data']['token']
        self.__AuthTokenId = r['data']['id']
        headers = {
            'Accept':'application/json',
            'Content-Type':'application/json;charset=UTF-8',
            'Authorization': 'FortifyToken {}'.format(self.__AuthToken)
        }
        self.__Client = RestClient(appSettings.GetSource(sourceName, 'BaseUrl'), sslVerify=appSettings.GetSource(sourceName, 'SslVerify'), defaultHeaders= headers)
        self.__Client.SessionEnabled = True

    def Cleanup(self):
        if self.__CleanedUp:
            return
        try:
            logging.info("Attempting to release SSC auth token")
            r = self.__TokenApiCall(self.__App.Settings, self.__SourceName, True)
            if r.ok:
                logging.info("Token released successfully")
            else:
                logging.warning(f"Failed to release SSC token ({r.status_code})")
        except Exception:
            logging.warning("Unable to release token", exc_info=True)

        try:
            for k in self.__CacheKeys:
                self.__DeleteCache(k)
        except Exception:
            logging.error("Error in removing temp cache files", exc_info=True)

        try:
            self.__DumpStats()
        except Exception:
            logging.error(f"Error reporting API stats", exc_info=True)

        self.__CleanedUp = True

    def __DumpStats(self):
        if not self.__RequestStatsReportEnabled:
            return
        fldr = self.__LoggingFolder
        file = os.path.join(fldr, f"{self.__DateForFile}.SaltMiner.SscClientStats-{self.__Id}.json")
        self.__App.CleanFiles(fldr, "*.SscClientStats*.json", 7)
        with open(file, "w") as f:
            f.write(json.dumps(self.__Client.RequestStatsReport()))
        self.__App.LogInfo(f"SSC client API session stats written to log file '{file}'")
    
    def __Get(self, url, navToData = False, suppressError = False, statKey = None, errorOnEmptyResponse = True, retryDelaySec=None):
        '''
        Calls GET and loads the response into an object, raising an error if not a 2xx response.

        Parameters:
        url - url to GET
        navToData - set to True if the return object should be automatically be navigated to the 'data' element (i.e. r['data']).
        suppressError - set to True to not raise an error if the response.status_code is not 2xx.  Returns None if error suppressed.
        statKey - used in recording stats for API calls
        '''
        wait = self.__RetrySec if not retryDelaySec or retryDelaySec < 0 else retryDelaySec
        ex = None

        try:
            resp = self.__GetResponseDataOrError(self.__Client.Get(url, statKey), navToData, suppressError, errorOnEmptyResponse)
            self.__RetryCount = 0
            return resp
            
        except SscClientAuthenticationException as e:
            self.__App.LogWarning("Authentication token invalid, attempting to get new token and retry previous operation")
            self.__GetAuthToken(self.__App.Settings, self.__SourceName)
            if self.__RetryCount == 1:
                wait = 1
            else:
                logging.warning("After first retry adding a longer wait of %s secs", wait)
            ex = e

        except (SscClientServerErrorException, SscClientEmptyResponseException, ConnectionError, RequestsConnectionError, RequestsReadTimeout) as e:
            logging.error("SSC API error encountered (%s), retrying in %s secs.", type(e).__name__, wait)
            ex = e

        except SscClientException:
            raise

        except Exception as e:
            logging.error("__Get failed to handle exception of type %s", type(e).__name__)
            raise

        # Retry
        self.__RetryCount += 1

        if self.__RetryCount == self.__MaxRetries:
            self.__RetryCount = 0
            logging.error("Reached retry limit - see earlier logged errors for details.")
            raise ex

        time.sleep(wait)
        return self.__Get(url, navToData, suppressError, statKey, errorOnEmptyResponse, retryDelaySec)

    def __GetResponseDataOrError(self, response, navToData = True, suppressError = False, errorOnEmptyResponse = True):
        '''
        Returns response loaded as an object or raises an error if not a 2xx response.

        Parameters:
        response - the response to load
        navToData - set to False if the return object shouldn't be automatically be navigated to the 'data' element (i.e. r['data']).
        suppressError - set to True to not raise an error if the response.status_code is not 2xx.  Returns None if error suppressed.
        '''
        if not response.status_code:
            sc = 0
        else:
            sc = int(response.status_code)
        if sc == 404:
            msg = f"(404) Not found for url '{response.url}'"
            logging.error(f"SSC API call failure: {msg}")
            raise SscClientException(msg)
        if 200 <= sc < 300:
            m = None if not response.text else json.loads(response.text)
            if not m and errorOnEmptyResponse:
                raise(SscClientEmptyResponseException("Empty response not expected."))
            if navToData and m:
                return m['data']
            else:
                return m
        elif sc == 400:
            # Validation error
            raise SscClientBadRequestException(response.text)
        elif sc == 401:
            # Our token may be missing/invalid, indicate we need a new one.
            raise SscClientAuthenticationException("Unauthorized")
        elif sc == 403 and "session" in response.text and "timed" in response.text:
            # Our token may have expired, indicate we need a new one.
            raise SscClientAuthenticationException(response.text)
        elif sc == 409:
            # Conflict status thrown sometimes that usually indicates a lookup ID could not be found.
            raise SscClient409ConflictException(f"{response.text}")
        elif sc > 499:
            # Separate 50x errors from the rest so can retry.
            raise SscClientServerErrorException(response.reason)
        else:
            msg = f"SSC API call failure: ({sc}) {response.text}"
            logging.error(msg)
            if not suppressError:
                raise SscClientException(msg)
            return None

    def __SetCache(self, key, data):
        logging.info(f"Writing cache data for key '{key}'")
        with open(f"{key}-{self.__Id}.tmp", "w") as f:
            f.write(json.dumps(data))
        self.__CacheKeys.append(key)

    def __GetCache(self, key):
        if os.path.exists(f"{key}-{self.__Id}.tmp"):
            logging.info(f"Cache hit for key '{key}'")
            with open(f"{key}-{self.__Id}.tmp", "r") as f:
                return json.loads(f.read())

    def __DeleteCache(self, key):
        logging.info(f"Clearing cache data for key '{key}'")
        if os.path.exists(f"{key}-{self.__Id}.tmp"):
            os.remove(f"{key}-{self.__Id}.tmp")
       
    @staticmethod
    def TestConnection(appSettings, sourceName):
        '''
        Connects to SSC
        '''
        try:
            SscClient(appSettings, sourceName)
            return True,""
        except Exception as e:
            return False, f"[{type(e).__name__}] {e}"

    def BulkRequest(self, uri, httpVerb='GET', postBody=None):
        return { 'uri': uri, 'httpVerb': httpVerb, 'postData': postBody }
    
    def Bulk(self, requests):
        '''
        Send in multiple requests at once - see https://ssc.saltworks.io/ssc/html/docs/docs.html#!/bulk for more info.
        This version assumes the requests will all return the same single doc structure and unpacks the responses into a list.
        
        Parameters:
        requests - list of items to process; can be the direct array or can contain an array element as specified by requestDataElement
        requestDataElement - if specified, 

        returns a list of data items unpacked from the bulk response
        '''
        lst = []       
        retry = False
        wait = self.__RetrySec
        try:
            rlst = self.__GetResponseDataOrError(self.__Client.Post("api/v1/bulk", { "requests": requests }))
        except (SscClientServerErrorException, SscClientEmptyResponseException, ConnectionError, RequestsConnectionError) as e:
            self.__App.LogError(f"SSC Bulk API error - [{type(e).__name__}] {e}, retrying in {wait} secs...")
            time.sleep(wait)
            retry = True
        except SscClientAuthenticationException as e:
            self.__App.LogWarning("Authentication token invalid, attempting to get new token and retry previous operation")
            self.__GetAuthToken(self.__App.Settings, self.__SourceName)
            retry = True
        except RequestsReadTimeout as e:
            self.__App.LogError(f"SSC Bulk API read timeout error ('{e}'), retrying in {wait} secs...")
            time.sleep(wait)
            retry = True
        
        if retry:
            rlst = self.__GetResponseDataOrError(self.__Client.Post("api/v1/bulk", { "requests": requests }))

        for rsp in rlst:
            if not 'responses' in rsp.keys() or not rsp['responses'] or len(rsp['responses']) < 1:
                continue
            if not 'body' in rsp['responses'][0].keys() or not rsp['responses'][0]['body']:
                continue
            body = rsp['responses'][0]['body']
            if not 'data' in body.keys() or not body['data']:
                continue
            lst.append(body['data'])
        return lst

    def GetRulePacks(self) -> dict:
        '''
        Return all rulepacks from SSC
        '''
        done = False
        lst = {}
        offset = 0
        url = f"api/v1/coreRulepacks?start={offset}&limit=200"
        while not done:
            rsp = self.__Get(url)
            for itm in rsp['data']:
                lst[itm['rulepackGUID']] = itm
            try:
                url = rsp['links']['next']['href']
            except KeyError:
                break
            offset += 200
        return lst


    def Get(self, url, suppressError = False):
        '''
        Allows for calling an endpoint that isn't directly setup in SscClient
        '''
        return self.__Get(url, False, suppressError)

    def GetRoles(self):
        '''
        Returns a list of SSC roles
        '''
        return self.__Get("api/v1/roles")

    def GetAuditHistory(self, id):
        '''
        Return a list of Audit History records for the given issue id
        '''
        return self.__Get(f"api/v1/issues/{id}/auditHistory")
        
    def GetUsers(self, includeRoles = False):
        '''
        Returns a list of all users in SSC (auth entities), both AD and local, optionally with roles
        '''
        if includeRoles:
            list = self.__Get("api/v1/authEntities?limit=0&embed=roles", True)
        else:
            list = self.__Get("api/v1/authEntities?limit=0", True)
        rList = {}
        for u in list:
            rList[u['entityName']] = u
        return rList

    def GetCustomTagDefinitions(self):
        '''
        Returns a list of custom tag definitions.  Currently assumes there are no more than 200 total definitions.
        '''
        return self.__Get("api/v1/customTags?limit=200", True)

    def GetProjectVersionCount(self, inactive = False):
        '''
        Returns a count of all project versions

        Parameters:
        inactive - set to True to include inactive project versions
        '''
        url = f"/api/v1/projectVersions?limit=1&fulltextsearch=false&includeInactive={str(inactive).lower()}"
        r = self.__Get(url, False, False, "GetProjectVersions")
        return r['count']

    def GetProjectVersionsGenerator(self, fields = None, inactive = False, batchSize = 200, limit = 0, startIndex = 0):
        '''
        Generates project versions, calling the API in batches.  No caching enabled.

        Parameters:
        fields - comma delimited list of fields to return, or None to return all
        inactive - set to True to include inactive project versions
        batchSize - how many project versions to load in a single API call
        limit - return no more than this number of project versions; if greater then batchSize then ignored
        '''
        if limit > 0 and limit < batchSize:
            batchSize = limit
        url = f"/api/v1/projectVersions?start={startIndex}&limit={batchSize}&fulltextsearch=false&includeInactive={str(inactive).lower()}&orderby=id"
        if fields:
            url += f"&fields={fields}"
        
        count = 0
        ccount = 0
        done = False
        while not done:
            projectVersions = self.__Get(url, False, False, "GetProjectVersions")
            ccount += len(projectVersions['data'])
            if count == 0:
                count = projectVersions['count']
                if limit > 0 or startIndex > 0:
                    count = limit
                logging.info(f"{count} total project versions, starting at index {startIndex}.")
            else:       
                logging.info(f"Downloaded {ccount} of {count} total project versions from SSC")

            for pv in projectVersions['data']:
                yield pv

            try:
                url = projectVersions['links']['next']['href']
            except KeyError:
                done = True
            except:
                logging.error('Unexpected error:{}'.format(sys.exc_info()[0]))
                done = True
            if limit <= ccount and limit > 0:
                done = True

    def GetProjectVersionByName(self, name = "", version = "", inactive = False):
        '''
        Find project version by project name and/or version

        :name: Project version name to find.
        :version: Project version/release to find.
        :inactive: Enable search to include inactive project versions.

        Must specify at least one search parameter (name or version).
        '''
        if not name and not version:
            raise ValueError("At least one search term must be specified (name or version).")
        q = ""
        ander = ""
        if name:
            q += f"project.name:{name}"
            ander = ","
        if version:
            q += f"{ander}name:{version}"
        url = f"/api/v1/projectVersions?limit=1&fulltextsearch=false&includeInactive={str(inactive).lower()}&q={q}"
        data = self.__Get(url, navToData=True, errorOnEmptyResponse=False)
        if data and len(data) > 0:
            return data[0]
        return None

    def GetProjectVersions(self, fields = None, inactive = False, batchSize = 200, forceRefresh = False, limit = 0, startIndex = 0):
        '''
        Returns complete list of project versions, calling the API in batches

        Original name: getProjectVersions

        Parameters:
        fields - comma delimited list of fields to return, or None to return all
        inactive - set to True to include inactive project versions
        batchSize - how many project versions to load in a single API call
        forceRefresh - use the API to refresh the data even if it's already been pulled and cached
        limit - return no more than this number of project versions; if greater then batchSize then ignored
        '''
        if limit > 0 and limit < batchSize:
            batchSize = limit
        url = f"/api/v1/projectVersions?start={startIndex}&limit={batchSize}&fulltextsearch=false&includeInactive={str(inactive).lower()}&orderby=id"
        if fields:
            url += f"&fields={fields}"
        orgUrl = url

        # attempt cache hit if no limit
        if limit == 0:
            cpv = self.__GetCache("GetProjectVersions")
            if cpv and cpv['url'] == orgUrl and not forceRefresh:
                return cpv['data']

        list = []
        count = 0
        ccount = 0
        done = False
        while not done:
            projectVersions = self.__Get(url, False, False, "GetProjectVersions")
            ccount += len(projectVersions['data'])
            if count == 0:
                count = projectVersions['count']
                if limit > 0 or startIndex > 0:
                    count = limit
                logging.info(f"{count} total project versions to load, starting at index {startIndex}.")
            else:       
                logging.info(f"Downloading {ccount} of {count} total records")

            for pv in projectVersions['data']:
                list.append(pv)

            try:
                url = projectVersions['links']['next']['href']
            except KeyError:
                done = True
            except:
                logging.error('Unexpected error:{}'.format(sys.exc_info()[0]))
                done = True
            if limit <= ccount and limit > 0:
                done = True

        logging.info(f"Downloaded {len(list)} project versions")
        # set cache if limit is 0 (full list)
        if limit == 0:
            self.__SetCache("GetProjectVersions", { "url": orgUrl, "data": list })
        return list

    def GetProjectVersionUsers(self, id):
        '''
        Returns a list of users that have access to the project version indicated
        '''
        return self.__Get(f"api/v1/projectVersions/{id}/authEntities", True)
        
    def GetProjectVersionIssueCounts(self, id, projDefFilter):
        '''
        Returns vulnerability counts for severities and statuses
        Original Name: getProjectVersionIssueCounts
        '''
        _issueCounts = {
            'projectVersionId': id,
            'critical': 0, 'high': 0, 'medium': 0, 'low': 0, 'count': 0,
            'hiddenCount': 0, 'suppressedCount': 0, 'removedCount': 0
        }
        
        #_url = '/api/v1/projectVersions/{}/issueGroups?groupingtype=11111111-1111-1111-1111-111111111150&filterset=a243b195-0a59-3f8b-1403-d55b7a7d78e6&filter=FOLDER:b968f72f-cc12-03b5-976e-ad4c13920c21&qm=issues&showhidden=false&showremoved=false&showshortfileNames=true&showsuppressed=false'.format(id)
        _url = '/api/v1/projectVersions/{}/issueGroups?groupingtype={}&filterset={}&qm=issues&showhidden=false&showremoved=false&showshortfileNames=true&showsuppressed=false'.format(id, self.__GroupingTypeId, projDefFilter)
        counts = self.__Get(_url, False, False, "GetProjectVersionIssueCounts")

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

        _summaryHidden = self.GetProjectVersionSummaryCounts(id, projDefFilter)

        try:
            #_issueCounts['hiddenCount'] = _summaryHidden['data'][0]['hiddenCount']
            _issueCounts['suppressedCount'] = _summaryHidden['data'][0]['suppressedCount']
            _issueCounts['removedCount'] = _summaryHidden['data'][0]['removedCount']
        except KeyError:
            _issueCounts['suppressedCount'] = 0
            _issueCounts['removedCount'] = 0
            logging.info('error getting count totals - force recalc')

        return _issueCounts

    def GetProjectVersionIssueCountsHidden(self, id):
        '''
        Returns hidden vulnerability counts for the given id

        Original Name: getProjectVersionIssueCountsHidden
        '''
        _issueCountsHidden = {
            'projectVersionId': id,
            'hiddenCount': 0
        }
        _summaryHidden = self.GetProjectVersionSummaryCounts(id)

        try:
            _issueCountsHidden['hiddenCount'] = _summaryHidden['data'][0]['hiddenCount']
        except KeyError:
            _issueCountsHidden['hiddenCount'] = 0
            logging.info('error getting count totals - force recalc')

        return _issueCountsHidden

    def IsIncompleteProjectVersion(self, id):
        '''
        Checks for incomplete setup in SSC (missing attributes, etc.)
        '''
        try:
            self.__Get(f"/api/v1/projectVersions/{id}/filterSets", statKey="IsIncompleteProjectVersion", navToData=False, retryDelaySec=5)
            return False
        except (SscClientBadRequestException, SscClient409ConflictException, SscClientAuthenticationException, SscClientServerErrorException) as e:
            if isinstance(e, SscClientServerErrorException):
                logging.warning("Server error (500) returned when checking for incomplete/invalid app version for ID %s.  This result may be hiding a system issue with SSC.")
            return True

    def GetProjectVersion(self, id, navToData = False):
        '''
        Returns a project version

        Parameters:
        id - project version id
        navToData - set to True to return response['data'] instead of just response
        '''
        return self.__Get(f'/api/v1/projectVersions/{id}', navToData, False, "GetProjectVersion")

    def GetProjectVersionLocCounts(self, id):
        '''
        Original name: getProjectVersionLOCCounts
        '''
        return self.__Get('/api/v1/projectVersions/{}/artifacts?embed=scans&start=0&limit=1000'.format(id), False, False, "GetProjectVersionLocCounts")

    def GetProjectVersionFilterset(self, pvId):
        '''
        Returns filtersets for a given project version id

        Original name: getProjectVersionFilterSet
        '''
        rsp = None
        try:
            rsp = self.__Get('/api/v1/projectVersions/{}/filterSets?&start=0&limit=200'.format(pvId), False, False, "GetProjectVersionFilterset")
        except SscClientServerErrorException:
            logging.error("500 Server error attempting to retrieve filterset for project version %s", pvId)
        return rsp

    def GetProjectVersionDefaultFilterset(self, pvId):
        '''
        Returns default filterset guid for given project version id
        '''
        list = self.GetProjectVersionFilterset(pvId)
        for filter in list['data']:
            if filter['defaultFilterSet'] == True:
                return filter['guid']

    def GetProjectVersionArtifacts(self, pvid, limit=1000):
        '''
        Returns artifacts for a given project version id, up to specified limit.  Currently ordered by uploadDate descending from API.

        :id: Project version ID
        :limit: Max artifacts to return (no paging in this method). Set 0 for no limit.
        '''
        return self.__Get(f"/api/v1/projectVersions/{pvid}/artifacts?start=0&limit={limit}", True, False)
    
    def GetProjectVersionScans(self, pvid, batchSize=1000):
        '''
        Returns scan artifacts for a given project version id

        Original name: getProjectVersionScans
        '''
        response = self.__Get(f"/api/v1/projectVersions/{pvid}/artifacts?embed=scans&start=0&limit={batchSize}", False, False, "GetProjectVersionScans")
        if not response['data']:
            return []
        data = response['data']
        done = (len(data) == 0)
        while not done:
            if 'links' in response.keys() and 'next' in response['links'].keys() and 'href' in response['links']['next'].keys():
                response = self.__Get(response['links']['next']['href'])
                if not response['data']:
                    done = True
                if not done:
                    done = True
                    for item in response['data']:
                        data.append(item)
                        done = False
            else:
                done = True
        return data

    def GetProjectVersionAttributeValues(self, id, attrId):
        '''
        Returns value(s) for given project version id and attribute id
        '''
        r = self.__Get('/api/v1/projectVersions/{}/attributes/{}'.format(id, attrId), False, False, "GetProjectVersionAttributeValues")
        v = []
        if r['data']['value']:
            v.append(r['data']['value'])
        else:
            for x in r['data']['values']:
                v.append(x['name'])
        return v

    def GetProjectVersionAttributes(self, id):
        '''
        Returns attributes for given project version id

        Original name: getProjectVersionAttributes
        '''
        return self.__Get('/api/v1/projectVersions/{}/attributes'.format(id), False, False, "GetProjectVersionAttributes")

    def GetProjectVersionAttributeDefinitions(self, limit=200):
        '''
        Returns all attribute definitions

        Original name: getProjectVersionAttributeDefinitions
        '''
        return self.__Get('/api/v1/attributeDefinitions?start=0&limit={}'.format(limit), False)

    def GetCloudJobs(self):
        '''
        Returns list of jobs from ScanCentral or Cloudscan controller

        Original name: getCloudscanJob
        '''
        return self.__Get('/api/v1/cloudjobs?start=-1&limit=-1', False)

    def GetProjectVersionSummaryCounts(self, id, projDefFilter):
        '''
        Get summary counts for given project version id

        Original name: getProjectVersionSummaryCounts
        '''
        #_url = '/api/v1/projectVersions/{}/issueStatistics?filterset={}'.format(id, self.filtersetId)
        return self.__Get('/api/v1/projectVersions/{}/issueStatistics?filterset={}'.format(id, projDefFilter), False, False, "GetProjectVersionSummaryCounts")

    def GetProjectVersionIssueDetail(self, issueid):
        '''
        Get issue details for given issue id from the issue details endpoint
        '''
        url = '/api/v1/issueDetails/{}'.format(issueid)
        try:
            return self.__Get(url, False, "IssueDetails", "GetProjectVersionIssueDetail")
            
        except SscClientAuthenticationException as e:
            self.__App.LogWarning("Authentication token invalid, attempting to get new token and retry previous operation")
            self.__GetAuthToken(self.__App.Settings, self.__SourceName)
            return self.__Get(url, False, "IssueDetails", "GetProjectVersionIssueDetail")

        except (SscClientServerErrorException, SscClientEmptyResponseException, ConnectionError, RequestsConnectionError) as e:
            wait = self.__RetrySec
            self.__App.LogError(f"SSC API error - [{type(e).__name__}] {e}, retrying in {wait} secs...")
            time.sleep(wait)
            return self.__Get(url, False, "IssueDetails", "GetProjectVersionIssueDetail")

        except Exception as e:
            print(f"__Get failed to handle exception of type {type(e).__name__}")
            raise

    def GetProjectVersionIssue(self, projid, issueid):
        '''
        Get single issue for given project version id and issue id

        Original name: getProjectVersionIssueDetail
        '''
        return self.__Get('/api/v1/projectVersions/{}/issues/{}'.format(projid, issueid), False, False, "GetProjectVersionIssue")

    def GetProjectVersionIssueTagCounts(self, id, projDefFilter):
        '''
        Get issue tag counts for given project version id and filterset.  NOTE: currently hardcoded for default grouping type ID

        Original name: getProjectVersionIssueTagCounts
        '''
        grp = '87f2364f-dcd4-49e6-861d-f8d3f351686b'
        url = f"/api/v1/projectVersions/{id}/issueGroups?filterset={projDefFilter}&start=0&limit=200&groupingtype={grp}&qm=issues&showhidden=false&showremoved=true&showshortfileNames=true&showsuppressed=true"
        return self.__Get(url, False)

    def GetProjectVersionIssuesV2(self, id, projDefFilter, fields = None, batchSize = 1000, showHidden = True, showRemoved = True, showSuppressed = True, showShortFilenames = True, restartScroll = False):
        '''
        Returns all issues for given project version id in batches

        Original name: getProjectVersionIssues

        This method remembers the URL called and how many documents have been returned so far for that URL.  
        It can be called repetitively to continue to get more data until there is a None response, signalling the end of available data and the reset of the URL (next call will start over).

        Parameters:
        id - project version id
        projDefFilter - filterset id (guid)
        fields - array of fields to return (returns all if not included)
        batchSize - how many issues to pull in a single API call

        Returns: a dict with the 'data' and a 'count' of the total results from SSC
        '''
        _key = 'GetProjectVersionIssues'
        _issues = {'data': [], 'count': 0}
        _curCount = 0

        # Return single batch of issues
        if not _key in self.__BatchUrls.keys() or restartScroll:
            self.__BatchUrls[_key] = { 'Url': '', 'CurCount': 0 }
        if self.__BatchUrls[_key]['Url']:
            _curCount = self.__BatchUrls[_key]['CurCount']
        if self.__BatchUrls[_key]['Url'] == 'Done':
            self.__BatchUrls[_key] = { 'Url': '', 'CurCount': 0 }
            return None
            
        if fields:
            _url = f"/api/v1/projectVersions/{id}/issues?filterset={projDefFilter}&fields={fields}&start={_curCount}&limit={batchSize}&showhidden={str(showHidden).lower()}&showremoved={str(showRemoved).lower()}&showsuppressed={str(showSuppressed).lower()}&showshortfilenames={str(showShortFilenames).lower()}"
        else:
            _url = f"/api/v1/projectVersions/{id}/issues?filterset={projDefFilter}&start={_curCount}&limit={batchSize}&showhidden={str(showHidden).lower()}&showremoved={str(showRemoved).lower()}&showsuppressed={str(showSuppressed).lower()}&showshortfilenames={str(showShortFilenames).lower()}"
        response = self.__Get(_url, False, False, _key)
        _issues['data'] = response['data']
        _issues['count'] = response['count']
        beforeCount = _curCount + 1
        _curCount += len(response['data'])
        self.__BatchUrls[_key]['CurCount'] = _curCount
        logging.info(f"Downloading issues for id {id}: {beforeCount}-{_curCount} of {_issues['count']} total records")
        try:
            self.__BatchUrls[_key]['Url'] = response['links']['next']['href']
        except KeyError:
            self.__BatchUrls[_key]['Url'] = "Done"
        except Exception as e:
            self.__BatchUrls[_key]['Url'] = "Done"
            logging.error(f"Failed to get next batch link for issue download: {e}")
        return _issues

    def GetProjectVersionIssues(self, id, projDefFilter, batchSize = 1000, showHidden = True, showRemoved = True, showSuppressed = True, showShortFilenames = True):
        '''
        Returns all issues for given project version id in batches

        Original name: getProjectVersionIssues

        This method remembers the URL called and how many documents have been returned so far for that URL.  
        It can be called repetitively to continue to get more data until there is a None response, signalling the end of available data and the reset of the URL (next call will start over).

        Parameters:
        id - project version id
        projDefFilter - filterset id (guid)
        batchSize - how many issues to pull in a single API call

        Returns: a dict with the 'data' and a 'count' of the total results from SSC
        '''
        return self.GetProjectVersionIssuesV2(id, projDefFilter, None, batchSize, showHidden, showRemoved, showSuppressed, showShortFilenames)
        
    def getProjectVersionsUsers(self, projectVersionId):
        '''
        Deprecated - this will be removed soonish.  Use GetProjectVersionUsers instead
        NOTE: GetProjectVersionUsers navigates to the data portion of the API response directly
        '''
        msg = "getProjectVersionsUsers is deprecated and will be removed soonish, please use GetProjectVersionUsers instead."
        logging.warning(msg)
        print(msg)
        return self.__Get(f"/api/v1/projectVersions/{projectVersionId}/authEntities", False)
    
    def PutProjectVersionTemplate(self, projectVersionId, data= None):
        
        
        try:
            self.__Client.Put(url=f'/api/v1/projectVersions/{projectVersionId}', json=data)
            
        except SscClientAuthenticationException as e:
            self.__App.LogWarning("Authentication token invalid, attempting to get new token and retry previous operation")
            self.__GetAuthToken(self.__App.Settings, self.__SourceName)
            self.__Client.Put(url=f'/api/v1/projectVersions/{projectVersionId}', data=data)

    def UploadFile(self, projectVersionId, engineType = None, filePath = None, fileName = "issues.zip"):
        '''
        Upload an FPR into SSC.

        :projectVersionId: project version ID, required
        :engineType: Engine type of the FPR (can be omitted if embedded in FPR)
        :filePath: File path to the upload file (FPR), required even if it seems like it isn't
        :fileName: File name to send, defaults to issues.zip probably because this method was originally single-purpose...
        '''

        if len(str(filePath).strip()) == 0:
            raise ValueError("filePath is required.")
        file_token = self.GetFileToken("UPLOAD")
        et = "?engineType=" + str(engineType) if len(str(engineType).strip()) > 0 else ""
        try:
            url = "/api/v1/projectVersions/" + str(projectVersionId) + "/artifacts" + et

            headers = {
                'Authorization': f'FortifyToken {file_token}',
                'Accept-Encoding': 'gzip, deflate, br, zstd',
                'Connection': 'keep-alive'
            }

            with open(filePath, 'rb') as file:
                return self.__Client.Post(url, headers=headers, files={"file": (fileName, file)})
            
        except SscClientAuthenticationException as e:
            self.__App.LogWarning(f"Authentication token invalid {e}, attempting to get new token and retry previous operation")
            self.__GetAuthToken(self.__App.Settings, self.__SourceName)
            return self.__Client.Post(url, headers=headers, files={"file": (fileName, file)})
    
    def CreateIssuesDeltaExport(self, pvidList, fileName, sinceDate):
        try:
           data = {
            "deltaSinceDate": sinceDate,
            "expiration": 0,
            "fileName": fileName,
            "projectVersionIds": pvidList,
            "userName": "currentUser"
           }

           return self.__GetResponseDataOrError(self.__Client.Post(url="/api/v1/issuesDeltaExports", data=json.dumps(data)))
        except SscClientAuthenticationException as e:
            self.__App.LogWarning("Authentication token invalid, attempting to get new token and retry previous operation")
            self.__GetAuthToken(self.__App.Settings, self.__SourceName)
            return self.__GetResponseDataOrError(self.__Client.Post(url="/api/v1/issuesDeltaExports", data=json.dumps(data)))
        except Exception as e:
            logging.error(f"Failed to create issues delta export: {e}")

    
    def GetIssuesDeltaExport(self, exportId):
        try:
           return self.__GetResponseDataOrError(self.__Client.Get(url=f"/api/v1/issuesDeltaExports/{exportId}"))
           
        except SscClientAuthenticationException as e:
            self.__App.LogWarning("Authentication token invalid, attempting to get new token and retry previous operation")
            self.__GetAuthToken(self.__App.Settings, self.__SourceName)
            return self.__GetResponseDataOrError(self.__Client.Get(url=f"/api/v1/issuesDeltaExports/{exportId}"))
        except Exception as e:
            logging.error(f"Failed to get issues delta export download: {e}")


    def GetIssuesDeltaExportDownload(self, exportId):
        file_token = self.GetFileToken("REPORT_FILE")

        try:
           return self.__Client.Get(url="/transfer/issuesDeltaExportDownload.html?mat=" + file_token + "&id=" + str(exportId))
        except SscClientAuthenticationException as e:
            self.__App.LogWarning("Authentication token invalid, attempting to get new token and retry previous operation")
            self.__GetAuthToken(self.__App.Settings, self.__SourceName)
            return self.__Client.Get(url="/transfer/issuesDeltaExportDownload.html?mat=" + file_token + "&id=" + str(exportId))
        except Exception as e:
            logging.error(f"Failed to get issues delta export download: {e}")

    def GetFileToken(self, fileTokenType):
        data = ({
                "fileTokenType": fileTokenType
            })
        
        token = self.__Client.Post(url="/api/v1/fileTokens", json=data)
        tokenObj = json.loads(token.text)
        return tokenObj['data']['token']

    def GetSscJobs(self):
        '''
        Return the Job Queue
        GET "/api/v1/jobs?start=0&limit=200&q=state!%3DFINISHED" -H "accept: application/json"
        '''
        r = self.__Client.Get(f"/api/v1/jobs?start=0&limit=200&q=state!%3DFINISHED")
        return r

    # getLdapObjects was removed, as it is not used anywhere and is probably untested
    # getLocalUsers was removed, as it is not used anywhere and is probably untested

class SscClientException(Exception):
    pass
class SscClientConfigurationException(SscClientException):
    pass
class SscClientAuthenticationException(SscClientException):
    pass
class SscClientServerErrorException(SscClientException):
    pass
class SscClientBadRequestException(SscClientException):
    pass
class SscClientEmptyResponseException(SscClientException):
    pass
class SscClient409ConflictException(SscClientException):
    pass