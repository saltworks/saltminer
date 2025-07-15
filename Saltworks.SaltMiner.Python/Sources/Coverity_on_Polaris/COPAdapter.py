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

    def run_sync(self):
        # if self.first_load:
        #     for project in self.cop_client.get_projects_generator():

        #         project_id = project['id']
        #         project_name = project['attributes']['name']


        #         for run in self.cop_client.get_runs_generator(project_id=project_id):
        #             run_id = run['id']

        #             for issue in self.cop_client.get_issues_by_run_generator(project_id=project_id, run_id=run_id):
        #                 self.counter += 1
        #             print(f"{project_name} {self.counter}")
        #             self.counter= 0
      
        #else:
        self.get_projects_data()
        for run in self.cop_client.get_runs_generator(project_id, recipe="latest-completed-run-by-project"):
            run_id = run['id']
            project_id = run['relationships']['project']['data']['id']
            self.sync_issues(project_id, run_id)
  


    def sync_issues(self, project_id, run_id):

        project_name = self.projects_data[project_id]['project_name']
        issues_generator = self.cop_client.get_issues_by_run_generator(project_id=project_id, run_id=run_id)
        first_page = next(issues_generator, None)
        if first_page and first_page.get('data'):
            included_lookup = {
                item["id"]: item for item in first_page.get("included", [])
            }

            mapped_scan = self.map_scan()
            queue_scan = self._sm_data_client.AddQueueScan(json.loads(mapped_scan.model_dump_json()))

            mapped_asset = self.map_asset()
            queue_asset = self._sm_data_client.AddQueueAsset(json.loads(mapped_asset.model_dump_json()))

            for issue in first_page:
                mapped_issue = self.map_issue(issue, queue_asset['id'], queue_scan['id'], queue_scan['saltminer']['scan']['reportId'], included_lookup)
                self._sm_data_client.AddQueueIssue(json.loads(mapped_issue.model_dump_json()))

            for page in issues_generator:
                included_lookup = {
                    item["id"]: item for item in page.get("included", [])
                }
                for issue in page.get('data', []):
                    mapped_issue = self.map_issue(issue, queue_asset['id'], queue_scan['id'], queue_scan['saltminer']['scan']['reportId'], included_lookup)
                    self._sm_data_client.AddQueueIssue(json.loads(mapped_issue.model_dump_json()))
            
            self._sm_data_client.SendAllBatchIssues()
            self._sm_data_client.FinalizeQueue(queue_scan['id'])


    def map_issue(self, issue, q_asset_id, q_scan_id, report_id, included_lookup):
        pass


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

        return MapAssetDocDTO(**q_asset_doc)
        

    def format_date(self, date_string):
        if date_string:
            dt = datetime.strptime(date_string, "%Y-%m-%dT%H:%M:%S.%fZ")
            formatted_date = dt.strftime("%Y-%m-%dT%H:%M:%S.%f")[:-3] + "Z"
            return formatted_date
        return None


    def get_projects_data(self):
        for project in self.cop_client.get_projects_generator():
            data_doc = self.projects_data_doc()
            data_doc['project_id'] = project.get('id')
            data_doc['project_name'] = project.get('name')
            data_doc['project_description'] = project['attributes'].get('description')
            data_doc['project_link'] = project.get('links',{}).get('self', {}).get('href')
            self.projects_data[project.get('id')] = data_doc


    def projects_data_doc(self):
        return {
            "project_id": None,
            "project_name": None,
            "project_description": None,
            "project_link": None
        }