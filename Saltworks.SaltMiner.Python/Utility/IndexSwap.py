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
import json
import time
from Core.Application import Application
from Core.ElasticClient import ElasticClient



app = Application()

class IndexSwap:
    def __init__(self, Source = "ISSource", Dest= "ISDest") -> None:
        
        self.esSource = ElasticClient(app.Settings, Source)
        self.esDest = ElasticClient(app.Settings, Dest)
        self.TargetIndex = None
        
        self.DataToSend = []
        self.SourceData = []
        
    def runIndexSwap(self, TargetIndex, withMapping):
        self.TargetIndex = TargetIndex
        
        logging.info("Running indexSwap for index %s", self.TargetIndex)
        if withMapping == True:
            self.mapping = self.esSource.GetIndexMapping(self.TargetIndex)
            self.esDest.MapIndexWithMapping(self.TargetIndex, self.mapping[self.TargetIndex], force=True)
        else: 
            self.mapping= None
        query= {
            "query": {
              "match_all": {}
            },
            "sort": [
              {
                "_id": {
                  "order": "desc"
                }
              }
            ]}
        with self.esSource.SearchScroll(self.TargetIndex,queryBody=query , scrollTimeout=None) as scroller:
            while len(scroller.Results):
                for p in scroller.Results:
                   
                    self.SourceData.append(p)
                    
                if len(self.SourceData) >= 1000:
                    logging.info("Sending data")
                    self.esDest.BulkInsert(self.SourceData)
                    time.sleep(0.25)
                    self.SourceData = []
                else:
                    continue
            
                scroller.GetNext()
            if len(self.SourceData) > 0:
                logging.info("Sending last of data")   
                self.esDest.BulkInsert(self.SourceData)
        
  