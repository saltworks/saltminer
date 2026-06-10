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

import json
import os
import time
import logging

from Core.Application import Application

class DevToolsHelperException(Exception):
    pass


class DevToolsHelper(object):

    def __init__(self, app:Application):
        self.__Es = app.GetElasticClient()


    def ExecuteDevToolsScript(self, method:str, action:str, log_file_path:str=None, body:str=None):
        time.sleep(0.5)
        logging.debug("Running [%s] %s", method, action)
        parsed_body = json.loads(body) if body and body.strip() else None
        response = self.__Es.PerformRequest(method=method, path=action, body=parsed_body)
        logging.info('[%s] %s processed with response of %s', method, action, response)
        if log_file_path:
            with open(log_file_path, 'a') as logFile:
                logFile.write(f"{json.dumps(dict(response.body))}\n")


    def ExecuteDevToolsFile(self, file_path:str, log_file_path:str=None):
        scriptMethods = ["GET", "PUT", "POST", "DELETE"]

        with open(file_path, "r") as file:
            logging.info('Processing file: %s', file_path)
            devMethod = []
            devURL = []
            requestBodyString = ''
            isNewMethod = False  # flag for when we found the NEXT method (which ends the previous one)
            for line in file:
                if line == '\r\n' or line == '\n':
                    continue
                if line.startswith('#'):
                    continue

                if any(line.startswith(method) for method in scriptMethods):
                    if isNewMethod:
                        if log_file_path:
                            with open(log_file_path, 'a') as logFile:
                                logFile.write(f'{devMethod[0]} {devURL[0]}\n')
                        self.ExecuteDevToolsScript(method=devMethod[0], action=devURL[0], body=requestBodyString, log_file_path=log_file_path)
                        devMethod = []
                        devURL = []
                        requestBodyString = ''
                        isNewMethod = False

                    methodList = line.split(' ')
                    devMethod.append(methodList[0])
                    devURL.append(methodList[1].strip('\n'))
                    isNewMethod = True
                    methodList = []
                    continue
                if not any(method in line for method in scriptMethods):
                    requestBodyString += line.strip('\n')
                methodList = []

        if isNewMethod:
            if log_file_path:
                with open(log_file_path, 'a') as logFile:
                    logFile.write(f'{devMethod[0]} {devURL[0]}\n')
            self.ExecuteDevToolsScript(method=devMethod[0], action=devURL[0], body=requestBodyString, log_file_path=log_file_path)
            isNewMethod = False


    def ExecuteDevToolsFiles(self, folder_path:list[str], file_ext:str=None, log_file_path:str=None, continue_on_error:bool=False):
        count = 0
        for path in folder_path:
            if file_ext:
                files = [f for f in os.listdir(path) if f.endswith(file_ext)]
            else:
                files = os.listdir(path)

            for file in files:
                file_path = os.path.join(path, file)
                try:
                    self.ExecuteDevToolsFile(file_path=file_path, log_file_path=log_file_path)
                    count += 1
                except Exception as e:
                    logging.exception("Error processing file %s: %s", file_path, e)
                    if not continue_on_error:
                        raise
        logging.info('%s files processed', count)

