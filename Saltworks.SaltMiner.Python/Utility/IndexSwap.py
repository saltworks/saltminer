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
        
  