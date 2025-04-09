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
import sys
import time
import datetime
import logging
import uuid
import os

import requests
from requests.exceptions import ConnectionError as RequestsConnectionError, ReadTimeout as RequestsReadTimeout

from Core.RestClient import RestClient

class SCDastClient(object):
        
    def __init__(self, appSettings, configName="SCDast"):
        '''
        Initializes the class.

        appSettings: Settings instance containing application settings
        sourceName: SourceName appearing in a config file in Config\Sources
        '''
        if type(appSettings).__name__ != "ApplicationSettings":
            raise SCDastClientConfigurationException("Type of appSettings must be 'ApplicationSettings'")
        self.__App = appSettings.Application
        self.__Client = None
        self.__Login(appSettings, configName)

        logging.debug("SCDastClient initialization complete.")

    def __Login(self, appSettings, configName):
        '''
        Get auth token and setup RestClient to make authenticated calls to the API
        '''
        headers = {
            'Accept':'application/json',
            'Content-Type':'application/json;charset=UTF-8'
        }
        # Make the token expire in a day
        body = {
            "type": "UnifiedLoginToken",
            "terminalDate": (datetime.datetime.now() + datetime.timedelta(days=1)).strftime("%Y-%m-%dT%H:%M:%S.%f")
        }
        url = appSettings.Get(configName, 'Url') + '/api/v2/auth'
        verify = (appSettings.Get(configName, 'SslVerify', 'True')=='True')
        #auth = '{"username": "' + self.__Settings.Get("SscDASTUsername") + '", "password": "' + self.__Settings.Get("SscDASTPassword") + '""}'
        auth = RestClient.basicAuth(appSettings.Get(configName, "Username"), appSettings.Get(configName, "Password"))
        RestClient.disableRequestWarnings()

        # Sample auth response
        # {"data":{"remainingUsages":-1,"terminalDate":"2020-06-19T20:36:42.697+0000","description":null,"id":1963,"creationDate":"2020-06-18T20:36:42.697+0000","type":"UnifiedLoginToken","token":"MGY4NDBkMzItZTlkYS00MjczLWI4OWQtNzEyZjVhMmQ2YTZj","username":"dennis.hurst"},"responseCode":201}
        response = requests.post(url, json=body, verify=verify, headers=headers, auth=auth)
        r = json.loads(response.text)
        if response.status_code == 401:
            raise SCDastClientAuthenticationException(f"(401) {r['message']}")
        authToken = r['token']

        headers = {
            'Accept':'application/json',
            'Content-Type':'application/json;charset=UTF-8',
            'Authorization': '{}'.format(authToken)
        }

        self.__Client = RestClient(appSettings.Get(configName, 'Url') + '/api/v2/', sslVerify=appSettings.Get(configName, 'SslVerify'), defaultHeaders= headers)
        # Nothing to return - setting up the client was the goal

    def __Get(self, url, navToData = False, suppressError = False):
        '''
        Calls GET and loads the response into an object, raising an error if not a 2xx response.

        Parameters:
        url - url to GET
        navToData - set to True if the return object should be automatically be navigated to the 'data' element (i.e. r['data']).
        suppressError - set to True to not raise an error if the response.status_code is not 2xx.  Returns None if error suppressed.
        '''
        return self.__GetResponseDataOrError(self.__Client.Get(url), suppressError, navToData)

    def __GetResponseDataOrError(self, response, suppressError = False, navToData = True):
        '''
        Returns response loaded as an object or raises an error if not a 2xx response.

        Parameters:
        response - the response to load
        navToData - set to False if the return object shouldn't be automatically be navigated to the 'data' element (i.e. r['data']).
        suppressError - set to True to not raise an error if the response.status_code is not 2xx.  Returns None if error suppressed.
        '''

        if response.status_code == 404:
            msg = f"(404) Not found for url '{response.url}'"
            logging.error(f"SSC API call failure: {msg}")
            raise SCDastClientException(msg)
        m = json.loads(response.text)
        if 200 <= response.status_code < 300:
            if navToData:
                return m['data']
            else:
                return m
        else:
            msg = f"SSC API call failure: ({response.status_code}) {m}"

            logging.error(msg)
            if not suppressError:
                raise SCDastClientException(msg)
            return None

        
    @staticmethod
    def TestConnection(settings):
        '''
        Connects to SCD
        '''
        try:
            SCDastClient(settings)
            return True,""
        except Exception as e:
            return False, f"{e}"

    def GetSCDSummaryListOrdered(self, offset=0, limit=100):
        '''
        Return a list scan summary from scancentraldast

        '''
        #return self.__Get(f"api/v2/scans/scan-summary-list")
        return self.__Get(f"scans/scan-summary-list?offset={offset}&limit={limit}&scanStatusType=Complete&orderBy=scanStatusDateTime&orderByDirection=DESC")

    def GetSCDApplicationsList(self):
        '''
        Return a list applications from scancentraldast
        '''
        return self.__Get(f"applications/")
      

class SCDastClientException(Exception):
    pass
class SCDastClientConfigurationException(SCDastClientException):
    pass
class SCDastClientAuthenticationException(SCDastClientException):
    pass
class SCDastClientServerErrorException(SCDastClientException):
    pass
class SCDastClientEmptyResponseException(SCDastClientException):
    pass