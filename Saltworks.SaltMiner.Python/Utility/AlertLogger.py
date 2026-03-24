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
