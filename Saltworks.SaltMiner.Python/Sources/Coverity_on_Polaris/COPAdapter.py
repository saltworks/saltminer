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
        self.data_client = SmDataClient(settings, sourceName='COP')
        self.sm_docs = SnykDocs()
        self.first_load = first_load
        self.counter = 0


    def run_sync(self):
        if self.first_load:
            for project in self.cop_client.get_projects_generator():

                project_id = project['id']
                project_name = project['attributes']['name']


                for run in self.cop_client.get_runs_generator(project_id=project_id):
                    run_id = run['id']

                    for issue in self.cop_client.get_issues_by_run_generator(project_id=project_id, run_id=run_id):
                        self.counter += 1
                    print(f"{project_name} {self.counter}")
                    self.counter= 0
      
        else:
            for run in self.cop_client.get_runs_generator(project_id, recipe="latest-completed-run-by-project"):
                run_id = run['id']
                project_id = run['relationships']['project']['data']['id']
                for issue in self.cop_client.get_issues_by_run_generator(project_id=project_id, run_id=run_id):
                    pass     


    def sync_issues(self, project, run_id):
        project_id = project['id']
        project_name = project['attributes']['name']
        for issue in self.cop_client.get_issues_by_run_generator(project_id=project_id, run_id=run_id):
            self.counter += 1
        print(f"{project_name} {self.counter}")
        self.counter= 0

    def map_issue(self):
        pass


    def map_scan(self):
        pass


    def map_asset(self):
        pass  