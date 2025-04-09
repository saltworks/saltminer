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

'''
/* Copyright (C) Saltworks Security, LLC - All Rights Reserved
* Unauthorized copying of this file, via any medium is strictly prohibited
* Proprietary and confidential
* Written by Saltworks Security, LLC  (www.saltworks.io) , 2020
*/
'''

import sys
import ast
import logging
import csv
import json
from datetime import date
import re

from Core.Application import Application

enrichment_policy_name = ''
logUpdatesOnly = False

app = Application()
es = app.GetElasticClient()

if len(sys.argv) == 3:
	enrichment_policy_name = sys.argv[1]
	logUpdatesOnly = ast.literal_eval(sys.argv[2])
	if isinstance(logUpdatesOnly, bool) == False or isinstance(enrichment_policy_name, str) == False:
		logging.error("Not all of the parameters supplied were valid. Please pass the arguments for Enrichment policy name (string) and logUpdateOnly (True/False)")
		sys.exit(1)
else:
	logging.error("Not all of the parameters were supplied. Please pass the arguments for Enrichment policy name (string) and logUpdateOnly (True/False)")
	sys.exit(1)


if logUpdatesOnly == True:
	logging.info("log updates only set to True")

def initInventoryAssetsObject():
	_inventoryAssetRec = {
		'id' : '',
		'is_production' : '',
		'key' : '',
		'name' : '',
		'description' : '',
		'version' : '',
		'source' : '',
		'attributes' : {
			'saltminer' : {
				"UID" : '',
                'Application Name' : '',
                'Application Description' : '',
                'Application Type' : '',
                'Development Type' : '',
                'Vendor Name' : '',
                'Risk Classification' : '',
                'Data Center Location' : '',
                'Data Center Level' : '',
                'CIO' : '',
                'Business Unit' : '',
                'Country' : '',
                'Internet Accessible' : '',
                'PCI' : '',
                'GDPR' : '',
                'Has Login' : '',
                'Division' : ''
			}
         }
	}

	return _inventoryAssetRec


try:
	logging.info('Reading the source file')
	
	INVENTORY_ASSETS = "inventory_assets"
	dirtyList = []

	iRow = 0

	with open("ACME_CMDB.csv") as csv_file:
		csv_reader = csv.reader(csv_file, dialect='excel')
    
		line_count = 0

		for row in csv_reader:
			line_count +=1
			if line_count ==1:
				print('skip header')
			else:
				inventoryAssetMap = initInventoryAssetsObject()
				inventoryAssetMap['id'] = row[0]
				inventoryAssetMap['is_production'] = True
				inventoryAssetMap['key'] = row[0]
				inventoryAssetMap['name'] = row[1]
				inventoryAssetMap['description'] = row[2]
				inventoryAssetMap['version'] = ''
				inventoryAssetMap['source'] = 'inventory_asset'
				inventoryAssetMap['attributes']['saltminer']['UID'] = row[0]
				inventoryAssetMap['attributes']['saltminer']['Application Name'] = row[1]
				inventoryAssetMap['attributes']['saltminer']['Application Description'] = row[2]
				inventoryAssetMap['attributes']['saltminer']['Application Type'] = row[3]
				inventoryAssetMap['attributes']['saltminer']['Development Type'] = row[4]
				inventoryAssetMap['attributes']['saltminer']['Vendor Name'] = row[5]
				inventoryAssetMap['attributes']['saltminer']['Risk Classification'] = row[6]
				inventoryAssetMap['attributes']['saltminer']['Data Center Location'] = row[7]
				inventoryAssetMap['attributes']['saltminer']['Data Center Level'] = row[8]
				inventoryAssetMap['attributes']['saltminer']['CIO'] = row[9]
				inventoryAssetMap['attributes']['saltminer']['Business Unit'] = row[10]
				inventoryAssetMap['attributes']['saltminer']['Country'] = row[11]
				inventoryAssetMap['attributes']['saltminer']['Internet Accessible'] = row[12]
				inventoryAssetMap['attributes']['saltminer']['PCI'] = row[13]
				inventoryAssetMap['attributes']['saltminer']['GDPR'] = row[14]
				inventoryAssetMap['attributes']['saltminer']['Has Login'] = row[15]
				inventoryAssetMap['attributes']['saltminer']['Division'] = row[16]
				
				es.IndexWithId(INVENTORY_ASSETS, row[0], inventoryAssetMap)

			doc = []
			if es.IndexExists(INVENTORY_ASSETS):
				query = {
					"query": {
						"match": {
							"key": row[1]
						}
					}
				}

				doc = es.Search(INVENTORY_ASSETS, query)

			if doc:
				sourceDoc = doc[0]['_source']
				dataUpdated = sourceDoc != inventoryAssetMap
		
				if dataUpdated:
					if logUpdatesOnly == False:
						es.UpdateDoc(INVENTORY_ASSETS, doc[0]['_id'], {"doc" : inventoryAssetMap})
						dirtyList.append(sourceDoc['id'])
					logging.info('Updated ID %s', sourceDoc['id'])
			else:
				if logUpdatesOnly == False:
					es.Index(INVENTORY_ASSETS, inventoryAssetMap)
				logging.info('Added ID %s', row[1])

			counter = counter + 1
			
			if (counter % 500 == 0):
				print (counter)
			row = cursor.fetchone()

		# Remove inventory assets that did not get an update (not in source) and append to dirty list
		if es.IndexExists(INVENTORY_ASSETS):
			search_query = {
				"query": {
					"bool" : {
						"must_not" : {
							"match" : {
								"attributes.Modified Date" : date.today()
							}
						}
					}
				}
			}

			docs = es.Search(INVENTORY_ASSETS, search_query)
			if doc:
				for doc in docs:
					key = doc['_source']['id']

					delete_query = {
						"query": {
							"match": {
								"key": key
							}
						}
					}

					if logUpdatesOnly == False:
						es.DeleteByQuery(INVENTORY_ASSETS, delete_query)
						dirtyList.append(key)
					logging.info('Removing ID %s', key)


		if logUpdatesOnly == False:
			logging.info('Executing Enrich Policy')
			# Enrichment Policy
			try:
				es.ExecuteEnrichPolicy(enrichment_policy_name)
			except:
				logging.error("Executing Enrichment Policy failed")


			logging.info('Trigger ingest policy to update affected indices')
			if dirtyList:
				for item in dirtyList:
					update_query = { 
						"query": 
							{ "term": 
								{ "saltminer.inventory_asset.key": item } 
							} 
						}
					es.UpdateByQuery("scans*", update_query, True)
					es.UpdateByQuery("assets*", update_query, True)
					es.UpdateByQuery("issues*", update_query, True)


		print(counter)
except Exception as e:
    logging.error(f"Error: [{type(e).__name__}] {e}")