import json
import logging 
import time
from datetime import datetime, timezone, timedelta

from Sources.SNYK.SnykClient import SnykClient
from Core.SmDocsAndDTOs import SnykDocs, MapAssetDocDTO, MapIssueDocDTO, MapScanDocDTO

from Core.SmDataClient import SmDataClient
from Core.ElasticClient import ElasticClient

class SnykAdapter:
    """
    Handles fetching and syncing issues, scans, and assets from Snyk to Saltminer.

    Process Summary:
    - Pull `snyk_last_updated` timestamps from Saltminer to determine what needs syncing.
    - Iterate through Snyk projects and retrieve associated issues if updated.
    - For updated projects:
        • Queue the Scan
        • Queue the Asset (linked to the Scan)
        • Queue the Issues (linked to the Scan and Asset)
    - Finalize the scan to trigger Saltminer’s ingestion of queued data.

    For detailed process flow and Saltminer integration logic, see the README.
    """
    def __init__(self, settings):
        self.snyk_client = SnykClient(settings)
        self.snyk_docs = SnykDocs()
        self._es = ElasticClient(settings)
        self._sm_data_client = SmDataClient(settings, "Snyk")
        self.base_gui_url = "https://app.snyk.io/org/"
        self.prj_version_last_updated = {}


    def run_sync(self, first_load= False):
        """
        This is used to call the needed functions for the sync process
        """
        if first_load:
            self.get_prj_last_updated()

        self.get_sync(first_load=first_load)


    def get_sync(self, first_load= False):  
        """
        Fetches all data from the Snyk client by calling necessary API requests.
        - Retrieves projects.
        - Checks if issues exist for each project before processing.
        - Runs sync_issues if they exist
        """
        start_date = None
        for org in self.snyk_client.get_snyk_orgs_generator():
            org_info = {
                "slug": org['attributes']['slug'], 
                "name": org['attributes']['name']
            }
            gui_url_with_org_slug = self.base_gui_url + org_info['slug'] + "/project/"
            for project in self.snyk_client.get_snyk_projects_generator(org_id=org['id']):
                
                project_id = project.get("id") # Ensure project ID is extracted properly
                if not project_id:
                    logging.warning("Skipping project with missing ID: %s", project)
                    continue
                gui_url_with_project = gui_url_with_org_slug + project_id + "#"

                if not first_load and project_id in self.prj_version_last_updated.keys() and self.prj_version_last_updated.get(project_id):
                    date = datetime.strptime(self.prj_version_last_updated.get(project_id), "%Y-%m-%dT%H:%M:%S.%fZ")
                    start_date = date.strftime("%Y-%m-%dT%H:%M:%S.%f")[:-3] + "Z"

                issues_generator = self.snyk_client.get_sync_issues_generator(limit=100, org_id=org['id'], project_id=project_id, start_date=start_date)
                first_issue = next(issues_generator, None) # Get first issue to check if there is any data

                if first_issue:
                    self.snyc_issues(project, first_issue, issues_generator, gui_url_with_project, org_info)
                    
                else:
                    logging.info("No issues found for project %s, skipping.", project_id)


    def snyc_issues(self, project, first_issue, issues_generator, gui_url, org_info):
        """
        Runs through issues of a specific project and sends them to Saltminer.
        -Maps Scan to SM valid scan document
        -Sends mapped scan to SM queueScans
        -Maps Asset to SM valid asset document
        -Sends mapped asset to SM queueAssets
        -Maps all issues to SM valid issue documents
        -Sends mapped issues to SM queueIssues
        -Runs FinalizeQueue to set all queue document's queue status to 'Pending' so SM will process all the queued data we just sent in. 
        """
        project_id = project.get("id") # Ensure project ID is extracted properly
        counter = 0 
        logging.info("Issues found for project %s, running mapAsset() and mapScan()", project_id)
        # Run mapping functions
        try:
            #Maps Scan to SM valid scan document
            mapped_scan = self.map_scan(project)
            #Sends mapped scan to SM queue_scans
            queue_scan = self._sm_data_client.AddQueueScan(json.loads(mapped_scan.model_dump_json()))
            #Maps Asset to SM valid asset document
            mapped_asset = self.map_asset(project, queue_scan['id'], org_info)
            #Sends mapped asset to SM queue_assets
            queue_asset = self._sm_data_client.AddQueueAsset(json.loads(mapped_asset.model_dump_json()))
            
            # Process the first issue
            mapped_issue = self.map_issue(first_issue, queue_scan['id'], queue_asset['id'], queue_scan['saltminer']['scan']['reportId'], project_id, gui_url)

            #Send the first issue to SM queue_issues
            self._sm_data_client.AddQueueIssue(json.loads(mapped_issue.model_dump_json()))
            counter += 1 

            #Maps all issues to SM valid issue documents
            for issue in issues_generator:
                mapped_issue = self.map_issue(issue, queue_scan['id'], queue_asset['id'], queue_scan['saltminer']['scan']['reportId'], project_id, gui_url)
                #Sends mapped issues to SM queue_issues
                self._sm_data_client.AddQueueIssue(json.loads(mapped_issue.model_dump_json()))
                #Tracking the counter so that we can use it to add to the Scan.Issue_count in the future.
                counter += 1
            #TODO:TEST SENDALLBATCHISSUESf
            self._sm_data_client.SendAllBatchIssues()
            self._sm_data_client.FinalizeQueue(queue_scan['id'])
            logging.info("[SyncAdapter][Sync Issues] FinalizeQueue run for project id : %s, number of issues sent: %s", project_id, counter)

        except Exception as e:
            logging.error("[SnykAdapter][Sync Issues] An exception occurred during issue processing: %s", e)


    def get_prj_last_updated(self):
        """
        This is going to call Saltminer to get the last updated date of all the Snyk project versions currently present in Saltminer.
        """
        if self._es.IndexExists('issues_app_saltworks.snyk_snyk1'):
            search = self._es.Search(index ='issues_app_saltworks.snyk_snyk1', queryBody=self.last_updated_query(), size=10000, navToData=False)
            for item in search['aggregations']['version_id']['buckets']:
                self.prj_version_last_updated[item['key']] = item['snyk_last_updated'].get('value_as_string')
        else:
            return None


    def map_issue(self, issue, queue_scan_id, queue_asset_id, report_id, project_id, gui_url):
        """
        This will map the issue data into the needed format for the queue_issues
        """
        q_issue_doc = self.snyk_docs.map_issue_doc()
        q_issue_doc['Timestamp'] = datetime.now().strftime("%Y-%m-%dT%H:%M:%S.%f")[:-3] + "Z"
        saltminer = q_issue_doc['Saltminer']
        saltminer['Attributes']['snyk_last_updated'] = issue['attributes']['updated_at']
        saltminer['Attributes']['status'] = issue['attributes']['status']
        saltminer['QueueScanId'] = queue_scan_id
        saltminer['QueueAssetId']= queue_asset_id
        vulnerability = q_issue_doc['Vulnerability']

        #Setting the removed date will trigger the IsRemoved boolean to change within SM
        if issue['attributes']['status'] == 'resolved':
            if issue['attributes'].get('resolution') and issue['attributes']['resolution'].get('resolved_at'):
                vulnerability['RemovedDate'] = issue['attributes']['resolution'].get('resolved_at')

        if issue['attributes'].get('ignored') is True:
            vulnerability['IsSuppressed'] = True

        vulnerability['FoundDate'] = issue['attributes']['created_at']
        vulnerability["Id"] = [problem['id'] for problem in issue['attributes']['problems']]
        if issue["attributes"].get('classes'):
            vulnerability["Id"].extend([cls['id'] for cls in issue['attributes']['classes']])
        vulnerability['Severity'] = issue['attributes']['effective_severity_level'].title()
        vulnerability['Name'] = issue['attributes']['title']
        vulnerability['LocationFull'] = "None"
        vulnerability['Location'] = "None"
        vulnerability['ReportId'] = report_id

        scanner = vulnerability['Scanner']
        scanner['Id'] = issue['id'] + "|" + project_id
        #TODO:DETERMINE THE CORRECT SCAN TYPES AND PUT THEM HERE
        scanner['AssessmentType'] = "Open"
        scanner['Product'] = "Snyk"
        scanner['Vendor']= "Snyk"
        scanner['GuiUrl'] = gui_url + issue['attributes']['key']

        return MapIssueDocDTO(**q_issue_doc)


    def map_scan(self, project):
        """
        This will map a scan into hte format needed for queue issue import
        """
        q_scan_doc = self.snyk_docs.map_scan_doc()
        q_scan_doc['Timestamp'] = datetime.now().strftime("%Y-%m-%dT%H:%M:%S.%f")[:-3] + "Z"
        q_scan_doc['Saltminer']['Internal']['IssueCount'] = -1  #Setting this value to -1 disables IssueCount validation
        q_scan_doc['Saltminer']['Internal']['ReplaceIssues'] = True
        scan = q_scan_doc['Saltminer']['Scan']

        scan['Product'] = "Snyk"
        scan['Vendor']= "Snyk"
        scan['ReportId'] = project['id'] + project['attributes']['name'] + "|" + datetime.now().isoformat()
        scan['ScanDate'] = datetime.now().strftime("%Y-%m-%dT%H:%M:%S.%f")[:-3] + "Z"
        scan['SourceType'] = "Saltworks.Snyk"
        scan['Instance'] = "Snyk1"
        scan['AssetType'] = "app"
        scan['AssessmentType'] = "Open"
        scan['ProductType'] = "Application"

        return MapScanDocDTO(**q_scan_doc)


    def map_asset(self, project, q_scan_id, org_info):
        """
        This will map an asset into the format needed for queue issue import
        """
        split_project_path = project['attributes']['name'].split(':')
        q_asset_doc = self.snyk_docs.map_asset_doc()
        q_asset_doc['Timestamp'] = datetime.now().strftime("%Y-%m-%dT%H:%M:%S.%f")[:-3] + "Z"
        q_asset_doc['Saltminer']['Internal']['QueueScanId'] = q_scan_id
        asset = q_asset_doc['Saltminer']['Asset']
        asset['Name'] = project['attributes']['name']
        asset['Version']= split_project_path[-1]
        asset['VersionId'] = project['id']
        asset['Instance'] = "Snyk1"
        asset['SourceId'] = project['id']
        asset['AssetType']= "app"
        asset['SourceType'] = "Saltworks.Snyk"

        attributes = asset['Attributes']
        attributes['org_slug'] = org_info['slug']
        attributes['org_name'] = org_info['name'] 
        return MapAssetDocDTO(**q_asset_doc)
    

    def get_assessment_type(self, source_assessment_type):
        assessment_types = {
            "package_vulnerability": "SAST"

        }

        return assessment_types.get(source_assessment_type)


    def get_issue_counts(self, issue_counts_object):
        counter = 0 
        for item in issue_counts_object.keys():
            if item in ["critical", "high", "medium"," low"]:
                counter += issue_counts_object[item]

        return counter


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
                                "field": "saltminer.attributes.snyk_last_updated"
                            }
                        }
                    }
                }
            },
            "size": 0
        }

