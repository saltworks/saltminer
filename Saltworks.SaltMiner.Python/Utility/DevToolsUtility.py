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

from array import array
import json
import time
import logging

from Core.RestClient import RestClient

class DevToolsUtility(object):

    def __init__(self, appSettings, elasticConfigName="Elastic"):
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
                  
        if scheme == 'http':            
            logging.warning("No password or https set, this is for POC use only!")            
            if not appSettings.Get(elasticConfigName, 'UseAuth', True):
                logging.debug('Not using username or password')
                self.__Es = RestClient(baseUrl=url, sslVerify=sslVerify, defaultHeaders=None, enableSession=True)
            else:
                self.__Es = RestClient(baseUrl=url, authUser=appSettings.Get(elasticConfigName, "Username"), authPass=appSettings.Get(elasticConfigName, "Password"), sslVerify=sslVerify, defaultHeaders=None, enableSession=True)
        else:
            self.__Es = RestClient(baseUrl=url, authUser=appSettings.Get(elasticConfigName, "Username"), authPass=appSettings.Get(elasticConfigName, "Password"), sslVerify=sslVerify, defaultHeaders=None, enableSession=True)


    def ExecuteDevScript(self, method, action,logFilepath=None, body=None):
        time.sleep(0.5)
        logging.info("Running [%s] %s", method, action)
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