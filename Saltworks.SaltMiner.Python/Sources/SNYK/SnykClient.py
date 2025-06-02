import json
import logging
import requests

class SnykClient:
    """
    Client class for all needed snyk requests.
    """
    def __init__(self, settings):
        self.snyk_url = settings.Get("SnykClient", 'Api Url')
        self.snyk_headers = {
            "Authorization": f"token {settings.Get('SnykClient','Api Key')}",
            "Content-Type": "application/vnd.api+json"
        }
       
    def get_snyk_orgs_generator(self):
        "Gets org info"
        orgs_endpoint = "rest/orgs"
        try:
            response = requests.get(
                url= self.snyk_url + orgs_endpoint,
                params= { "version": "2024-10-14"},
                headers= self.snyk_headers,
                timeout= 30 
            )
            response.raise_for_status()
            data = response.json()

        except requests.exceptions.RequestException as e:
            logging.error("Request failed: %s", e)
            return
        
        yield from data.get('data', [])

        while data.get('links', {}).get('next'):
            try:
                data = self.get_next(url=data['links']['next'])

                yield from data.get('data', [])
            except requests.exceptions.RequestException as e:
                logging.error("Pagination request failed: %s", e)

                break


    def get_snyk_projects_generator(self, org_id):
        #https://api.snyk.io/rest/orgs/{org_id}/projects/{project_id} ???
        "Gets project info by Org Id"
        projects_endpoint = f"rest/orgs/{org_id}/projects"
        try:
            response = requests.get(
                url=self.snyk_url + projects_endpoint,
                params={"version": "2024-10-14",
                        "limit": 100,
                        "meta.latest_issue_counts": "true"},
                headers=self.snyk_headers,
                timeout=30
            )
            response.raise_for_status()
            data = response.json()

        except requests.exceptions.RequestException as e:
            logging.error("Request failed: %s", e)
            return
        
        yield from data.get('data', [])

        while data.get('links', {}).get('next'):
            try:
                data = self.get_next(url=data['links']['next'])

                yield from data.get('data', [])
            except requests.exceptions.RequestException as e:
                logging.error("Pagination request failed: %s", e)

                break
        
    def get_v1_project_details(self, org_id, project_id):
        """
        This function gets the details of a single project and returns it.
        """
        v1_projects_endpoint = f"/v1/org/{org_id}/project/{project_id}"
        url = self.snyk_url +v1_projects_endpoint
        try:
            response = requests.get(url, headers=self.snyk_headers, timeout=30)
            response.raise_for_status()  # Raise an error for HTTP failures
            return response.json()
        except requests.exceptions.RequestException as e:
            logging.error("Request failed: %s", e)
            return



    def get_sync_issues_generator(self, limit, org_id, project_id, start_date = None):
        """
        This function calls the /issues endpoint from snyk and yields one issue document at a time.
        Function Params:
        -limit: amount of docs to return at a time. Max limit is 200 at a time.
        -project_id: The Id of the project that you want to get your issues from. 
        -start_date: The date in which you want to set the api param of "updated_after" to return issues from. 
            -if this value is set to None the generator will return all issues
        """
        issues_endpoint = f"rest/orgs/{org_id}/issues"
        url = self.snyk_url + issues_endpoint
        if start_date:
            params = {
                "version": "2024-10-15",
                "limit": limit,
                "updated_after": start_date,
                "scan_item.id": project_id,
                "scan_item.type": "project"
            }
        else:
            params = {
                "version": "2024-10-15",
                "limit": limit,
                "scan_item.id": project_id,
                "scan_item.type": "project"
            }

        try:
            response = requests.get(url, params=params, headers=self.snyk_headers, timeout=30)
            response.raise_for_status()  # Raise an error for HTTP failures
            data = response.json()
        except requests.exceptions.RequestException as e:
            logging.error("Request failed: %s", e)

            return

        yield from data.get('data', [])

        while data.get('links', {}).get('next'):
            try:
                data = self.get_next(url=data['links']['next'])
                yield from data.get('data', [])
            except requests.exceptions.RequestException as e:
                logging.error("Pagination request failed: %s", e)

                break


    def get_next(self, url):
        """
        This method allows us to get the next page within the the Snyk api's enabling pagination.
        """
        try:
            response= requests.get(url=self.snyk_url+url, headers=self.snyk_headers, timeout=30)
            response.raise_for_status()

            return response.json()
        
        except Exception as e:
            logging.error("[SnykClient][get_next] Get_next failed with error of %s", e)

