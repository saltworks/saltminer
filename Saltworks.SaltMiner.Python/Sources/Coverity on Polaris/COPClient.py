''' --[auto-generated, do not modify this block]--
*
* Copyright (c) 2025 Saltworks Security, LLC
*
* Use of this software is governed by the Business Source License included
* in the LICENSE file.
*
* Change Date: 2029-06-30
*
* On the date above, in accordance with the Business Source License, use
* of this software will be governed by version 2 or later of the General
* Public License.
*
* ----
'''

import json
import logging
import requests

class COPClient:

    def __init__(self, settings):
        self.api_key = settings.Get("COPClient", "API_Key")
        self.base_url = settings.Get("COPClient", "Base Url")

        self.token = None
        self.headers = {
            "Authorization": f"Bearer {self.token}"
        }

    def get_token(self):
        token_url = self.base_url + "/api/auth/authenticate"
        payload = {
            "accesstoken":self.api_key
        }
        try:
            response = requests.post(url=token_url, data=payload, timeout=30)
            response.raise_for_status()
            data = response.json()
            self.token = data['jwt']
        
        except requests.exceptions.RequestException as e:
            logging.error("[COPClient] Token request failed: %s", e)
            return
        
    def get_projects_generator(self):
        projects_url = self.base_url + "/api/common/v0/projects"
        params = {
            "page[limit]": 100,
            "page[offset]": 0
        }
        try:
            response = requests.get(
                url=projects_url,
                params=params,
                headers=self.headers,
                timeout=30
            )
            response.raise_for_status()
            data = response.json()

        except requests.exceptions.RequestException as e:
            logging.error("[COPClient] Token request failed: %s", e)
            return
        
        yield from data.get('data', [])

        while data.get('links', {}).get('next'):
            try:
                data = self.get_next(url=data['links']['next'], headers=self.headers)

                yield from data.get('data', [])
            except requests.exceptions.RequestException as e:
                logging.error("Pagination request failed: %s", e)

                break


    def get_project_branches_generator(self, project_id):
        branches_endpoint = self.base_url + "/api/common/v0/branches"
        params = {
            "page[limit]": 1000,
            "filter[branch][project][id][$eq]": project_id
        }
        try:
            response = requests.get(
                url=branches_endpoint,
                params=params,
                headers=self.headers,
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
                data = self.get_next(url=data['links']['next'], headers=self.headers)

                yield from data.get('data', [])
            except requests.exceptions.RequestException as e:
                logging.error("Pagination request failed: %s", e)

                break

    def get_runs_generator(self, project_id = None, recipe= None):
        runs_endpoint = self.base_url +  "/api/common/v0/runs"
        params = {
            "page[limit]":100
        }
        if project_id:
            params["filter[run][project][id][$eq]"] = project_id

        if recipe:
            params["recipe"] = recipe



    def get_next(self, url, headers):
        """
        This method allows us to get the next page within the COP api's enabling pagination.
        """
        try:
            response= requests.get(url=url, headers=headers, timeout=30)
            response.raise_for_status()

            return response.json()
        
        except Exception as e:
            logging.error("[SnykClient][get_next] Get_next failed with error of %s", e)
 
     