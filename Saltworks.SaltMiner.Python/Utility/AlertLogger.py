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

import datetime

alMapping = {
  "settings": {
    "number_of_shards": 1,
    "number_of_replicas": 1,
    "analysis": {
      "normalizer": {
        "lc_normalizer": {
          "type": "custom",
          "char_filter": [],
          "filter": [ "lowercase", "asciifolding" ]
        }
      }
    }
  },
  "mappings": {
    "properties": {
      "timestamp": { "type": "date" },
      "tag": { "type": "keyword" },
      "status": { "type": "keyword" },
      "data": { "type": "text" }
    }
  }
}
alIndex = "alertlog"

class AlertLogger(object):
    """
    Used to write an alert log entry locally
    """
    def __init__(self, app):
        self.__Es = app.GetElasticClient()
        self.__Es.MapIndexWithMapping(alIndex, alMapping, False)

    def Log(self, tag, status, data=None):
        if data:
            doc = { "id": tag, "timestamp": datetime.datetime.utcnow().isoformat(), "status": status, "data": data }
        else:
            doc = { "id": tag, "timestamp": datetime.datetime.utcnow().isoformat(), "status": status }
        self.__Es.IndexWithId(alIndex, tag, doc)
