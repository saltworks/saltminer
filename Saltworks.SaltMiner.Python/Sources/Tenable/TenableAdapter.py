import json 
import logging 

from datetime import datetime, timezone, timedelta

from Sources.Tenable.TenableClient import TenableClient
from Core.SmDocsAndDTOs import SnykDocs, MapAssetDocDTO, MapIssueDocDTO, MapScanDocDTO
from Core.ElasticClient import ElasticClient
from Core.SmDataClient import SmDataClient


class TenableAdapter:
    def __init__(self, settings):
        self.tenable_client = TenableClient(settings)
        self._es = ElasticClient(settings)
        self.data_client = SmDataClient(settings, sourceName='Tenable')
        self.sm_docs = SnykDocs()
        
        self.sm_scan_data_dict = {}
        self.current_scan_asset_dict = {}
        
    
    def run_sync(self, first_load = False):
        if first_load:
            self.first_load = True

        else:
            self.first_load = False
        
        self.get_sm_scans()
        self.compare_tenable_scans()


    def get_sm_scans(self):
        if self.first_load:
            return None
        #pull scan doc per scan? 
        if self._es.IndexExists("scans_net_saltworks.tenable_tenable1"):
            with self._es.SearchScroll('scans_net_saltworks.tenable_tenable1') as scroller:
                while len(scroller.Results):
                    for p in scroller.Results:
                        self.sm_scan_data_dict[p['source']['saltminer']['attributes']['tenable_schedule_uuid']] = p['_source']['saltminer']['attributes']['tenable_last_modified']
    

    def compare_tenable_scans(self):
        for scan_record in self.tenable_client.get_scans_generator():
            if not self.first_load:
                if self.sm_scan_data_dict.get(scan_record['schedule_uuid']):
                    last_modification_date = self.sm_scan_data_dict[scan_record['last_modification_date']] #Added for readbility
                    sm_last_modification_date = self.sm_scan_data_dict[scan_record['schedule_uuid']] #Added for readibility
                    if last_modification_date >= sm_last_modification_date:
                        continue
            self.sync_scan(scan_record)


    def sync_scan(self, scan_record):
        if scan_record.get('uuid'):
            for issue_record in self.tenable_client.get_vuln_export_generator(scan_record['uuid']):
                #TODO: FIND THE APPROPRIATE ASSET ID 
                if not self.current_scan_asset_dict.get(issue_record['asset']['uuid']):
                    mapped_scan = self.map_scan(scan_record, issue_record)
                    queue_scan = self.data_client.AddQueueScan(mapped_scan)
                    mapped_asset = self.map_asset(issue_record, queue_scan['id'])
                    queue_asset = self.data_client.AddQueueAsset(mapped_asset)
                    self.current_scan_asset_dict[issue_record['asset']['uuid']] ={
                        "queue_scan_id":queue_scan['id'],
                        "queue_asset_id": queue_asset['id'],
                        "report_id": mapped_scan['Saltminer']['Scan']['ReportId'],
                        "schedule_uuid": scan_record['schedule_uuid']
                    } 
                mapped_issue = self.map_issue(issue_record, current_scan_dict = self.current_scan_asset_dict[issue_record['asset']['uuid']])
                self.data_client.AddQueueIssue(mapped_issue)
            self.finalize_all_scans()
    
    def finalize_all_scans(self):
        self.data_client.SendAllBatchIssues()
        for asset_id, queus_scan_data in self.current_scan_asset_dict.items():
            self.data_client.FinalizeQueue(queus_scan_data['queue_scan_id'])
        self.current_scan_asset_dict = {}


    def map_scan(self, scan_record, issue_record):
        q_scan_doc = self.sm_docs.map_scan_doc()
        q_scan_doc['Timestamp'] = datetime.now().strftime("%Y-%m-%dT%H:%M:%S.%f")[:-3] + "Z"
        q_scan_doc['Saltminer']['Internal']['IssueCount'] = -1  #Setting this value to -1 disables IssueCount validation
        scan = q_scan_doc['Saltminer']['Scan']
        scan['Attributes'] = {}
        scan['Product'] = "Tenable"
        scan['Vendor'] = "Tenable"
        scan['ReportId'] = scan_record['uuid'] + " | " + issue_record['asset']['uuid']
        dt = datetime.fromtimestamp(scan_record['last_modification_date'], tz=timezone.utc)
        scan['ScanDate'] = dt.strftime("%Y-%m-%dT%H:%M:%S.%f")[:-3] + "Z"
        scan['SourceType'] = "Saltworks.Tenable"
        scan['Instance'] = "Tenable1"
        scan["AssetType"] = "net"
        scan['AssessmentType'] = "Open"
        scan['ProductType'] = 'Net'
        
        return q_scan_doc

    def map_asset(self, issue_record, queue_scan_id):
        asset_name = issue_record["asset"]["netbios_name"] if issue_record['asset'].get('name') else issue_record['asset']['hostname']

        q_asset_doc = self.sm_docs.map_asset_doc()
        q_asset_doc['Timestamp'] = datetime.now().strftime("%Y-%m-%dT%H:%M:%S.%f")[:-3] + "Z"
        q_asset_doc['Saltminer']['Internal']['QueueScanId'] = queue_scan_id

        asset = q_asset_doc['Saltminer']["Asset"]
        asset['Name'] = asset_name
        asset["Version"] = asset_name
        asset['VersionId'] =  issue_record['asset']['uuid']
        asset['SourceId'] = issue_record['asset']['uuid']
        asset['Instance'] = 'Tenable1'
        asset['AssetType'] = 'net'
        asset['SourceType'] = 'Saltworks.Tenable'
        asset['ip'] = asset.get('ipv4')

        return q_asset_doc



    def map_issue(self, issue_record, current_scan_dict):
        asset_name = issue_record['asset']['netbios_name'] if issue_record['asset'].get('netbios_name') else issue_record['asset']['hostname']
        queue_scan_id = current_scan_dict['queue_scan_id']
        queue_asset_id = current_scan_dict['queue_asset_id']
        report_id = current_scan_dict['report_id']
        schedule_uuid = current_scan_dict['schedule_uuid']

        q_issue_doc = self.sm_docs.map_issue_doc()
        q_issue_doc['Timestamp'] = datetime.now().strftime("%Y-%m-%dT%H:%M:%S.%f")[:-3] + "Z"

        saltminer = q_issue_doc['Saltminer']
        saltminer['QueueScanId'] = queue_scan_id
        saltminer['QueueAssetId']= queue_asset_id
        saltminer['Attributes']['status'] = issue_record['state']
        saltminer['Attributes']['issue_last_found'] = issue_record['last_found']
        saltminer['Attributes']['tenable_schedule_uuid'] = schedule_uuid

        vulnerability = q_issue_doc['Vulnerability']
        vulnerability['Severity'] = issue_record['severity'].title()
        vulnerability['FoundDate'] = issue_record['first_found']
        vulnerability['Description'] = issue_record['plugin'].get('description')
        vulnerability['Id'] = (
        [item for item in issue_record['plugin'].get('cve', [])]
        if issue_record['plugin'].get('cve')
        else ["None"]
        )
        vulnerability['Name'] = issue_record['plugin']['name']
        vulnerability['ReportId'] = report_id
        vulnerability['Location'] = asset_name
        vulnerability['LocationFull'] = asset_name + "|" + str(issue_record['port']['port'])+ "|" + issue_record['port']['protocol']
        vulnerability['Recommendation'] = issue_record['plugin'].get('solution')
        
        scanner = vulnerability['Scanner']
        scanner['Id'] = issue_record['finding_id']
        scanner['AssessmentType'] = "Open"
        scanner['Product'] = 'Tenable'
        scanner['Vendor'] = 'Tenable'

        return q_issue_doc

