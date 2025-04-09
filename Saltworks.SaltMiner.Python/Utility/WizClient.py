''' --[auto-generated, do not modify this block]--
*
* Copyright (c) 2025 Saltworks Security, LLC
*
* Use of this software is governed by the Business Source License included
* in the LICENSE file.
*
* Change Date: 2029-04-09
*
* On the date above, in accordance with the Business Source License, use
* of this software will be governed by version 2 or later of the General
* Public License.
*
* ----
'''

import requests
import logging
import time
import json
import pandas as pd


class WizClient:
    """
    This class will be used to handle all python requests into Wiz
    We will add functions to this class as needed
    """
    def __init__(self, settings):
        self.settings = settings
        self.connected = False

        self.auth_headers = {"Content-Type": "application/x-www-form-urlencoded"}
        self.headers = {"Content-Type": "application/json"}
        self.client_id = self.settings.Get("WizClient", 'ClientId')
        self.client_secret = self.settings.Get("WizClient", "ClientSecret")
        self.auth_url = self.settings.Get("WizClient", "AuthUrl")
        self.api_url = self.settings.Get("WizClient", "ApiUrl")
        self.ssl_verify = self.settings.Get("WizClient", "SslVerify")
        self.headers["Authorization"] = "Bearer " + self.get_wiz_auth_token()
        

    def get_wiz_auth_token(self, proxies = None):
        """Retrieve an OAuth access token to be used against Wiz API"""
        auth_payload = {
        'grant_type': 'client_credentials',
        'audience': 'wiz-api',
        'client_id': self.client_id,
        'client_secret': self.client_secret
        }
        response = requests.post(url=self.auth_url,
                                headers=self.auth_headers, data=auth_payload, proxies= proxies, verify=self.ssl_verify)

        if response.status_code != requests.codes.ok:
            logging.error('Error authenticating to Wiz [%d] - %s', response.status_code, response.text)

        try:
            response_json = response.json()
            token = response_json.get('access_token')
            if not token:
                message = 'Could not retrieve token from Wiz: {}'.format(
                        response_json.get("message"))
                logging.error(message)
            
        except ValueError as exception:
            logging.error('Could not parse API response: %s', exception)

        self.connected= True
        return token
    

    def send_ticket_to_wiz(self, ticket_id, ticket_url, issue_id):
        ticket_doc = self.send_ticket_doc()
        ticket_doc['variables']['input']["ticketId"] = ticket_id
        ticket_doc['variables']['input']["ticketUrl"] = ticket_url
        ticket_doc['variables']['input']["issueId"] = issue_id
        response = self.post_wiz_query(query=ticket_doc['query'], variables=ticket_doc['variables'])
        return response


    def post_wiz_query(self, query, variables, proxies = None):
            """
            Query WIZ API for the given query data schema
            """
            data = {"variables": variables, "query": query}
            try:
                result = requests.post(url=self.api_url,
                                    json=data, headers=self.headers, proxies= proxies, verify=self.ssl_verify)

            except Exception as e:
                logging.error("[Wiz Client]Issue completing your request:\n%s", e)

            # if "errors" in result.json().keys():
            #     response = (json.dumps(result.json(),indent=4))
            #     logging.error("[WizClient] Errors present in response: %s", response)

            return result.json()
    

    def send_ticket_doc(self):
        """
        Assigns a ticket to a particular issue
        """
        send_ticket_doc ={
            "query":"""
            mutation AssociateServiceTicket($input: AssociateServiceTicketInput!) {
            associateServiceTicket(input: $input) {
                serviceTicket {
                id
                externalId
                name
                url
                }
            }
            }
        """,
            "variables": {
                "input": {
                    "ticketId": None,
                    "ticketUrl": None,
                    "issueId": None
                }
            }
        }
        return send_ticket_doc

