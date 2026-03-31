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

import json
import logging 
import time

from datetime import datetime, timezone, timedelta
from dateutil import parser 
from dateutil.tz import gettz
from Sources.Seeker.SeekerClient import SeekerClient
from Core.SmDocsAndDTOs import SnykDocs, MapAssetDocDTO, MapIssueDocDTO, MapScanDocDTO

from Core.DataClient import DataClient, QueueStatus
from Core.ElasticClient import ElasticClient


class SeekerAdapter:

    def __init__(self, app):
        settings = app.Settings
        self.seeker_client = SeekerClient(settings)
        self.sm_docs = SnykDocs()

        self._data_client = DataClient(app)
        self._es = ElasticClient(settings)
        self.first_load = settings.GetSource("Seeker", 'First_Load')
        self.project_last_updated = {}

    def run_sync(self):
        logging.info("Run Sync Start")

        if not self.first_load:
            self.get_sm_prj_last_updated()
            
        for project in self.seeker_client.get_projects_generator():
            project_id = project['key']
            last_updated = None

            if not self.first_load:
                last_updated = self.project_last_updated.get(project_id)

            self.sync_issues(project, last_updated)
            logging.info("Sync Issues for project key %s sent", project['key'])
                    

    def sync_issues(self, project, last_updated= None):
        issues_generator = self.seeker_client.get_vulnerabilities_generator(project_key=project['key'], last_updated=last_updated)
        first_issue = next(issues_generator, None) # Get first issue to check if there is any data
        if first_issue:
            mapped_scan = self.map_scan(first_issue)
            queue_scan = self._data_client.queue_scan_add_update(json.loads(mapped_scan.model_dump_json()))

            mapped_asset = self.map_asset(project, first_issue, queue_scan['id'])
            queue_asset = self._data_client.queue_asset_add_update(json.loads(mapped_asset.model_dump_json()))

            mapped_issue = self.map_issue(first_issue, queue_asset['id'], queue_scan['id'], queue_scan['saltminer']['scan']['reportId'])
            self._data_client.queue_issue_add_update_batch(json.loads(mapped_issue.model_dump_json()))
            for issue in issues_generator:
                mapped_issue = self.map_issue(issue, queue_asset['id'], queue_scan['id'], queue_scan['saltminer']['scan']['reportId'])
                self._data_client.queue_issue_add_update_batch(json.loads(mapped_issue.model_dump_json()))

            self._data_client.queue_issue_add_update_batch(None)
            self._data_client.queue_scan_update_status(queue_scan['id'], QueueStatus.PENDING)


    def map_issue(self, issue, queue_asset_id, queue_scan_id, report_id):
        q_issue_doc = self.sm_docs.map_issue_doc()
        q_issue_doc['Saltminer']['QueueAssetId'] = queue_asset_id
        q_issue_doc['Saltminer']['QueueScanId'] = queue_scan_id
        q_issue_doc['Timestamp'] = datetime.now().strftime("%Y-%m-%dT%H:%M:%S.%f")[:-3] + "Z"

        vulnerability = q_issue_doc['Vulnerability']
        vulnerability['Name'] = issue['VulnerabilityName']
        if issue.get('Severity') == 'Informative':
            severity = 'Info'
        else:
            severity = issue.get('Severity')
        vulnerability['Severity'] = severity
        vulnerability['FoundDate'] = self.format_datetime(issue['FirstDetectionTime'])
        vulnerability['Description'] = issue['Description']
        vulnerability['Recommendation'] = issue.get('Remediation')
        vulnerability['Location'] = issue['CodeLocation'] or 'None'
        vulnerability['LocationFull'] = issue['CodeLocation'] or "None"
        vulnerability['Details'] = issue['StackTrace']
        vulnerability['ReportId'] = report_id
        vulnerability['Scanner']['GuiUrl'] = issue['SeekerServerLink']
        vulnerability['Scanner']['Id'] = issue['ItemKey']
        vulnerability['Scanner']['AssessmentType'] = 'DAST'
        vulnerability['Scanner']['Product'] = "Seeker"
        vulnerability['Scanner']['Vendor']= "Black Duck"

        issue_attributes = q_issue_doc['Saltminer']['Attributes']
        issue_attributes['VerificationProof'] = issue.get('VerificationProof')
        issue_attributes['Status'] = issue.get('Status')
        issue_attributes['VerificationTag'] = issue.get('VerificationTag')
        issue_attributes['LastDetectionTime'] = issue.get('LastDetectionTime')
        issue_attributes['LatestVersion'] = issue.get('LatestVersion')
        issue_attributes['IssueType'] = 'IAST'
        issue_attributes['OWASP2013'] = issue.get('OWASP2013') or 'None'
        issue_attributes['OWASP2017'] = issue.get('OWASP2017') or 'None'
        issue_attributes['CAPEC'] = issue.get('CAPEC') or 'None'
        issue_attributes['CWE-SANS'] = issue.get('CWE-SANS') or 'None'
        issue_attributes['PCI-DSS'] = issue.get('PCI-DSS') or 'None'
        issue_attributes['GDPR'] = issue.get('GDPR') or 'None'
        issue_attributes['OWASPAPI2019'] = issue.get('OWASPAPI2019') or 'None'
        issue_attributes['OWASPAPI2023'] = issue.get('OWASPAPI2023') or 'None'
        issue_attributes['OWASP2021'] = issue.get('OWASP2013') or 'None'
        issue_attributes['CustomTags'] = issue.get('CustomTags') or 'None'
        issue_attributes['LastDetectionURL'] = issue.get('LastDetectionURL') or 'None'
        issue_attributes['LastDetectionSourceName'] = issue.get('LastDetectionSourceName') or 'None'
        issue_attributes['LastDetectionSourceType'] = issue.get('LastDetectionSourceType') or 'None'
        
        issue_attributes['Owner'] = issue.get('Owner') or "None"
        return MapIssueDocDTO(**q_issue_doc)


    def map_asset(self, project, issue, q_scan_id):
        q_asset_doc = self.sm_docs.map_asset_doc()
        q_asset_doc['Timestamp'] = datetime.now().strftime("%Y-%m-%dT%H:%M:%S.%f")[:-3] + "Z"
        q_asset_doc['Saltminer']['Internal']['QueueScanId'] = q_scan_id
        asset = q_asset_doc['Saltminer']['Asset']
        asset['Name'] = project['name']
        asset['Version'] = project['type']
        asset['VersionId'] = project['key']
        asset['Instance'] = 'Seeker1'
        asset['SourceId'] = project['key']
        asset['AssetType'] = 'app'
        asset['SourceType'] = "Saltworks.Seeker"
        asset['Attributes']['project_url'] = project['projectUrl']
        asset['Attributes']['template_key'] = project['templateKey']
        asset['Attributes']['default_owner'] = project['defaultOwner']
        asset['Attributes']['custom_tags'] = ', '.join(project.get('customTags')) if project.get('customTags') else "None"

        return MapAssetDocDTO(**q_asset_doc)



    def map_scan(self, issue):
        q_scan_doc = self.sm_docs.map_scan_doc()
        q_scan_doc['Timestamp'] = datetime.now().strftime("%Y-%m-%dT%H:%M:%S.%f")[:-3] + "Z"
        q_scan_doc['Saltminer']['Internal']['IssueCount'] = -1  #Setting this value to -1 disables IssueCount validation
        q_scan_doc['Saltminer']['Internal']['ReplaceIssues'] = False
        q_scan_doc['Saltminer']['Internal']['QueueStatus'] = QueueStatus.LOADING

        scan = q_scan_doc['Saltminer']['Scan']
        scan['Product'] = 'Seeker'
        scan['Vendor'] = 'Black Duck'
        scan['Instance'] = 'Seeker1'
        scan['SourceType'] = 'Saltworks.Seeker'
        scan['ReportId'] = issue['ProjectKey'] + "|" + datetime.now().isoformat()
        scan['ScanDate'] = datetime.now().strftime("%Y-%m-%dT%H:%M:%S.%f")[:-3] + "Z"
        scan['AssetType'] = 'app'
        scan['AssessmentType'] = 'DAST'
        scan['ProductType'] = "Application"

        return MapScanDocDTO(**q_scan_doc)
        

    def get_sm_prj_last_updated(self):
        """
        This is going to call Saltminer to get the last updated date of all the Snyk project versions currently present in Saltminer.
        """
        if self._es.IndexExists('issues_app_saltworks.seeker_seeker1'):
            search = self._es.Search(index ='issues_app_saltworks.seeker_seeker1', queryBody=self.last_updated_query(), size=10000, navToData=False)
            for item in search['aggregations']['version_id']['buckets']:
                self.project_last_updated[item['key']] = item['seeker_last_updated'].get('value_as_string')
        else:
            return None


    def format_datetime(self, date_str: str):
        """
        Convert a datetime string with 'EDT' timezone abbreviation
        to ISO 8601 format (without timezone).

        Args:
            date_str (str): Date string in the format 'YYYY-MM-DD HH:MM:SS EDT'

        Returns:
            str: ISO 8601 formatted date string without timezone
        """
        tzinfos = {
            'EST': gettz('America/New_York'),
            'EDT': gettz('America/New_York'),
        }
        dt_with_tz = parser.parse(date_str, tzinfos=tzinfos)
        return dt_with_tz.replace(tzinfo=None).isoformat(sep='T')


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
                        "snyk_last_updated": {
                            "max": {
                                "field": "saltminer.attributes.seeker_last_updated"
                            }
                        }
                    }
                }
            },
            "size": 0
        }



