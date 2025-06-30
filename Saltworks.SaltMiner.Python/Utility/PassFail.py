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
import sys
import json
import array as arr
from datetime import datetime as dt
from datetime import timedelta
from datetime import date
from Core.Application import ElasticClient
from Core.ApplicationSettings import ApplicationSettings
from elasticsearch.exceptions import NotFoundError


class PassFail(object):

    def __init__(self, settings):
        if not isinstance(settings, ApplicationSettings):
            raise ValueError(
                "Parameter 'settings' must be of type 'ApplicationSettings'.")
        self._Es = ElasticClient(settings)
        self.runFunctions = True
        self.getTimeNow()
        self.getAssetIdList()
        self.getInventoryAssetIdList()
        

    def passFailEval(self):
        if self.runFunctions == False:
            logging.error('Unable to continue due to lack of required indices')
            return
        else:
            logging.info('Starting push from Issues to Assets')
            self.pfIssuesToAssets()
            logging.info('10 second delay begin')
            time.sleep(10)
            logging.info('10 second delay complete, continuing')
            logging.info('Starting scan frequency assessment')
            self.pfScanFrequencyRoller()
            time.sleep(10)
            logging.info('Starting push from Assets to Inventory Assets')
            self.pfAssetsToInvAssets()
            self.sendReasonsForInvAssets()
        


    def pfIssuesToAssets(self):

        issuesIndex = self._Es.Search(
            index='issues_active*', queryBody=self.passFailIssuesQuery(), navToData=False)
        
        for item in issuesIndex['aggregations']['index']['buckets']:
            newIndex = item['key'].replace("issues_", "assets_")
            passFailStatus = True

            try:

                for bucket in item['asset_id']['buckets']:
                    self.failureReasonsList = []
                    assetId = self.assetIdList[bucket['key']]
                    noFailFound = True
                    while noFailFound == True:
                        for assessment in bucket['assessment_type']['buckets']:
                            for date in assessment['scan_date']['buckets']:
                                for passFail in date['pass_fail']['buckets']:
                                    if passFail['key_as_string'] == 'false':
                                        passFailStatus = False
                                        if 'Vulnerability grace period failure' not in self.failureReasonsList:
                                            self.failureReasonsList.append(
                                                'Vulnerability grace period failure')
                                        noFailFound = False
                        noFailFound = False

                    passFailDoc = self.passFailDoc(
                        passFailStatus=passFailStatus)

                    passFailDoc['saltminer']['score']['pass_fail_reasons'] = self.failureReasonsList
                    self.failureReasonsList = []
                    self._Es.UpdateDoc(
                        index=newIndex, docId=assetId, doc=passFailDoc)
                    passFailStatus = True
            except Exception as e:
                logging.critical(
                    "Following issue occurred with process: %s", e)

    def pfAssetsToInvAssets(self):
        assetsIndex = self._Es.Search(index='assets*', queryBody=self.passFailAssetsQuery(), navToData=False)
        self.updatedInvAssets= {}
        
        for item in assetsIndex['aggregations']['index']['buckets']:
            

            for bucket in item['key']['buckets']:
                try:
                    inv_asset_id = self.invAssetIdList[bucket['key']]

                    passFailBuckets = []
                    for i in range(len(bucket['pass_fail']['buckets'])):
                        passFailBuckets.append(bucket['pass_fail']['buckets'][i]['key_as_string'])
                    
                    if 'false' in passFailBuckets:
                        self.updatedInvAssets[bucket['key']] = False
                        self.createAndSendDoc(index='inventory_assets', docID=inv_asset_id, passFailStatus=False)
                    
                    else:
                        if 'true' in passFailBuckets:
                            verifiedPassFail = self.checkIfInvAssetUpdated(bucketKey=bucket['key'], passFailStatus=True)
                            if verifiedPassFail == True:
                                self.updatedInvAssets[bucket['key']] = True

                            self.createAndSendDoc(index= 'inventory_assets', docID=inv_asset_id, passFailStatus=verifiedPassFail)
                        elif len(passFailBuckets) == 0:
                            self.createAndSendDoc(index= 'inventory_assets', docID=inv_asset_id, passFailStatus=None)
                    # for passFail in bucket['pass_fail']['buckets']:
                        # if passFail['key_as_string'] == 'false':
                            # self.updatedInvAssets[bucket['key']] = False
                            # self.createAndSendDoc(index='inventory_assets', docID=inv_asset_id, passFailStatus=False)
                            # 
                        # elif passFail['key_as_string'] == 'true':
                            # 
                            # verifiedPassFail = self.checkIfInvAssetUpdated(bucketKey=bucket['key'], passFailStatus=True)
                            # if verifiedPassFail == True:
                                # self.updatedInvAssets[bucket['key']] = True
                            # 
                            # self.createAndSendDoc(index= 'inventory_assets', docID=inv_asset_id, passFailStatus=verifiedPassFail)
                    # 
                except Exception as e:
                    logging.warning(
                        "Following issue occurred with key: %s", e)
        counter = 0 
        for item in self.updatedInvAssets:
            if self.updatedInvAssets[item] == True:
                counter += 1 
        
        
        
        
    def checkIfInvAssetUpdated(self, bucketKey, passFailStatus):

        if bucketKey in self.updatedInvAssets.keys():
            if self.updatedInvAssets[bucketKey] == False:
                if passFailStatus == True:
                    verifiedpassFail = False
                    return verifiedpassFail
        else:
            verifiedpassFail = passFailStatus 
            return verifiedpassFail


    def createAndSendDoc(self, index, docID, passFailStatus):
        doc = self.passFailDoc(passFailStatus=passFailStatus)
        if passFailStatus == True:

            doc ['saltminer']['score']['pass_fail_reasons'] = []
        
        self._Es.UpdateDoc(
            index=index, docId=docID, doc=doc)
    def sendReasonsForInvAssets(self):
        reasonsList = self.getReasonsForInvAssets()
        
        for invAsset in reasonsList.keys():
            
            
            tempReasonList = []
            for reason in reasonsList[invAsset].keys():
                tempReasonList.append(reason)
            
            reasonsDoc = self.reasonsDoc(tempReasonList)
            try:
                self._Es.UpdateDoc(index='inventory_assets', docId=self.invAssetIdList[invAsset], doc=reasonsDoc)
            except KeyError as e:
                logging.info('The Following issue occurred with key: %s', e)
            tempReasonList = []
    def getReasonsForInvAssets(self):
        reasonsQuery = {
            "query": {
                "exists": {"field": "saltminer.score.pass_fail_reasons"}
            }
        }
        reasonsList = {}
        with self._Es.SearchScroll('assets*', queryBody=reasonsQuery,scrollSize=10) as scroller:
            while len(scroller.Results):

                for p in scroller.Results:
                    if p['_source']['saltminer']['inventory_asset']['key'] not in reasonsList.keys():
                        reasonsList[p['_source']['saltminer']['inventory_asset']['key']] = {}
                    for reason in p['_source']['saltminer']['score']['pass_fail_reasons']:
                        try:
                            if reasonsList[p['_source']['saltminer']['inventory_asset']['key']][reason]:
                                reasonsList[p['_source']['saltminer']['inventory_asset']['key']][reason] += 1
                        except KeyError:  
                            reasonsList[p['_source']['saltminer']['inventory_asset']['key']][reason] = 1
                scroller.GetNext()
        return reasonsList
    def reasonsDoc(self, reasons):
        reasonsDoc = {
            'saltminer':{
                "score":{
                    "pass_fail_reasons": reasons
                }
            }
        }
        return reasonsDoc
    def pfScanFrequencyRoller(self):
        scanFreqIndex = self._Es.Search(
            'issues_active', queryBody=self.scanFrequencyQuery(), navToData=False)
        for item in scanFreqIndex['aggregations']['index']['buckets']:
            newIndex = item['key'].replace("issues_", "assets_")

            for bucket in item['asset_id']['buckets']:
                try:
                    assetId = self.assetIdList[bucket['key']]
                    for assessment in bucket['assessment_type']['buckets']:
                        assessmentType = assessment['key']
                        assesmentValue = self.getAssessmentValue(
                            assessment=assessment, assessmentType=assessmentType)
                        scanFreqDoc = self.scanFrequencyDoc(
                            assessmentType=assessmentType, assessmentValue=assesmentValue)
                        self._Es.UpdateDoc(
                            index=newIndex, docId=assetId, doc=scanFreqDoc)
                except Exception as e:
                    logging.critical(
                        "Following issue occurred with key: %s", e)

        
    def getTimeNow(self):
        stringTime = dt.strftime(dt.now(), "%Y-%m-%dT%H:%M:%S.%fZ")
        self.trueTime = dt.strptime(stringTime, "%Y-%m-%dT%H:%M:%S.%fZ")

    def getAssetIdList(self):
        self.assetIdList = {}
        if self.assetsExist() == True:
            with self._Es.SearchScroll('assets*', scrollSize=10,) as scroller:
                while len(scroller.Results):
                
                    for p in scroller.Results:
                        self.assetIdList[p['_source']['saltminer']
                                        ['asset']['source_id']] = p['_id']

                    scroller.GetNext()
        elif self.assetsExist() == False:
            self.runFunctions = False
            logging.error('No assets* index found, please make sure you have the required indices to run PassFail: issues_active*, assets*, inventory_assets')
            

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
        
    def getInventoryAssetIdList(self):
        self.invAssetIdList = {}
        if self.inventoryAssetsExist() == True:
            with self._Es.SearchScroll('inventory_assets', scrollSize=10) as scroller:
                while len(scroller.Results):

                    for p in scroller.Results:
                        if p['_source']['name'].lower() != 'unknown':

                            self.invAssetIdList[p['_source']['key']] = p['_id']

                    scroller.GetNext()
        elif self.inventoryAssetsExist() == False:
            self.runFunctions = False
            logging.error('No inventory_assets index found, please make sure you have the required indices to run PassFail: issues_active*, assets*, inventory_assets')   

    def passFailIssuesQuery(self):

        query = {
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
                                "size": 10000
                            },
                            "aggs": {
                                "assessment_type": {
                                    "terms": {
                                        "field": "vulnerability.scanner.assessment_type",
                                        "order": {
                                            "_count": "desc"
                                        },
                                        "size": 100
                                    },
                                    "aggs": {
                                        "scan_date": {
                                            "terms": {
                                                "field": "saltminer.scan.scan_date",
                                                "order": {
                                                    "_count": "desc"
                                                },
                                                "size": 1000
                                            },
                                            "aggs": {
                                                "pass_fail": {
                                                    "terms": {
                                                        "field": "saltminer.score.pass_fail",
                                                        "order": {
                                                            "_count": "desc"
                                                        },
                                                        "size": 5,
                                                        "shard_size": 25
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
                    "filter": [],
                    "should": [],
                    "must_not": []
                }
            }
        }
        return query

    def getAssessmentValue(self, assessment, assessmentType):
        scandateBucket = assessment['scan_date']['buckets']
        sastBucket = scandateBucket[0]['last_scan_sast']
        dastBucket = sastBucket['buckets'][0]['last_scan_dast']
        mobileBucket = dastBucket['buckets'][0]['last_scan_mobile']
        openBucket = mobileBucket['buckets'][0]['last_scan_open']
        if assessmentType == 'SAST':
            assessmentValue = sastBucket['buckets'][0]['key']
        if assessmentType == 'DAST':
            assessmentValue = dastBucket['buckets'][0]['key']
        if assessmentType == 'Mobile':
            assessmentValue = mobileBucket['buckets'][0]['key']
        if assessmentType == 'Open':
            assessmentValue = openBucket['buckets'][0]['key']
        return assessmentValue

    def passFailDoc(self, passFailStatus):
        doc = {
            'saltminer': {
                'score': {
                    'pass_fail': passFailStatus
                },
            }
        }

        return doc

    def passFailAssetsQuery(self):
        query = {
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
                        "key": {
                            "terms": {
                                "field": "saltminer.inventory_asset.key",
                                "order": {
                                    "_count": "desc"
                                },
                                "size": 1000
                            },
                            "aggs": {
                                "pass_fail": {
                                    "terms": {
                                        "field": "saltminer.score.pass_fail",
                                        "order": {
                                            "_count": "desc"
                                        },
                                        "size": 5,
                                        "shard_size": 25
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
                    "filter": [],
                    "should": [],
                    "must_not": []
                }
            }
        }

        return query

    def scanFrequencyDoc(self, assessmentType, assessmentValue):

        scanFrequencyDoc = {
            'saltminer': {
                'score': {
                    'last_scan': {
                        assessmentType.lower(): assessmentValue
                    }
                }
            }
        }
        return scanFrequencyDoc

    def scanFrequencyQuery(self):

        scanFreqQuery = {
            "aggs": {
                "index": {
                    "terms": {
                        "field": "_index",
                        "order": {
                            "_count": "desc"
                        },
                        "size": 5,
                        "shard_size": 25
                    },
                    "aggs": {
                        "asset_id": {
                            "terms": {
                                "field": "saltminer.asset.source_id",
                                "order": {
                                    "_count": "desc"
                                },
                                "size": 1000
                            },
                            "aggs": {
                                "assessment_type": {
                                    "terms": {
                                        "field": "saltminer.scan.assessment_type",
                                        "order": {
                                            "_count": "desc"
                                        },
                                        "size": 10,
                                        "shard_size": 25
                                    },
                                    "aggs": {
                                        "scan_date": {
                                            "terms": {
                                                "field": "saltminer.scan.scan_date",
                                                "order": {
                                                    "_count": "desc"
                                                },
                                                "size": 1,
                                                "shard_size": 25
                                            },
                                            "aggs": {
                                                "last_scan_sast": {
                                                    "terms": {
                                                        "field": "saltminer.score.last_scan.sast",
                                                        "order": {
                                                            "_count": "desc"
                                                        },
                                                        "size": 1,
                                                        "shard_size": 25
                                                    },
                                                    "aggs": {
                                                        "last_scan_dast": {
                                                            "terms": {
                                                                "field": "saltminer.score.last_scan.dast",
                                                                "order": {
                                                                    "_count": "desc"
                                                                },
                                                                "size": 1,
                                                                "shard_size": 25
                                                            },
                                                            "aggs": {
                                                                "last_scan_mobile": {
                                                                    "terms": {
                                                                        "field": "saltminer.score.last_scan.mobile",
                                                                        "order": {
                                                                            "_count": "desc"
                                                                        },
                                                                        "size": 1,
                                                                        "shard_size": 25
                                                                    },
                                                                    "aggs": {
                                                                        "last_scan_open": {
                                                                            "terms": {
                                                                                "field": "saltminer.score.last_scan.open",
                                                                                "order": {
                                                                                    "_count": "desc"
                                                                                },
                                                                                "size": 1,
                                                                                "shard_size": 25
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
        }
        return scanFreqQuery
