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

# NOTE: this script is meant to run specifically for terms aggregations and summation or count metrics.
# Success with other kinds of aggregations is not guaranteed

# 10/8/2021 TD
# This appears to be a utilty class, but there are two of them with the same name.  Can we pick one, or do we need to merge?

import json
import sys
import logging

class AggregationFlattener(object):

    def __init__(self, appSettings):
        self.__Es = appSettings.Application.GetElasticClient()
        self.__Format = "{\n\t\"index\": \"...\",\n\t\"query\": ...\n}"

    def __ParseInput(self, argument):
        if argument.endswith('.txt') or argument.endswith('.json'):
            try:
                with open(argument) as aggRequestJson:
                    indexQueryJson = json.load(aggRequestJson)
            except Exception as e:
                logging.error("Error reading file " + "argument: {}".format(e))
                return
        else:
            try:
                indexQueryJson = json.loads(argument)
            except Exception as e:
                logging.error("Error reading input. Usage (as a string arg or within a .txt or .json): " + self.__Format)
                sys.exit()
        
        if "index" in indexQueryJson and "query" in indexQueryJson:
            return indexQueryJson
        else:
            logging.error("Input formatted improperly. Usage (as a string arg or within a .txt or .json): " + self.__Format)

    def __UseAndParseQuery(self, aggRequest):
        # Try a search using Elasticsearch
        try:
            aggResults = self.__Es.Search(aggRequest['index'], aggRequest['query'])
        except Exception as e:
            logging.error("Elasticsearch failed: {}".format(e))
            return None

        subQuery = aggRequest['query']
        self.aggNameToFieldName = dict()
        firstTerm = True
        while len(subQuery['aggs']) == 1:  # terms aggregation queries have only one 'bucket', whereas metrics are bunched together
            aggName = list(subQuery['aggs'].keys())[0]
            fieldName = subQuery['aggs'][aggName]['terms']['field']
            self.aggNameToFieldName[aggName] = fieldName  # package this mapping for digesting response
            subQuery = subQuery['aggs'][aggName]

        for metricName, metricObject in subQuery['aggs'].items():
            metricType = list(metricObject.keys())[0]
            metricFieldName = metricObject[metricType]['field']
            self.aggNameToFieldName[metricName] = metricFieldName
        
        return aggResults

    def __ScrapeBuckets(self, subResult, row, numFieldsRemaining):
        # finding the custom aggregation name, assuming the only others would be 'key' and 'doc_count'
        currentAggNames = []
        for key in subResult:
            if key != "key" and key != "doc_count":
                currentAggNames.append(key)

        for currentAggName in currentAggNames:
            if 'value' in subResult[currentAggName]:
                row[self.aggNameToFieldName[currentAggName]] = subResult[currentAggName]['value']
                numFieldsRemaining -= 1
                if numFieldsRemaining == 0:
                    self.resultRows.append(row.copy())
                    for aggNameToRemove in currentAggNames:
                        del row[self.aggNameToFieldName[aggNameToRemove]]
                    break
            else:
                for bucket in subResult[currentAggName]['buckets']:
                    fieldName = self.aggNameToFieldName[currentAggName]
                    row[fieldName] = bucket['key']  # i.e. 'application_version_id': 10135
                    self.__ScrapeBuckets(bucket, row, numFieldsRemaining - 1)
                    del row[fieldName]
    

    def FlattenAggregation(self, argument):
        aggRequest = self.__ParseInput(argument)
        aggResults = self.__UseAndParseQuery(aggRequest)
        if aggResults is None:
            return None
        
        aggregations = aggResults['aggregations']
        self.resultRows = []
        self.__ScrapeBuckets(aggregations, dict(), len(self.aggNameToFieldName))
        return self.resultRows
