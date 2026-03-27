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

from Core.RestClient import RestClient


class DataClientException(Exception):
    pass


class DataClientNotFoundException(DataClientException):
    pass


class DataClient:
    '''
    SaltMiner DataApi client.

    Wraps RestClient to provide typed, named methods for every DataApi endpoint
    used by the Python project, mirroring the endpoints structure of the C# DataClient.

    Constructor reads connection settings from Config/DataClient.json (or the
    config section named by config_name) via application.Settings.
    Set ValidateOnInit=true in config to verify connectivity on construction.
    '''

    def __init__(self, application, config_name='DataClient', validate_on_init=None):
        '''
        :param application:  Application instance (provides .Settings)
        :param config_name:  Config section name; defaults to "DataClient"
        '''
        settings = application.Settings
        api_url = settings.Get(config_name, 'ApiUrl')
        api_key = settings.Get(config_name, 'ApiKey')
        manager_key = settings.Get(config_name, 'ManagerApiKey')
        ssl_verify = settings.Get(config_name, 'SslVerify', True)
        ssl_cert = settings.Get(config_name, 'SslVerifyCert', '')
        timeout = settings.Get(config_name, 'TimeoutSec', 30)
        validate_on_init = settings.Get(config_name, 'ValidateOnInit', False) if validate_on_init is None else validate_on_init

        if not api_url or not api_key:
            raise DataClientException(
                f"Check '{config_name}' configuration — ApiUrl and ApiKey are required."
            )

        ssl_verify = self._resolve_ssl(ssl_verify, ssl_cert)

        RestClient.disableRequestWarnings()
        self._client = self._get_client(api_url, api_key, ssl_verify, timeout)
        self._manager_client = None
        self._manager_key = manager_key
        self._timeout = timeout
        self._ssl_verify = ssl_verify

        if validate_on_init:
            self.register_get_role()

        logging.debug(
            "[DataClient] Initialized — url: '%s', manager_client: %s, validate_on_init: %s",
            api_url, self._manager_client is not None, validate_on_init
        )

    @property
    def client(self) -> RestClient:
        return self._client

    @property
    def manager_client(self) -> RestClient:
        if self._manager_client is None:
            if not self._manager_key:
                raise DataClientException(
                    "ManagerApiKey is not configured for this DataClient instance."
                )
            self._manager_client = self._get_client(
                self._client.BaseUrl, self._manager_key, self._ssl_verify, self._timeout
            )
        return self._manager_client

    # ------------------------------------------------------------------
    # Internal helpers
    # ------------------------------------------------------------------

    @staticmethod
    def _get_client(api_url, api_key, ssl_verify, timeout):
        headers = {
            'Accept': 'application/json',
            'Content-Type': 'application/json;charset=UTF-8',
            'Authorization': api_key,
        }
        return RestClient(api_url, sslVerify=ssl_verify, defaultHeaders=headers, timeout=timeout, retryConnectionErrors=True)

    @staticmethod
    def _resolve_ssl(ssl_verify, ssl_cert):
        '''Mirrors SmApiClient SSL resolution: use cert path when provided.'''
        if ssl_verify and ssl_cert:
            if ',' in ssl_cert:
                return (ssl_cert.split(',')[0], ssl_cert.split(',')[1])
            return ssl_cert
        return ssl_verify

    def _verify_response(self, base_msg, response):
        '''Raises DataClientException (or DataClientNotFoundException for 404) on non-2xx responses.'''
        if 200 <= response.status_code < 300:
            return
        msg = base_msg
        try:
            body = json.loads(response.text)
            detail = body.get('message') or ''
            errors = body.get('errorMessages')
            if errors:
                detail = f"{detail}; {'; '.join(errors)}" if detail else '; '.join(errors)
            msg = f"{base_msg}: {detail}" if detail else f"{base_msg}: [{response.status_code}] {response.reason}"
        except Exception:
            msg = f"{base_msg}: [{response.status_code}] {response.reason}"

        if response.status_code == 404:
            raise DataClientNotFoundException(msg)
        raise DataClientException(msg)

    def _require_manager(self, op_name):
        '''Raises DataClientException if no manager client is configured.'''
        if not self._manager_key:
            raise DataClientException(
                f"Operation '{op_name}' requires a ManagerApiKey in DataClient config."
            )

    # ------------------------------------------------------------------
    # Register endpoints
    # ------------------------------------------------------------------

    def register_get_role(self):
        '''Returns the role string for the configured API key (e.g. "agent").'''
        r = self._client.Get('register/role')
        self._verify_response('Error retrieving role', r)
        return json.loads(r.text).get('message', '')

    def register_get_agent_id(self):
        '''Returns the agent ID for the configured API key.'''
        r = self._client.Get('register/agent')
        self._verify_response('Error retrieving agent ID', r)
        return json.loads(r.text).get('message', '')

    # ------------------------------------------------------------------
    # Utility endpoints
    # ------------------------------------------------------------------

    def get_version(self):
        '''Returns the API version info.'''
        r = self._client.Get('admin/version')
        self._verify_response('Error retrieving API version', r)
        return json.loads(r.text)

    def webhook_get(self, source):
        '''
        Returns webhook events for the given source ID, or None if no data.
        '''
        r = self._client.Get(f'utility/webhook/{source}')
        self._verify_response(f"Error retrieving webhook events for source '{source}'", r)
        body = json.loads(r.text)
        data = body.get('data')
        return data if data else None

    # ------------------------------------------------------------------
    # QueueScan endpoints
    # ------------------------------------------------------------------

    def queue_scan_add_update(self, q_scan):
        '''
        Adds or updates a queue scan document.  Returns the response data dict.
        '''
        r = self._client.Post('queuescan', {'Id': None, 'Entity': q_scan})
        self._verify_response('Error submitting queue scan', r)
        return json.loads(r.text).get('data')

    def queue_scan_update_status(self, scan_id, status):
        '''
        Updates the status of a queue scan (e.g. "Pending").
        '''
        r = self._client.Get(f'queuescan/status/{scan_id}/{status}')
        self._verify_response(f"Error updating queue scan '{scan_id}' status to '{status}'", r)

    def queue_scan_delete(self, scan_id):
        '''Deletes a queue scan by ID.'''
        r = self._client.Delete(f'queuescan/{scan_id}')
        self._verify_response(f"Error deleting queue scan '{scan_id}'", r)

    def queue_scan_delete_all(self, scan_id):
        '''Deletes a queue scan and all associated queue assets and queue issues.'''
        r = self._client.Delete(f'queuescan/all/{scan_id}')
        self._verify_response(f"Error deleting queue scan and children for '{scan_id}'", r)

    # ------------------------------------------------------------------
    # QueueAsset endpoints
    # ------------------------------------------------------------------

    def queue_asset_add_update(self, q_asset):
        '''
        Adds or updates a queue asset document.  Returns the response data dict.
        '''
        r = self._client.Post('queueasset', {'Id': None, 'Entity': q_asset})
        self._verify_response('Error submitting queue asset', r)
        return json.loads(r.text).get('data')

    def queue_asset_delete(self, asset_id):
        '''Deletes a queue asset by ID.'''
        r = self._client.Delete(f'queueasset/{asset_id}')
        self._verify_response(f"Error deleting queue asset '{asset_id}'", r)

    # ------------------------------------------------------------------
    # QueueIssue endpoints
    # ------------------------------------------------------------------

    def queue_issues_add_update_bulk(self, batch):
        '''
        Submits a batch of queue issues.
        :param batch: dict of shape {"Documents": [...]}
        '''
        r = self._client.Post('queueissue/bulk', batch)
        self._verify_response('Error submitting queue issues (bulk)', r)

    # ------------------------------------------------------------------
    # Scan endpoints  (manager key required)
    # ------------------------------------------------------------------

    def scan_search(self, search_request):
        '''
        Searches scans using the provided search request body.
        Returns the data list, or None if no results.
        Requires manager API key.
        '''
        self._require_manager('scan_search')
        r = self.manager_client.Post('scan/search', search_request)
        self._verify_response('Error searching scans', r)
        body = json.loads(r.text)
        data = body.get('data')
        return data if data else None

    def scan_delete(self, scan_id, asset_type, source_type, instance):
        '''
        Deletes a scan by composite key.  Requires manager API key.
        '''
        self._require_manager('scan_delete')
        r = self.manager_client.Delete(f'scan/{scan_id}/{asset_type}/{source_type}/{instance}')
        self._verify_response(f"Error deleting scan '{scan_id}'", r)
        logging.debug("[DataClient] Deleted scan '%s' (%s/%s/%s)", scan_id, asset_type, source_type, instance)

    # ------------------------------------------------------------------
    # Asset endpoints  (manager key required)
    # ------------------------------------------------------------------

    def asset_delete(self, asset_id, asset_type, source_type, instance):
        '''
        Deletes an asset by composite key.  Requires manager API key.
        '''
        self._require_manager('asset_delete')
        r = self.manager_client.Delete(f'asset/{asset_id}/{asset_type}/{source_type}/{instance}')
        self._verify_response(f"Error deleting asset '{asset_id}'", r)
        logging.debug("[DataClient] Deleted asset '%s' (%s/%s/%s)", asset_id, asset_type, source_type, instance)

    # ------------------------------------------------------------------
    # Issue endpoints  (manager key required)
    # ------------------------------------------------------------------

    def issues_delete_by_scan(self, scan_id, asset_type, source_type, instance):
        '''
        Deletes all issues associated with a scan.  Requires manager API key.
        '''
        self._require_manager('issues_delete_by_scan')
        r = self.manager_client.Delete(f'issue/scan/{scan_id}/{asset_type}/{source_type}/{instance}')
        self._verify_response(f"Error deleting issues for scan '{scan_id}'", r)
        logging.debug("[DataClient] Deleted issues for scan '%s' (%s/%s/%s)", scan_id, asset_type, source_type, instance)

    # ------------------------------------------------------------------
    # Index endpoints
    # ------------------------------------------------------------------

    def refresh_index(self, index_name):
        '''Refreshes an Elasticsearch index via the DataApi.'''
        r = self._client.Post(f'index/refresh/{index_name}')
        self._verify_response(f"Error refreshing index '{index_name}'", r)

    # ------------------------------------------------------------------
    # Event endpoints
    # ------------------------------------------------------------------

    def event_add(self, payload):
        '''
        Posts an event log entry to the DataApi /Eventlog endpoint.
        :param payload: dict matching DataItemRequest<Eventlog> shape
        :returns: parsed response body dict, or {} on 202 with no body
        :raises DataClientException: on non-202 response
        '''
        r = self._client.Post('Eventlog', json=payload)
        if r.status_code == 202:
            try:
                return r.json()
            except Exception:
                return {}
        self._verify_response('EventLog POST failed', r)
