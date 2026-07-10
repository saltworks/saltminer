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

import asyncio
import json
import logging

from Core.RestClient import AsyncRestClient, RestClient
from Core.Application import Application


class DataClientException(Exception):
    pass


class DataClientNotFoundException(DataClientException):
    pass


class QueueStatus:
    PENDING = "Pending"
    LOADING = "Loading"


class DataClient:
    '''
    SaltMiner DataApi client.

    Wraps RestClient to provide typed, named methods for every DataApi endpoint
    used by the Python project, mirroring the endpoints structure of the C# DataClient.

    Constructor reads connection settings from Config/DataClient.json (or the
    config section named by config_name) via application.Settings.
    Set ValidateOnInit=true in config to verify connectivity on construction.
    '''

    def __init__(self, app:Application, config_name:str='DataClient', validate_on_init:bool=None):
        '''
        :param app:  Application instance (provides .Settings)
        :param config_name:  Config section name; defaults to "DataClient"
        :param validate_on_init:  Whether to validate connectivity on initialization; defaults to config setting
        '''
        settings = app.Settings
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
        self._loop = asyncio.new_event_loop()
        self._client = self._get_async_client(api_url, api_key, ssl_verify, timeout)
        self._manager_client = None
        self._manager_key = manager_key
        self._timeout = timeout
        self._ssl_verify = ssl_verify
        self._issue_batch_size = 1000
        self._issue_batch = []
        self._queue_batch_size = 1000
        self._queue_batch = []

        if validate_on_init:
            self.register_get_role()

        logging.debug(
            "[DataClient] Initialized — url: '%s', manager_client: %s, validate_on_init: %s",
            api_url, self._manager_client is not None, validate_on_init
        )

    @property
    def client(self) -> AsyncRestClient:
        return self._client

    @property
    def manager_client(self) -> AsyncRestClient:
        if self._manager_client is None:
            if not self._manager_key:
                raise DataClientException(
                    "ManagerApiKey is not configured for this DataClient instance."
                )
            self._manager_client = self._get_async_client(
                self._client.BaseUrl, self._manager_key, self._ssl_verify, self._timeout
            )
        return self._manager_client
    
    @property
    def issue_batch_size(self) -> int:
        return self._issue_batch_size
    @issue_batch_size.setter
    def issue_batch_size(self, value:int):
        self._issue_batch_size = value

    @property
    def issue_batch(self) -> list:
        return self._issue_batch
    @issue_batch.setter
    def issue_batch(self, value:list):
        self._issue_batch = value

    @property
    def queue_batch_size(self) -> int:
        return self._queue_batch_size
    @queue_batch_size.setter
    def queue_batch_size(self, value:int):
        self._queue_batch_size = value

    @property
    def queue_batch(self) -> list:
        return self._queue_batch
    @queue_batch.setter
    def queue_batch(self, value:list):
        self._queue_batch = value

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
    def _get_async_client(api_url, api_key, ssl_verify, timeout) -> AsyncRestClient:
        headers = {
            'Accept': 'application/json',
            'Content-Type': 'application/json;charset=UTF-8',
            'Authorization': api_key,
        }
        return AsyncRestClient(api_url, sslVerify=ssl_verify, defaultHeaders=headers, timeout=timeout, retryConnectionErrors=True)

    def _run_async(self, coro):
        '''Run a coroutine on this instance's persistent event loop. Used by sync methods to delegate to their async counterparts.'''
        return self._loop.run_until_complete(coro)

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
            _reason = getattr(response, 'reason', None) or getattr(response, 'reason_phrase', '')
            msg = f"{base_msg}: {detail}" if detail else f"{base_msg}: [{response.status_code}] {_reason}"
        except Exception:
            _reason = getattr(response, 'reason', None) or getattr(response, 'reason_phrase', '')
            msg = f"{base_msg}: [{response.status_code}] {_reason}"

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
        return self._run_async(self.register_get_role_async())

    async def register_get_role_async(self):
        '''Async version of register_get_role.'''
        r = await self._client.Get('register/role')
        self._verify_response('Error retrieving role', r)
        return json.loads(r.text).get('message', '')

    # ------------------------------------------------------------------
    # Utility endpoints
    # ------------------------------------------------------------------

    def get_version(self):
        '''Returns the API version info.'''
        return self._run_async(self.get_version_async())

    async def get_version_async(self):
        '''Async version of get_version.'''
        r = await self._client.Get('utility/version')
        self._verify_response('Error retrieving API version', r)
        return json.loads(r.text)

    def webhook_get(self, source):
        '''Returns webhook events for the given source ID, or None if no data.'''
        return self._run_async(self.webhook_get_async(source))

    async def webhook_get_async(self, source):
        '''Async version of webhook_get.'''
        r = await self._client.Get(f'utility/webhook/{source}')
        self._verify_response(f"Error retrieving webhook events for source '{source}'", r)
        body = json.loads(r.text)
        data = body.get('data')
        return data if data else None

    # ------------------------------------------------------------------
    # QueueScan endpoints
    # ------------------------------------------------------------------

    def queue_scan_add_update(self, q_scan):
        '''Adds or updates a queue scan document.  Returns the response data dict.'''
        return self._run_async(self.queue_scan_add_update_async(q_scan))

    async def queue_scan_add_update_async(self, q_scan):
        '''Async version of queue_scan_add_update.'''
        r = await self._client.Post('queuescan', {'Id': None, 'Entity': q_scan})
        self._verify_response('Error submitting queue scan', r)
        return json.loads(r.text).get('data')

    def queue_scans_add_update_bulk(self, batch:list):
        '''
        Submits a list of queue scan documents in bulk.
        :param batch: list of queue scan docs
        '''
        return self._run_async(self.queue_scans_add_update_bulk_async(batch))

    async def queue_scans_add_update_bulk_async(self, batch:list):
        '''Async version of queue_scans_add_update_bulk.'''
        r = await self._client.Post('queuescan/bulk', {'Documents': batch})
        self._verify_response('Error submitting queue scans (bulk)', r)

    def queue_scan_update_status(self, scan_id, status):
        '''Updates the status of a queue scan (e.g. "Pending").'''
        return self._run_async(self.queue_scan_update_status_async(scan_id, status))

    async def queue_scan_update_status_async(self, scan_id, status):
        '''Async version of queue_scan_update_status.'''
        r = await self._client.Get(f'queuescan/status/{scan_id}/{status}')
        self._verify_response(f"Error updating queue scan '{scan_id}' status to '{status}'", r)

    def queue_scan_delete(self, scan_id):
        '''Deletes a queue scan by ID.'''
        return self._run_async(self.queue_scan_delete_async(scan_id))

    async def queue_scan_delete_async(self, scan_id):
        '''Async version of queue_scan_delete.'''
        r = await self._client.Delete(f'queuescan/{scan_id}')
        self._verify_response(f"Error deleting queue scan '{scan_id}'", r)

    def queue_scan_delete_all(self, scan_id):
        '''Deletes a queue scan and all associated queue assets and queue issues.'''
        return self._run_async(self.queue_scan_delete_all_async(scan_id))

    async def queue_scan_delete_all_async(self, scan_id):
        '''Async version of queue_scan_delete_all.'''
        r = await self._client.Delete(f'queuescan/all/{scan_id}')
        self._verify_response(f"Error deleting queue scan and children for '{scan_id}'", r)

    def queue_bulk_add_update(self, queue_item:dict, batch_size=None):
        '''
        Adds a queue item to the batch for bulk processing. Automatic batch build/send when batch_size is reached.
        :param queue_item: queue scan/asset/issue doc to add to the batch, or None to send any remainder.
        :param batch_size: batch size to trigger automatic send; defaults to self.queue_batch_size if None
        '''
        return self._run_async(self.queue_bulk_add_update_async(queue_item, batch_size))

    async def queue_bulk_add_update_async(self, queue_item:dict, batch_size=None):
        '''Async version of queue_bulk_add_update.'''
        if batch_size is None:
            batch_size = self.queue_batch_size
        if queue_item is not None:
            self._queue_batch.append(queue_item)
        if len(self._queue_batch) >= batch_size or (queue_item is None and len(self._queue_batch) > 0):
            await self.queue_bulk_async(self._queue_batch)
            self._queue_batch = []

    def queue_bulk(self, batch:list):
        '''
        Submits a batch of queue items (queue scans with their queue assets and issues) for processing.

        :param batch: list of queue scan/asset/issue docs (scan should exist for asset, both should exist for issue)
        '''
        return self._run_async(self.queue_bulk_async(batch))

    async def queue_bulk_async(self, batch:list):
        '''Async version of queue_bulk.'''
        r = await self._client.Post('queuescan/bulkqueue', batch)
        self._verify_response('Error submitting bulk queue', r)

    # ------------------------------------------------------------------
    # QueueAsset endpoints
    # ------------------------------------------------------------------

    def queue_asset_add_update(self, q_asset):
        '''Adds or updates a queue asset document.  Returns the response data dict.'''
        return self._run_async(self.queue_asset_add_update_async(q_asset))

    async def queue_asset_add_update_async(self, q_asset):
        '''Async version of queue_asset_add_update.'''
        r = await self._client.Post('queueasset', {'Id': None, 'Entity': q_asset})
        self._verify_response('Error submitting queue asset', r)
        return json.loads(r.text).get('data')

    def queue_asset_delete(self, asset_id):
        '''Deletes a queue asset by ID.'''
        return self._run_async(self.queue_asset_delete_async(asset_id))

    async def queue_asset_delete_async(self, asset_id):
        '''Async version of queue_asset_delete.'''
        r = await self._client.Delete(f'queueasset/{asset_id}')
        self._verify_response(f"Error deleting queue asset '{asset_id}'", r)

    # ------------------------------------------------------------------
    # QueueIssue endpoints
    # ------------------------------------------------------------------

    def queue_issue_add_update_batch(self, qissue:dict, batch_size=None):
        '''
        Add issue to batch for bulk update.  Automatic batch build/send when batch_size is reached.

        :qissue: queue issue to send. If None, send remainder of batch if any.
        :batch_size: batch size to trigger automatic send; defaults to self.issue_batch_size if None
        '''
        return self._run_async(self.queue_issue_add_update_batch_async(qissue, batch_size))

    async def queue_issue_add_update_batch_async(self, qissue:dict, batch_size=None):
        '''Async version of queue_issue_add_update_batch.'''
        if batch_size is None:
            batch_size = self.issue_batch_size
        if qissue is not None:
            self.issue_batch.append(qissue)
        if len(self.issue_batch) >= batch_size or (qissue is None and len(self.issue_batch) > 0):
            await self.queue_issues_add_update_bulk_async({'Documents': self.issue_batch})
            self.issue_batch = []

    def queue_issues_add_update_bulk(self, batch:dict):
        '''
        Submits a batch of queue issues.
        :param batch: dict with 'Documents' key containing list of queue issue docs
        '''
        return self._run_async(self.queue_issues_add_update_bulk_async(batch))

    async def queue_issues_add_update_bulk_async(self, batch:dict):
        '''Async version of queue_issues_add_update_bulk.'''
        r = await self._client.Post('queueissue/bulk', batch)
        self._verify_response('Error submitting queue issues (bulk)', r)

    # ------------------------------------------------------------------
    # Scan endpoints  (manager key required)
    # ------------------------------------------------------------------

    def scan_search(self, search_request:dict):
        '''
        Searches scans using the provided search request body.
        Returns the data list, or None if no results.  Requires manager API key.
        '''
        return self._run_async(self.scan_search_async(search_request))

    async def scan_search_async(self, search_request:dict):
        '''Async version of scan_search.'''
        self._require_manager('scan_search')
        r = await self.manager_client.Post('scan/search', search_request)
        self._verify_response('Error searching scans', r)
        body = json.loads(r.text)
        data = body.get('data')
        return data if data else None

    def scan_delete(self, scan_id, asset_type, source_type, instance):
        '''Deletes a scan by composite key.  Requires manager API key.'''
        return self._run_async(self.scan_delete_async(scan_id, asset_type, source_type, instance))

    async def scan_delete_async(self, scan_id, asset_type, source_type, instance):
        '''Async version of scan_delete.'''
        self._require_manager('scan_delete')
        r = await self.manager_client.Delete(f'scan/{scan_id}/{asset_type}/{source_type}/{instance}')
        self._verify_response(f"Error deleting scan '{scan_id}'", r)
        logging.debug("[DataClient] Deleted scan '%s' (%s/%s/%s)", scan_id, asset_type, source_type, instance)

    # ------------------------------------------------------------------
    # Asset endpoints  (manager key required)
    # ------------------------------------------------------------------

    def asset_delete(self, asset_id, asset_type, source_type, instance):
        '''Deletes an asset by composite key.  Requires manager API key.'''
        return self._run_async(self.asset_delete_async(asset_id, asset_type, source_type, instance))

    async def asset_delete_async(self, asset_id, asset_type, source_type, instance):
        '''Async version of asset_delete.'''
        self._require_manager('asset_delete')
        r = await self.manager_client.Delete(f'asset/{asset_id}/{asset_type}/{source_type}/{instance}')
        self._verify_response(f"Error deleting asset '{asset_id}'", r)
        logging.debug("[DataClient] Deleted asset '%s' (%s/%s/%s)", asset_id, asset_type, source_type, instance)

    # ------------------------------------------------------------------
    # Issue endpoints  (manager key required)
    # ------------------------------------------------------------------

    def issues_delete_by_scan(self, scan_id, asset_type, source_type, instance):
        '''Deletes all issues associated with a scan.  Requires manager API key.'''
        return self._run_async(self.issues_delete_by_scan_async(scan_id, asset_type, source_type, instance))

    async def issues_delete_by_scan_async(self, scan_id, asset_type, source_type, instance):
        '''Async version of issues_delete_by_scan.'''
        self._require_manager('issues_delete_by_scan')
        r = await self.manager_client.Delete(f'issue/scan/{scan_id}/{asset_type}/{source_type}/{instance}')
        self._verify_response(f"Error deleting issues for scan '{scan_id}'", r)
        logging.debug("[DataClient] Deleted issues for scan '%s' (%s/%s/%s)", scan_id, asset_type, source_type, instance)

    # ------------------------------------------------------------------
    # Index endpoints
    # ------------------------------------------------------------------

    def refresh_index(self, index_name):
        '''Refreshes an Elasticsearch index via the DataApi.'''
        return self._run_async(self.refresh_index_async(index_name))

    async def refresh_index_async(self, index_name):
        '''Async version of refresh_index.'''
        r = await self._client.Post(f'index/refresh/{index_name}')
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
        return self._run_async(self.event_add_async(payload))

    async def event_add_async(self, payload):
        '''Async version of event_add.'''
        r = await self._client.Post('Eventlog', json=payload)
        if r.status_code == 202:
            try:
                return r.json()
            except Exception:
                return {}
        self._verify_response('EventLog POST failed', r)

    # ------------------------------------------------------------------
    # Lifecycle
    # ------------------------------------------------------------------

    def close(self):
        '''
        Close the underlying async HTTP clients and event loop.
        Call when finished with this DataClient instance to release connections.
        '''
        async def _close_clients():
            await self._client.close()
            if self._manager_client is not None:
                await self._manager_client.close()
        self._run_async(_close_clients())
        self._loop.close()
