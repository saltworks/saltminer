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

import logging
import os
import time
import datetime

from Core.Application import Application

SHARD_COUNT = 2
BULK_BATCH_SIZE = 5000
REPORT_EVERY = 1000
INDEX_NAME = 'fortify_scan_rulepacks'
SSC_SRC_INDEX_NAME = 'sscprojscans'
SSC_QUERY = {
    "_source": [ "projectVersionId", "id", "projectName", "versionName", "artifactId", "lastUpdated", "lastScanDate", "uploadDate", "artifactUploadDate", "rulepacks" ],
    "sort": [ "lastUpdated" ]
}
SSC_AGG_QUERY = {
    "_source": False,
    "aggs": {
        "pvid": {
            "terms": { "field": "projectVersionId", "size": 50000 },
            "aggs": { "max_scan_date": { "max": { "field": "lastScanDate" } } }
        }
    }
}
MAPPING = {
    "settings": {
        "number_of_replicas": 0,
        "number_of_shards": SHARD_COUNT
    },
    "mappings": {
        "properties": {
            "id": { "type": "keyword", "fields": { "text": { "type": "text" } } },
            "asset_id": { "type": "keyword", "fields": { "text": { "type": "text" } } },
            "scan_id": { "type": "keyword", "fields": { "text": { "type": "text" } } },
            "asset_name": { "type": "keyword", "fields": { "text": { "type": "text" } } },
            "asset_version": { "type": "keyword", "fields": { "text": { "type": "text" } } },
            "artifact_id": { "type": "keyword", "fields": { "text": { "type": "text" } } },
            "rulepack_id": { "type": "keyword", "fields": { "text": { "type": "text" } } },
            "rulepack_name": { "type": "keyword", "fields": { "text": { "type": "text" } } },
            "rulepack_version": { "type": "keyword", "fields": { "text": { "type": "text" } } },
            "rulepack_language": { "type": "keyword", "fields": { "text": { "type": "text" } } },
            "saltminer_updated": { "type": "date" },
            "last_scan_date": { "type": "date" },
            "artifact_upload_date": { "type": "date" },
            "upload_date": { "type": "date" },
            "asset_last_scan_date": { "type": "date" },
            "is_latest_scan": { "type": "boolean" },
            "is_core":  { "type": "boolean" },
            "source": { "type": "keyword", "fields": { "text": { "type": "text" } } }
        }
    }
}

class GenScanRulepacks():
    '''GenScanRulepacks utility class - generates fn1_scan_rulepacks data'''    
    def __init__(self, settings):
        self.__Es = settings.Application.GetElasticClient()
        self.__Prog = os.path.splitext(os.path.basename(__file__))[0]
        self.__BulkDocs = []

    def __SendBulkDoc(self, index=None, doc=None):
        '''Queue a bulk insert doc'''
        finishIt = bool(not doc)
        if doc and index:
            self.__BulkDocs.append(self.__Es.BulkInsertDocument(index, doc))
        if len(self.__BulkDocs) >= BULK_BATCH_SIZE or (finishIt and len(self.__BulkDocs) > 0):
            logging.info("Bulk queue send (%s items)", len(self.__BulkDocs))
            self.__Es.BulkInsert(self.__BulkDocs)
            self.__BulkDocs = []

    def __FormatDate(self, dt):
        return datetime.datetime.fromisoformat(dt).strftime("%Y-%m-%dT%H:%M:%SZ")

    def __WriteSsc(self, sscScan, sscLastScan):
        '''Write doc(s) for a single SSC scan'''
        for rulepack in sscScan['rulepacks']:
            rec = {
                "id": f"{sscScan['projectVersionId']}-{sscScan['id']}-{rulepack['guid']}",
                "asset_id": sscScan['projectVersionId'],
                "scan_id": sscScan['id'],
                "asset_name": sscScan['projectName'],
                "asset_version": sscScan['versionName'],
                "artifact_id": sscScan['artifactId'],
                "rulepack_id": rulepack['guid'],
                "rulepack_name": rulepack['name'],
                "rulepack_version": rulepack['version'],
                "rulepack_language": rulepack['language'] if 'language' in rulepack else None,
                "saltminer_updated": self.__FormatDate(sscScan['lastUpdated']),
                "last_scan_date": self.__FormatDate(sscScan['lastScanDate']),
                "artifact_upload_date": self.__FormatDate(sscScan['artifactUploadDate']),
                "upload_date": self.__FormatDate(sscScan['uploadDate']),
                "asset_last_scan_date": sscLastScan,
                "is_core": "core" in rulepack['name'] or "Core" in rulepack['name'],
                "is_latest_scan": sscLastScan == self.__FormatDate(sscScan['lastScanDate']),
                "source": "SSC"
            }
            logging.debug("[%s - WriteOne] Bulk queue doc for app '%s', ver '%s'", self.__Prog, sscScan['projectName'], sscScan['versionName'])
            self.__SendBulkDoc(INDEX_NAME, rec)

    def Run(self):
        '''Run'''
        # SSC
        try:
            # index mapping and setup
            self.__Es.MapIndexWithMapping(INDEX_NAME, MAPPING, False)
            srt = { "sort": [ { "saltminer_updated": { "order": "desc" } } ] }
            logging.info("[%s - Setup] Running inital queries for SSC scans...", self.__Prog)

            # find latest existing date
            dto = self.__Es.Search(INDEX_NAME, srt, 1, navToData=True)
            startDate = 0 if not dto else dto[0]['_source']['saltminer_updated']

            # get last scan dates
            qry = { "range": { "lastUpdated": { "gte": startDate } } }
            body = SSC_AGG_QUERY
            body['query'] = qry
            rsp = self.__Es.Search(SSC_SRC_INDEX_NAME, body, navToData=False)
            sscLastScans = {}
            if rsp and 'aggregations' in rsp and 'pvid' in rsp['aggregations']:
                for b in rsp['aggregations']['pvid']['buckets']:
                    sscLastScans[b['key']] = b['max_scan_date']['value_as_string']

            # main processing
            count = 0
            body = SSC_QUERY
            body['query'] = qry
            scroller = self.__Es.SearchScroll(SSC_SRC_INDEX_NAME, body, 5000, scrollTimeout=None)
            while len(scroller.Results):
                for scanDto in scroller.Results:
                    if 'rulepacks' in scanDto['_source']:
                        lastScan = self.__FormatDate(sscLastScans[str(scanDto['_source']['projectVersionId'])])
                        self.__WriteSsc(scanDto['_source'], lastScan)
                        count += 1
                        if count % REPORT_EVERY == 0:
                            logging.info("[%s - SSC] Rulepack entries written for %s scans so far.", self.__Prog, count)
                scroller.GetNext()
            logging.info("[%s - SSC] Wrote rulepack entries for a total of %s scans.", self.__Prog, count)
            self.__SendBulkDoc() # complete bulk send for remainder

            # update asset last scan, is latest fields for all
            count = 0
            for sid, lastScan1 in sscLastScans.items():
                lastScan = self.__FormatDate(lastScan1)
                body = {
                    "query": { "bool": { "must": [
                        { "term": { "asset_id": { "value": str(sid) } } },
                        { "term": { "source": { "value": "SSC" } } }
                    ] } },
                    "script": {
                    "source": f"""
                      ctx._source.asset_last_scan_date = '{lastScan}';
                      ZonedDateTime a = ZonedDateTime.parse(ctx._source.last_scan_date);
                      ZonedDateTime b = ZonedDateTime.parse('{lastScan}');
                      ctx._source.is_latest_scan = a.equals(b);
                    """,
                    "lang": "painless"
                    }
                }
                self.__Es.UpdateByQuery(INDEX_NAME, body, noWait=True, ignoreConflicts=True)
                count += 1
                if count % 250 == 0:
                    logging.info("[%s - SSC] Updating scans, %s assets.", self.__Prog, count)
                    time.sleep(5)

            logging.info("[%s - SSC] Updated scans, %s total assets complete.  Processing complete.", self.__Prog, count)
        except Exception as e:
            logging.critical("[%s - SSC] Exception: [%s] %s", self.__Prog, type(e).__name__, e)
            raise


app = Application()
util = GenScanRulepacks(app.Settings)
util.Run()
