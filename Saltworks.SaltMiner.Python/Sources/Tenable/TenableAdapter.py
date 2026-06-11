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
from abc import ABC, abstractmethod
from datetime import datetime, timezone, timedelta

from Sources.Tenable.TenableClient import TenableClient
from Core.SmDocsAndDTOs import SnykDocs
from Core.SmDataClient import SmDataClient
from Core.ElasticClient import ElasticClient


class TenableAdapterBase(ABC):
    def __init__(self, appSettings):
        self.tenable_client = TenableClient(appSettings)
        self.data_client = SmDataClient(appSettings, "Tenable")
        self.sm_docs = SnykDocs()
        self._es = ElasticClient(appSettings)
        self.asset_data = {}

    def _load_asset_data(self):
        self.asset_data = {}
        for asset in self.tenable_client.get_assets_generator():
            entry = {}
            if asset.get('tags'):
                entry['tags'] = ",".join(
                    [item['key'] + "|" + item['value'] + "|" + item["uuid"] for item in asset['tags']]
                )
            attributes = {}
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
            entry['attributes'] = attributes
            self.asset_data[asset['id']] = entry

    def _sm_scans_generator(self, index, agg_query):
        if self._es.IndexExists(index):
            search = self._es.Search(index=index, queryBody=agg_query, navToData=False, size=1)
            if search and search.get('aggregations'):
                yield from search['aggregations']['2']['buckets']

    def _get_sm_scans(self, index, agg_query):
        sm_scan_data_dict = {}
        for agg in self._sm_scans_generator(index, agg_query):
            if agg['key'] not in sm_scan_data_dict:
                sm_scan_data_dict[agg['key']] = agg['4']['value_as_string']
        return sm_scan_data_dict

    def _build_scan_doc(self):
        q_scan_doc = self.sm_docs.map_scan_doc()
        q_scan_doc['Timestamp'] = datetime.now(timezone.utc).strftime("%Y-%m-%dT%H:%M:%S.%f")[:-3] + "Z"
        q_scan_doc['Saltminer']['Internal']['IssueCount'] = -1
        scan = q_scan_doc['Saltminer']['Scan']
        scan['Attributes'] = {}
        scan['Product'] = "Tenable"
        scan['Vendor'] = "Tenable"
        scan['SourceType'] = "Saltworks.Tenable"
        scan['Instance'] = "Tenable1"
        scan['AssetType'] = "app"
        return q_scan_doc

    def map_asset(self, record, queue_scan_id):
        q_asset_doc = self.sm_docs.map_asset_doc()
        q_asset_doc['Timestamp'] = datetime.now(timezone.utc).strftime("%Y-%m-%dT%H:%M:%S.%f")[:-3] + "Z"
        q_asset_doc['Saltminer']['Internal']['QueueScanId'] = queue_scan_id
        sm_asset = q_asset_doc['Saltminer']['Asset']
        sm_asset['VersionId'] = record['asset']['uuid']
        sm_asset['SourceId'] = record['asset']['uuid']
        sm_asset['Instance'] = 'Tenable1'
        sm_asset['AssetType'] = 'app'
        sm_asset['SourceType'] = 'Saltworks.Tenable'
        self._map_asset_specifics(sm_asset, record)
        return q_asset_doc

    @abstractmethod
    def _map_asset_specifics(self, sm_asset, record): ...

    def map_issue(self, record, current_scan_dict):
        q_issue_doc = self.sm_docs.map_issue_doc()
        q_issue_doc['Timestamp'] = datetime.now(timezone.utc).strftime("%Y-%m-%dT%H:%M:%S.%f")[:-3] + "Z"
        saltminer = q_issue_doc['Saltminer']
        saltminer['QueueScanId'] = current_scan_dict['queue_scan_id']
        saltminer['QueueAssetId'] = current_scan_dict['queue_asset_id']
        vulnerability = q_issue_doc['Vulnerability']
        vulnerability['Severity'] = record['severity'].title()
        vulnerability['FoundDate'] = record['first_found']
        vulnerability['ReportId'] = current_scan_dict['report_id']
        vulnerability['Recommendation'] = record['plugin'].get('solution')
        if record['state'] == 'FIXED':
            vulnerability['RemovedDate'] = record.get('last_fixed')
        scanner = vulnerability['Scanner']
        scanner['Product'] = 'Tenable'
        scanner['Vendor'] = 'Tenable'
        self._map_issue_specifics(q_issue_doc, record, current_scan_dict)
        return q_issue_doc

    @abstractmethod
    def _map_issue_specifics(self, q_issue_doc, record, current_scan_dict): ...


class TenableAdapter:
    def __init__(self, appSettings):
        self._settings = appSettings
        self.vuln_management = appSettings.GetSource("Tenable", "VulnManagement")
        self.was = appSettings.GetSource("Tenable", "WAS")

    def run_sync(self, first_load=False):
        if self.was:
            TenableWasAdapter(self._settings).run_process(first_load)
        if self.vuln_management:
            TenableVulnManagementAdapter(self._settings).run_process(first_load)


class TenableVulnManagementAdapter(TenableAdapterBase):
    def __init__(self, appSettings):
        super().__init__(appSettings)
        self.sm_scan_data_dict = {}
        self.current_scan_asset_dict = {}
        self.first_load = False

    def run_process(self, first_load=False):
        self.first_load = first_load
        self._load_asset_data()
        if not self.first_load:
            self.sm_scan_data_dict = self._get_sm_scans(
                index="issues_app_saltworks.tenable_tenable1",
                agg_query=self.schedule_uuid_agg_query()
            )
        self.compare_tenable_scans()

    def compare_tenable_scans(self, scan_id_key='schedule_uuid', date_field='last_modification_date'):
        if self.first_load:
            scan_record = {
                "uuid": "None",
                "last_modification_date": int((datetime.now(timezone.utc) - timedelta(days=30)).timestamp())
            }
            self.sync_scan(scan_record)
        else:
            for scan_record in self.tenable_client.get_vm_scans_generator():
                if self.sm_scan_data_dict.get(scan_record[scan_id_key]):
                    sm_last_found = datetime.strptime(
                        self.sm_scan_data_dict[scan_record[scan_id_key]], "%Y-%m-%dT%H:%M:%S.%fZ"
                    )
                    scan_last_mod = datetime.fromtimestamp(scan_record[date_field], tz=timezone.utc).replace(tzinfo=None)
                    if scan_last_mod <= sm_last_found:
                        continue
                self.sync_scan(scan_record)

    def sync_scan(self, scan_record):
        if scan_record.get('uuid'):
            for issue_record in self.tenable_client.get_vm_vuln_export_generator(scan_record['uuid']):
                if not self.current_scan_asset_dict.get(issue_record['asset']['uuid']):
                    mapped_scan = self.map_scan(scan_record, issue_record)
                    queue_scan = self.data_client.AddQueueScan(mapped_scan)
                    queue_asset = self.data_client.AddQueueAsset(self.map_asset(issue_record, queue_scan['id']))
                    self.current_scan_asset_dict[issue_record['asset']['uuid']] = {
                        "queue_scan_id": queue_scan['id'],
                        "queue_asset_id": queue_asset['id'],
                        "report_id": mapped_scan['Saltminer']['Scan']['ReportId'],
                        "schedule_uuid": scan_record['schedule_uuid'] if scan_record.get('schedule_uuid') else "None",
                    }
                self.data_client.AddQueueIssue(
                    self.map_issue(issue_record, self.current_scan_asset_dict[issue_record['asset']['uuid']])
                )
            self.finalize_all_scans()

    def finalize_all_scans(self):
        self.data_client.SendAllBatchIssues()
        for _, queue_scan_data in self.current_scan_asset_dict.items():
            self.data_client.FinalizeQueue(queue_scan_data['queue_scan_id'])
        self.current_scan_asset_dict = {}

    def map_scan(self, scan_record, issue_record):
        q_scan_doc = self._build_scan_doc()
        scan = q_scan_doc['Saltminer']['Scan']
        scan['ReportId'] = (
            scan_record['uuid'] + " | " +
            issue_record['asset']['uuid'] + " | " +
            datetime.now(timezone.utc).strftime("%Y-%m-%dT%H:%M:%S.%f")[:-3] + "Z"
        )
        dt = datetime.fromtimestamp(scan_record['last_modification_date'], tz=timezone.utc)
        scan['ScanDate'] = dt.strftime("%Y-%m-%dT%H:%M:%S.%f")[:-3] + "Z"
        scan['AssessmentType'] = "SAST"
        scan['ProductType'] = 'app'
        return q_scan_doc

    def _map_asset_specifics(self, sm_asset, record):
        asset_name = record["asset"]["netbios_name"] if record['asset'].get('name') else record['asset']['hostname']
        sm_asset['Name'] = asset_name
        sm_asset['Version'] = asset_name
        sm_asset['Ip'] = record['asset'].get('ipv4')
        sm_asset['Host'] = record['asset'].get('hostname')
        sm_asset['Port'] = record['port']['port'] if record.get('port') else 'None'
        sm_asset['Scheme'] = record['port']['protocol'] if record.get('port') else 'None'

        asset_info = self.asset_data.get(record['asset'].get('uuid'), {})
        sm_asset['Attributes'] = {}
        if (asset_tags := asset_info.get('tags')):
            sm_asset['Attributes']['tenable_asset_tags'] = asset_tags
        for key, value in asset_info.get('attributes', {}).items():
            sm_asset['Attributes'][key] = value
        sm_asset['Attributes']['agent_uuid'] = record['asset'].get('agent_uuid')
        sm_asset['Attributes']['bios_uuid'] = record['asset'].get('bios_uuid')
        sm_asset['Attributes']['fqdn'] = record['asset'].get('fqdn')
        sm_asset['Attributes']['last_scan_target'] = record['asset'].get('last_scan_target')

    def _map_issue_specifics(self, q_issue_doc, record, current_scan_dict):
        asset_name = record['asset']['netbios_name'] if record['asset'].get('netbios_name') else record['asset']['hostname']
        vulnerability = q_issue_doc['Vulnerability']
        scanner = vulnerability['Scanner']

        vulnerability['Description'] = record['plugin'].get('description')
        vulnerability['Id'] = (
            list(record['plugin'].get('cve', []))
            if record['plugin'].get('cve')
            else ["None"]
        )
        vulnerability['Name'] = record['plugin']['name']
        vulnerability['Location'] = asset_name
        vulnerability['LocationFull'] = asset_name + "|" + str(record['port']['port']) + "|" + record['port']['protocol']
        scanner['Id'] = record['finding_id'] + " | " + asset_name
        scanner['AssessmentType'] = "SAST"
        scanner['GuiUrl'] = f"https://cloud.tenable.com/vm/#/explore/findings/host-vulnerabilities/finding-details/{record['finding_id']}"

        self.map_issue_attributes(q_issue_doc, record, current_scan_dict)

    def map_issue_attributes(self, q_issue_doc, record, current_scan_dict):
        attributes = q_issue_doc['Saltminer']['Attributes']
        operating_systems = record['asset'].get('operating_system') or ['None']
        operating_systems_joined = ", ".join(operating_systems) if len(operating_systems) > 1 else operating_systems[0]

        attributes['status'] = record['state']
        attributes['issue_last_found'] = record['last_found']
        attributes['tenable_schedule_uuid'] = current_scan_dict['schedule_uuid']
        attributes['operating_systems'] = operating_systems_joined
        attributes['operating_system'] = operating_systems[0]
        attributes['ipv6'] = record['asset'].get('ipv6')
        attributes['mac_address'] = record['asset'].get('mac_address')
        attributes["exploit_available"] = str(record['plugin'].get('exploit_available'))
        attributes["exploit_framework_canvas"] = str(record['plugin'].get('exploit_framework_canvas'))
        attributes["exploit_framework_core"] = str(record['plugin'].get('exploit_framework_core'))
        attributes["exploit_framework_d2_elliot"] = str(record['plugin'].get('exploit_framework_d2_elliot'))
        attributes["exploit_framework_exploithub"] = str(record['plugin'].get('exploit_framework_exploithub'))
        attributes["exploit_framework_metasploit"] = str(record['plugin'].get('exploit_framework_metasploit'))
        attributes["exploited_by_malware"] = str(record['plugin'].get('exploited_by_malware'))
        attributes["exploited_by_nessus"] = str(record['plugin'].get('exploited_by_nessus'))
        attributes["has_patch"] = str(record['plugin'].get('has_patch'))
        attributes["risk_factor"] = record['plugin'].get('risk_factor')
        attributes["in_the_news"] = str(record['plugin'].get('in_the_news'))
        attributes["unsupported_by_vendor"] = str(record['plugin'].get('unsupported_by_vendor'))
        attributes["has_workaround"] = str(record['plugin'].get('has_workaround'))
        if (vpr := record['plugin'].get("vpr")):
            attributes['vpr_score'] = str(vpr['score'])
        if (cvss3 := record['plugin'].get('cvss3_base_score')):
            attributes['cvss3_base_score'] = str(cvss3)
        if (cvss3_temp := record['plugin'].get('cvss3_temporal_score')):
            attributes['cvss3_temporal_score'] = str(cvss3_temp)
        if (cvss := record['plugin'].get('cvss_base_score')):
            attributes['cvss_base_score'] = str(cvss)
        if (cvss_temp := record['plugin'].get('cvss_base_score')):
            attributes['cvss_temporal_score'] = str(cvss_temp)

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


class TenableWasAdapter(TenableAdapterBase):
    def __init__(self, appSettings):
        super().__init__(appSettings)
        self.current_scan_asset_dict = {}
        self.was_scan_data_dict = {}

    def run_process(self, first_load=False):
        self._load_asset_data()
        if not first_load:
            self.was_scan_data_dict = self._get_sm_scans(
                index="issues_app_saltworks.tenable_tenable1",
                agg_query=self.was_last_updated_query()
            )
        for finding in self.tenable_client.get_was_export_generator():
            asset_uuid = finding['asset']['uuid']

            if self.was_scan_data_dict.get(asset_uuid):
                sm_last_found = datetime.strptime(
                    self.was_scan_data_dict[asset_uuid], "%Y-%m-%dT%H:%M:%S.%fZ"
                )
                finding_last_found = datetime.strptime(finding['last_found'], "%Y-%m-%dT%H:%M:%S.%fZ")
                if finding_last_found <= sm_last_found:
                    continue

            if asset_uuid not in self.current_scan_asset_dict:
                mapped_scan = self.map_scan(finding)
                queue_scan = self.data_client.AddQueueScan(mapped_scan)
                queue_asset = self.data_client.AddQueueAsset(self.map_asset(finding, queue_scan['id']))
                self.current_scan_asset_dict[asset_uuid] = {
                    "queue_scan_id": queue_scan['id'],
                    "queue_asset_id": queue_asset['id'],
                    "report_id": mapped_scan['Saltminer']['Scan']['ReportId'],
                }

            self.data_client.AddQueueIssue(
                self.map_issue(finding, self.current_scan_asset_dict[asset_uuid])
            )

        self.finalize_all_scans()
        logging.info("Tenable WAS sync completed - %s", datetime.now(timezone.utc).isoformat())

    def finalize_all_scans(self):
        self.data_client.SendAllBatchIssues()
        for _, scan_data in self.current_scan_asset_dict.items():
            self.data_client.FinalizeQueue(scan_data['queue_scan_id'])
        self.current_scan_asset_dict = {}

    def map_scan(self, finding):
        q_scan_doc = self._build_scan_doc()
        scan = q_scan_doc['Saltminer']['Scan']
        scan['ReportId'] = finding['asset']['uuid'] + " | " + finding['asset']['fqdn'] + " | " + str(datetime.now(timezone.utc))
        scan['ScanDate'] = finding['scan']['completed_at']
        scan['AssessmentType'] = "DAST"
        scan['ProductType'] = 'App'
        return q_scan_doc

    def _map_asset_specifics(self, sm_asset, record):
        asset = record['asset']
        url = record.get('url', '')
        scheme = url.split("://")[0] if "://" in url else "https"

        sm_asset['Name'] = asset['fqdn']
        sm_asset['Version'] = asset['fqdn']
        sm_asset['Ip'] = asset.get('ipv4')
        sm_asset['Host'] = asset['fqdn']
        sm_asset['Port'] = record['port']['port'] if record.get('port') else 0
        sm_asset['Scheme'] = scheme

        asset_info = self.asset_data.get(asset['uuid'], {})
        sm_asset['Attributes'] = {
            "was_asset_id": asset['uuid'],
            "was_asset_fqdn": asset['fqdn'],
        }
        if (asset_tags := asset_info.get('tags')):
            sm_asset['Attributes']['tenable_asset_tags'] = asset_tags

    def _map_issue_specifics(self, q_issue_doc, record, _current_scan_dict):
        plugin = record['plugin']
        vulnerability = q_issue_doc['Vulnerability']
        scanner = vulnerability['Scanner']

        vulnerability['Description'] = plugin.get('description') or record.get('output')
        vulnerability['Name'] = plugin.get('name') or str(plugin['id'])
        vulnerability['Location'] = record.get('url', '')
        vulnerability['LocationFull'] = record.get('url', '')

        ids = [f"CWE-{c}" for c in plugin.get('cwe', [])]
        if not ids:
            owasp = (plugin.get('owasp_2021') or plugin.get('owasp_2017') or plugin.get('owasp_api_2019') or [])
            ids = list(owasp)
        if not ids:
            ids = [str(plugin['id'])]
        vulnerability['Id'] = ids

        scanner['Id'] = record['finding_id']
        scanner['AssessmentType'] = "DAST"
        scanner['GuiUrl'] = (
            f"https://cloud.tenable.com/was/scans/{record['scan']['uuid']}"
            f"/vulnerabilities/{record['finding_id']}"
        )

        self.map_issue_attributes(q_issue_doc, record)

    def map_issue_attributes(self, q_issue_doc, record):
        plugin = record['plugin']
        attributes = q_issue_doc['Saltminer']['Attributes']

        attributes['was_asset_id'] = record['asset']['uuid']
        attributes['was_plugin_id'] = str(plugin['id'])
        attributes['was_vuln_id'] = record['finding_id']
        attributes['was_scan_id'] = record['scan']['uuid']
        attributes['was_uri'] = record.get('url')
        attributes['was_output'] = record.get('output')
        attributes['issue_last_found'] = record['last_found']

        attributes['was_risk_factor'] = plugin.get('risk_factor')
        attributes['was_synopsis'] = plugin.get('synopsis')

        mod_type = record.get('severity_modification_type')
        if mod_type and mod_type != 'NONE':
            attributes['was_original_severity'] = mod_type

        if plugin.get('cvss2_base_score') is not None:
            attributes['cvss_base_score'] = str(plugin['cvss2_base_score'])
        if plugin.get('cvss3_base_score') is not None:
            attributes['cvss3_base_score'] = str(plugin['cvss3_base_score'])
        if plugin.get('cvss4_base_score') is not None:
            attributes['cvss4_base_score'] = str(plugin['cvss4_base_score'])

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

    def was_last_updated_query(self):
        return {
            "aggs": {
                "2": {
                    "terms": {
                        "field": "saltminer.attributes.was_asset_id",
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
