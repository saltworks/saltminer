import json
import logging 
import time
from datetime import datetime, timezone, timedelta

from Sources.Axonius.AxoniusClient import AxoniusClient
from Core.SmDocsAndDTOs import SnykDocs, MapAssetDocDTO, MapIssueDocDTO, MapScanDocDTO

from Core.SmDataClient import SmDataClient
from Core.ElasticClient import ElasticClient


class AxoniusAdapter:
    
    def __init__(self, settings):
        self.axonius_client = AxoniusClient(settings)
        self.sm_docs = SnykDocs()
        self._sm_data_client = SmDataClient(settings, "Axonius")
        self._es = ElasticClient(settings)
        self.last_updated_dict = {}
        self.queue_scan_id_list = []


    def run_sync(self):
        self.get_last_updated()

        for asset in self.axonius_client.get_asset_query_generator(query_id= "69b42afef8e2bcfbf5f2bfcb", asset_type= "application_settings"):
            print(asset.get("specific_data.data.product_name", []))


    def process_assets(self, assets):
        pass


    def map_scan(self, asset, query_id):
        q_scan_doc = self.sm_docs.map_scan_doc()
        q_scan_doc['Timestamp'] = datetime.now().strftime("%Y-%m-%dT%H:%M:%S.%f")[:-3] + "Z"
        q_scan_doc['Saltminer']['Internal']['IssueCount'] = -1  #Setting this value to -1 disables IssueCount validation
        q_scan_doc['Saltminer']['Internal']['ReplaceIssues'] = True
        scan = q_scan_doc['Saltminer']['Scan']
        scan['Product'] = "Axonius"
        scan['Vendor']= "Axonius"
        scan['ReportId'] = ", ".join([item for item in asset.get("specific_data.data.product_name", [])]) + "|" + query_id + datetime.now().isoformat()
        scan['ScanDate'] = datetime.now().strftime("%Y-%m-%dT%H:%M:%S.%f")[:-3] + "Z"
        scan['SourceType'] = "Saltworks.Axonius"
        scan['Instance'] = "Axonius1"
        scan['AssetType'] = 'app' 
        scan['AssessmentType'] = "Open"
        scan['ProductType'] = 'Application'

        return MapScanDocDTO(**q_scan_doc)
    


    def map_asset(self, asset, queue_scan_id):
        q_asset_doc = self.sm_docs.map_asset_doc()
        q_asset_doc['Timestamp'] = datetime.now().strftime("%Y-%m-%dT%H:%M:%S.%f")[:-3] + "Z"
        q_asset_doc['Saltminer']['Internal']['QueueScanId'] = queue_scan_id
        asset = q_asset_doc['Saltminer']['Asset']
        asset['Name'] = ", ".join([item for item in asset.get("specific_data.data.product_name", [])])
        asset['AssetId'] = asset.get("internal_axon_id", "")
        asset['AssetType'] = 'app'
        asset['SourceType'] = "Saltworks.Axonius"
        asset['Instance'] = "Axonius1"
        asset['Version'] =  ", ".join([item for item in asset.get("specific_data.data.product_name", [])])
        asset['VersionId'] =  ", ".join([item for item in asset.get("specific_data.data.product_name", [])])
        q_asset_doc = self.map_asset_attributes(asset, q_asset_doc)
        return MapAssetDocDTO(**q_asset_doc)

    def map_asset_attributes(self, asset, q_asset_doc):
        modified_q_asset_doc = q_asset_doc
        return modified_q_asset_doc


    def map_issue(self, issue, queue_asset_id, queue_scan_id, report_id):
        pass

    def get_last_updated(self):
        """
        This is going to call Saltminer to get the last updated date of all the Axonius project versions currently present in Saltminer.
        """
        if self._es.IndexExists('issues_app_saltworks.axonius_axonius1'):
            search = self._es.Search(index ='issues_app_saltworks.axonius_axonius1', queryBody=self.last_updated_query, size=10000, navToData=False)
            for item in search['aggregations']['version_id']['buckets']:
                self.last_updated_dict[item['key']] = item['last_updated'].get('value_as_string')
        else:
            return None

    @property
    def last_updated_query(self):
        return {
            "aggs": {
                "version_id": {
                    "terms": {
                        "field": "saltminer.asset.version_id",
                        "order": {
                            "_key": "desc"
                        },
                        "size": 10000
                    },
                    "aggs": {
                        "last_updated": {
                            "max": {
                                "field": "saltminer.last_updated"
                            }
                        }
                    }
                }
            },
            "size": 0
        }


