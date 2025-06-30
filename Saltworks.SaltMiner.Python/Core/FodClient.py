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

from urllib3.exceptions import ReadTimeoutError
import urllib3
import urllib.parse
import requests

class FodClient(object):

    def __init__(self, appSettings, sourceName):
        '''
        Initializes the class.

        inSettings: Settings instance containing application settings
        '''
        if type(appSettings).__name__ != "ApplicationSettings":
            raise TypeError("Type of appSettings must be 'ApplicationSettings'")
        if not sourceName or not sourceName in appSettings.GetSourceNames():
            raise FodClientConfigurationException(f"Invalid or missing source configuration for source name '{sourceName}'")
        sourceType = appSettings.GetSource(sourceName, "Source", "")
        if not sourceType == "FOD":
            raise FodClientConfigurationException(f"Invalid source type '{sourceType}', should be 'FOD'. (in source config '{sourceName}', property 'Source')")
        
        self.__App = appSettings.Application
        self.__SourceName = sourceName
        self.__VerifySsl = (appSettings.GetSource(sourceName, 'SslVerify', True))
        self.__MaxRetries = appSettings.GetSource(sourceName, 'ServerErrorMaxRetries', 3)
        self.__RetrySec = appSettings.GetSource(sourceName, 'ServerErrorRetrySeconds', 300)
        self.__DefaultTimeout = appSettings.GetSource(sourceName, 'RequestDefaultTimeoutSeconds', 300)
        self.__ApiMaxLimit = appSettings.GetSource(sourceName, 'MaxResultsLimit', 50)
        proxy = appSettings.GetSource(sourceName, 'Proxy', '')
        if proxy and len(proxy) > 0:
            self.__ProxyDict = { "https": proxy, "http": proxy }
            logging.info("Using proxy %s", proxy)
        else:
            self.__ProxyDict = None
        self.__BatchSize = appSettings.GetSource(sourceName, 'BatchSize', 50)
        if (not self.__BatchSize or self.__BatchSize < 50 or self.__BatchSize > 500):
            self.__BatchSize = 50 # default if unreasonable
        self.__BaseAddress = appSettings.GetSource(sourceName, 'BaseUrl')
        clientId = appSettings.GetSource(sourceName, 'ClientId')
        clientSecret = appSettings.GetSource(sourceName, 'ClientSecret')
        if self.__VerifySsl == "False":
            urllib3.disable_warnings(urllib3.exceptions.InsecureRequestWarning)
            logging.warning("SSL verification has been disabled in config.  This is insecure and should be enabled in production systems.")

        # body for auth request
        body = urllib.parse.urlencode({
            'grant_type': 'client_credentials',
            'scope': 'api-tenant',
            'client_id': clientId,
            'client_secret': clientSecret
        })

        # headers for auth request
        headers = {
            'Accept': 'application/json',
            'Content-Type': 'application/x-www-form-urlencoded; charset=UTF-8'
        }
        response = self.__Post('/oauth/token', data=body, headers=headers, timeout=5)
        auth = response.Content if response.Content else {}

        try:
            self.__Token = auth["access_token"]
        except (KeyError, AttributeError):
            logging.error("FodClient initialization failure (auth): (%s) %s", response.Status, response.Reason)
            raise FodClientAuthenticationException(f"FodClient initialization failure (auth): ({response.Status}) {response.Reason}")

        # default headers used for all data requests
        self.__DefaultHeaders = {
            'Authorization': f"Bearer {self.__Token}",
            'Accept': 'application/json'
        }
        logging.info(f"FodClient initialized. BaseAddress: '%s', ClientId: '%s', Proxy? %s", self.__BaseAddress, clientId, len(proxy) > 0)

    @property
    def ApiMaxLimit(self):
        return self.__ApiMaxLimit

    #region Requests Mini-Client
    # ***********************************************************************************************************
    # Requests mini-client
    # ***********************************************************************************************************
    def __Request(self, method, url, json=None, data=None, headers=None, verify=None, timeout=None):
        if self.__BaseAddress is not None and not url.startswith("http:") and not url.startswith("https:"):
            _url = self.__BaseAddress + ("/" if not url.startswith("/") else "") + url
        else:
            _url = url
        if not verify:
            verify = self.__VerifySsl
        if not verify:
            verify = True
        if str(verify).lower() == "false":
            verify = False
        if headers is None:
            headers = self.__DefaultHeaders
        retryCount = 0
        if not timeout:
            timeout = self.__DefaultTimeout
        ok = False

        while not ok and retryCount < self.__MaxRetries:
            try:
                if (self.__ProxyDict):
                    resp = requests.request(method, _url, json=json, data=data, headers=headers, timeout=timeout, verify=verify, proxies=self.__ProxyDict)
                else:
                    resp = requests.request(method, _url, json=json, data=data, headers=headers, timeout=timeout, verify=verify)
                ok = True

            except (ConnectionError, ReadTimeoutError) as e:
                if retryCount >= self.__MaxRetries:
                    logging.error("Max retry count reached, api call '%s' failed.", url)
                    retryCount = None
                    raise
                else:
                    retryCount += 1
                    logging.warning("Server error attempting api call ('%s'), attempt %s/%s, will retry after %s sec delay...", e.__str__(), retryCount, self.__MaxRetries + 1, self.__RetrySec)
                    time.sleep(self.__RetrySec)

        retryCount = None
        msg = f"FodClient {method} called. Url: '{_url}', Headers: {headers}.  Response: ({resp.status_code}) {resp.reason}"
        logging.debug(msg)
        return FodClientResponse(resp)

    def Get(self, url, json=None, headers=None):
        ''' 
        Returns API GET result, adding default headers (incl. auth) automatically if headers param is None

        url: URL endpoint to use.  Don't include base URL unless "BaseUrl" isn't present in source config
        json: json body content.
        headers: optional override headers.  Don't use this unless you know what you're doing (and include auth)
        '''
        return self.__Request("get", url, json, headers)

    def __GetStream(self, url, headers=None):
        '''
        Calls an API GET with a stream result (application/octet-stream).  Handles headers (incl. auth) if headers param is None

        url: URL endpoint to use.  Don't include base URL unless "BaseUrl" isn't present in source config
        headers: optional override headers.  Don't use this unless you know what you're doing (and include auth)
        '''
        headers = {
            'Authorization': 'Bearer {}'.format(self.__Token),
            'Accept': 'application/octet-stream'
        }
        return requests.get(url, headers=headers, stream=True, timeout=240, proxies=self.__ProxyDict)
        
    def __Post(self, url, json=None, data=None, headers=None, timeout=None):
        ''' 
        Returns FOD API POST result, adding default headers (incl. auth) automatically if headers param is None

        url: URL endpoint to use.  Don't include base URL unless "BaseUrl" isn't present in source config
        json: json body content.
        data: don't use unless you know what you're doing.
        headers: optional override headers.  Don't use this unless you know what you're doing (and include auth)
        timeout: optional timeout (in sec).  Uses default from config if not passed
        '''
        return self.__Request("post", url, json, data, headers, timeout=timeout)

    def __Put(self, url, json=None, headers=None):
        ''' 
        Returns FOD API PUT result, adding default headers (incl. auth) automatically if headers param is None

        url: URL endpoint to use.  Don't include base URL unless "BaseUrl" isn't present in source config
        json: json body content.
        headers: optional override headers.  Don't use this unless you know what you're doing (and include auth)
        '''
        return self.__Request("put", url, json, headers)

    def __Delete(self, url, json=None, headers=None):
        ''' 
        Returns FOD API DELETE result, adding default headers (incl. auth) automatically if headers param is None

        url: URL endpoint to use.  Don't include base URL unless "BaseUrl" isn't present in source config
        json: json body content.
        headers: optional override headers.  Don't use this unless you know what you're doing (and include auth)
        '''
        return self.__Request("delete", url, json, headers)

    #endregion

    #region Helpers
    # ***********************************************************************************************************
    # Shortcut/Helper Methods
    # ***********************************************************************************************************
    def __GetPaged(self, url, limit=None, offset=None, headers=None, logPrefix=None):
        '''
        Get list of records by "paging" (offset/limit)

        url: URL to use excluding the paging parameters
        limit: Max record count to return. Set to 0 for all
        offset: Starting offset.  Defaults to 0 (first result)
        headers: Optional override headers (must include auth)
        logPrefix: If present, will cause an info severity log message with the prefix and a progress suffix
        '''
        batchSize = self.__BatchSize
        returnResponse = None
        response = None
        try:
            if "?" in url:
                op = "&"
            else:
                op = "?"
            if not offset:
                offset = 0
            if not limit:
                limit = 0
            myurl = f"{url}{op}offset={offset}&limit={batchSize}"
            if (batchSize > limit and limit > 0):
                batchSize = limit
            returnResponse = self.Get(myurl, headers=headers)
            dto = returnResponse.Content
            if not dto or not isinstance(dto, dict):
                returnResponse.Content = { "items": [] }
                return returnResponse
            else:
                if 'items' not in returnResponse.Content.keys():
                    returnResponse.Content['items'] = []
            returnContent = returnResponse.Content
            total = dto['totalCount'] if 'totalCount' in dto.keys() else 0
            logging.debug(f"GetPaged found %s total records for url %s", total, url)
            offset += len(dto['items'])
            while (offset < total and (limit == 0 or offset < limit)):
                if logPrefix:
                    logging.info('%s: retrieved %s of %s documents', logPrefix, len(returnContent['items']), total)
                if (offset + batchSize > limit and limit != 0):
                    batchSize = limit - offset
                myurl = f"{url}{op}offset={offset}&limit={batchSize}"
                response = self.Get(myurl, headers=headers)
                dto = response.Content
                returnContent['items'].extend(dto['items'])
                offset += len(dto['items'])
            if logPrefix:
                logging.info('%s: retrieved %s of %s documents', logPrefix, len(returnContent['items']), total)
            return returnResponse
        except Exception as ex:
            if not response and returnResponse:
                response = returnResponse
            if not response:
                response = FodClientResponse()
            response.Reason = 'Error'
            response.Status = response.Status if response else 500
            response.Text = f"Error getting multi-call data.  Last response: {response.Text if response.Text else '[not available]'}"
            logging.error("Get_Paged: %s", ex, exc_info=ex)
            return response

    def ManageError(self, postData, response):

        if response.status_code == 429:
            timeToPause = int(response.headers['X-Rate-Limit-Reset']) + 2
            logging.info("Rate limit hit, pausing: {}".format(timeToPause))
            time.sleep(timeToPause)

        elif response.status_code == 500:
            logging.info("Error 500 returned, pausing for 30 seconds for system reset.")
            logging.info(response)
            time.sleep((30))

        elif response.status_code == 400:
            # Bad Request
            logging.info("Error 400, bad request.")
            logging.info(postData)
            sys.exit()

        else:
            logging.info("Unknown state, exiting")
            logging.info(response)
            sys.exit()

    #endregion

    #region Methods
    # ***********************************************************************************************************
    # Data Methods
    # ***********************************************************************************************************

    @staticmethod
    def TestConnection(appSettings, sourceName):
        '''
        Connects to FOD
        '''
        try:
            FodClient(appSettings, sourceName)
            return True,""
        except Exception as e:
            return False, f"[{type(e).__name__}] {e}"

    def GetReleases(self, limit=None, offset=None, fields=None, scroller=False):
        '''
        Return list of all releases
        original name: getAllreleases

        :limit: max records to return (0 for all, defaults to 0 if missing)
        :offset: 0 means first record.
        :fields: comma-delimited list of fields to return (as string, not array)
        :scroller: return an FodScroller instead of pulling all results at once (limit is ignored)
        '''
        url = 'api/v3/releases?orderBy=applicationId'
        if fields:
            url += f'&fields={fields}'
        if scroller:
            return FodScroller(self, url, offset, logPrefix="FOD Releases")
        return self.__GetPaged(url, limit, offset, logPrefix="Downloading releases")

    def GetReleaseCount(self):
        '''
        Returns count of all releases
        '''
        return self.GetReleases(fields="releaseId", scroller=True).TotalHits
    
    def GetRelease(self, releaseId):
        '''
        Return specified release
        '''
        return self.Get(f'api/v3/releases/{releaseId}')

    def GetVulnerabilities(self, releaseId, includeFixed=False, includeSuppressed=False, limit=None, offset=None, scroller=False, logPrefix=None):
        '''
        Return list of vulnerabilities for a given release Id
        original name: getFODVulnerability

        :releaseId: release Id for which to return vulnerabilities
        :limit: max records to return (0 for all, defaults to 0 if missing)
        :offset: 0 means first record.
        :scroller: return an FodScroller instead of pulling all results at once (limit is ignored)
        :logPrefix: if present, adds a progress log message (info) prefixed with the passed value
        '''
        logging.debug(f"GetVulnerabilities: ReleaseId {releaseId}")
        url = f"api/v3/releases/{releaseId}/vulnerabilities?includeFixed={'true' if includeFixed else 'false'}&includeSuppressed={'true' if includeSuppressed else 'false'}"
        if scroller:
            return FodScroller(self, url, offset, logPrefix=logPrefix)
        return self.__GetPaged(url, limit, offset, logPrefix=logPrefix)
 
    def LoadVulnerabilitiesIntoElastic(self, releaseId, elasticUtility):
        ''' 
        Inserts vulnerabilities for given releaseId into elastic
        original name: getAndLoadFODVulnerability
        TODO: move this method outside of this class (single responsiblity principal)

        :releaseId: release Id for which to retrieve vulnerabilities
        :elasticUtility: elastic client class instance
        '''
        vuls = self.GetVulnerabilities(releaseId, True, True)

        for vulrel in vuls['items']:
            #logging.info(vulrel)
            elasticUtility.postFODRelIssues(vulrel)
        return True

    def GetSummaryCounts(self, releaseId):
        '''
        Get summary counts for a given Release Id
        original name: getFODSummaryCounts

        :releaseId: release Id for which to pull summary counts
        '''

        logging.debug(f'GetSummaryCounts: release id {releaseId}')
        vulsum = None
        rsp = self.GetVulnerabilities(releaseId, includeFixed=True, includeSuppressed=True, limit=1)
        if rsp and rsp.Content:
            vulsum = rsp.Content

        summary = {
            'releaseId': releaseId,
            'FixedIssue': 0,
            'SuppressedIssues': 0
        }

        found = False
        if vulsum and 'filters' in vulsum.keys():
            for filter in vulsum['filters']:
                #logging.debug(f'Filter: {filter}')
                if filter['fieldName'] == 'isSuppressed':
                        found = True
                        for value in filter['fieldFilterValues']:
                            #logging.info(f'fieldName = isSupressed, value = {value}')
                            if value['value'] == 'true':
                                summary['SuppressedIssues'] = value['count']

                if filter['fieldName'] == 'status':
                        found = True
                        for value in filter['fieldFilterValues']:
                            #logging.info(f'fieldName = status, value = {value}')
                            if value['value'] == 'Fix Validated':
                                summary['FixedIssue'] = value['count']
        else:
            logging.debug("Empty response attempting to retrieve summary counts for release ID %s", releaseId)

        if not found:
            logging.debug("Unable to find summary fields in response for release ID %s", releaseId)
        
        return summary


    def GetScans(self, releaseId, limit=None, offset=None, scroller=False):
        '''
        Returns a list of scans for a given release ID
        original name: getAllFODScans

        :releaseId: release ID for which to return scans
        :limit: max records to return (0 for all, defaults to 0 if missing)
        :offset: 0 means first record.
        :scroller: return an FodScroller instead of pulling all results at once (limit is ignored)
        '''
        url = f"api/v3/releases/{releaseId}/scans"
        if scroller:
            return FodScroller(self, url, offset, logPrefix=f"FOD Scans for release {releaseId}")
        return self.__GetPaged(url, limit, offset, logPrefix=f"FOD Scans for release {releaseId}")

    def GetScanSummary(self, scanId):
        '''
        Returns scan summary for a given scan ID
        original name: getFODScanSummary 

        :scanId: scan ID for which to return summary
        '''
        return self.Get(f'api/v3/scans/{scanId}/summary')

    def GetApplications(self, appName=None, appTypeId=None, appType=None, orderBy=None, orderByDirection=None, fields=None, offset=None, limit=None, scroller=False):
        ''' 
        Gets FOD User(s) based on passed criteria, returned as an FodScroller (call .GetAll() to return all results immediately).
        Returns all users if no parameters are included.

        :orderBy: field name by which to order the results (only one allowed according to docs)
        :orderByDirection: ASC or DESC
        :fields: comma separated list of fields to return
        :limit: max records to return (0 for all) - if over MaxResultsLimit setting (50 by default) then multiple calls will be made
        :offset: starting record - 0 is the first one
        :scroller: return an FodScroller instead of pulling all results at once (limit is ignored)
        '''
        ANDER = '+'
        AMPER = '&'
        url = '/api/v3/users'
        qner = '?'
        ander = ''
        amper = ''
        filters = ''

        if appName:
            filters += f'applicationName:{appName}'
            ander = ANDER
            qner = ''
        if appTypeId:
            filters += f'{ander}applicationTypeId:{appTypeId}'
            ander = ANDER
            qner = ''
        if appType:
            filters += f'{ander}applicationType:{appType}'
            ander = ANDER
            qner = ''
        if filters:
            url += f'{qner}filters={filters}'
            amper = AMPER
            qner = ''
        if orderBy:
            url += f'{qner}{amper}orderBy={orderBy}'
            amper = AMPER
            qner = ''
        if orderByDirection:
            url += f'{qner}{amper}orderByDirection={orderByDirection}'
            amper = AMPER
            qner = ''
        if fields:
            url += f'{qner}fields={fields}'
            amper = AMPER
            qner = ''
        logging.debug("GetUsers called")
        if scroller:
            return FodScroller(self, url, offset)
        else:
            return FodScroller(self, url, offset).GetAll(limit)

    def GetApplication(self, applicationId):
        '''
        Returns Application for a given application ID
        original name: getFODApplicationbyApplicationId

        :applicationId: application ID for which to return an application
        '''
        return self.Get(f"api/v3/applications/{applicationId}")

    def GetApplicationUserAccess(self, applicationId):
        '''
        Returns users who may access given application.  Includes users with universal access.

        :applicationId: application ID for which to return user access results
        '''
        return self.Get(f"api/v3/applications/{applicationId}/users")

    def DownloadFpr(self, releaseId, fileName, fullFileName):
        '''
        Downloads FPR for a given release ID and stores it to a file

        :releaseId: release ID for which to download an FPR
        :fileName: um, mostly unused parameter (it does show up in returned result object but that seems to be it)
        :fullFileName: local full file path to write contents of the response
        '''
        # _dlFileName = "{}-{}.fpr".format(SSCPVID, datetime.today().strftime('%Y.%m.%d'))
        logging.warning("This method is untested and may not work.  It may also have a problem in that it currently expects a Dynamic type FPR.")

        dlResult = {
            'status': "OK",
            'downloadFileName': fileName,
            'downloadFullFileName': fullFileName,
            'error': ''
        }

        response = self.__GetStream(f'api/v3/releases/{releaseId}/fpr?scanType=Dynamic')
        # print("Status: {}".format(response.status_code))
        if response.status_code != 200:
            dlResult = {
                'status': response.status_code,
                'downloadFileName': fileName,
                'downloadFullFileName': fullFileName,
                'error': 'Error with Download {}'.format(response.status_code)
            }
            return dlResult

        logging.info('Writing: {}'.format(dlResult['downloadFullFileName']))
        handle = open(dlResult['downloadFullFileName'], "wb")
        for chunk in response.iter_content(chunk_size=512):
            if chunk:  # filter out keep-alive new chunks
                handle.write(chunk)

        return dlResult
   
    def GetUsers(self, userId=None, email=None, userName=None, firstName=None, lastName=None, roleId=None, roleName=None, isSuspended=None, orderBy=None, orderByDirection=None, fields=None, limit=None, offset=None, scroller=False):
        ''' 
        Gets FOD User(s) based on passed criteria, returned as an FodScroller (call .GetAll() to return all results immediately).
        Returns all users if no parameters are included.

        :orderBy: field name by which to order the results (only one allowed according to docs)
        :orderByDirection: ASC or DESC
        :fields: comma separated list of fields to return
        :limit: max records to return (0 for all) - if over MaxResultsLimit setting (50 by default) then multiple calls will be made
        :offset: starting record - 0 is the first one
        :scroller: return an FodScroller instead of pulling all results at once (limit is ignored)
        '''
        ANDER = '+'
        AMPER = '&'
        url = '/api/v3/users'
        qner = '?'
        ander = ''
        amper = ''
        filters = ''

        if userId:
            filters += f'userId:{userId}'
            ander = ANDER
            qner = ''
        if email:
            filters += f'{ander}email:{email}'
            ander = ANDER
            qner = ''
        if userName:
            filters += f'{ander}userName:{userName}'
            ander = ANDER
            qner = ''
        if firstName:
            filters += f'{ander}firstName:{firstName}'
            ander = ANDER
            qner = ''
        if lastName:
            filters += f'{ander}lastName:{lastName}'
            ander = ANDER
            qner = ''
        if roleId:
            filters += f'{ander}roleId:{roleId}'
            ander = ANDER
            qner = ''
        if roleName:
            filters += f'{ander}roleName:{roleName}'
            ander = ANDER
            qner = ''
        if isSuspended:
            filters += f'{ander}isSuspended:{"true" if isSuspended else "false"}'
            ander = ANDER
            qner = ''
        if filters:
            url += f'{qner}filters={filters}'
            amper = AMPER
            qner = ''
        if orderBy:
            url += f'{qner}{amper}orderBy={orderBy}'
            amper = AMPER
            qner = ''
        if orderByDirection:
            url += f'{qner}{amper}orderByDirection={orderByDirection}'
            amper = AMPER
            qner = ''
        if fields:
            url += f'{qner}fields={fields}'
            amper = AMPER
            qner = ''
        logging.debug("GetUsers called")
        if scroller:
            return FodScroller(self, url, offset, limit)
        else:
            return FodScroller(self, url, offset).GetAll(limit)

    def AddUser(self, json=None):
        ''' 
        Add an FOD user based on data in a json object.

        :json: should look like this:
            'userName': string,
            'email': string,
            'firstName': string,
            'lastName' : string,
            'phoneNumber' : string,
            'roleId' : int,
            'passwordNeverExpires' : boolean,
            'isSuspended' : boolean
        '''
        url = '/api/v3/users'
        logging.debug("AddUser called")
        return self.__Post(url,json)

    def DeleteUser(self, userId):
        ''' 
        Delete an FOD User based on a User ID.

        :userId: id of user to delete
        '''
        url = '/api/v3/users' + str(userId)
        logging.debug("Deleting user with id %s", userId)
        return self.__Delete(url,json)

    def UpdateUser(self, userId, json=None):
        ''' 
        Update a FOD User called by their userid based on data in a json object.

        :userId: id of user to update
        :json: should look like this
            'userName': string,
            'email': string,
            'firstName': string,
            'lastName' : string,
            'phoneNumber' : string,
            'roleId' : int,
            'passwordNeverExpires' : boolean,
            'isSuspended' : boolean
        '''
        url = '/api/v3/users/' + str(userId)
        logging.debug("Updating user with id %s", userId)
        return self.__Put(url,json)

    def GetGroups(self):
        ''' 
        Returns a list of all FOD Groups
        '''
        url = '/api/v3/user-management/user-groups'
        return self.Get(url)

    def GetRoles(self):
        ''' 
        Returns a list of all FOD Roles
        '''
        return self.GetLookups('Roles')

    def GetLookups(self, lookupType):
        ''' 
        Returns a list of FOD Lookup Items by type

        :lookupType: type of lookup items to retrieve
        '''
        url = '/api/v3/lookup-items'
        if lookupType != None:
            url += '?type=' + lookupType
        return self.Get(url)

    def GetUserApplicationAccess(self, userId):
        '''
        Returns a list of applications (including applicationId, applicationName) authorized for the passed user ID.
        
        :userId: ID of user for which to return application access results
        '''
        url = f"/api/v3/user-application-access/{userId}"
        return self.Get(url)

#endregion

class FodScroller(object):
    def __init__(self, client, url, offset=None, batchSize=None, logPrefix=None):
        '''
        Initialize a new FodScroller

        :client: FodClient to use to make calls
        :url: URL (not including limit/offset) to GET
        :offset: Starts at 0 for the first document
        :batchSize: How many docs to pull in a batch
        :logPrefix: Log message prefix
        '''
        if type(client).__name__ != "FodClient":
            raise TypeError("Type of client must be 'FodClient'")
        self.__Url = url
        self.__Offset = offset if offset else 0
        self.__Limit = batchSize if batchSize else client.ApiMaxLimit
        if self.__Limit > client.ApiMaxLimit:
            self.__Limit = client.ApiMaxLimit
        self.__Client = client
        self.__TotalHits = None
        self.__TotalDownloaded = 0
        self.__LogPrefix = logPrefix if logPrefix else "FodClient"
        self.__Results = None
        self.GetNext()

    @property
    def TotalHits(self):
        return self.__TotalHits

    @property
    def Results(self):
        return self.__Results

    def GetNext(self):
        '''
        Returns next results
        '''
        if not self.__Url:
            return None
        url = self.__Url
        url += ('&' if '?' in url else '?') + f'limit={self.__Limit}&offset={self.__Offset}'
        r = self.__Client.Get(url)
        if r and r.Content and 'items' in r.Content.keys():
            rsp = r.Content['items']
            if 'totalCount' in r.Content.keys():
                self.__TotalHits = r.Content['totalCount']
        else:
            self.Clear()
            return None
        self.__Offset += len(rsp)
        self.__TotalDownloaded += len(rsp)
        # don't log too often
        m = self.__Limit
        while m < 200:
            m = m * 2
        if self.__TotalDownloaded % m == 0 or self.__TotalDownloaded == self.__TotalHits and rsp and len(rsp) > 0:
            logging.info("%s - downloaded %s of %s", self.__LogPrefix, self.__TotalDownloaded, self.__TotalHits)
        self.__Results = rsp

    def Clear(self):
        self.__Url = None

    def GetAll(self, limit=None):
        rsp = []
        if limit and limit == 0:
            limit = None
        while self.__Results and len(self.__Results) > 0:
            rsp.extend(self.__Results)
            if limit:
                if len(rsp) >= limit:
                    break
                if limit and limit - self.__TotalDownloaded < self.__Limit:
                    self.__Limit = limit - self.__TotalDownloaded
            self.GetNext()
        self.__Results = rsp
        self.Clear()
        return self.__Results

class FodClientResponse(object):
    def __init__(self, response=None):
        if response != None:
            self.__Content = response.json() if response and callable(getattr(response, "json", None)) else None
            self.__Status = getattr(response, "status_code", 200)
            self.__Text = getattr(response, "text", None)
            self.__Reason = getattr(response, "reason", "Ok")
            self.__Ok = getattr(response, "ok", True)
        else:
            self.__Content = None
            self.__Status = None
            self.__Text = None
            self.__Reason = None
            self.__Ok = False

    @property
    def Content(self):
        return self.__Content

    @Content.setter
    def Content(self, value):
        self.__Content = value

    @property
    def Status(self):
        return self.__Status

    @Status.setter
    def Status(self, value):
        self.__Status = value

    @property
    def Text(self):
        return self.__Text

    @Text.setter
    def Text(self, value):
        self.__Text = value

    @property
    def Reason(self):
        return self.__Reason

    @Reason.setter
    def Reason(self, value):
        self.__Reason = value

    @property
    def Ok(self):
        return self.__Ok

    @Ok.setter
    def Ok(self, value):
        self.__Ok = value


class FodClientException(Exception):
    pass
class FodClientConfigurationException(FodClientException):
    pass
class FodClientAuthenticationException(FodClientException):
    pass
class FodClientServerErrorException(FodClientException):
    pass
class FodClientEmptyResponseException(FodClientException):
    pass