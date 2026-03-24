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

import logging
import json, requests

class Remapper(object):
    def __init__(self, appSettings):
        '''
        Initializes the class.

        appSettings: Settings instance containing application settings
        '''
        if type(appSettings).__name__ != "ApplicationSettings":
            raise TypeError("Type of appSettings must be 'ApplicationSettings'")
        self.app = appSettings.Application
        self.__Es = self.app.GetElasticClient()
        self.app.LogDebug("Remapper initialization complete.")

    def __Method(self, param):
        pass

    def Remap(self, jsonFile, targetIndex, type="merge"):

        tmpIndex = "remap_tmp"

        if self.__Es.IndexExists(tmpIndex):
            self.delete_all_by_query(index=tmpIndex)
        
        with open(jsonFile,'r') as file:
            data= json.load(file)
            
        aliasList = self.FindAlias(targetIndex)

        
        try:
            settings = data['template']['settings']
            mappings= data['template']['mappings']
            mapping = {
                "settings": settings,
                "mappings": mappings
            }
        except:
            mappings= data['template']['mappings']
            mapping ={
                "mappings": mappings
            } 


        logging.info("Building index name : %s, with new mapping", tmpIndex)
        self.__Es.MapIndexWithMapping(tmpIndex, mapping, True)
        logging.info("Reindexing data from %s to %s", targetIndex, tmpIndex)
        self.__Es.Reindex(targetIndex, tmpIndex)
        logging.info('Deleting index name: %s', targetIndex)
        self.__Es.DeleteIndex(targetIndex)
        logging.info('Rebuilding index %s with new mapping', targetIndex)
        self.__Es.MapIndexWithMapping(targetIndex, mapping, True)
        logging.info('Reindexing data back from %s to %s', tmpIndex, targetIndex)
        self.__Es.Reindex(tmpIndex, targetIndex)
        logging.info('Deleting %s', tmpIndex)
        self.__Es.DeleteIndex(tmpIndex)

        if aliasList != None:
            
            for alias in aliasList:
                logging.info('Replacing alias named %s', alias)
                self.__Es.PutAlias(targetIndex, alias, aliasBody='', force= True)
        else:
            logging.info('No alias found process complete')

    def FindAlias(self, targetIndex):

        aliasData = self.__Es.GetAlias(targetIndex)
        if aliasData == None:
            return None
        else:
            aliasName = aliasData[targetIndex]['aliases']
            aliasList = []
            for key in aliasName.keys():
                aliasList.append(key)
            return aliasList
        
    
    def delete_all_by_query(self, index):
       query = { "query": { "match_all": {} } }
       logging.info("Clearing data from index '%s' for reload", index)
       self.__Es.DeleteByQuery(index, query)


        