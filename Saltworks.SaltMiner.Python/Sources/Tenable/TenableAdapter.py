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
        self.counter = 0

        self.sm_scan_data_dict = {}
        self.current_scan_asset_dict = {}
        self.tenable_att_tags = {}


    def run_sync(self, first_load=False):
        if first_load:
            self.first_load = True

        else:
            self.first_load = False

        self.get_asset_attributes()
        self.sm_scans_generator()
        self.get_sm_scans()
        self.compare_tenable_scans()


    def get_sm_scans(self):
        if self.first_load:
            return None
        for agg in self.sm_scans_generator():
            if agg['key'] not in self.sm_scan_data_dict:
                self.sm_scan_data_dict[agg['key']] =agg['4']['value']


    def sm_scans_generator(self):
        if self._es.IndexExists("issues_net_saltworks.tenable_tenable1"):
            search = self._es.Search(index="issues_net_saltworks.tenable_tenable1",
                                    queryBody=self.schedule_uuid_agg_query(), 
                                    navToData=False,
                                    size=1)
            yield from search['aggregations']['2']['buckets']


    def compare_tenable_scans(self):
        if self.first_load:
            scan_record = {
                "uuid": "None", "last_modification_date":int((datetime.now() - timedelta(days=30)).timestamp())}
            self.sync_scan(scan_record)
        else:
            for scan_record in self.tenable_client.get_scans_generator():
                if not self.first_load:
                    if self.sm_scan_data_dict.get(scan_record['schedule_uuid']):
                        # Added for readbility
                        last_modification_date = self.sm_scan_data_dict[
                            scan_record['schedule_uuid']]
                        # Added for readibility
                        sm_last_modification_date = self.sm_scan_data_dict[scan_record['schedule_uuid']]
                        if last_modification_date >= sm_last_modification_date:
                            continue
                self.sync_scan(scan_record)


    def sync_scan(self, scan_record):
        if scan_record.get('uuid'):

            for issue_record in self.tenable_client.get_vuln_export_generator(scan_record['uuid']):
                if not self.current_scan_asset_dict.get(issue_record['asset']['uuid']):
                    mapped_scan = self.map_scan(scan_record, issue_record)
                    queue_scan = self.data_client.AddQueueScan(mapped_scan)
                    mapped_asset = self.map_asset(
                        issue_record, queue_scan['id'])
                    queue_asset = self.data_client.AddQueueAsset(mapped_asset)
                    self.current_scan_asset_dict[issue_record['asset']['uuid']] = {
                        "queue_scan_id": queue_scan['id'],
                        "queue_asset_id": queue_asset['id'],
                        "report_id": mapped_scan['Saltminer']['Scan']['ReportId'],
                        "schedule_uuid": scan_record['schedule_uuid'] if scan_record.get('schedule_uuid') else "None",
                    }
                mapped_issue = self.map_issue(
                    issue_record, current_scan_dict=self.current_scan_asset_dict[issue_record['asset']['uuid']])
                self.data_client.AddQueueIssue(mapped_issue)
            self.finalize_all_scans()


    def finalize_all_scans(self):
        self.data_client.SendAllBatchIssues()
        for asset_id, queus_scan_data in self.current_scan_asset_dict.items():
            self.data_client.FinalizeQueue(queus_scan_data['queue_scan_id'])
        self.current_scan_asset_dict = {}


    def map_scan(self, scan_record, issue_record):
        q_scan_doc = self.sm_docs.map_scan_doc()
        q_scan_doc['Timestamp'] = datetime.now().strftime(
            "%Y-%m-%dT%H:%M:%S.%f")[:-3] + "Z"
        # Setting this value to -1 disables IssueCount validation
        q_scan_doc['Saltminer']['Internal']['IssueCount'] = -1
        scan = q_scan_doc['Saltminer']['Scan']
        scan['Attributes'] = {}
        scan['Product'] = "Tenable"
        scan['Vendor'] = "Tenable"
        scan['ReportId'] = scan_record['uuid'] + " | " 
        issue_record['asset']['uuid'] + " | " + str(datetime.now())
        timestamp = scan_record['last_modification_date']  # e.g., 1746616689
        dt = datetime.fromtimestamp(timestamp, tz=timezone.utc)
        scan['ScanDate'] = dt.strftime("%Y-%m-%dT%H:%M:%S.%f")[:-3] + "Z"
        scan['SourceType'] = "Saltworks.Tenable"
        scan['Instance'] = "Tenable1"
        scan["AssetType"] = "net"
        scan['AssessmentType'] = "Open"
        scan['ProductType'] = 'Net'

        return q_scan_doc


    def map_asset(self, issue_record, queue_scan_id):
        asset_name = issue_record["asset"]["netbios_name"] if issue_record['asset'].get(
            'name') else issue_record['asset']['hostname']

        q_asset_doc = self.sm_docs.map_asset_doc()
        q_asset_doc['Timestamp'] = datetime.now().strftime(
            "%Y-%m-%dT%H:%M:%S.%f")[:-3] + "Z"
        q_asset_doc['Saltminer']['Internal']['QueueScanId'] = queue_scan_id

        asset = q_asset_doc['Saltminer']["Asset"]
        asset['Name'] = asset_name
        asset["Version"] = asset_name
        asset['VersionId'] = issue_record['asset']['uuid']
        asset['SourceId'] = issue_record['asset']['uuid']
        asset['Instance'] = 'Tenable1'
        asset['AssetType'] = 'net'
        asset['SourceType'] = 'Saltworks.Tenable'
        asset['Ip'] = issue_record['asset'].get('ipv4')
        asset['Host'] = issue_record['asset'].get('hostname')
        asset['Port'] = issue_record['port']['port'] if issue_record.get('port') else 'None'
        asset['Scheme'] = issue_record['port']['protocol'] if issue_record.get('port') else 'None'

        q_asset_doc = self.map_asset_attributes(issue_record, q_asset_doc)

        return q_asset_doc


    def map_issue(self, issue_record, current_scan_dict):
        asset_name = issue_record['asset']['netbios_name'] if issue_record['asset'].get(
            'netbios_name') else issue_record['asset']['hostname']
        queue_scan_id = current_scan_dict['queue_scan_id']
        queue_asset_id = current_scan_dict['queue_asset_id']
        report_id = current_scan_dict['report_id']

        q_issue_doc = self.sm_docs.map_issue_doc()
        q_issue_doc['Timestamp'] = datetime.now().strftime(
            "%Y-%m-%dT%H:%M:%S.%f")[:-3] + "Z"

        saltminer = q_issue_doc['Saltminer']
        saltminer['QueueScanId'] = queue_scan_id
        saltminer['QueueAssetId'] = queue_asset_id
        

        vulnerability = q_issue_doc['Vulnerability']
        if issue_record['state'] == "FIXED":
            vulnerability['RemovedDate'] = issue_record['last_fixed']

        vulnerability['Severity'] = issue_record['severity'].title()
        vulnerability['FoundDate'] = issue_record['first_found']
        vulnerability['Description'] = issue_record['plugin'].get(
            'description')
        vulnerability['Id'] = (
            [item for item in issue_record['plugin'].get('cve', [])]
            if issue_record['plugin'].get('cve')
            else ["None"]
        )
        vulnerability['Name'] = issue_record['plugin']['name']
        vulnerability['ReportId'] = report_id
        vulnerability['Location'] = asset_name
        vulnerability['LocationFull'] = asset_name + "|" + \
            str(issue_record['port']['port']) + \
            "|" + issue_record['port']['protocol']
        vulnerability['Recommendation'] = issue_record['plugin'].get(
            'solution')
        scanner = vulnerability['Scanner']
        scanner['Id'] = issue_record['finding_id'] + " | " + asset_name
        scanner['AssessmentType'] = "Open"
        scanner['Product'] = 'Tenable'
        scanner['Vendor'] = 'Tenable'
        scanner['GuiUrl'] = f"https://cloud.tenable.com/vm/#/explore/findings/host-vulnerabilities/finding-details/{issue_record['finding_id']}"

        q_issue_doc = self.map_issue_attributes(q_issue_doc, issue_record, current_scan_dict)

        return q_issue_doc


    def map_issue_attributes(self, q_issue_doc, issue_record, current_scan_dict):
        saltminer = q_issue_doc['Saltminer']
        attributes = saltminer['Attributes']
        operating_systems= issue_record['asset']['operating_system'] if issue_record['asset'].get('operating_system') else ['None']
        schedule_uuid = current_scan_dict['schedule_uuid']
        if len(operating_systems) > 1:
            operating_systems_joined =  ", ".join(operating_systems)
        elif len(operating_systems) > 0:
            operating_systems_joined = operating_systems[0]
        else:
            operating_systems_joined = "None"

        attributes['status'] = issue_record['state']
        attributes['issue_last_found'] = issue_record['last_found']
        attributes['tenable_schedule_uuid'] = schedule_uuid
        attributes['operating_systems'] = operating_systems_joined
        attributes['operating_system'] = operating_systems[0] if len(operating_systems) > 0 else "None"
        attributes['ipv6'] = issue_record['asset'].get('ipv6')
        attributes['mac_address'] = issue_record['asset'].get('mac_address')
        attributes["exploit_available"]= str(issue_record['plugin'].get('exploit_available'))
        attributes["exploit_framework_canvas"]= str(issue_record['plugin'].get('exploit_framework_canvas'))
        attributes["exploit_framework_core"]= str(issue_record['plugin'].get('exploit_framework_core'))
        attributes["exploit_framework_d2_elliot"]= str(issue_record['plugin'].get('exploit_framework_d2_elliot'))
        attributes["exploit_framework_exploithub"]= str(issue_record['plugin'].get('exploit_framework_exploithub'))
        attributes["exploit_framework_metasploit"]= str(issue_record['plugin'].get('exploit_framework_metasploit'))
        attributes["exploited_by_malware"]= str(issue_record['plugin'].get('exploited_by_malware'))
        attributes["exploited_by_nessus"]= str(issue_record['plugin'].get('exploited_by_nessus'))
        attributes["has_patch"]= str(issue_record['plugin'].get('has_patch'))
        attributes["risk_factor"]= issue_record['plugin'].get('risk_factor')
        attributes["in_the_news"]= str(issue_record['plugin'].get('in_the_news'))
        attributes["unsupported_by_vendor"]= str(issue_record['plugin'].get('unsupported_by_vendor'))
        attributes["has_workaround"]= str(issue_record['plugin'].get('has_workaround'))
        if (vpr := issue_record['plugin'].get("vpr")):
            attributes['vpr_score'] = str(vpr['score'])
        
        if(cvss3 := issue_record['plugin'].get('cvss3_base_score')):
            attributes['cvss3_base_score'] = str(cvss3)
        
        if(cvss3_temp := issue_record['plugin'].get('cvss3_temporal_score')):
            attributes['cvss3_temporal_score'] = str(cvss3_temp)
        
        if(cvss := issue_record['plugin'].get('cvss_base_score')):
            attributes['cvss_base_score'] = str(cvss)

        if(cvss_temp := issue_record['plugin'].get('cvss_base_score')):
            attributes['cvss_temporal_score'] = str(cvss_temp)

        
        return q_issue_doc


    def map_asset_attributes(self, issue_record, q_asset_doc):
        asset = q_asset_doc['Saltminer']["Asset"]
        asset_info = self.tenable_att_tags.get(issue_record['asset'].get('uuid'), {})
        asset_tags = asset_info.get('tags', [])
        asset_attributes = asset_info.get('attributes', {})

        if asset_tags:
            asset['Attributes'] = {"tenable_asset_tags": asset_tags}

        for key in asset_attributes.keys():
            asset['Attributes'][key] = asset_attributes[key]

        asset['Attributes']['agent_uuid'] = issue_record['asset'].get('agent_uuid')
        asset['Attributes']['bios_uuid'] = issue_record['asset'].get('bios_uuid')
        asset['Attributes']['fqdn'] = issue_record['asset'].get('fqdn')
        asset['Attributes']['last_scan_target'] = issue_record['asset'].get('last_scan_target')

        return q_asset_doc

    def get_asset_attributes(self):
        for asset in self.tenable_client.get_assets_generator():
            self.tenable_att_tags[asset['id']] = {}
            if asset.get('tags'):
                self.tenable_att_tags[asset['id']]['tags'] = ",".join([item['key'] + "|" + item['value'] + "|" + item["uuid"] for item in asset['tags']])

            attributes = self.tenable_att_tags[asset['id']]['attributes'] = {}
            if(vm_id  :=  asset.get('azure_vm_id')):
                attributes['azure_vm_id'] = vm_id
            if(resource_id := asset.get('azure_resource_id')):
                attributes['azure_resource_id'] = resource_id
            if(system_types := asset.get('system_types')):
                attributes['system_types'] = ", ".join(system_types)
            if(installed_software := asset.get('installed_software')):
                attributes['installed_software'] = str(installed_software)

            if(ipv6 := asset.get('ipv6')):
                attributes['ipv6s'] = ", ".join(ipv6)
                attributes['ipv6'] = ipv6[0]

    def schedule_uuid_agg_query(self):
        return {
            "aggs": {
                "2": {
                    "terms": {
                        "field": "saltminer.attributes.tenable_schedule_uuid",
                        "order": {
                            "_count": "desc"
                        },
                        "size": 500
                    },
                    "aggs": {
                        "4": {
                            "max": {
                                "field": "saltminer.attributes.issue_last_found"
                            }
                        }
                    }
                }
            },
            "size": 0

        }
