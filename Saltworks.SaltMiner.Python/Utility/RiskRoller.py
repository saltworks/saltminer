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
import time
import os
import json
import uuid
from datetime import datetime as dt
from datetime import timedelta
from elasticsearch.exceptions import NotFoundError
from Core.Application import ElasticClient
from Core.ApplicationSettings import ApplicationSettings


class RiskRoller(object):
    """Class for the RiskRoller Object"""

    def __init__(self, settings, trueTime, delayTime):
        if not isinstance(settings, ApplicationSettings):
            raise ValueError(
                "Parameter 'settings' must be of type 'ApplicationSettings'.")
        self._Es = ElasticClient(settings)
        self.riskRollerDocuments = riskRollerDocuments(self)
        self.queries = self.riskRollerDocuments.riskRollerQuery()
        self.trueTime = trueTime
        self.__Prog = os.path.splitext(os.path.basename(__file__))[0]
        self.runFunctions = True
        self.oneUnknownSent = False

        if self.indicesExist() == False:
            self.issueIndex = 'issues_test_risk_roller'
            self.assetIndex = 'assets_test_risk_roller'
            self.invAssetIndex = 'inventory_assets_test_risk_roller'
            logging.error('IF RUNNING RunTestRiskRollup PLEASE DISREGARD ERROR \nRequired indices not found, please make sure you have the required indices to run RiskRoller: issues_active*, assets*, inventory_assets')

        else:
            self.issueIndex = 'issues_active*'
            self.assetIndex = 'assets*'
            self.invAssetIndex = 'inventory_assets'
            self.assetIdList = self.getassetIdList()

        self.delayTime = delayTime

    def testFunctions(self, method):
        self.changePipelines(conditionalChange='return true;')
        self.issueIndex = 'issues_test_risk_roller'
        self.assetIndex = 'assets_test_risk_roller'
        self.invAssetIndex = 'inventory_assets_test_risk_roller'
        self.addTestDocsTOSm()
        time.sleep(5)
        self.assetIdList = self.getassetIdList()
        
        self.riskRollup(method=method, isTestFunctions=True)
        self.checkTestRollup(method=method)
        self.deleteTestIndices()
        self.changePipelines(conditionalChange='return false;')
        

    def checkTestRollup(self, method):
        issuesData = self._Es.Search(index=self.issueIndex)
        assetsData = self._Es.Search(index=self.assetIndex)
        invAssetsData = self._Es.Search(index=self.invAssetIndex)
        logging.info('Issue Test Document result: %s, expected result: %s', issuesData[0]['_source']['saltminer']['score'][method]['prod'], 64)
        if issuesData[0]['_source']['saltminer']['score'][method]['prod'] > 0:
            logging.info('Issue test document passed quality check for %s', method)

        logging.info('Asset Test Document result: %s, expected result: %s', issuesData[0]['_source']['saltminer']['score'][method]['prod'], 64)
        if assetsData[0]['_source']['saltminer']['score'][method]['prod'] > 0:
            logging.info('Asset test document passed quality check for %s', method)

        logging.info('Inventory Asset Test Document result: %s, expected result: %s', issuesData[0]['_source']['saltminer']['score'][method]['prod'], 64) 
        if invAssetsData[0]['_source']['saltminer']['score'][method]['prod'] > 0:
            logging.info('Inventory Asset test document passed quality check for %s', method)

        
    def changePipelines(self, conditionalChange):
        
        issuesData = self._Es.GetPipeline(pipelineName= "saltminer-issue-pipeline")
        assetsData = self._Es.GetPipeline(pipelineName= "saltminer-asset-pipeline")
        for processor in issuesData['saltminer-issue-pipeline']['processors']:

            for key in processor.keys():
                if key == 'pipeline':
                    if processor[key]['name'] == 'saltminer-issues-risk-roller-pipeline':

                        processor[key]['if'] = conditionalChange
                        self._Es.PutPipeline(id='saltminer-issue-pipeline', body=issuesData['saltminer-issue-pipeline'])
        for processor in assetsData['saltminer-asset-pipeline']['processors']:

            for key in processor.keys():
                if key == 'pipeline':
                    if processor[key]['name'] == 'saltminer-asset-risk-roller-pipeline':

                        processor[key]['if'] = conditionalChange
                        self._Es.PutPipeline(id='saltminer-asset-pipeline', body=assetsData['saltminer-asset-pipeline'])
    def addTestDocsTOSm(self):
        self.indexList = [self.issueIndex, self.assetIndex, self.invAssetIndex]
        testDocs = self.riskRollerDocuments.testDocs()
        for index in self.indexList:
            self._Es.Index(index=index, doc=testDocs[index])
            self._Es.UpdateByQuery(index=index, queryBody={
                "query": {"match_all": {}}})

    def standardMappingDetermination(self, index):
        standardIndex = ['issues_test_risk_roller', 'assets_test_risk_roller']
        mappings = self.riskRollerDocuments.testIndexMappings()
        if index in standardIndex:
            return mappings['standard']
        return mappings['non-standard']

    def deleteTestIndices(self):
        for index in self.indexList:
            self._Es.DeleteIndex(index=index)

    def putTestSettings(self, index):
        settingsDocs = self.riskRollerDocuments.settingsDocs()

        self._Es.PutSettings(index=index, settings=settingsDocs[index])

    def riskRollup(self, method, isTestFunctions=False):
        if self.runFunctions == False and isTestFunctions == False:
            logging.error('Unable to continue due to lack of required indices')
            return

        else:
            time.sleep(3)
            self.rollIssuesToAssets(method=method)
            self.implementDelay()
            logging.info("Beginning Push from %s to %s",
                         self.assetIndex, self.invAssetIndex)
            self.rollAssetsToInvAssets(method=method)
            logging.info('Push to %s has completed', self.invAssetIndex)
            self.implementDelay()
            logging.info('Starting time check')
            self.docTimeCheck(method=method)
            logging.info('Time Check complete')

    def rollIssuesToAssets(self, method):

        issuesIndex = self._Es.Search(
            index=self.issueIndex, queryBody=self.queries[f"{method}IssuesQuery"], navToData=False)
        logging.info("Beginning push from Issues to Assets for %s", method)
        for item in issuesIndex['aggregations']['index']['buckets']:
            newIndex = item['key'].replace("issues_", "assets_")

            for bucket in item['id']['buckets']:
                try:
                    timeNow = dt.now()
                    assetID = self.assetIdList[bucket['key']]
                    prodValue = int(bucket['prod']['value'])
                    devValue = int(bucket['dev']['value'])
                    testValue = int(bucket['test']['value'])
                    overallValue = int(bucket['overall']['value'])
                    rollerDoc = self.riskRollerDocuments.rollerDoc(
                        method=method)
                    rollerDoc['saltminer']['score'][method]['prod'] = prodValue
                    rollerDoc['saltminer']['score'][method]['dev'] = devValue
                    rollerDoc['saltminer']['score'][method]['test'] = testValue
                    rollerDoc['saltminer']['score'][method]['overall'] = overallValue
                    rollerDoc['saltminer']['score'][method]['last_updated'] = timeNow.strftime(
                        "%Y-%m-%dT%H:%M:%S.%fZ")
                    self._Es.UpdateDoc(
                        index=newIndex, docId=assetID, doc=rollerDoc)
                except Exception as e:
                    logging.error(
                        "Following issue occurred with process: %s", e)

    def rollAssetsToInvAssets(self, method):

        assetsIndex = self._Es.Search(
            index=self.assetIndex, queryBody=self.queries[f"{method}AssetQuery"], navToData=False)
        invAssetIdList = self.inventoryAssetKeyToID(data=assetsIndex)
        for item in assetsIndex['aggregations']['asset_key']['buckets']:

            docId = invAssetIdList[item['key']]
            rollerDoc = self.riskRollerDocuments.rollerDoc(method=method)
            if docId == 'unknown':

                if self.oneUnknownSent == False:
                    docId = invAssetIdList['unknown']['id']

                    inv_AssetProdValue = invAssetIdList['unknown']['prod']
                    inv_AssetDevValue = invAssetIdList['unknown']['dev']
                    inv_AssetTestValue = invAssetIdList['unknown']['test']
                    inv_AssetOverallValue = invAssetIdList['unknown']['overall']
                    rollerDoc['name'] = 'unknown'
                    isUnknown = True
                else:
                    continue

            else:
                isUnknown = False
                inv_AssetProdValue = item['prod']['value']
                inv_AssetDevValue = item['dev']['value']
                inv_AssetTestValue = item['test']['value']
                inv_AssetOverallValue = item['overall']['value']
            timeNow = dt.now()

            rollerDoc['saltminer']['score'][method]['prod'] = inv_AssetProdValue
            rollerDoc['saltminer']['score'][method]['dev'] = inv_AssetDevValue
            rollerDoc['saltminer']['score'][method]['test'] = inv_AssetTestValue
            rollerDoc['saltminer']['score'][method]['overall'] = inv_AssetOverallValue
            rollerDoc['saltminer']['score'][method]['last_updated'] = timeNow.strftime(
                "%Y-%m-%dT%H:%M:%S.%fZ")

            try:
                if isUnknown == False:
                    self._Es.UpdateDoc(index=self.invAssetIndex,
                                       docId=docId, doc=rollerDoc)
                else:
                    if docId:
                        self._Es.UpdateDoc(index=self.invAssetIndex,
                                           docId=docId, doc=rollerDoc)

                    else:
                        self._Es.Index(index=self.invAssetIndex, doc=rollerDoc)
                    self.oneUnknownSent == True
                    isUnknown = False

            except NotFoundError as e:

                logging.critical(
                    "Following issue occurred with process: %s", e)

    def indicesExist(self):
        issues = self.issuesExist()
        assets = self.assetsExist()
        inventory_assets = self.inventoryAssetsExist()
        if issues == False or assets == False or inventory_assets == False:
            self.runFunctions = False
            return False

    def issuesExist(self):
        if self._Es.Search('issues_active*'):
            return True
        else:
            return False

    def assetsExist(self):
        if self._Es.Search('assets*'):
            return True
        else:
            return False

    def inventoryAssetsExist(self):
        if self._Es.Search('inventory_assets'):
            return True
        else:
            return False

    def docTimeCheck(self, method):
        invAssetTimeQuery = {
            "_source": [
                "saltminer.inventory_asset.key",
                f"saltminer.score.{method}.last_updated"
            ],
            "size": 5000
        }

        timeCheck = self._Es.Search(index=self.invAssetIndex,
                                    queryBody=invAssetTimeQuery, size=5000)
        # verifying time, if time is before start of job update time to now and set values to 0
        # MAKE TIME CHECK FUNCTON
        for doc in timeCheck:

            try:
                if len(doc['_source']) == 0:
                    continue
                if dt.strptime(doc['_source']['saltminer']['score'][method]['last_updated'], "%Y-%m-%dT%H:%M:%S.%fZ") < self.trueTime:
                    timeID = doc['_id']
                    # add updated time

                    rollerDoc = rollerDoc = self.riskRollerDocuments.rollerDoc(
                        method=method)
                    rollerDoc['saltminer']['score'][method]['prod'] = 0
                    rollerDoc['saltminer']['score'][method]['dev'] = 0
                    rollerDoc['saltminer']['score'][method]['test'] = 0
                    rollerDoc['saltminer']['score'][method]['overall'] = 0
                    rollerDoc['saltminer']['score'][method]['last_updated'] = self.timeNow.strftime(
                        "%Y-%m-%dT%H:%M:%S.%fZ")

                    self._Es.UpdateDoc(index=self.invAssetIndex,
                                       docId=timeID, doc=rollerDoc)

            except Exception as e:
                logging.error(
                    "Following issue occurred with process: %s", e)

    def inventoryAssetKeyToID(self, data):
        idList = {'unknown': {
            "id": self.getUnknownId(),
            'prod': 0.0,
            'dev': 0.0,
            'test': 0.0,
            'overall': 0.0
        }}

        dataBuckets = data['aggregations']['asset_key']['buckets']
        for item in dataBuckets:
            idQuery = {
                'query': {
                    'bool': {
                        'must': {
                            'match': {
                                "key": item['key']
                            }
                        }
                    }
                }
            }
            idSearch = self._Es.Search(
                index=self.invAssetIndex, queryBody=idQuery)
            if len(idSearch) == 0:
                idList[item['key']] = 'unknown'
                idList['unknown']['prod'] += item['prod']['value']
                idList["unknown"]['dev'] += item['dev']['value']
                idList['unknown']['test'] += item['test']['value']
                idList['unknown']['overall'] += item['overall']['value']
            else:
                idList[item['key']] = idSearch[0]['_id']
        return (idList)

    def getUnknownId(self):

        unknownIdSearch = self._Es.Search(index=self.invAssetIndex, size=5000)

        for inventory_asset in unknownIdSearch:
            try:
                if inventory_asset['_source']['name'] == 'unknown':

                    return inventory_asset['_id']

            except KeyError:
                continue
        return None

    def getassetIdList(self):
        assetIdList = {}
        if self.assetsExist() == True:
            with self._Es.SearchScroll(self.assetIndex, scrollSize=10) as scroller:
                while len(scroller.Results):

                    for p in scroller.Results:
                        assetIdList[p['_source']['saltminer']
                                    ['asset']['source_id']] = p['_id']

                    scroller.GetNext()
            return assetIdList
        elif self.assetsExist() == False:
            self.runFunctions = False
        
    def implementDelay(self):
        logging.info("Starting %s second delay", self.delayTime)
        time.sleep(self.delayTime)
        logging.info("%s second delay has completed", self.delayTime)


class riskRollerDocuments:
    def __init__(self, riskRoller):
        self.riskRoller = riskRoller

    def testDocs(self):
        testDocs = {
            'issues_test_risk_roller': {
                'saltminer': {
                    'asset': {
                        'source_id': "RRTEST"
                    },
                    'critical': 1
                }
            },
            'assets_test_risk_roller': {
                'saltminer': {
                    'asset': {
                        'source_id': "RRTEST"
                    },
                    "inventory_asset": {
                        "key": "RRTEST"
                    }
                }
            },
            'inventory_assets_test_risk_roller': {
                'key': 'RRTEST'
            }}
        return testDocs

    def settingsDocs(self):

        settingsDocs = {
            'issues_test_risk_roller': {
                "settings": {
                    "index": {
                        "default_pipeline": "saltminer-issue-pipeline",
                        "analysis": {
                            "analyzer": {
                                "lc_keyword": {
                                    "filter": [
                                        "uppercase"
                                    ],
                                    "tokenizer": "keyword"
                                }
                            }
                        },
                        "number_of_shards": "2",
                        "number_of_replicas": "0"
                    }
                }
            },
            'assets_test_risk_roller': {
                "settings": {
                    "index": {
                        "default_pipeline": "saltminer-asset-pipeline",
                        "analysis": {
                            "analyzer": {
                                "lc_keyword": {
                                    "filter": [
                                        "uppercase"
                                    ],
                                    "tokenizer": "keyword"
                                }
                            }
                        },
                        "number_of_shards": "1",
                        "number_of_replicas": "0"

                    }}
            },
            'inventory_assets_test_risk_roller': {
                'settings': {
                    "index": {
                        "analysis": {
                            "analyzer": {
                                "lc_keyword": {
                                    "filter": [
                                        "uppercase"
                                    ],
                                    "tokenizer": "keyword"
                                }
                            }
                        },
                        "number_of_shards": "1",
                        "number_of_replicas": "0"
                    }
                }}
        }
        return settingsDocs

    def testIndexMappings(self):

        testIndexMappings = {
            'standard': {
                'mappings': {
                    "properties": {
                        "saltminer": {
                            "properties": {
                                "score": {
                                    "properties": {
                                        "pass_fail_reviewed": {
                                            "type": "date"
                                        },
                                        "compliance": {
                                            "properties": {
                                                "total": {
                                                    "type": "integer"
                                                },
                                                "prod": {
                                                    "type": "integer"
                                                },
                                                "dev": {
                                                    "type": "integer"
                                                },
                                                "test": {
                                                    "type": "integer"
                                                },
                                                "overall": {
                                                    "type": "integer"
                                                }
                                            }
                                        },
                                        "star_rating": {
                                            "type": "integer"
                                        },
                                        "risk": {
                                            "properties": {
                                                "total": {
                                                    "type": "integer"
                                                },
                                                "prod": {
                                                    "type": "integer"
                                                },
                                                "dev": {
                                                    "type": "integer"
                                                },
                                                "test": {
                                                    "type": "integer"
                                                },
                                                "overall": {
                                                    "type": "integer"
                                                }
                                            }
                                        }
                                    }
                                }}}}}},
            'non-standard': {
                "mappings": {
                    "properties": {
                        "score": {
                            "properties": {
                                "compliance": {
                                    "properties": {
                                        "total": {
                                            "type": "integer"
                                        },
                                        "prod": {
                                            "type": "integer"
                                        },
                                        "dev": {
                                            "type": "integer"
                                        },
                                        "test": {
                                            "type": "integer"
                                        },
                                        "overall": {
                                            "type": "integer"
                                        }
                                    }
                                },
                                "star_rating": {
                                    "type": "integer"
                                },
                                "risk": {
                                    "properties": {
                                        "total": {
                                            "type": "integer"
                                        },
                                        "prod": {
                                            "type": "integer"
                                        },
                                        "dev": {
                                            "type": "integer"
                                        },
                                        "test": {
                                            "type": "integer"
                                        },
                                        "overall": {
                                            "type": "integer"
                                        }
                                    }
                                }
                            }
                        },
                        "last_updated": {
                            "type": "date"
                        }
                    }
                }
            }
        }
        return testIndexMappings

    def rollerDoc(self, method):
        rollerDoc = {
            'saltminer': {
                'score': {
                    method: {
                        'prod': None,
                        'dev': None,
                        'test': None,
                        'overall': None,
                        'last_updated': None,

                    }
                }
            }
        }
        return rollerDoc

    def riskRollerQuery(self):

        query = {
            "complianceIssuesQuery": {
                "aggs": {
                    "index": {
                        "terms": {
                            "field": "_index",
                            "order": {
                                "prod": "desc"
                            },
                            "size": 10,
                            "shard_size": 25
                        },
                        "aggs": {
                            "prod": {
                                "sum": {
                                    "field": "saltminer.score.compliance.prod"
                                }
                            },
                            "id": {
                                "terms": {
                                    "field": "saltminer.asset.source_id",
                                    "order": {
                                        "prod": "desc"
                                    },
                                    "size": 10000
                                },
                                "aggs": {
                                    "prod": {
                                        "sum": {
                                            "field": "saltminer.score.compliance.prod"
                                        }
                                    },
                                    "dev": {
                                        "sum": {
                                            "field": "saltminer.score.compliance.dev"
                                        }
                                    },
                                    "test": {
                                        "sum": {
                                            "field": "saltminer.score.compliance.test"
                                        }
                                    },
                                    "overall": {
                                        "sum": {
                                            "field": "saltminer.score.compliance.overall"
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
                        "filter": [
                        ],
                        "should": [],
                        "must_not": []
                    }
                }
            },
            "complianceAssetQuery": {
                "aggs": {
                    "asset_key": {
                        "terms": {
                            "field": "saltminer.inventory_asset.key",
                            "order": {
                                "_key": "desc"
                            },
                            "size": 5000
                        },
                        "aggs": {
                            "prod": {
                                "sum": {
                                    "field": "saltminer.score.compliance.calculated.prod"
                                }
                            },
                            "dev": {
                                "sum": {
                                    "field": "saltminer.score.compliance.calculated.dev"
                                }
                            },
                            "test": {
                                "sum": {
                                    "field": "saltminer.score.compliance.calculated.test"
                                }
                            },
                            "overall": {
                                "sum": {
                                    "field": "saltminer.score.compliance.calculated.overall"
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
            },
            "complianceUnknownQuery": {
                "query": {
                    "bool": {
                        "must": [
                            {
                                "exists": {
                                    "field": "saltminer.score.compliance.calculated.prod"
                                }
                            },
                            {
                                "exists": {
                                    "field": "saltminer.score.compliance.calculated.dev"
                                }
                            },
                            {
                                "exists": {
                                    "field": "saltminer.score.compliance.calculated.test"
                                }
                            },
                            {
                                "exists": {
                                    "field": "saltminer.score.compliance.calculated.overall"
                                }
                            },
                            {
                                "bool": {
                                    "must_not": {
                                        "exists": {
                                            "field": "inventory_asset.key"
                                        }
                                    }
                                }
                            }
                        ]
                    }
                },
                "_source": [
                    "saltminer.inventory_asset.key",
                    "saltminer.score.compliance.prod",
                    "saltminer.score.compliance.dev",
                    "saltminer.score.compliance.test",
                    "saltminer.score.compliance.overall"
                ],
                "size": 5000
            },
            "riskIssuesQuery": {
                "aggs": {
                    "index": {
                        "terms": {
                            "field": "_index",
                            "order": {
                                "prod": "desc"
                            },
                            "size": 10,
                            "shard_size": 25
                        },
                        "aggs": {
                            "prod": {
                                "sum": {
                                    "field": "saltminer.score.risk.prod"
                                }
                            },
                            "id": {
                                "terms": {
                                    "field": "saltminer.asset.source_id",
                                    "order": {
                                        "prod": "desc"
                                    },
                                    "size": 10000
                                },
                                "aggs": {
                                    "prod": {
                                        "sum": {
                                            "field": "saltminer.score.risk.prod"
                                        }
                                    },
                                    "dev": {
                                        "sum": {
                                            "field": "saltminer.score.risk.dev"
                                        }
                                    },
                                    "test": {
                                        "sum": {
                                            "field": "saltminer.score.risk.test"
                                        }
                                    },
                                    "overall": {
                                        "sum": {
                                            "field": "saltminer.score.risk.overall"
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
                        "filter": [
                        ],
                        "should": [],
                        "must_not": []
                    }
                }
            },
            "riskAssetQuery": {
                "aggs": {
                    "asset_key": {
                        "terms": {
                            "field": "saltminer.inventory_asset.key",
                            "order": {
                                "_key": "desc"
                            },
                            "size": 5000
                        },
                        "aggs": {
                            "prod": {
                                "sum": {
                                    "field": "saltminer.score.risk.calculated.prod"
                                }
                            },
                            "dev": {
                                "sum": {
                                    "field": "saltminer.score.risk.calculated.dev"
                                }
                            },
                            "test": {
                                "sum": {
                                    "field": "saltminer.score.risk.calculated.test"
                                }
                            },
                            "overall": {
                                "sum": {
                                    "field": "saltminer.score.risk.calculated.overall"
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
            },
            "riskUnknownQuery": {
                "query": {
                    "bool": {
                        "must": [
                            {
                                "exists": {
                                    "field": "saltminer.score.risk.calculated.prod"
                                }
                            },
                            {
                                "exists": {
                                    "field": "saltminer.score.risk.calculated.dev"
                                }
                            },
                            {
                                "exists": {
                                    "field": "saltminer.score.risk.calculatedtest"
                                }
                            },
                            {
                                "exists": {
                                    "field": "saltminer.score.risk.calculated.overall"
                                }
                            },
                            {
                                "bool": {
                                    "must_not": {
                                        "exists": {
                                            "field": "inventory_asset.key"
                                        }
                                    }
                                }
                            }
                        ]
                    }
                },
                "_source": [
                    "saltminer.inventory_asset.key",
                    "saltminer.score.risk.prod",
                    "saltminer.score.risk.dev",
                    "saltminer.score.risk.test",
                    "saltminer.score.risk.overall"
                ],
                "size": 5000
            }
        }
        return query
