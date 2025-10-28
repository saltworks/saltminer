''' --[auto-generated, do not modify this block]--
*
* Copyright (c) 2025 Saltworks Security, LLC
*
* Use of this software is governed by the Business Source License included
* in the LICENSE file.
*
* Change Date: 2029-10-28
*
* On the date above, in accordance with the Business Source License, use
* of this software will be governed by version 2 or later of the General
* Public License.
*
* ----
'''

import json
import logging

from datetime import datetime, timezone, timedelta

from Sources.Coverity_on_Polaris.COPClient import COPClient
from Core.SmDocsAndDTOs import SnykDocs, MapAssetDocDTO, MapIssueDocDTO, MapScanDocDTO
from Core.ElasticClient import ElasticClient
from Core.SmDataClient import SmDataClient

class COPAdapter:
    
    def __init__(self, settings, first_load = False):
        self.cop_client = COPClient(settings)
        self._es = ElasticClient(settings)
        self._sm_data_client = SmDataClient(settings, sourceName='COP')
        self.sm_docs = SnykDocs()
        self.first_load = first_load
        self.counter = 0
        self.projects_data = {}
        self.issues_included = {}
        self.sm_scan_dates = {}

    def run_sync(self, first_load):
        
        self.get_projects_data()
        self.get_projects_data(in_trash=True)
        if not first_load:
            self.get_sm_scan_dates()
        for run in self.cop_client.get_runs_generator(recipe="latest-completed-run-by-project"):
            if not first_load:
                if self.parse_date(run['attributes']['completed-date']) <= self.parse_date(self.sm_scan_dates[run['id']]):
                    continue
            project_id = run['relationships']['project']['data']['id']
            if self.projects_data.get(project_id):
                self.sync_issues(project_id, run)
    

    def sync_issues(self, project_id, run):
        run_id = run ['id']
        issues_generator = self.cop_client.get_issues_by_run_generator(project_id=project_id, run_id=run_id)
        first_page = next(issues_generator, None)
        if first_page and first_page.get('data'):
            included_lookup = {
                item["id"]: item for item in first_page.get("included", [])
            }

            mapped_scan = self.map_scan(run)
            queue_scan = self._sm_data_client.AddQueueScan(json.loads(mapped_scan.model_dump_json()))


            mapped_asset = self.map_asset(queue_scan['id'], project_id)
            queue_asset = self._sm_data_client.AddQueueAsset(json.loads(mapped_asset.model_dump_json()))

            for issue in first_page['data']:
                mapped_issue = self.map_issue(issue, queue_asset['id'], queue_scan['id'], queue_scan['saltminer']['scan']['reportId'], included_lookup, run)
                self._sm_data_client.AddQueueIssue(json.loads(mapped_issue.model_dump_json()))

            for page in issues_generator:
                included_lookup = {
                    item["id"]: item for item in page.get("included", [])
                }
                for issue in page.get('data', []):
                    mapped_issue = self.map_issue(issue, queue_asset['id'], queue_scan['id'], queue_scan['saltminer']['scan']['reportId'], included_lookup, run)
                    self._sm_data_client.AddQueueIssue(json.loads(mapped_issue.model_dump_json()))

            
            self._sm_data_client.SendAllBatchIssues()
            self._sm_data_client.FinalizeQueue(queue_scan['id'])


    def map_issue(self, issue, q_asset_id, q_scan_id, report_id, included_lookup, run):
        relationships = issue['relationships']
        q_issue_doc = self.sm_docs.map_issue_doc()
        q_issue_doc['Saltminer']['QueueAssetId'] = q_asset_id
        q_issue_doc['Saltminer']['QueueScanId'] = q_scan_id
        q_issue_doc['Timestamp'] = datetime.now().strftime("%Y-%m-%dT%H:%M:%S.%f")[:-3] + "Z"
        
        vulnerability = q_issue_doc['Vulnerability']
        vulnerability['Severity'] = (issue['relationships'].get('severity')['data']['id']).title() if issue['relationships'].get('severity') else "Info"
        vulnerability['Name'] = included_lookup.get(relationships['issue-type']['data']['id'], {}).get('attributes', {}).get('name')
        vulnerability['Description'] = included_lookup.get(relationships['issue-type']['data']['id'], {}).get('attributes', {}).get('description')
        vulnerability['Implication'] = included_lookup.get(relationships['issue-type']['data']['id'], {}).get('attributes', {}).get('local-effect')
        vulnerability['Location']=",".join(included_lookup.get(relationships['path']['data']['id'], {}).get('attributes', {}).get('path'))
        vulnerability['LocationFull']=",".join(included_lookup.get(relationships['path']['data']['id'], {}).get('attributes', {}).get('path'))
        vulnerability['FoundDate'] = self.get_transition_date(
            ids=[item['data'].get('id') for item in relationships['transitions']], 
            included_lookup= included_lookup, 
            transition_type='opened'
            )
        if not vulnerability['FoundDate']:
            vulnerability['FoundDate'] = self.format_date(run['attributes']['completed-date'])
        
        if (removed_date := self.get_transition_date(
            ids=[item['data'].get('id') for item in relationships['transitions']], 
            included_lookup= included_lookup, 
            transition_type='closed'
            )):
            vulnerability['RemovedDate'] = removed_date

        vulnerability['ReportId']= report_id
        vulnerability['Scanner']['GuiUrl'] = issue.get('links', {}).get('self',{}).get('href')
        vulnerability['Scanner']['Id'] = issue['attributes'].get('issue-key')
        vulnerability['Scanner']['AssessmentType'] = 'SAST'
        vulnerability['Scanner']['Product'] = "Coverity on Polaris"
        vulnerability['Scanner']['Vendor']= "Black Duck"

        issue_attributes = q_issue_doc['Saltminer']['Attributes']
        issue_attributes['sub-tool'] = issue['attributes'].get('sub-tool')
        if (tool_domain_id := relationships.get('tool-domain-service', {}).get('data', {}).get('id')):

            issue_attributes['tool-domain-service'] = included_lookup.get(tool_domain_id).get('attributes', {}).get('name')

        if (tool_id := relationships.get('tool', {}).get('data', {}).get('id')):
            tool_name = included_lookup.get(tool_id).get('attributes', {}).get('name')
            tool_version =included_lookup.get(tool_id).get('attributes', {}).get('version')
            issue_attributes['tool'] = f"{tool_name} {tool_version}"

        return MapIssueDocDTO(**q_issue_doc)


    def map_scan(self, run):
        q_scan_doc = self.sm_docs.map_scan_doc()
        q_scan_doc['Timestamp'] = datetime.now().strftime("%Y-%m-%dT%H:%M:%S.%f")[:-3] + "Z"
        q_scan_doc['Saltminer']['Internal']['IssueCount'] = -1  #Setting this value to -1 disables IssueCount validation
        q_scan_doc['Saltminer']['Internal']['ReplaceIssues'] = False

        scan= q_scan_doc['Saltminer']['Scan']
        scan['Product'] = "Coverity on Polaris"
        scan['Vendor'] = 'Black Duck'
        scan['Instance'] = 'CoP1'
        scan['SourceType']= 'Saltworks.CoP'
        scan['ReportId'] = run['id']
        scan['ScanDate'] = self.format_date(run['attributes'].get('completed-date'))
        scan['AssessmentType'] = 'SAST'
        scan['ProductType'] = 'Application'
        scan['AssetType'] = 'app'

        return MapScanDocDTO(**q_scan_doc)


    def map_asset(self, q_scan_id, project_id):
        project_data = self.projects_data[project_id]
        q_asset_doc = self.sm_docs.map_asset_doc()
        q_asset_doc['Timestamp'] = datetime.now().strftime("%Y-%m-%dT%H:%M:%S.%f")[:-3] + "Z"
        q_asset_doc['Saltminer']['Internal']['QueueScanId'] = q_scan_id
        asset = q_asset_doc['Saltminer']['Asset']
        asset['Name'] = project_data['project_name']
        asset['Version'] = project_data['project_name']
        asset['VersionId'] = project_id
        asset['SourceId'] = project_id
        asset['AssetType'] = 'app'
        asset['Instance'] = 'CoP1'
        asset['SourceType'] = 'Saltworks.Cop'
        asset['Attributes']['project_url'] = project_data['project_link']
        asset['Attributes']['in_trash'] = str(project_data['in_trash'])

        return MapAssetDocDTO(**q_asset_doc)
        

    def get_sm_scan_dates(self):
        if self._es.IndexExists('scans_app_saltworks.cop_cop1'):
            with self._es.SearchScroll('scans_app_saltworks.cop_cop1', scrollSize=10) as scroller:
                while len(scroller.Results):
                    for p in scroller.Results:
                        self.sm_scan_dates[p['_source']['saltminer']['scan']['report_id']] = p['_source']['saltminer']['scan']['scan_date']
                    scroller.GetNext()
        else:
            return None

    def format_date(self, date_string):
        if date_string:
            dt = datetime.strptime(date_string, "%Y-%m-%dT%H:%M:%S.%fZ")
            formatted_date = dt.strftime("%Y-%m-%dT%H:%M:%S.%f")[:-3] + "Z"
            return formatted_date
        return None

    def parse_date(self, date_string):
        return datetime.strptime(date_string, "%Y-%m-%dT%H:%M:%S.%fZ")

    def get_transition_date(self, ids, included_lookup, transition_type):
        for id in ids:
            data = included_lookup.get(id)
            if data['attributes'].get('transition-type') == transition_type:
                return self.format_date(data['attributes'].get('transition-date'))
            else:
                return None
            

    def get_projects_data(self, in_trash= False):
        for project in self.cop_client.get_projects_generator(in_trash=in_trash):
            
            if project.get('id') not in self.projects_data.keys():
                data_doc = self.projects_data_doc()
                data_doc['project_id'] = project.get('id')
                data_doc['project_name'] = project['attributes'].get('name')
                data_doc['project_description'] = project['attributes'].get('description')
                data_doc['project_link'] = project.get('links',{}).get('self', {}).get('href')
                if in_trash:
                    data_doc['in_trash'] = True
                self.projects_data[project.get('id')] = data_doc


    def projects_data_doc(self):
        return {
            "project_id": None,
            "project_name": None,
            "project_description": None,
            "project_link": None,
            "in_trash": False
        }