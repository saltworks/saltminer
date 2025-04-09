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

# 10/8/21 TD
# Originally RWSSCESutil.py and class SSCESUtils

import logging
import datetime

class SscEsUtils:

    def __init__(self, appSettings):

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
        logging.info('in ensureSSCIndices')
        errs = []
        indices = ["sscprojects", "sscprojcounts", "sscprojattrs", "sscprojattr2", "sscprojscans", "sscprojissues"]
        for i in indices:
            if not self.__ElasticClient.IndexExists(i):
                errs.append(i)
        for e in errs:
            logging.error("{} index does not exist.".format(e))
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
                logging.warning('sscprojects does not exist, cant look for old records.')
                return 0
            else:
                logging.ERROR('Unknown error getting sscprojects from elastic, need to debug')
                return 0

    def __ElasticGetToCollection(self, index, collection, label, scrollSize=1000):
        scroller = self.__ElasticClient.SearchScroll(index, scrollSize=scrollSize)
        logging.info(f"Total {label} in ES: {scroller.TotalHits}")
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
        logging.info("Starting 'Find Orphaned Ssc App Versions' process")

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
                logging.info("App version ID %s not found, will add to update queue as a delete.", b['key'])
                es.Index("sscupdatequeue", self.GetUpdateQueueDoc(datetime.datetime.now().strftime("%Y-%m-%dT%H:%M:%S"), b['key'], 'D'))
                dc += 1
            c += 1
            if c % 100 == 0:
                logging.info("Processed %s of %s", c, len(buckets))

        logging.info("Processing complete.  Queued %s drop(s)", dc)
