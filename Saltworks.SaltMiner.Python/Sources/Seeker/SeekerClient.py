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
import requests

class SeekerClient:
    """
    Client class for all needed Seeker requests.
    """
    def __init__(self, settings):
        self.seeker_url = settings.GetSource("Seeker", 'Base_Url')
        self.token = settings.GetSource("Seeker", "API_Key")


    def get_vulnerabilities_generator(self, project_key, last_updated= None):
        vulnerabilities_endpoint = self.seeker_url + "/latest/vulnerabilities"
        offset_counter = 0
        limit = 5000

        while True:
            params = {
                "format": "JSON",
                "language": "en",
                "limit": limit,
                "offset": offset_counter,
                "projectKeys": project_key,
                "includeStacktrace": True,
                "includeDescription": True,
                "includeRemediation": True
            }

            if last_updated:
                params["fromDatetime"] = last_updated

            try:
                response = requests.get(
                    url=vulnerabilities_endpoint,
                    params=params,
                    headers={"Authorization": f"Bearer {self.token}"},
                    timeout=30
                )
                response.raise_for_status()
                data = response.json()

            except Exception as e:
                logging.error("[Seeker Client][Get Vulns] There was an issue pulling seeker vulnerabilities: %s", e)
                break

            if not data:
                break

            yield from data
            offset_counter += limit


    def get_projects_generator(self):
        projects_endpoint = self.seeker_url + "/latest/projects"
        params = {
            "projectType": "REGULAR"
        }
        try:
            response = requests.get(
                url= projects_endpoint,
                params= params,
                headers= {"Authorization": f"Bearer {self.token}"},
                timeout= 30 
            )
            response.raise_for_status()
            data = response.json()


        except Exception as e:
                logging.error("[Seeker Client][Get Projects] There was an issue pulling seeker projects: %s", e)

        yield from data.get('projects', [])
