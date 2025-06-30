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
import os
import json
import time
import datetime
from datetime import timedelta
from Core.Application import ElasticClient
from Core.Application import Application
from Core.ApplicationSettings import ApplicationSettings

class IssueAggregationDocBuilder:
    def __init__(self, settings, aggSettings):
        self._Es = ElasticClient(settings)
        self.formattedAggregations = []
        
        self.aggsettings = aggSettings
        self.needVulnCounts = aggSettings[4]

    def getIndexAggregation(self):

        issuesAgg = self._Es.Search(index=self.aggsettings[0],queryBody=self.aggregationQuery(), size=10000, navToData=False)

        for asset in issuesAgg['aggregations']['1']['buckets']:
            agg1 = asset['key']
            for id_bucket in asset['2']['buckets']:
                agg2 = id_bucket['key']
                for metrics in id_bucket['3']['buckets']:
                    if metrics['key_as_string']:
                        agg3 = metrics['key_as_string']
                    else:
                        agg3 = metrics['key']
                    if self.needVulnCounts:
                        critical_count = metrics['critical']['value']
                        high_count = metrics['high']['value']
                        medium_count = metrics['medium']['value']
                        low_count = metrics['low']['value']
                        info_count = metrics['info']['value']
                    else:
                        critical_count = None
                        high_count = None
                        medium_count = None
                        low_count = None
                        info_count = None

                    self.formatAggregation(agg1=agg1, agg2=agg2, agg3=agg3 , critical=critical_count, high=high_count, medium=medium_count, low=low_count, info=info_count)

    def formatAggregation(self, agg1=None, agg2=None, agg3=None, critical=None, high=None, medium=None, low=None, info=None):
        aggregationDoc = self.aggregationDoc()
        aggregationDoc['name']= agg1
        aggregationDoc['id']= agg2
        aggregationDoc['date']= agg3
        if self.needVulnCounts:
            aggregationDoc['critical']= critical
            aggregationDoc['high']= high
            aggregationDoc['medium']= medium
            aggregationDoc['low']= low
            aggregationDoc['info']= info
        self.formattedAggregations.append(aggregationDoc)


    def getFormattedAggregations(self):
        logging.info('Getting issues aggregation')
        self.getIndexAggregation()
        logging.info('Aggregation formatting complete')
        return self.formattedAggregations

    def aggregationDoc(self):
        aggregationDoc = {
            self.aggsettings[0]:None,
            self.aggsettings[1]:None,
            self.aggsettings[2]:None
        }
        if self.needVulnCounts:
            aggregationDoc['critical']=None,
            aggregationDoc['high']=None,
            aggregationDoc['medium']=None,
            aggregationDoc['low']=None,
            aggregationDoc['info']=None
        
        return aggregationDoc

    def aggregationQuery(self):
        
        aggQuery = {
            "aggs": {
                "1": {
                    "terms": {
                        "field": self.aggsettings[1],
                        "order": {
                            "critical": "desc"
                        },
                        "size": 1000
                    },
                    "aggs": {
                        "critical": {
                            "sum": {
                                "field": "saltminer.critical"
                            }
                        },
                        "2": {
                            "terms": {
                                "field": self.aggsettings[2],
                                "order": {
                                    "_key": "desc"
                                },
                                "size": 1000
                            },
                            "aggs": {
                                "3": {
                                    "terms": {
                                        "field": self.aggsettings[3],
                                        "order": {
                                            "_key": "desc"
                                        },
                                        "size": 10,
                                        "shard_size": 25
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
                                        },
                                        "info": {
                                            "sum": {
                                                "field": "saltminer.info"
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
            "fields": [

            ],
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
                    "filter": [
                    ],
                    "should": [],
                    "must_not": []
                }
            }
        }
        return aggQuery
    
