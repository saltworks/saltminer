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
import logging
import os
import datetime
import ntpath

from .EncryptionHelper import EncryptionHelper
from .ApplicationExceptions import *

class ApplicationSettings(object):
    """Settings class - automatic handling of encryption/decryption for settings ending in 'Secret' or 'Password'"""

    def __init__(self, configFiles, sourceConfigFiles, keyFile = 'saltminer.key', autoEncryptCreds = True):
        self.EnableLogging = False
        self.__EncryptionTag = "e$Fernet$"
        self.__Flags = []
        self.__Config = {}
        self.__ConfigFiles = {}
        self.__Sources = {}
        self.__SourceConfigFiles = {}
        self.Load(configFiles, sourceConfigFiles)
        self.__KeyFile = keyFile
        self.__Eh = EncryptionHelper(keyFile)
        self.Application = None
        if autoEncryptCreds:
            self.EncryptCredentials()
        self.__Log("debug", "Settings class initialization complete")

    @staticmethod
    def __PathLeaf(path):
        head, tail = ntpath.split(path)
        return tail or ntpath.basename(head)
        
    def __Log(self, sev, msg):
        # Can't log until Application configures it, so just print something if not yet enabled
        if not self.EnableLogging:
            print(f"{datetime.datetime.now():%Y-%m-%d %H:%M} [PRELOG] [{sev.upper()}] {msg}")
            return
        if sev.lower() == "info":
            logging.info(msg)
        elif sev.lower() == "warn":
            logging.warning(msg)
        elif sev.lower() == "error":
            logging.error(msg)
        elif sev.lower() == "critical":
            logging.critical(msg)
        else:
            logging.debug(msg)

    def __IsEncrypted(self, value):
        return str(value).startswith(self.__EncryptionTag)

    def __EncryptSetting(self, config, section, key, isSource, save=True):
        self.__Log("debug", f"Encrypting '{section}.{key}' setting")
        val = self.__GetFromConfig(config, section, key, None, isSource)
        if not val:
            raise ApplicationConfigurationException("Cannot encrypt empty / null value")
        val = self.__Eh.Encrypt(val)
        if isSource:
            self.SetSource(section, key, val, save)
        else:
            self.Set(section, key, val, save)

    def __ShouldBeEncrypted(self, key, value):
        k = key.lower()
        return (k.endswith("password") or k.endswith("secret") or k.endswith("apikey")) and not self.__IsEncrypted(value) and value

    def __MakeItDirty(self, section, isSource):
        if isSource:
            self.__SourceConfigFiles[section]['dirty'] = True
        else:
            self.__ConfigFiles[section]['dirty'] = True

    def __GetFromConfig(self, config, section, key, default, isSource):
        template = (f"source '{section}' and key '{key}'." if isSource else f"config '{section}' and key '{key}'.")
        if not section in config.keys() or not key in config[section].keys():
            if not default is None:
                return default
            msg = f"Settings incorrect or missing value for {template}"
            self.__Log("error", msg)
            raise ApplicationConfigurationException(msg)

        # Ok we have the value
        value = config[section][key]

        # Decrypt if needed
        try:
            if not value:
                return value
            if self.__IsEncrypted(value):
                self.__Log("debug", f"Decrypting {template}")
                return self.__Eh.Decrypt(value)
            return value
        except Exception as e:
            errMsg = f"Failed to decrypt encrypted {template}"
            self.__Log("error", errMsg)
            raise ApplicationConfigurationException(errMsg) from e

    def __SetToConfig(self, config, section, key, value, isSource=False, save=True):
        template = (f"source '{section}' and key '{key}'." if isSource else f"config '{section} and key '{key}'.")

        config[key] = value
        self.__MakeItDirty(section, isSource)

        # Encrypt if needed
        try:
            if value and self.__ShouldBeEncrypted(key, value):
                self.__Log("debug", f"Encrypting {template}")
                config[key] = self.__Eh.Encrypt(value)
        except Exception as e:
            errMsg = f"Failed to encrypt {template}"
            self.__Log("error", errMsg)
            raise ApplicationConfigurationException(errMsg) from e

        try:
            if save:
                self.Save()
        except Exception as e:
            errMsg = f"Failed to save {template}"
            self.__Log("error", errMsg)
            raise ApplicationConfigurationException(errMsg) from e

    def Load(self, configFiles, sourceConfigFiles):
        ''' Loads settings from disk '''
        self.__Log("debug", f"Loading settings - {len(configFiles)} config file(s) and {len(sourceConfigFiles)} source config file(s)")
        if not self.__Config:
            self.__Config = {}
        for cf in configFiles:
            fname = self.__PathLeaf(cf)[:-5]
            try:
                with open(cf) as jsonData:
                    self.__Config[fname] = json.loads(jsonData.read())
                    self.__ConfigFiles[fname] = { "file": cf, "dirty": False }
            except Exception as e:
                raise ApplicationConfigurationException(f"Failed to load config json file '{fname}'") from e
        if not self.__Sources:
            self.__Sources = {}
        for cf in sourceConfigFiles:
            fname = self.__PathLeaf(cf)[:-5]
            try:
                with open(cf) as jsonData:
                    data = json.loads(jsonData.read())
                    self.__Sources[data['SourceName']] = data
                    self.__SourceConfigFiles[data['SourceName']] = { "file": cf, "dirty": False }
            except Exception as e:
                raise ApplicationConfigurationException(f"Failed to load source config json file '{fname}'") from e
        if 'Flags' in self.__Config.keys():
            self.__Flags = self.__Config['Flags']['Flags']
        else:
            self.__Flags = []

    def Save(self):
        ''' Writes settings to disk for files that have changed '''
        self.__Log("debug", "Saving settings")
        for cf in self.__ConfigFiles.keys():
            key = self.__ConfigFiles[cf]
            if key['dirty']:
                self.__Log("debug", f"Saving config '{cf}' to file '{key['file']}'")
                with open(key['file'], "w") as file:
                    json.dump(self.__Config[cf], file, indent=2)
        for cf in self.__SourceConfigFiles.keys():
            key = self.__SourceConfigFiles[cf]
            if key['dirty']:
                self.__Log("debug", f"Saving source config '{cf}' to file '{key['file']}'")
                with open(key['file'], "w") as file:
                    json.dump(self.__Sources[cf], file, indent=2)

    def FlagSet(self, key):
        ''' Returns True if requested flag is present in settings, False if not '''
        return key in self.__Flags

    def Clear(self, section, key, isSource=False, save=True):
        ''' Clears a setting, optionally saving the update to disk '''
        if isSource:
            self.SetSource(section, key, "", save)
        else:
            self.SetConfig(section, key, "", save)

    def GetSourceDict(self, sourceName):
        ''' Returns an entire source config as a Dict '''
        if not sourceName in self.__Sources.keys():
            msg = f"Source '{sourceName}' not found in configuration."
            self.__Log("error", msg)
            raise ApplicationConfigurationException(msg)
        return self.__Sources[sourceName]

    def GetSource(self, sourceName, key, default=None):
        ''' Gets a source config setting value by passed source name and key, returning an optional default value if setting is not found and automatically decrypting if needed '''
        return self.__GetFromConfig(self.__Sources, sourceName, key, default, True)

    def GetSourceNames(self):
        ''' Gets a list of source config names '''
        return self.__Sources.keys()

    def GetDict(self, section):
        ''' Gets an entire config file as a Dict '''
        if not section in self.__Config.keys():
            msg = f"Configuration section '{section}' not found - check to make sure '{section}.json' exists."
            self.__Log("error", msg)
            raise ApplicationConfigurationException(msg)
        return self.__Config[section]

    def Get(self, section, key, default=None):
        ''' Gets a setting value by passed section and key, returning an optional default value if setting is not found and automatically decrypting if needed '''
        return self.__GetFromConfig(self.__Config, section, key, default, False)
    
    def GetConfigNames(self):
        ''' Gets a list of config names '''
        return self.__Config.keys()

    def Set(self, section, key, value, save=True):
        ''' Sets a config setting, optionally saving it '''
        config = self.GetDict(section)
        self.__SetToConfig(config, section, key, value, False, save)

    def SetSource(self, sourceName, key, value, save=True):
        ''' Sets a source config setting, optionally saving it '''
        config = self.GetSourceDict(sourceName)
        self.__SetToConfig(config, sourceName, key, value, True, save)

    def EncryptCredentials(self):
        ''' Searches all settings for those ending in "Password" or "Secret" and encrypts them if not already encrypted, saving the updates to the settings file on disk.'''
        didit = False
        self.__Log("debug", "Encrypting credential settings")
        for section in self.__Config.keys():
            for key in self.__Config[section].keys():
                if self.__ShouldBeEncrypted(key, self.__Config[section][key]):
                    self.__EncryptSetting(self.__Config, section, key, False)
                    didit = True
        for source in self.__Sources.keys():
            for key in self.__Sources[source].keys():
                if self.__ShouldBeEncrypted(key, self.__Sources[source][key]):
                    self.__EncryptSetting(self.__Sources, source, key, True)
                    didit = True
        if didit:
            self.Save()
