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

import datetime
import logging
import time

from Core.Application import Application

class SettingsHelper(object):

    def __init__(self, app):
        if not isinstance(app, Application):
            raise ValueError("Parameter app expected to be of type Application.")
        logging.debug("SettingsHelper initializing")
        self.__es = app.GetElasticClient()
        self.__idx = "sm25settings"
        self.__es.MapIndex(self.__idx, False)
        logging.debug("SettingsHelper initialized")

    def Get(self, key):
        '''
        Retrieve settings object containing the setting value requested by key
        '''
        logging.debug("Get called with key '%s'.", key)
        rsp = self.__es.Search(self.__idx, { "query": { "term": { "key": { "value": key } } } }, includeLockingInfo=True)
        if not rsp or not rsp[0] or not '_source' in rsp[0]:
            return None
        return Sm25Setting(rsp[0])
    
    def SetValue(self, key, value):
        '''
        Sets setting value (works for when setting doesn't exist also).  Waits 1 sec to return the resulting setting information
        '''
        logging.debug("SetValue called - key '%s', value '%s'", key, value)
        setting = self.Get(key)
        if not setting:
            doc = {
                "key": key,
                "value": value,
                "timestamp": datetime.datetime.now(datetime.timezone.utc)
            }
            self.__es.Index(self.__idx, doc)
            self.__es.FlushIndex(self.__idx)
        return setting

    def Set(self, setting):
        '''
        Send settings object to data store (with updates)
        '''
        if not isinstance(setting, Sm25Setting):
            raise ValueError("Parameter setting expected to be of type Sm25Setting.")
        logging.debug("Set called for key '%s'.", setting.Key)
        self.__es.UpdateWithLocking(self.__idx, setting.Dto, setting.Id, setting.Seq, setting.Pri)
        
    def Delete(self, setting):
        '''
        Delete setting entry
        '''
        logging.debug("Delete called for setting with key '%s'.", setting.Key)
        self.__es.DeleteById(self.__idx, setting.Id)

    def DeleteByKey(self, key):
        '''
        Delete setting entry by key
        '''
        logging.debug("DeleteByKey called for key '%s'.", key)
        self.__es.DeleteByQuery(self.__idx, { "query": { "term": { "key": { "value": key } } } }, False, False)
        
    
    
class Sm25Setting(object):
    
    def __init__(self, dto):
        self.__id = dto['_id']
        self.__key = dto['_source']['key']
        self.__value = dto['_source']['value']
        self.__timestamp = dto['_source']['timestamp']
        self.__pri = dto['_primary_term']
        self.__seq = dto['_seq_no']
    
    @property
    def Id(self): return self.__id
    
    @Id.setter
    def Id(self, value): self.__id = value

    @property
    def Key(self): return self.__key
    
    @Key.setter
    def Key(self, value): self.__key = value

    @property
    def Value(self): return self.__value
    
    @Value.setter
    def Value(self, value): self.__value = value
    
    @property
    def Timestamp(self): return self.__timestamp
    
    @Timestamp.setter
    def Timestamp(self, value): self.__timestamp = value
    
    @property
    def Pri(self): return self.__pri
    
    @Pri.setter
    def Pri(self, value): self.__pri = value
    
    @property
    def Seq(self): return self.__seq
    
    @Seq.setter
    def Seq(self, value): self.__seq = value
    
    @property
    def Dto(self): 
        return { 
            "key": self.__key,
            "value": self.__value,
            "timestamp": self.__timestamp
        }
    