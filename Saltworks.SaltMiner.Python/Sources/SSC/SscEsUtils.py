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

# 10/8/21 TD
# Originally RWSSCESutil.py and class SSCESUtils

import logging
import datetime

class SscEsUtils:

    def __init__(self, appSettings, logger=None):

        self.__Logger = logger or logging.getLogger(__name__)
        self.AllSscProjects = []
        self.AllSscProjectCounts = []
        self.AllSscProjectAttrs = []
        self.AllSscProjectIssues = []
        self.__ElasticClient = appSettings.Application.GetElasticClient()

    def GetUpdateQueueDoc(self, processedDateTime = None, projectVersionId = None, updateType = None):
        return {
            'processedDateTime' : '' if not processedDateTime else processedDateTime,
            'projectVersionId' : 0 if not projectVersionId else projectVersionId,
            'updateType' : '' if not updateType else updateType,
            'completedDateTime' : '1900-01-01T00:00:00.000-0000'
        }

    def ensureSSCIndices(self):
        self.__Logger.info('in ensureSSCIndices')
        errs = []
        indices = ["sscprojects", "sscprojcounts", "sscprojattrs", "sscprojattr2", "sscprojscans", "sscprojissues"]
        for i in indices:
            if not self.__ElasticClient.IndexExists(i):
                errs.append(i)
        for e in errs:
            self.__Logger.error("{} index does not exist.".format(e))
        return (len(errs) == 0)


    def elasticHits(self, response):
        try:
            if isinstance(response['hits']['total'], dict) and 'value' in response['hits']['total']:
                return response['hits']['total']['value']
            else:
                return response['hits']['total']
        except:
            #Unable to get the 
            if response['error']['index'] == 'sscprojects':
                self.__Logger.warning('sscprojects does not exist, cant look for old records.')
                return 0
            else:
                self.__Logger.error('Unknown error getting sscprojects from elastic, need to debug')
                return 0

    def __ElasticGetToCollection(self, index, collection, label, scrollSize=1000):
        scroller = self.__ElasticClient.SearchScroll(index, scrollSize=scrollSize)
        self.__Logger.info(f"Total {label} in ES: {scroller.TotalHits}")
        while scroller.Results:
            for item in scroller.Results:
                collection.append(item['_source'])
            scroller.GetNext()

    # GregLook: refactored to use ElasticClient and shared helper instead of direct calls
    def getAllESSSCProjects(self):
        self.__ElasticGetToCollection("sscprojects", self.AllSscProjects, "SSC Projects")

    # GregLook: refactored to use ElasticClient and shared helper instead of direct calls
    def getAllESSSCProjCounts(self):
        self.__ElasticGetToCollection("sscprojcounts", self.AllSscProjectCounts, "SSC Project Counts")

    # GregLook: refactored to use ElasticClient and shared helper instead of direct calls
    def getAllESSSCProjAttrs(self):
        self.__ElasticGetToCollection("sscprojattrs", self.AllSscProjectAttrs, "SSC Project Attributes")

    # GregLook: refactored to use ElasticClient and shared helper instead of direct calls
    def getAllESSSCProjIssues(self):
        self.__ElasticGetToCollection("sscprojissues", self.AllSscProjectIssues, "SSC Project Issues")

    # Finds app versions in app_vuls_ssc that do not exist in sscprojects, and adds any found to the update queue as deletes
    def FindOrphanedSscApplicationVersions(self):
        self.__Logger.info("Starting 'Find Orphaned Ssc App Versions' process")

        es = self.__ElasticClient

        # Query app_vuls_ssc using a bucket query to pull out all the app version ids
        query = {
          "aggs": {
            "avid": {
              "terms": {
                "field": "application_version_id",
                "size": 100000
              }
            }
          },
          "size": 0
        }
        r = es.Search("app_vuls_ssc", query, navToData=False)
        buckets = r['aggregations']['avid']['buckets']
        c = 0
        dc = 0
        for b in buckets:
            if es.Count("sscprojects", { "query": { "term": { "id": { "value": b['key'] } } } }) == 0:
                self.__Logger.info("App version ID %s not found, will add to update queue as a delete.", b['key'])
                es.Index("sscupdatequeue", self.GetUpdateQueueDoc(datetime.datetime.now().strftime("%Y-%m-%dT%H:%M:%S"), b['key'], 'D'))
                dc += 1
            c += 1
            if c % 100 == 0:
                self.__Logger.info("Processed %s of %s", c, len(buckets))

        self.__Logger.info("Processing complete.  Queued %s drop(s)", dc)
