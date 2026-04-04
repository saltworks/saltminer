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

import asyncio
import logging
import urllib3
import time

import httpx
import requests
from requests.auth import HTTPBasicAuth
from requests.exceptions import ConnectionError as RequestsConnectionError, ReadTimeout as RequestsReadTimeout

class RestClientException(Exception):
    pass
class RestClientConfigurationException(RestClientException):
    pass

class RestClient:
    
    def __init__(self, baseUrl=None, authUser=None, authPass=None, sslVerify=None, defaultHeaders=None, enableSession=True, timeout=240, retryConnectionErrors=False, retryDelaySec=3, proxy=None, proxyUser=None, proxyPass=None, overrideProtocol=None):
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
        if overrideProtocol != None and overrideProtocol not in ["https://", "http://"]:
            raise RestClientConfigurationException(f"Invalid overrideProtocol '{overrideProtocol}', expected 'https://' or 'http://'")
        self.__OverrideProtocol = overrideProtocol
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
        if self.__OverrideProtocol != None:
            url = url.replace("https://", self.__OverrideProtocol).replace("http://", self.__OverrideProtocol)
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
            if not self.__RetryConnectionErrors:
                logging.error(f"API error encountered ({type(e).__name__}).  Last url: '{url}'")
                raise
            logging.error(f"API error encountered ({type(e).__name__}) when calling url '{url}', retrying in {self.__RetryDelaySec} secs.")
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


class AsyncRestClient:
    '''
    Async counterpart to RestClient.  Uses httpx.AsyncClient internally so all
    HTTP methods are coroutines (async def).  API mirrors RestClient exactly so
    DataClient can delegate sync calls to async implementations via a persistent
    event loop without duplicating endpoint logic.
    '''

    def __init__(self, baseUrl=None, authUser=None, authPass=None, sslVerify=None,
                 defaultHeaders=None, timeout=240, retryConnectionErrors=False,
                 retryDelaySec=3, proxy=None, proxyUser=None, proxyPass=None):
        if sslVerify is None:
            self.__SslVerify = True
        elif sslVerify == "False":
            self.__SslVerify = False
        else:
            self.__SslVerify = sslVerify
        self._auth = httpx.BasicAuth(authUser, authPass) if authUser and authPass else None
        if not self.__SslVerify:
            logging.warning("SSL connections will not be verified by AsyncRestClient as configured. This is unsafe and not recommended outside a development environment.")
        self.__DefHeaders = defaultHeaders
        if baseUrl and not baseUrl.endswith("/"):
            baseUrl += "/"
        self.__RetryConnectionErrors = retryConnectionErrors
        self.__RetryDelaySec = 3 if not retryDelaySec or retryDelaySec < 1 else retryDelaySec
        self.__BaseUrl = baseUrl
        self.__RequestStats = {}
        self.__Timeout = float(timeout)
        proxy_url = None
        if proxy:
            creds = f"{proxyUser}:{proxyPass}@" if proxyUser else ""
            proxy_url = f"http://{creds}{proxy}"
        self.__Client = httpx.AsyncClient(
            verify=self.__SslVerify,
            headers=defaultHeaders or {},
            timeout=self.__Timeout,
            auth=self._auth,
            proxy=proxy_url,
        )
        logging.debug("AsyncRestClient initialized. baseUrl: '%s', sslVerify: '%s', defaultHeaders: %s", baseUrl, sslVerify, defaultHeaders)

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
        if "_all" not in self.__RequestStats:
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
        if statKey not in self.__RequestStats:
            self.__RequestStats[statKey] = { "Count": 1, "LastDuration": elapsedTime, "AvgDuration": elapsedTime }
        else:
            self.__RequestStats[statKey]['Count'] += 1
            self.__RequestStats[statKey]['LastDuration'] = elapsedTime
            self.__RequestStats[statKey]['AvgDuration'] = ((self.__RequestStats[statKey]['AvgDuration'] * (self.__RequestStats[statKey]['Count'] - 1)) + elapsedTime) / self.__RequestStats[statKey]['Count']

    def RequestStatsReport(self):
        return { "BaseUrl": self.__BaseUrl, "Stats": self.__RequestStats }

    def RequestCount(self, statKey=None):
        if not statKey:
            statKey = "_all"
        return self.__RequestStats[statKey]['Count']

    def RequestAvgDuration(self, statKey=None):
        if not statKey:
            statKey = "_all"
        return self.__RequestStats[statKey]['AvgDuration']

    def RequestLastDuration(self, statKey=None):
        if not statKey:
            statKey = "_all"
        return self.__RequestStats[statKey]['LastDuration']

    async def Get(self, url, json=None, data=None, headers=None, proxies=None, statKey=None, queryParams=None):
        return await self.Request("get", url, json, data, headers, proxies, statKey, queryParams)

    async def Post(self, url, json=None, data=None, headers=None, proxies=None, statKey=None, queryParams=None, files=None):
        return await self.Request("post", url, json, data, headers, proxies, statKey, queryParams, files)

    async def Put(self, url, json=None, data=None, headers=None, proxies=None, statKey=None, queryParams=None):
        return await self.Request("put", url, json, data, headers, proxies, statKey, queryParams)

    async def Delete(self, url, json=None, data=None, headers=None, proxies=None, statKey=None, queryParams=None):
        return await self.Request("delete", url, json, data, headers, proxies, statKey, queryParams)

    async def Request(self, method, url, json=None, data=None, headers=None, proxies=None, statKey=None, queryParams=None, files=None):
        '''
        Async HTTP request — mirrors RestClient.Request signature exactly.

        :arg method: HTTP method (get/post/put/delete)
        :arg url: Full or partial URL; baseUrl is prepended for relative paths
        :arg json: JSON-serialisable body (use json OR data, not both)
        :arg data: Form-encoded body (use json OR data, not both)
        :arg headers: Per-request headers; override defaults when provided
        :arg proxies: Unused — proxy is configured at construction time
        :arg statKey: Stats bucket key for duration tracking
        :arg queryParams: Dict of query-string parameters
        :arg files: Multipart file body
        '''
        if url.startswith("/"):
            url = url[1:]
        if self.__BaseUrl is not None and not url.startswith("http:") and not url.startswith("https:"):
            _url = self.__BaseUrl + url
        else:
            _url = url
        _headers = headers if headers is not None else self.__DefHeaders
        if queryParams and not isinstance(queryParams, dict):
            raise ValueError("queryParams must be type dict")
        queryParams = None if not queryParams else queryParams
        st = time.perf_counter()
        retry_attempted = False
        resp = None
        try:
            resp = await self.__Client.request(method, _url, json=json, data=data, headers=_headers, params=queryParams, files=files)
        except (httpx.ConnectError, httpx.ReadTimeout) as e:
            if retry_attempted or not self.__RetryConnectionErrors:
                logging.error("API error encountered (%s). Last url: '%s'", type(e).__name__, url)
                raise
            logging.error("API error (%s) calling '%s', retrying in %s secs.", type(e).__name__, url, self.__RetryDelaySec)
            retry_attempted = True
            await asyncio.sleep(self.__RetryDelaySec)
            st = time.perf_counter()
            resp = await self.__Client.request(method, _url, json=json, data=data, headers=_headers, params=queryParams, files=files)

        et = time.perf_counter() - st
        self.__CollectRequestStats(et, statKey)
        logging.debug("[AsyncRestClient] %s: %s, Headers: %s, Duration (sec): %s.  Response: (%s) %s",
                      method, _url, _headers, et, resp.status_code, resp.reason_phrase)
        return resp

    async def close(self):
        '''Close the underlying httpx.AsyncClient and release connections.'''
        await self.__Client.aclose()

    @staticmethod
    def disableRequestWarnings():
        urllib3.disable_warnings(urllib3.exceptions.InsecureRequestWarning)
