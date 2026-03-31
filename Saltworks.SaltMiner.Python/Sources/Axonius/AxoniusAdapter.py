import json
import logging 
import time
from datetime import datetime, timezone, timedelta

from Sources.Axonius.AxoniusClient import AxoniusClient
from Core.SmDocsAndDTOs import SnykDocs, MapAssetDocDTO, MapIssueDocDTO, MapScanDocDTO

from Core.DataClient import DataClient, QueueStatus
from Core.ElasticClient import ElasticClient


class AxoniusAdapter:

    def __init__(self, app):
        settings = app.Settings
        self.axonius_client = AxoniusClient(settings)
        self.sm_docs = SnykDocs()
        self._data_client = DataClient(app)
        self._es = ElasticClient(settings)
        self.last_updated_dict = {}
        self.found_date_dict = {} 
        self.queue_scan_id_dict = {}
        self.queue_asset_id_dict = {}
        #self.queue_scan_id_list = []
        self.test_scanner_id = []


    def run_sync(self):
        self.get_last_updated()
        self.get_found_dates()
        counter = 0
        for asset in self.axonius_client.get_asset_query_generator(query_id= "69b42afef8e2bcfbf5f2bfcb", asset_type= "application_settings"):
            self.process_asset(asset, query_id= "69b42afef8e2bcfbf5f2bfcb")
            counter += 1 
        
        self._data_client.queue_issue_add_update_batch(None)
        for key, value in self.queue_scan_id_dict.items():
            self._data_client.queue_scan_update_status(value, QueueStatus.PENDING)
            logging.info("[Axonius Adapter] Finalized QueueScan with id: %s", value)
        print(counter)

            


    def process_asset(self, asset, query_id):
        asset_name =  ", ".join([item for item in asset.get("specific_data.data.product_name", [])])
        if asset_name not in self.queue_scan_id_dict.keys():
            mapped_scan = self.map_scan(asset, query_id=query_id)  
            queue_scan = self._data_client.queue_scan_add_update(json.loads(mapped_scan.model_dump_json()))

            self.queue_scan_id_dict[asset_name] = queue_scan['id']
            mapped_asset= self.map_asset(asset, queue_scan_id=queue_scan['id'])
            queue_asset = self._data_client.queue_asset_add_update(json.loads(mapped_asset.model_dump_json()))
            self.queue_asset_id_dict[asset_name] = queue_asset['id']
        mapped_issue = self.map_issue(asset, queue_asset_id=self.queue_asset_id_dict[asset_name], queue_scan_id=self.queue_scan_id_dict[asset_name], query_id=query_id)
        self._data_client.queue_issue_add_update_batch(json.loads(mapped_issue.model_dump_json()))

            
    def map_scan(self, asset, query_id):
        q_scan_doc = self.sm_docs.map_scan_doc()
        q_scan_doc['Timestamp'] = datetime.now().strftime("%Y-%m-%dT%H:%M:%S.%f")[:-3] + "Z"
        q_scan_doc['Saltminer']['Internal']['IssueCount'] = -1  #Setting this value to -1 disables IssueCount validation
        q_scan_doc['Saltminer']['Internal']['ReplaceIssues'] = True
        q_scan_doc['Saltminer']['Internal']['QueueStatus'] = QueueStatus.LOADING
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
        asset_doc = q_asset_doc['Saltminer']['Asset']
        product_name = asset.get("specific_data.data.product_name")

        if isinstance(product_name, str):
            product_name = [product_name]
        elif not product_name:
            product_name = []

        joined_product_name = ", ".join(product_name) if product_name else "Unknown"
        asset_doc['Name'] = joined_product_name
        asset_doc['AssetId'] = self.get_first(asset, "internal_axon_id", "")
            
        asset_doc['AssetType'] = 'app'
        asset_doc['SourceType'] = "Saltworks.Axonius"
        asset_doc['Instance'] = "Axonius1"
        asset_doc['Version'] =  joined_product_name
        asset_doc['VersionId'] =  joined_product_name
        asset_doc['SourceId'] =  joined_product_name
        q_asset_doc = self.map_asset_attributes(asset, q_asset_doc)
        return MapAssetDocDTO(**q_asset_doc)

    def map_asset_attributes(self, asset, q_asset_doc):
        modified_q_asset_doc = q_asset_doc
        asset = modified_q_asset_doc['Saltminer']['Asset']
        attributes = asset.get('Attributes', {})

        attributes['RawSettingName'] = self.get_first(asset, "specific_data.data.raw_setting_name", "")
        attributes['MetaDataClientUsed'] = self.get_first(asset, "meta_data.client_used", "")
        attributes['Labels'] = ", ".join(asset.get("labels", []))
        attributes['SettingName'] = self.get_first(asset, "specific_data.data.setting_name", "")
        attributes['SettingNameDetails'] = self.get_first(asset, "specific_data.data.setting_name_details", "")
        attributes['SettingsStatus'] = self.get_first(asset, "specific_data.data.settings_status", "")

        return modified_q_asset_doc

    def map_issue(self, asset, queue_asset_id, queue_scan_id, query_id):
        q_issue_doc = self.sm_docs.map_issue_doc()
        q_issue_doc['Timestamp'] = datetime.now().strftime("%Y-%m-%dT%H:%M:%S.%f")[:-3] + "Z"
        saltminer = q_issue_doc['Saltminer']
        saltminer['QueueScanId'] = queue_scan_id
        saltminer['QueueAssetId']= queue_asset_id
        saltminer['IssueType'] = self.get_first(asset, "specific_data.data.settings_status", "")
        vulnerability = q_issue_doc['Vulnerability']
        finding_id = None
        for label in asset.get("labels", []):
            if 'finding' in label.lower():
                finding_id = label
                break
        vulnerability['Id'] = [finding_id if finding_id else "None"]
        vulnerability['Name'] = self.get_first(asset, "specific_data.data.raw_setting_name", "")
        scanner_id = self.get_first(asset, "internal_axon_id", "") + "|" + finding_id if finding_id else self.get_first(asset, "internal_axon_id", "")
        vulnerability['FoundDate'] = self.found_date_dict.get(scanner_id, datetime.now().strftime("%Y-%m-%dT%H:%M:%S.%f")[:-3] + "Z")
        vulnerability['ReportId'] = ", ".join([item for item in asset.get("specific_data.data.product_name", [])]) + "|" + query_id + datetime.now().isoformat()
        vulnerability['recommendation']= self.get_first(asset, "specific_data.data.recommendation_description", "")
        vulnerability['Details']= self.get_first(asset, "specific_data.data.raw_setting_name_details", "")
        vulnerability['Location']= self.get_first(asset, "specific_data.data.product_name", "")
        vulnerability['LocationFull'] = self.get_first(asset, "specific_data.data.product_name", "")
        vulnerability_severity = self.get_first(asset, "specific_data.data.impact", "")
        if vulnerability_severity not in ["Low", "Medium", "High", "Critical"]:
            vulnerability_severity = 'Info'
        vulnerability['Severity'] = vulnerability_severity
        scanner = vulnerability['Scanner']
        scanner['Product'] = "Axonius"
        scanner['Vendor'] = "Axonius"
        scanner['AssessmentType'] = "Open"
        scanner['Id'] = scanner_id


        return MapIssueDocDTO(**q_issue_doc)
        



    def get_first(self, asset, key, default=""):
        value = asset.get(key, default)
        if isinstance(value, list) and len(value) > 0:
            return value[0]
        return value

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
        

    def get_found_dates(self):
        if self._es.IndexExists('issues_app_saltworks.axonius_axonius1'):
            search = self._es.Search(index ='issues_app_saltworks.axonius_axonius1', size=10000, navToData=False)
            for item in search['hits']['hits']:
                self.found_date_dict[item['_source']['vulnerability']['scanner']['id']] = item['_source']['vulnerability']['found_date']

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


