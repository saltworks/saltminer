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

from array import array
import json
import time
import logging

from Core.RestClient import RestClient

class DevToolsUtility(object):

    DEFAULT_TIMEOUT_SEC = 30

    def __init__(self, appSettings, elasticConfigName="Elastic", timeout=DEFAULT_TIMEOUT_SEC):
        '''
        :arg appSettings: ApplicationSettings instance
        :arg elasticConfigName: config section holding the Elasticsearch connection
        :arg timeout: request timeout in seconds applied to every statement in the script.
            30 suits ordinary dev-tools queries; raise it for a run containing a heavy
            aggregation, _reindex, _forcemerge or _delete_by_query (RunDevToolsUtility.py
            exposes this as --timeout).  Per-run, not per-statement: RestClient fixes its
            timeout at construction.
        '''
        if type(appSettings).__name__ != "ApplicationSettings":
            raise TypeError("Type of appSettings must be 'ApplicationSettings'")
        if not elasticConfigName or not elasticConfigName in appSettings.GetConfigNames():
            raise DevToolsUtiltyConfigurationException(f"Invalid or missing configuration for name '{elasticConfigName}'")

        host = appSettings.Get(elasticConfigName, "Host")
        host = host if not isinstance(host, array) else host[0]
        scheme = appSettings.Get(elasticConfigName, "Scheme")
        url = scheme + "://" + host + ":" + appSettings.Get(elasticConfigName, "Port")
        sslVerify = appSettings.Get(elasticConfigName, "SslVerify")
        try:
            # in case we get a string and not a bool try to manage it
            if sslVerify.lower() == "true":
                sslVerify = True
            if sslVerify.lower() == "false":
                sslVerify = False
        except Exception:
            pass
                  
        # coerce like the other numeric settings in this tree, floor of 1 sec
        self.__Timeout = max(1, int(timeout)) if timeout else DevToolsUtility.DEFAULT_TIMEOUT_SEC
        logging.debug("DevToolsUtility request timeout is %s sec", self.__Timeout)

        if scheme == 'http':
            logging.warning("No password or https set, this is for POC use only!")
            if not appSettings.Get(elasticConfigName, 'UseAuth', True):
                logging.debug('Not using username or password')
                self.__Es = RestClient(baseUrl=url, sslVerify=sslVerify, defaultHeaders=None, enableSession=True, timeout=self.__Timeout)
            else:
                self.__Es = RestClient(baseUrl=url, authUser=appSettings.Get(elasticConfigName, "Username"), authPass=appSettings.Get(elasticConfigName, "Password"), sslVerify=sslVerify, defaultHeaders=None, enableSession=True, timeout=self.__Timeout)
        else:
            self.__Es = RestClient(baseUrl=url, authUser=appSettings.Get(elasticConfigName, "Username"), authPass=appSettings.Get(elasticConfigName, "Password"), sslVerify=sslVerify, defaultHeaders=None, enableSession=True, timeout=self.__Timeout)


    @property
    def Timeout(self):
        return self.__Timeout

    def ExecuteDevScript(self, method, action,logFilepath=None, body=None):
        time.sleep(0.5)
        logging.info("Running [%s] %s (timeout %s sec)", method, action, self.__Timeout)
        if body:

            response= self.__Es.Request(method=method, url=f'{action}', json=json.loads(body))
            logging.info('[%s] %s processed with response of %s', method, action, response)
        else:

            response= self.__Es.Request(method=method, url=f'{action}')
            
            logging.info('[%s] %s processed with response of %s', method, action, response)
        if logFilepath:
            with open(logFilepath,'a') as logFile:
                logFile.write(f"{response.text}\n")
            



    def ExecuteDevScriptFile(self, devScriptFilePath, logFilepath=None):
        scriptMethods= ["GET", "PUT", "POST", "DELETE"]

        with open(devScriptFilePath, "r") as file:

            devMethod = []
            devURL= []
            requestBodyString = ''
            isNewMethod = False  # flag for when we found the NEXT method (which ends the previous one)
            for line in file:
                if line =='\r\n' or line == '\n':
                    continue
                if line.startswith('#'):
                    continue

                if any(line.startswith(method) for method in scriptMethods):
                    if isNewMethod == True:
                        if logFilepath:
                            with open(logFilepath,'a') as logFile:
                                logFile.write(f'{devMethod[0]} {devURL[0]}\n')
                        self.ExecuteDevScript(method=devMethod[0], action=devURL[0], body=requestBodyString, logFilepath=logFilepath)
                        devMethod = []
                        devURL = []
                        requestBodyString = '' 
                        isNewMethod = False

                    methodList = line.split(' ')
                    devMethod.append(methodList[0])
                    devURL.append(methodList[1].strip('\n'))
                    isNewMethod = True
                    methodList=[]
                    continue
                if not any(method in line for method in scriptMethods):
                    requestBodyString += line.strip('\n')
                methodList = []    
         
            if isNewMethod == True:
                if logFilepath:
                    with open(logFilepath,'a') as logFile:
                        logFile.write(f'{devMethod[0]} {devURL[0]}\n')
                self.ExecuteDevScript(method=devMethod[0], action=devURL[0], body=requestBodyString, logFilepath=logFilepath)
                isNewMethod = False
            logging.info('Processing complete')

class DevToolsUtilityException(Exception):
    pass
class DevToolsUtiltyConfigurationException(DevToolsUtilityException):
    pass