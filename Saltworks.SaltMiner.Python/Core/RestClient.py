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

import logging
import urllib3
import time

import requests
from requests.auth import HTTPBasicAuth
from requests.exceptions import ConnectionError as RequestsConnectionError, ReadTimeout as RequestsReadTimeout

class RestClient:
    
    def __init__(self, baseUrl=None, authUser=None, authPass=None, sslVerify=None, defaultHeaders=None, enableSession=True, timeout=240, retryConnectionErrors=False, retryDelaySec=3, proxy=None, proxyUser=None, proxyPass=None):
        if sslVerify is None:
            self.__SslVerify = True
        elif sslVerify == "False":
            self.__SslVerify = False
        else:
            self.__SslVerify = sslVerify
        if authUser is None or authPass is None:
            self._auth = None
        else:
            self._auth = RestClient.basicAuth(authUser, authPass)
        if self.__SslVerify == False:
            msg = "SSL connections will not be verified by RestClient as configured.  This is unsafe and not recommended outside a development environment."
            logging.warning(msg)
        self.__DefHeaders = defaultHeaders
        if not baseUrl.endswith("/"):
            baseUrl += "/"
        self.__RetryConnectionErrors = retryConnectionErrors
        self.__RetryDelaySec = 3 if not retryDelaySec or retryDelaySec < 1 else retryDelaySec
        self.__Retry = False
        self.__BaseUrl = baseUrl
        self.__RequestStats = {}
        self.__Session = requests.Session()
        self.SessionEnabled = enableSession
        self.__Warnings = { "request": 0, "get": 0, "put": 0, "post": 0, "delete": 0 }
        self.__Timeout = timeout
        self.__Proxy = None
        if proxy:
            creds = ""
            if proxyUser:
                creds = f"{proxyUser}:{proxyPass}@"
            self.__Proxy = { "http": creds + proxy, "https": creds + proxy }
        logging.debug("RestClient initialized. baseUrl: '{}', authUser: '{}', sslVerify: '{}', defaultHeaders: {}".format(baseUrl, authUser, sslVerify, defaultHeaders))

    @property
    def BaseUrl(self):
        return self.__BaseUrl

    @property
    def SslVerify(self):
        return self.__SslVerify

    @property
    def DefaultHeaders(self):
        return self.__DefHeaders

    def __CollectRequestStats(self, elapsedTime, statKey=None):
        '''
        Collects stats for the request, and buckets according to passed key if present
        '''
        if not "_all" in self.__RequestStats.keys():
            self.__RequestStats['_all'] = { "Count": 1, "LastDuration": elapsedTime, "AvgDuration": elapsedTime }
            self.__RequestStats['_uncat'] = { "Count": 0, "LastDuration": 0, "AvgDuration": 0 }
        else:
            self.__RequestStats['_all']['Count'] += 1
            self.__RequestStats['_all']['LastDuration'] = elapsedTime
            self.__RequestStats['_all']['AvgDuration'] = ((self.__RequestStats['_all']['AvgDuration'] * (self.__RequestStats['_all']['Count'] - 1)) + elapsedTime) / self.__RequestStats['_all']['Count']
        if not statKey:
            self.__RequestStats['_uncat']['Count'] += 1
            self.__RequestStats['_uncat']['LastDuration'] = elapsedTime
            self.__RequestStats['_uncat']['AvgDuration'] = ((self.__RequestStats['_uncat']['AvgDuration'] * (self.__RequestStats['_uncat']['Count'] - 1)) + elapsedTime) / self.__RequestStats['_uncat']['Count']
            return
        if not statKey in self.__RequestStats.keys():
            self.__RequestStats[statKey] = { "Count": 1, "LastDuration": elapsedTime, "AvgDuration": elapsedTime }
        else:
            self.__RequestStats[statKey]['Count'] += 1
            self.__RequestStats[statKey]['LastDuration'] = elapsedTime
            self.__RequestStats[statKey]['AvgDuration'] = ((self.__RequestStats[statKey]['AvgDuration'] * (self.__RequestStats[statKey]['Count'] - 1)) + elapsedTime) / self.__RequestStats[statKey]['Count']

    def RequestStatsReport(self):
        '''
        Returns base url and all collected stats
        '''
        return { "BaseUrl": self.__BaseUrl, "Stats": self.__RequestStats }

    def RequestCount(self, statKey=None):
        '''
        Total requests made during RestClient lifetime for the passed statKey (or all if not passed)
        '''
        if not statKey:
            statKey = "_all"
        return self.__RequestStats[statKey]['Count']

    def RequestAvgDuration(self, statKey=None):
        '''
        Average request duration in fractional seconds for all requests made during RestClient lifetime for the passed statKey (or all if not passed)
        '''
        if not statKey:
            statKey = "_all"
        return self.__RequestStats[statKey]['AvgDuration']

    def RequestLastDuration(self, statKey=None):
        '''
        Last request duration in fractional seconds for the passed statKey (or all if not passed)
        '''
        if not statKey:
            statKey = "_all"
        return self.__RequestStats[statKey]['LastDuration']
    
    # backward compatibility
    #region
    def request(self, method, url, json=None, data=None, headers=None, proxies=None):
        '''
        Deprecated, use Request instead
        '''
        if self.__Warnings["request"] < 10:
             logging.warning("[DEPRECATED] request() is deprecated, use Request() instead")
             self.__Warnings["request"] += 1
        return self.Request(method, url, json, data, headers, proxies)

    def get(self, url, json=None, data=None, headers=None, proxies=None):
        '''
        Deprecated, use Get instead
        '''
        if self.__Warnings["get"] < 10:
            logging.warning("[DEPRECATED] get() is deprecated, use Get() instead")
            self.__Warnings["get"] += 1
        return self.request("get", url, json, data, headers)
        
    def post(self, url, json=None, data=None, headers=None, proxies=None):
        '''
        Deprecated, use Post instead
        '''
        if self.__Warnings["post"] < 10:
            logging.warning("[DEPRECATED] post() is deprecated, use Post() instead")
            self.__Warnings["post"] += 1
        return self.request("post", url, json, data, headers)

    def put(self, url, json=None, data=None, headers=None, proxies=None):
        '''
        Deprecated, use Put instead
        '''
        if self.__Warnings["put"] < 10:
            logging.warning("[DEPRECATED] put() is deprecated, use Put() instead")
            self.__Warnings["put"] += 1
        return self.request("put", url, json, data, headers)

    def delete(self, url, json=None, data=None, headers=None, proxies=None):
        '''
        Deprecated, use Delete instead
        '''
        if self.__Warnings["delete"] < 10:
            logging.warning("[DEPRECATED] delete() is deprecated, use Delete() instead")
            self.__Warnings["delete"] += 1
        return self.request("delete", url, json, data, headers)
    #endregion

    def Get(self, url, json=None, data=None, headers=None, proxies=None, statKey=None, queryParams=None):
        '''
        Calls HTTP GET for specified partial or full URL
        :arg url: URL to use - if default URL set, then can be partial or full
        :arg json: don't use this param with GET unless you are sure
        :arg data: don't use this param with GET unless you are sure
        :arg headers: Headers to include with request - these override default headers if present
        :arg proxies: Proxy dict
        :arg statKey: Stats key, used to track duration stats for API calls
        :arg queryParams: Dict used to include query parameter key/values
        '''
        return self.Request("get", url, json, data, headers, proxies, statKey, queryParams)
        
    def Post(self, url, json=None, data=None, headers=None, proxies=None, statKey=None, queryParams=None, files=None):
        '''
        Calls HTTP POST for specified partial or full URL
        :arg url: URL to use - if default URL set, then can be partial or full
        :arg json: JSON body to send (use json OR data, not both)
        :arg data: Body to send (use json OR data, not both)
        :arg headers: Headers to include with request - these override default headers if present
        :arg proxies: Proxy dict
        :arg statKey: Stats key, used to track duration stats for API calls
        :arg queryParams: Dict used to include query parameter key/values
        '''
        return self.Request("post", url, json, data, headers, proxies, statKey, queryParams, files)

    def Put(self, url, json=None, data=None, headers=None, proxies=None, statKey=None, queryParams=None):
        '''
        Calls HTTP PUT for specified partial or full URL
        :arg url: URL to use - if default URL set, then can be partial or full
        :arg json: JSON body to send (use json OR data, not both)
        :arg data: Body to send (use json OR data, not both)
        :arg headers: Headers to include with request - these override default headers if present
        :arg proxies: Proxy dict
        :arg statKey: Stats key, used to track duration stats for API calls
        :arg queryParams: Dict used to include query parameter key/values
        '''
        return self.Request("put", url, json, data, headers, proxies, statKey, queryParams)

    def Delete(self, url, json=None, data=None, headers=None, proxies=None, statKey=None, queryParams=None):
        '''
        Calls HTTP DELETE for specified partial or full URL
        :arg url: URL to use - if default URL set, then can be partial or full
        :arg json: JSON body to send (use json OR data, not both)
        :arg data: Body to send (use json OR data, not both)
        :arg headers: Headers to include with request - these override default headers if present
        :arg proxies: Proxy dict
        :arg statKey: Stats key, used to track duration stats for API calls
        :arg queryParams: Dict used to include query parameter key/values
        '''
        return self.Request("delete", url, json, data, headers, proxies, statKey, queryParams)

    def Request(self, method, url, json=None, data=None, headers=None, proxies=None, statKey=None, queryParams=None, files=None):
        '''
        HTTP request - specify full or partial url and method
        
        :arg method: HTTP method to use, i.e. get/put/post/delete
        :arg url: URL to use - if default URL set, then can be partial or full
        :arg json: JSON body to send (use json OR data, not both)
        :arg data: Body to send (use json OR data, not both)
        :arg headers: Headers to include with request - these override default headers if present
        :arg proxies: Proxy dict
        :arg statKey: Stats key, used to track duration stats for API calls
        :arg queryParams: Dict used to include query parameter key/values
        :arg files: multipart file body
        '''
        if url.startswith("/"):
            url = url[1:]
        if self.__BaseUrl is not None and not url.startswith("http:") and not url.startswith("https:"):
            _url = self.__BaseUrl + url
        else:
            _url = url
        if headers is not None:
            _headers = headers
        else:
            _headers = self.__DefHeaders
        proxies = self.__Proxy if not proxies else proxies
        if queryParams and not isinstance(queryParams, dict):
            raise ValueError("queryParams must be type dict")
        queryParams = None if not queryParams else queryParams
        RestClient.disableRequestWarnings()
        st = time.perf_counter()
        resp = None
        try:
            if self.SessionEnabled:
                resp = self.__Session.request(method, _url, json=json, data=data, headers=_headers, verify=self.__SslVerify, auth=self._auth, timeout=self.__Timeout, proxies=proxies, params=queryParams, files=files)
            else:
                resp = requests.request(method, _url, json=json, data=data, headers=_headers, verify=self.__SslVerify, auth=self._auth, timeout=self.__Timeout, proxies=proxies, params=queryParams)
        except (ConnectionError, RequestsConnectionError, RequestsReadTimeout) as e:
            if self.__Retry and self.__RetryConnectionErrors:
                self.__Retry = False
                raise RestClientException("Failed request on retry.") from e
            logging.error(f"API error encountered ({type(e).__name__}), retrying in {self.__RetryDelaySec} secs.")
            self.__Retry = True
            time.sleep(self.__RetryDelaySec)
            st = time.perf_counter()
            if self.SessionEnabled:
                resp = self.__Session.request(method, _url, json=json, data=data, headers=_headers, verify=self.__SslVerify, auth=self._auth, timeout=self.__Timeout, proxies=proxies, params=queryParams, files=files)
            else:
                resp = requests.request(method, _url, json=json, data=data, headers=_headers, verify=self.__SslVerify, auth=self._auth, timeout=self.__Timeout, proxies=proxies, params=queryParams, files=files)
        finally:
            self.__Retry = False

        et = time.perf_counter() - st
        self.__CollectRequestStats(et, statKey)
        msg = "[RestClient] {}: {}, Headers: {}, Duration (sec): {}.  Response: ({}) {}".format(method, _url, _headers, et, resp.status_code, resp.reason)
        logging.debug(msg)
        return resp

    @staticmethod
    def basicAuth(username, password):
        return HTTPBasicAuth(username, password)

    @staticmethod
    def disableRequestWarnings():
        urllib3.disable_warnings(urllib3.exceptions.InsecureRequestWarning)

class RestClientException(Exception):
    pass