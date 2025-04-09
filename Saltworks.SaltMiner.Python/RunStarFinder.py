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
import time
import json
import traceback
import datetime
from Core.Application import Application
from Core.Application import ElasticClient
from elasticsearch.exceptions import NotFoundError
# Add imports here, ex:
# import Custom.SW.AppVulsWidget
timers = {}


QueryJson = {
    "starRatingIssuesQuery": {
        "aggs": {
            "index": {
                "terms": {
                    "field": "_index",
                    "order": {
                        "_count": "desc"
                    },
                    "size": 100
                },
                "aggs": {
                    "asset_id": {
                        "terms": {
                            "field": "saltminer.asset.source_id",
                            "order": {
                                "_count": "desc"
                            },
                            "size": 9999
                        },
                        "aggs": {
                            "issue_status": {
                                "terms": {
                                    "field": "saltminer.source.issue_status",
                                    "order": {
                                        "_count": "desc"
                                    },
                                    "size": 5,
                                    "shard_size": 25
                                },
                                "aggs": {
                                    "scan_date": {
                                        "terms": {
                                            "field": "saltminer.scan.scan_date",
                                            "order": {
                                                "_count": "desc"
                                            },
                                            "size": 100
                                        },
                                        "aggs": {
                                            "critical": {
                                                "sum": {
                                                    "field": "saltminer.critical"
                                                }
                                            },
                                            "high": {
                                                "sum": {
                                                    "field": "saltminer.high"
                                                }
                                            },
                                            "medium": {
                                                "sum": {
                                                    "field": "saltminer.medium"
                                                }
                                            },
                                            "low": {
                                                "sum": {
                                                    "field": "saltminer.low"
                                                }
                                            }
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
            }
        },
        "size": 0,
        "fields": [],
        "script_fields": {},
        "stored_fields": [
            "*"
        ],
        "runtime_mappings": {},
        "_source": {
            "excludes": []
        },
        "query": {
            "bool": {
                "must": [],
                "filter": [],
                "should": [],
                "must_not": []
            }
        }
    },
    "starRatingAssetsQuery": {
        "aggs": {
            "inv_asset_key": {
                "terms": {
                    "field": "saltminer.inventory_asset.key",
                    "order": {
                        "star_rating": "desc"
                    },
                    "size": 1000
                },
                "aggs": {
                    "star_rating": {
                        "min": {
                            "field": "saltminer.score.star_rating"
                        }
                    }
                }
            }
        },
        "size": 0,
        "script_fields": {},
        "stored_fields": [
            "*"
        ],
        "runtime_mappings": {},
        "query": {
            "bool": {
                "must": [],
                "filter": [],
                "should": [],
                "must_not": []
            }
        }
    }
}


def StartTimer(key):
    timers[key] = time.perf_counter()


def EndTimer(key, prt=True):
    if key in timers.keys() and timers[key]:
        elapsed = time.perf_counter() - timers[key]
        if prt:
            print(f"{key}: {elapsed}")
        return elapsed
    else:
        raise ValueError(f"Invalid timer key '{key}'")


def starFinder():

    starQuery = ec.Search(index="issues_active*",
                          queryBody=queries['starRatingIssuesQuery'], navToData=False)

    assetIdList = {}
    with ec.SearchScroll('assets*', scrollSize=10) as scroller:
        while len(scroller.Results):
            for p in scroller.Results:
                assetIdList[p['_source']['saltminer']['asset']
                            ['source_id']] = p['_source']['id']
            scroller.GetNext()

    for index in starQuery['aggregations']['index']['buckets']:
        newIndex = index['key'].replace("issues_", "assets_")

        for item in index["asset_id"]["buckets"]:
            ratingList = {}
            dateList = []
            try:
                asset_id = assetIdList[item['key']]
                
                for valueSet in item['issue_status']['buckets']:
                    values = valueSet['scan_date']['buckets'][0]
                    scan_date = values['key_as_string']
                    if valueSet['key'] == "Zero":
                        rating = 0
                    
                    if int(values["critical"]["value"]) > 0:
                        rating = 1
                    elif int(values["high"]["value"]) > 0:
                        rating = 2
                    elif int(values["medium"]["value"]) > 0:
                        rating = 3
                    elif int(values["low"]["value"]) > 0:
                        rating = 4
                    else:
                        rating = 5
                      
                    ratingList[scan_date] = rating
                    dateList.append(scan_date)
                        
                sortedList = sorted(dateList, key=lambda d:datetime.datetime.strptime(d, "%Y-%m-%dT%H:%M:%S.%fZ"), reverse=True)
                minRating = ratingList[dateList[0]]

                starRatingDoc = { 
                    "saltminer": {
                        "score": {
                            "star_rating": minRating
                        }
                    }
                }
                ec.UpdateDoc(index=newIndex, docId=asset_id, doc=starRatingDoc)

            except Exception as e:
                logging.critical(
                    "Following issue occurred with process: %s", e)

    logging.info('Assets Updated, starting 30 second delay')
    time.sleep(30)
    logging.info(
        '30 second delay complete resuming with push to inventory assets')
    starAssetsQuery = ec.Search(
        index="assets*", queryBody=queries["starRatingAssetsQuery"], navToData=False)
    try:
        for item in starAssetsQuery['aggregations']['inv_asset_key']['buckets']:
            inv_asset_key = item["key"]
            inv_asset_doc = {
                "score": {
                    "star_rating": int(item["star_rating"]['value'])
                }
            }
            try:
                ec.Get(index='inventory_assets', docId=inv_asset_key)
                ec.UpdateDoc(index='inventory_assets',
                             docId=inv_asset_key, doc=inv_asset_doc, )
            except NotFoundError as e:
                logging.critical(
                    "Following issue occurred with inventory asset process: %s", e)
                continue
    except Exception as e:
        logging.critical(
            "Following issue occurred with inventory asset process: %s", e)


try:
    app = Application()
    ec = ElasticClient(app.Settings)
    queries = QueryJson
    StartTimer("main")
    starFinder()
    logging.info('Process complete')
    EndTimer("main")

except Exception as e:
    error_msg = traceback.format_exc()
    print(error_msg)
    logging.critical(error_msg)
