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
import csv

from Core.ElasticClient import ElasticClient


class AggregationFlattener2(object):

    def __init__(self):
        self.headings = []
        self.dataRows = []
        self.AggNameWannabes = {"key", "doc_count", "from", "from_as_string", "to", "to_as_string"}

    def __InputToJsonObject(self, argument):
        if argument.endswith('.txt') or argument.endswith('.json'):
            try:
                with open(argument) as aggRequestJson:
                    indexQueryJson = json.load(aggRequestJson)
            except Exception as e:
                logging.error("Error reading file " + "argument: {}".format(e))
                return
        elif type(argument) is dict:
            return argument
        else:
            try:
                indexQueryJson = json.loads(argument)
            except Exception as e:
                logging.error("Error reading input. Pass in response as text or in a .json file")
                sys.exit()

        return indexQueryJson

    # This function takes the aggregation results and flattens them into a pair of lists--one with heading names and
    # one with lists for each row of data.
    def __ParseResults(self, aggregationResults):
        aggregations = aggregationResults["aggregations"]
        self.__PublishRows(aggregations, [])

    def __GetAggregationName(self, subAggregation):
        for key, value in subAggregation.items():
            if key not in self.AggNameWannabes:
                return key
        return None

    def __PublishRows(self, subAggregation, aggRow):
        aggName = self.__GetAggregationName(subAggregation)
        if aggName is None:
            self.dataRows.append(aggRow)
            return

        subAggregation = subAggregation[aggName]
        if "buckets" not in subAggregation:  # this tells us we're entering the metrics zone
            aggRow.extend(self.__CollectMetrics(subAggregation, aggName + ": "))
            self.dataRows.append(aggRow)
        else:
            subAggregation = subAggregation["buckets"]
            for bucket in subAggregation:
                bucketInfoArr = []
                for attributeName in self.AggNameWannabes:
                    if attributeName in bucket:
                        attributeFieldName = aggName + ": " + attributeName
                        if attributeFieldName not in self.headings:
                            self.headings.append(attributeFieldName)
                        bucketInfoArr.append(bucket[attributeName])
                self.__PublishRows(bucket, aggRow + bucketInfoArr)


    def __CollectMetrics(self, metricLayer, metricNameChain):
        metricList = []
        for metricName, metricValue in metricLayer.items():
            if type(metricValue) is dict:
                # recurse with additional namechain; I use endswith ": " to check if it's the first element
                metricList.extend(self.__CollectMetrics(metricValue, metricNameChain + ("" if metricNameChain.endswith(": ") else ", ") + metricName))
            else:
                finalMetricNameChain = metricNameChain + ("" if metricNameChain.endswith(": ") else ", ") + metricName
                if finalMetricNameChain not in self.headings:  # "value" seems to refer to the aggName above it
                    self.headings.append(finalMetricNameChain)
                metricList.append(metricValue)
        return metricList


    def CreateCsv(self, csvfilename="testAggFlattener2/aggResults.csv"):
        msg = "Creating CSV..."
        print(msg)
        logging.info(msg)
        with open(csvfilename, "w", newline="") as csvfile:
            csvwriter = csv.writer(csvfile)
            csvwriter.writerow(self.headings)
            for dataRow in self.dataRows:
                csvwriter.writerow(dataRow)
        print("Done")



    def FlattenAggregation(self, results):
        msg = "Flattening aggregation..."
        print(msg)
        logging.info(msg)
        results = self.__InputToJsonObject(results)
        self.headings = []
        self.dataRows = []
        self.__ParseResults(results)
        print("Done")
