import json
import logging
import requests


class AxoniusClient:
    def __init__(self, settings):
        self.settings = settings
        self.url = self.settings.GetSource("Axonius", "Url", "")
        self.api_key = self.settings.GetSource("Axonius", "ApiKey", "")
        self.api_secret = self.settings.GetSource("Axonius", "ApiSecret", "")
        self.query_ids = self.settings.GetSource("Axonius", "QueryIds", [])
        self.headers = {
            "accept": "application/json",
            "api-key": self.api_key,
            "api-secret": self.api_secret,
            "content-type": "application/json"
        }

    def get_all_asset_queries_generator(self):
        for query_id, asset_type in self.query_ids.items():
            yield from self.get_asset_query_generator(query_id, asset_type)


    def get_asset_query_generator(self, query_id, asset_type):
        endpoint = f"{self.url}/api/v2/assets/{asset_type}"
        
        body = {
            "include_metadata": True,
            "page": {
                "limit": 1000
            },
            "use_cache_entry": True,
            "include_details": True,
            "saved_query_id": query_id
        }

        while True:
            try:
                response = requests.post(
                    url=endpoint,
                    data=json.dumps(body),
                    headers=self.headers,
                    timeout=30
                )
                response.raise_for_status()
                data = response.json()

            except Exception as e:
                logging.error("[Axonius Client][Get Assets] There was an issue pulling assets for query %s: %s", query_id, e)
                break
            
            if not data.get("assets"):
                break

            yield from data["assets"]

            if data['meta'].get("next_page"):
                body['next_page'] = data['meta']['next_page']
            


