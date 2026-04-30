''' --[auto-generated, do not modify this block]--
*
* SaltMiner - The open source vulnerability and pen testing management platform
* Copyright (C) 2024-2026 Saltworks Security, LLC
*
* This program is free software: you can redistribute it and/or modify
* it under the terms of the GNU General Public License as published by
* the Free Software Foundation, either version 3 of the License.
*
* This program is distributed in the hope that it will be useful,
* but WITHOUT ANY WARRANTY; without even the implied warranty of
* MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the
* GNU General Public License for more details.
*
* You should have received a copy of the GNU General Public License
* along with this program. If not, see <https://www.gnu.org/licenses/>.
*
* ----
'''

import logging
from datetime import datetime, timezone, timedelta

from Sources.Tenable.TenableClient import TenableClient
from Core.SmDocsAndDTOs import SnykDocs
from Core.SmDataClient import SmDataClient


class TenableAdapter:
    def __init__(self, appSettings):
        settings = appSettings
        self.tenable_client = TenableClient(settings)
        #self._es = ElasticClient(settings)
        self.data_client = SmDataClient(settings, "Tenable")
        self.sm_docs = SnykDocs()
        self.vuln_management = settings.GetSource("Tenable", "VulnManagement")
        self.was = settings.GetSource("Tenable", "WAS")

    def run_sync(self, first_load=False):
        if self.was:
            was = TenableWasAdapter(self)
            was.run_process(first_load)
        if self.vuln_management:
            vm = TenableVulnManagementAdapter(self)
            vm.run_process(first_load)

    # def sm_scans_generator(self, index, agg_query):
    #     if self._es.IndexExists(index):
    #         search = self._es.Search(
    #             index=index,
    #             queryBody=agg_query,
    #             navToData=False,
    #             size=1
    #         )
    #         yield from search['aggregations']['2']['buckets']

    # def get_sm_scans(self, index, agg_query):
    #     sm_scan_data_dict = {}
    #     for agg in self.sm_scans_generator(index, agg_query):
    #         if agg['key'] not in sm_scan_data_dict:
    #             sm_scan_data_dict[agg['key']] = agg['4']['value']
    #     return sm_scan_data_dict


class TenableVulnManagementAdapter:
    def __init__(self, base):
        self.base = base
        self.sm_scan_data_dict = {}
        self.current_scan_asset_dict = {}
        self.tenable_att_tags = {}
        self.first_load = False

    def run_process(self, first_load=False):
        self.first_load = first_load
        self.get_asset_attributes()
        # if not self.first_load:
        #     self.sm_scan_data_dict = self.base.get_sm_scans(
        #         index="issues_app_saltworks.tenable_tenable1",
        #         agg_query=self.schedule_uuid_agg_query()
        #     )
        self.compare_tenable_scans()

    def compare_tenable_scans(self, scan_id_key='schedule_uuid', date_field='last_modification_date'):
        if self.first_load:
            scan_record = {
                "uuid": "None",
                "last_modification_date": int((datetime.now(timezone.utc) - timedelta(days=30)).timestamp())
            }
            self.sync_scan(scan_record)
        else:
            for scan_record in self.base.tenable_client.get_vm_scans_generator():
                if not self.first_load:
                    if self.sm_scan_data_dict.get(scan_record[scan_id_key]):
                        last_modification_date = self.sm_scan_data_dict[scan_record[scan_id_key]]
                        sm_last_modification_date = self.sm_scan_data_dict[scan_record[scan_id_key]]
                        if last_modification_date >= sm_last_modification_date:
                            continue
                self.sync_scan(scan_record)

    def sync_scan(self, scan_record):
        if scan_record.get('uuid'):
            for issue_record in self.base.tenable_client.get_vm_vuln_export_generator(scan_record['uuid']):
                if not self.current_scan_asset_dict.get(issue_record['asset']['uuid']):
                    mapped_scan = self.map_scan(scan_record, issue_record)
                    queue_scan = self.base.data_client.AddQueueScan(mapped_scan)
                    mapped_asset = self.map_asset(issue_record, queue_scan['id'])
                    queue_asset = self.base.data_client.AddQueueAsset(mapped_asset)
                    self.current_scan_asset_dict[issue_record['asset']['uuid']] = {
                        "queue_scan_id": queue_scan['id'],
                        "queue_asset_id": queue_asset['id'],
                        "report_id": mapped_scan['Saltminer']['Scan']['ReportId'],
                        "schedule_uuid": scan_record['schedule_uuid'] if scan_record.get('schedule_uuid') else "None",
                    }
                mapped_issue = self.map_issue(
                    issue_record,
                    current_scan_dict=self.current_scan_asset_dict[issue_record['asset']['uuid']]
                )
                self.base.data_client.AddQueueIssue(mapped_issue)
            self.finalize_all_scans()

    def finalize_all_scans(self):
        self.base.data_client.SendAllBatchIssues()
        for asset_id, queue_scan_data in self.current_scan_asset_dict.items():
            self.base.data_client.FinalizeQueue(queue_scan_data['queue_scan_id'])
        self.current_scan_asset_dict = {}

    def map_scan(self, scan_record, issue_record):
        q_scan_doc = self.base.sm_docs.map_scan_doc()
        q_scan_doc['Timestamp'] = datetime.now(timezone.utc).strftime("%Y-%m-%dT%H:%M:%S.%f")[:-3] + "Z"
        q_scan_doc['Saltminer']['Internal']['IssueCount'] = -1
        scan = q_scan_doc['Saltminer']['Scan']
        scan['Attributes'] = {}
        scan['Product'] = "Tenable"
        scan['Vendor'] = "Tenable"
        scan['ReportId'] = scan_record['uuid'] + " | " + issue_record['asset']['uuid'] + " | " + datetime.now(timezone.utc).strftime("%Y-%m-%dT%H:%M:%S.%f")[:-3] + "Z"
        timestamp = scan_record['last_modification_date']
        dt = datetime.fromtimestamp(timestamp, tz=timezone.utc)
        scan['ScanDate'] = dt.strftime("%Y-%m-%dT%H:%M:%S.%f")[:-3] + "Z"
        scan['SourceType'] = "Saltworks.Tenable"
        scan['Instance'] = "Tenable1"
        scan["AssetType"] = "app"
        scan['AssessmentType'] = "SAST"
        scan['ProductType'] = 'app'
        return q_scan_doc

    def map_asset(self, issue_record, queue_scan_id):
        asset_name = issue_record["asset"]["netbios_name"] if issue_record['asset'].get(
            'name') else issue_record['asset']['hostname']

        q_asset_doc = self.base.sm_docs.map_asset_doc()
        q_asset_doc['Timestamp'] = datetime.now(timezone.utc).strftime("%Y-%m-%dT%H:%M:%S.%f")[:-3] + "Z"
        q_asset_doc['Saltminer']['Internal']['QueueScanId'] = queue_scan_id

        asset = q_asset_doc['Saltminer']["Asset"]
        asset['Name'] = asset_name
        asset["Version"] = asset_name
        asset['VersionId'] = issue_record['asset']['uuid']
        asset['SourceId'] = issue_record['asset']['uuid']
        asset['Instance'] = 'Tenable1'
        asset['AssetType'] = 'app'
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

        q_issue_doc = self.base.sm_docs.map_issue_doc()
        q_issue_doc['Timestamp'] = datetime.now(timezone.utc).strftime("%Y-%m-%dT%H:%M:%S.%f")[:-3] + "Z"

        saltminer = q_issue_doc['Saltminer']
        saltminer['QueueScanId'] = queue_scan_id
        saltminer['QueueAssetId'] = queue_asset_id

        vulnerability = q_issue_doc['Vulnerability']
        if issue_record['state'] == "FIXED":
            vulnerability['RemovedDate'] = issue_record['last_fixed']

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
        vulnerability['LocationFull'] = asset_name + "|" + \
            str(issue_record['port']['port']) + \
            "|" + issue_record['port']['protocol']
        vulnerability['Recommendation'] = issue_record['plugin'].get('solution')
        scanner = vulnerability['Scanner']
        scanner['Id'] = issue_record['finding_id'] + " | " + asset_name
        scanner['AssessmentType'] = "SAST"
        scanner['Product'] = 'Tenable'
        scanner['Vendor'] = 'Tenable'
        scanner['GuiUrl'] = f"https://cloud.tenable.com/vm/#/explore/findings/host-vulnerabilities/finding-details/{issue_record['finding_id']}"

        q_issue_doc = self.map_issue_attributes(q_issue_doc, issue_record, current_scan_dict)
        return q_issue_doc

    def map_issue_attributes(self, q_issue_doc, issue_record, current_scan_dict):
        saltminer = q_issue_doc['Saltminer']
        attributes = saltminer['Attributes']
        operating_systems = issue_record['asset']['operating_system'] if issue_record['asset'].get('operating_system') else ['None']
        schedule_uuid = current_scan_dict['schedule_uuid']
        if len(operating_systems) > 1:
            operating_systems_joined = ", ".join(operating_systems)
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
        attributes["exploit_available"] = str(issue_record['plugin'].get('exploit_available'))
        attributes["exploit_framework_canvas"] = str(issue_record['plugin'].get('exploit_framework_canvas'))
        attributes["exploit_framework_core"] = str(issue_record['plugin'].get('exploit_framework_core'))
        attributes["exploit_framework_d2_elliot"] = str(issue_record['plugin'].get('exploit_framework_d2_elliot'))
        attributes["exploit_framework_exploithub"] = str(issue_record['plugin'].get('exploit_framework_exploithub'))
        attributes["exploit_framework_metasploit"] = str(issue_record['plugin'].get('exploit_framework_metasploit'))
        attributes["exploited_by_malware"] = str(issue_record['plugin'].get('exploited_by_malware'))
        attributes["exploited_by_nessus"] = str(issue_record['plugin'].get('exploited_by_nessus'))
        attributes["has_patch"] = str(issue_record['plugin'].get('has_patch'))
        attributes["risk_factor"] = issue_record['plugin'].get('risk_factor')
        attributes["in_the_news"] = str(issue_record['plugin'].get('in_the_news'))
        attributes["unsupported_by_vendor"] = str(issue_record['plugin'].get('unsupported_by_vendor'))
        attributes["has_workaround"] = str(issue_record['plugin'].get('has_workaround'))
        if (vpr := issue_record['plugin'].get("vpr")):
            attributes['vpr_score'] = str(vpr['score'])
        if (cvss3 := issue_record['plugin'].get('cvss3_base_score')):
            attributes['cvss3_base_score'] = str(cvss3)
        if (cvss3_temp := issue_record['plugin'].get('cvss3_temporal_score')):
            attributes['cvss3_temporal_score'] = str(cvss3_temp)
        if (cvss := issue_record['plugin'].get('cvss_base_score')):
            attributes['cvss_base_score'] = str(cvss)
        if (cvss_temp := issue_record['plugin'].get('cvss_base_score')):
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
        for asset in self.base.tenable_client.get_vm_assets_generator():
            self.tenable_att_tags[asset['id']] = {}
            if asset.get('tags'):
                self.tenable_att_tags[asset['id']]['tags'] = ",".join(
                    [item['key'] + "|" + item['value'] + "|" + item["uuid"] for item in asset['tags']]
                )
            attributes = self.tenable_att_tags[asset['id']]['attributes'] = {}
            if (vm_id := asset.get('azure_vm_id')):
                attributes['azure_vm_id'] = vm_id
            if (resource_id := asset.get('azure_resource_id')):
                attributes['azure_resource_id'] = resource_id
            if (system_types := asset.get('system_types')):
                attributes['system_types'] = ", ".join(system_types)
            if (installed_software := asset.get('installed_software')):
                attributes['installed_software'] = str(installed_software)
            if (ipv6 := asset.get('ipv6')):
                attributes['ipv6s'] = ", ".join(ipv6)
                attributes['ipv6'] = ipv6[0]

    def schedule_uuid_agg_query(self):
        return {
            "aggs": {
                "2": {
                    "terms": {
                        "field": "saltminer.attributes.tenable_schedule_uuid",
                        "order": {"_count": "desc"},
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


class TenableWasAdapter():
    def __init__(self, base):
        self.base = base
        self.current_scan_asset_dict = {}

    def run_process(self, first_load=False):  # first_load kept for interface parity with VM adapter
        # Pull all WAS findings in one export and group by asset UUID.
        # One scan + asset record is created per unique web app asset,
        # then every finding for that asset is attached as an issue.
        for finding in self.base.tenable_client.get_was_export_generator():
            asset_uuid = finding['asset']['uuid']

            if asset_uuid not in self.current_scan_asset_dict:
                mapped_scan = self.map_scan(finding)
                queue_scan = self.base.data_client.AddQueueScan(mapped_scan)
                mapped_asset = self.map_asset(finding, queue_scan['id'])
                queue_asset = self.base.data_client.AddQueueAsset(mapped_asset)
                self.current_scan_asset_dict[asset_uuid] = {
                    "queue_scan_id": queue_scan['id'],
                    "queue_asset_id": queue_asset['id'],
                    "report_id": mapped_scan['Saltminer']['Scan']['ReportId'],
                }

            mapped_issue = self.map_issue(finding, self.current_scan_asset_dict[asset_uuid])
            self.base.data_client.AddQueueIssue(mapped_issue)

        self.finalize_all_scans()
        logging.info("Tenable WAS sync completed - %s", datetime.now(timezone.utc).isoformat())

    def finalize_all_scans(self):
        self.base.data_client.SendAllBatchIssues()
        for _, scan_data in self.current_scan_asset_dict.items():
            self.base.data_client.FinalizeQueue(scan_data['queue_scan_id'])
        self.current_scan_asset_dict = {}

    def map_scan(self, finding):
        q_scan_doc = self.base.sm_docs.map_scan_doc()
        q_scan_doc['Timestamp'] = datetime.now(timezone.utc).strftime("%Y-%m-%dT%H:%M:%S.%f")[:-3] + "Z"
        q_scan_doc['Saltminer']['Internal']['IssueCount'] = -1

        scan = q_scan_doc['Saltminer']['Scan']
        scan['Attributes'] = {}
        scan['Product'] = "Tenable"
        scan['Vendor'] = "Tenable"
        scan['ReportId'] = finding['asset']['uuid'] + " | " + finding['asset']['fqdn'] + " | " + str(datetime.now(timezone.utc))
        scan['ScanDate'] = finding['scan']['completed_at']
        scan['SourceType'] = "Saltworks.Tenable"
        scan['Instance'] = "Tenable1"
        scan['AssetType'] = "app"
        scan['AssessmentType'] = "DAST"
        scan['ProductType'] = 'App'
        return q_scan_doc

    def map_asset(self, finding, queue_scan_id):
        asset = finding['asset']
        url = finding.get('url', '')
        scheme = url.split("://")[0] if "://" in url else "https"

        q_asset_doc = self.base.sm_docs.map_asset_doc()
        q_asset_doc['Timestamp'] = datetime.now(timezone.utc).strftime("%Y-%m-%dT%H:%M:%S.%f")[:-3] + "Z"
        q_asset_doc['Saltminer']['Internal']['QueueScanId'] = queue_scan_id

        sm_asset = q_asset_doc['Saltminer']['Asset']
        sm_asset['Name'] = asset['fqdn']
        sm_asset['Version'] = asset['fqdn']
        sm_asset['VersionId'] = asset['uuid']
        sm_asset['SourceId'] = asset['uuid']
        sm_asset['Instance'] = 'Tenable1'
        sm_asset['AssetType'] = 'app'
        sm_asset['SourceType'] = 'Saltworks.Tenable'
        sm_asset['Host'] = asset['fqdn']
        sm_asset['Port'] = finding['port']['port'] if finding.get('port') else 0
        sm_asset['Ip'] = asset.get('ipv4')
        sm_asset['Scheme'] = scheme
        sm_asset['Attributes'] = {
            "was_asset_id": asset['uuid'],
            "was_asset_fqdn": asset['fqdn'],
        }
        return q_asset_doc

    def map_issue(self, finding, current_scan_dict):
        plugin = finding['plugin']

        q_issue_doc = self.base.sm_docs.map_issue_doc()
        q_issue_doc['Timestamp'] = datetime.now(timezone.utc).strftime("%Y-%m-%dT%H:%M:%S.%f")[:-3] + "Z"

        saltminer = q_issue_doc['Saltminer']
        saltminer['QueueScanId'] = current_scan_dict['queue_scan_id']
        saltminer['QueueAssetId'] = current_scan_dict['queue_asset_id']

        vulnerability = q_issue_doc['Vulnerability']
        vulnerability['Severity'] = finding['severity'].title()
        vulnerability['FoundDate'] = finding['first_found']
        vulnerability['Description'] = plugin.get('description') or finding.get('output')
        vulnerability['Name'] = plugin.get('name') or str(plugin['id'])
        vulnerability['ReportId'] = current_scan_dict['report_id']
        vulnerability['Location'] = finding.get('url', '')
        vulnerability['LocationFull'] = finding.get('url', '')
        vulnerability['Recommendation'] = plugin.get('solution')

        if finding['state'] == 'FIXED':
            vulnerability['RemovedDate'] = finding.get('last_fixed')

        ids = [f"CWE-{c}" for c in plugin.get('cwe', [])]
        if not ids:
            owasp = (plugin.get('owasp_2021') or plugin.get('owasp_2017') or
                     plugin.get('owasp_api_2019') or [])
            ids = list(owasp)
        if not ids:
            ids = [str(plugin['id'])]
        vulnerability['Id'] = ids

        scanner = vulnerability['Scanner']
        scanner['Id'] = finding['finding_id']
        scanner['AssessmentType'] = "DAST"
        scanner['Product'] = 'Tenable'
        scanner['Vendor'] = 'Tenable'
        scanner['GuiUrl'] = (
            f"https://cloud.tenable.com/was/scans/{finding['scan']['uuid']}"
            f"/vulnerabilities/{finding['finding_id']}"
        )

        q_issue_doc = self.map_issue_attributes(q_issue_doc, finding)
        return q_issue_doc

    def map_issue_attributes(self, q_issue_doc, finding):
        plugin = finding['plugin']
        attributes = q_issue_doc['Saltminer']['Attributes']

        # Core WAS identifiers
        attributes['was_asset_id'] = finding['asset']['uuid']
        attributes['was_plugin_id'] = str(plugin['id'])
        attributes['was_vuln_id'] = finding['finding_id']
        attributes['was_scan_id'] = finding['scan']['uuid']
        attributes['was_uri'] = finding.get('url')
        attributes['was_output'] = finding.get('output')
        attributes['issue_last_found'] = finding['last_found']

        # Plugin metadata
        attributes['was_risk_factor'] = plugin.get('risk_factor')
        attributes['was_synopsis'] = plugin.get('synopsis')

        # Severity override (only set when Tenable changed the default severity)
        mod_type = finding.get('severity_modification_type')
        if mod_type and mod_type != 'NONE':
            attributes['was_original_severity'] = mod_type

        # CVSS scores
        if plugin.get('cvss2_base_score') is not None:
            attributes['cvss_base_score'] = str(plugin['cvss2_base_score'])
        if plugin.get('cvss3_base_score') is not None:
            attributes['cvss3_base_score'] = str(plugin['cvss3_base_score'])
        if plugin.get('cvss4_base_score') is not None:
            attributes['cvss4_base_score'] = str(plugin['cvss4_base_score'])

        # Risk classification
        if plugin.get('wasc'):
            attributes['wasc'] = ", ".join(str(w) for w in plugin['wasc'])

        owasp_keys = ['owasp_2010', 'owasp_2013', 'owasp_2017', 'owasp_2021', 'owasp_api_2019']
        owasp_all = []
        for k in owasp_keys:
            owasp_all.extend(plugin.get(k, []))
        if owasp_all:
            attributes['owasp'] = ", ".join(str(o) for o in owasp_all)

        if (vpr := plugin.get('vpr')):
            attributes['vpr_score'] = str(vpr['score'])

        return q_issue_doc
