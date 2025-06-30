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

import logging
import os
import sys
import datetime
import time
import glob
import json

import ecs_logging
from logging.handlers import RotatingFileHandler

from .ApplicationSettings import ApplicationSettings
from .ApplicationExceptions import *
from .ElasticClient import ElasticClient

class AppLoggingFilter(logging.Filter):
    def __init__(self, name="", suppressBaseWarnings=True):
        super(AppLoggingFilter, self).__init__(name)
        self.__suppressBaseWarnings = suppressBaseWarnings

    def filter(self, record):
        if self.__suppressBaseWarnings:
            return record.module not in ["base", "connectionpool"] or record.levelno > logging.WARNING
        return record.module not in ["base", "connectionpool"] or record.levelno > logging.INFO


class Application(object):
    """Application class - Settings, Logging, Helpers, and more"""

    def __init__(self, configPath=None, keyFile='saltworks.key', autoEncryptCreds = True, loggingInstance=None, skipCleanFiles=False):
        self.__Init = False
        self.__ElasticClient = None
        self.__ElasticConfigName = "Elastic"
        self.__DeprecatedWarnings = {}
        self.DebugMode = True
        verfile = "version.txt"
        ver = "version not specified"
        if os.path.isfile(verfile):
            with open(verfile) as fs:
                ver = fs.read()
        self.__Log(logging.INFO, f"SaltMiner {ver}")
        try:
            self.__GetConfig(configPath, keyFile, autoEncryptCreds)
        except ApplicationConfigurationException as e:
            self.HandleException(e, "Configuration error when loading application configuration")
        except Exception as e:
            self.HandleException(e, "Failed to load application configuration", "CRITICAL")
        self.__SuppressBaseWarnings = self.Settings.Get("logging", "SuppressBaseWarnings", True)
        try:
            self.__ConfigLogging(loggingInstance)
            self.Settings.EnableLogging = True
        except Exception as e:
            self.HandleException(e, f"Failed to configure application logging", "CRITICAL")
        self.DebugMode = self.Settings.Get('Logging', 'LogLevel', 'DEBUG') == "DEBUG"
        self.__Init = True
        if not skipCleanFiles:
            self.CleanFiles(self.Settings.Get('Logging', 'Folder', '.\\logs\\'), "*SaltMiner*", self.Settings.Get('Logging', 'FileRemoveAfterDays', 7))
            self.CleanFiles('.', "*.tmp", 1)
        self.__Log(logging.DEBUG, "Application class initialization complete")

    def __GetConfig(self, configPath, keyFileName, autoEncrypt):
        # Config rework - now looking for Env variable or local "Config" folder to contain config
        # All config files in config folder will be loaded automatically (they should have unique keys to avoid one stepping on another)
        # Source configs will be expected to be in a "Source" folder within the config folder
        envVar = "SALTMINER_2_CONFIG_PATH"

        # Don't use settings.json
        if os.path.isfile("settings.json"):
            raise ApplicationConfigurationException(f"Local settings.json is no longer supported.  Config files have been separated by function and should now be placed into the Config folder.  Config folder location also can be specified using the {envVar} environment variable.")

        # Many checks for new path
        # 1. Passed into class init
        ok = configPath and os.path.isdir(configPath)
        pathSource = "local"
        # 2. Env variable
        if not ok:
            configPath = os.getenv(envVar)
            ok = configPath and os.path.isdir(configPath)
            pathSource = "EnvVar" if ok else pathSource
        # 3. File - ConfigPath.txt
        if not ok and os.path.exists("ConfigPath.txt"):
            with open("ConfigPath.txt") as fs:
                configPath = fs.read()
            ok = configPath and os.path.isdir(configPath)
            pathSource = "ConfigPath.txt" if ok else pathSource
        # 4. Default - look for local Config dir
        if not ok and os.path.isdir("Config"):
            ok = True
            configPath = "Config"
        # 5. If all else fails and it exists, use the run location from last run
        # TODO: add this
        # Dump resulting config path to help with troubleshooting
        with open("sm-run-config-location.json", "w") as f:
            f.write(json.dumps({ "Method": pathSource, "Path": configPath if ok else "Unknown" }))
        self.LogInfo(f"Configuration location: {configPath if ok else 'Unknown'}")
        if not ok:
            raise ApplicationConfigurationException(f"Unable to locate configuration path.  Please do one of the following:\n1. Make sure it can be found in a local Config folder\n2. Set the {envVar} environment variable to the correct config directory path (ex. export {envVar}=/etc/saltworks/saltminer-2.5.0).\n3. Create a ConfigPath.txt file and put the config directory path in it (straight text, no json).")
        if os.path.isfile(os.path.join(configPath, "settings.json")): 
            raise ApplicationConfigurationException(f"settings.json is no longer supported.  Config files have been separated by function and should now be placed into the Config folder.  Config folder location also can be specified using the {envVar} environment variable.")
        if not os.path.isdir(os.path.join(configPath, "Sources")):
            raise ApplicationConfigurationException(f"Could not find source config folder 'Sources' in '{configPath}'.  Please check the configuration path and try again.")

        # Get config files
        configFiles = glob.glob(os.path.join(configPath, "*.json"))
        sourceConfigFiles = glob.glob(os.path.join(configPath, "Sources", "*.json"))
        if not configFiles or not sourceConfigFiles:
            raise ApplicationConfigurationException(f"Configuration file(s) are missing.  At least one .json file should be found in '{configPath}' and in '{os.path.join(configPath, 'Source')}'")

        # Locate key file (default to config path if missing)
        keyFilePath = os.path.join(configPath, keyFileName)
        if not os.path.isfile(keyFilePath) and os.path.isfile(keyFileName):
            keyFilePath = keyFileName          

        self.Settings = ApplicationSettings(configFiles, sourceConfigFiles, keyFilePath, autoEncrypt)
        self.Settings.Application = self

    def __ConfigLogging(self, loggingInstance):
        logFolder = self.Settings.Get('Logging', 'Folder', '.\\logs\\')
        logMaxBytes = self.Settings.Get('Logging', 'FileMaxBytes', 536870912)
        logMaxFileCount = self.Settings.Get('Logging', 'FileCountLimit', 9)
        if not os.path.exists(logFolder):
            os.makedirs(logFolder)

        t = datetime.datetime.now()
        level = self.__LogLevelParse(self.Settings.Get('Logging', 'LogLevel', 'DEBUG'))
        logFile = '{}{}.SaltMiner.'.format(logFolder, t.strftime('%Y.%m.%d'))
        if loggingInstance:
            logFile += str(loggingInstance) + "."
        logFile += "log"
        print('SaltMiner logging to: {}'.format(logFile))

        fileHandler = RotatingFileHandler(logFile, 'a', maxBytes=logMaxBytes, backupCount=logMaxFileCount)
        fileHandler.setFormatter(ecs_logging.StdlibFormatter(stack_trace_limit=self.Settings.Get('Logging', 'StackTraceLimit', 3)))
        fileHandler.level = level
        fileHandler.addFilter(AppLoggingFilter(suppressBaseWarnings=self.__SuppressBaseWarnings))
        consoleHandler = logging.StreamHandler(sys.stdout)
        consoleHandler.setFormatter(logging.Formatter('%(asctime)s [%(levelname)s] %(funcName)s in %(filename)s: %(message)s', '%Y-%m-%d %H:%M'))
        consoleHandler.level = level
        consoleHandler.addFilter(AppLoggingFilter(suppressBaseWarnings=self.__SuppressBaseWarnings))
        logging.basicConfig(level=level, handlers=[consoleHandler, fileHandler])

        # GROK PATTERN:
        # %{TIMESTAMP_ISO8601:timestamp},%{LOGLEVEL:loglevel},%{DATA:file},%{DATA:function},%{QUOTEDSTRING:message}.*

    def __LogLevelParse(self, logLevel):
        logLevel = logLevel.upper()
        if logLevel == "WARN":
            return logging.WARN
        if logLevel == "DEBUG":
            return logging.DEBUG
        if logLevel == "ERROR":
            return logging.ERROR
        if logLevel == "INFO":
            return logging.INFO
        if logLevel == "CRITICAL":
            return logging.CRITICAL
        return logging.NOTSET

    def __Log(self, level, msg):
        if self.__Init:
            logging.log(level, msg)
        else:
            print(f"{datetime.datetime.now():%Y-%m-%d %H:%M} [PRELOG] [{logging.getLevelName(level)}] {msg}")

    @property
    def ElasticConfigName(self):
        return self.__ElasticConfigName

    @ElasticConfigName.setter
    def ElasticConfigName(self, value):
        self.__ElasticConfigName = value

    def Deprecated(self, key, msg=None):
        '''
        Helper for warning about deprecated code things.  Warning will be logged for each call up to MAX_DEPRECATION_WARNINGS.
        Key should be unique for the application, message will be "{key} has been deprecated and will be removed in a future release." if not passed.

        Example call: app.Deprecated("ElasticClient.SearchWithCursor()", "ElasticClient.SearchWithCursor() has been deprecated, please use SearchScroll() instead.")
        '''
        if not msg:
            msg = f"{key} has been deprecated and will be removed in a future release."
        MAX_DEPRECATION_WARNINGS = 10
        if key in self.__DeprecatedWarnings.keys():
            if self.__DeprecatedWarnings[key] >= MAX_DEPRECATION_WARNINGS:
                return
        else:
            self.__DeprecatedWarnings[key] = 0
        logging.warning('[DEPRECATED] %s', msg)
        self.__DeprecatedWarnings[key] += 1
        if self.__DeprecatedWarnings[key] >= MAX_DEPRECATION_WARNINGS:
            logging.info("Suppressing further deprecation warnings for key '%s'", key)

    def GetElasticClient(self) -> ElasticClient:
        if self.__ElasticClient is None:
            self.__ElasticClient = ElasticClient(self.Settings, self.__ElasticConfigName)
        return self.__ElasticClient
            
    def LogWarning(self, msg):
        self.__Log(logging.WARNING, msg)
    
    def LogDebug(self, msg):
        self.__Log(logging.DEBUG, msg)
    
    def LogError(self, msg):
        self.__Log(logging.ERROR, msg)
    
    def LogCritical(self, msg):
        self.__Log(logging.CRITICAL, msg)
    
    def LogInfo(self, msg):
        self.__Log(logging.INFO, msg)
    
    def HandleException(self, exception, message = None, severity = logging.ERROR, exit=True):
        '''
        General catch-all application exception handler - logs (or prints if logging broken) exception info then optionally exits
        '''
        msg = f"{message + ': ' if message is not None else ''}[type(exception).__name__] {exception}"
        try:
            self.__Log(severity, msg)
        except:
            # eat logging exception and just print info about exception
            print(f"{datetime.datetime.now():%Y-%m-%d %H:%M} [{severity.upper()}] {msg}")
        # if debug mode, raise the exception so can see the stack
        if self.DebugMode:
            raise exception
        if exit:
            sys.exit(1)

    def CleanFiles(self, folder, pattern, age):
        '''
        Removes files that are older than [age] day(s)

        Parameters:
        folder - the folder in which to clean files
        pattern - the pattern to use to id files (i.e. *.log)
        age - the age of the file(s) to remove (i.e. 30 for files over 30 days)

        Set age to 0 to disable cleaning
        '''
        if age <= 0:
            logging.info("CleanFiles skipping folder '%s' and pattern '%s', age set to 0", folder, pattern)
            return
        logging.info("CleanFiles: folder '%s' and pattern '%s', age set to %s", folder, pattern, age)
        now = time.time()
        for f in glob.glob(os.path.join(folder, pattern)):
            if os.path.isfile(f) and os.stat(f).st_mtime < now - (age * 86400):
                logging.info(f"Removing old file '{f}'")
                os.remove(f)
