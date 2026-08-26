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

'''
Vendor API client + the adapter's exception taxonomy.

ALL HTTP, auth, and paging against the source lives here and nowhere else -
the adapter, loader, and worker never import requests.

What to fill in when copying the template:
- Auth: replace the header construction in __init__ with the vendor's scheme.
- get_assets_generator(): the vendor's "list projects/endpoints/apps" call.
- get_asset(): fetch one asset by id (single-asset adapters only; delete for
  batch adapters that carry the full payload through the loader).
- get_issues_generator(): the vendor's findings/issues call for one asset.
  This must yield the asset's FULL current issue set - see the retirement rule
  in the folder README.

Every collection method is a generator so memory stays constant regardless of
result size, and every failure leaves this module as a SourceExceptions
subclass so callers can distinguish transient from fatal without knowing the
vendor.

MockTemplateClient at the bottom is the no-vendor stand-in: same surface,
canned payloads.  It documents the payload shape the mapping functions expect
and lets the whole adapter path run without a vendor connection.
'''

import logging

import requests

from Utility.SaltminerExceptions import SaltminerException

REQUEST_TIMEOUT_SECS = 30
PAGE_SIZE = 100


# ===========================================================================
# exception taxonomy
# ===========================================================================
#
# TemplateClient translates every vendor/HTTP failure into one of these before
# it leaves the client, so the loader and worker can react to the *kind* of
# failure without knowing the vendor's API.  Nothing else in the adapter
# catches broadly: a bare `except Exception` anywhere outside the per-asset
# boundary hides bugs.
#
# Transient vs fatal is the distinction that matters operationally:
# - SourceTransientException (and subclass SourceRateLimitException): the
#   request might succeed later.
# - SourceAuthException / SourceConfigException: no retry can help; fail the
#   run loudly and immediately.
# - SourceMappingException: the payload could not be mapped - usually schema
#   drift on the vendor side or a template field left unfilled.

class SourceExceptions(SaltminerException):
    ''' Base for all failures raised by this adapter - config, client, mapping, and workers. '''
    pass


class SourceConfigException(SourceExceptions):
    ''' Adapter configuration is missing or invalid (config keys, worker counts, etc). '''
    pass


class SourceAuthException(SourceExceptions):
    ''' The source rejected our credentials.  Not retryable - fix the config. '''
    pass


class SourceTransientException(SourceExceptions):
    ''' A request failed in a way that may succeed on retry (timeout, 5xx, connection reset). '''
    pass


class SourceRateLimitException(SourceTransientException):
    ''' The source is throttling us (HTTP 429 or vendor equivalent). '''
    pass


class SourceFatalException(SourceExceptions):
    ''' The source answered in a way that makes continuing pointless (bad endpoint, schema gone). '''
    pass


class SourceMappingException(SourceExceptions):
    ''' A vendor payload could not be mapped to a Scan/Asset/Issue document. '''
    pass


class SourceWorkerException(SourceExceptions):
    ''' A work item failed end-to-end.  Raised by SourceWorker so Core.Worker counts it. '''
    pass


# ===========================================================================
# TemplateClient
# ===========================================================================

class TemplateClient:
    '''
    HTTP client for the vendor API.

    :settings: ApplicationSettings (app.Settings)
    :source_name: the config lookup key - the SourceName value in
        Config/Sources/SourceTemplate.json, ex "TEMPLATE1".
    '''

    def __init__(self, settings, source_name: str):
        self._source_name = source_name
        self.base_url = settings.GetSource(source_name, "BaseUrl", None)
        api_key = settings.GetSource(source_name, "ApiKey", None)
        if not self.base_url:
            raise SourceConfigException(
                f"BaseUrl is not set in the '{source_name}' source config.")
        if not self.base_url.endswith("/"):
            self.base_url += "/"
        # Replace with the vendor's auth scheme (Bearer, token, session header, ...).
        self.headers = {
            "Authorization": f"token {api_key}",
            "Content-Type": "application/json"
        }

    # -- vendor calls ---------------------------------------------------------

    def get_assets_generator(self):
        '''
        Yields every asset (project/endpoint/app) the source knows about,
        paging transparently.  Each yielded dict must carry enough to build a
        SourceMetric (id, name, last-updated, issue counts if the vendor
        provides them) - see MockTemplateClient for the expected shape.
        '''
        yield from self._get_paged("assets", params={"limit": PAGE_SIZE})

    def get_asset(self, asset_id: str) -> dict:
        '''
        Fetches one asset by id.  Used by SourceWorker, which carries only the
        id through the queue and re-fetches the payload itself.
        '''
        data = self._get(f"assets/{asset_id}")
        if not data:
            raise SourceTransientException(
                f"Asset '{asset_id}' returned no data on refetch - it may have been "
                "removed between enumeration and processing.")
        return data

    def get_issues_generator(self, asset_id: str):
        '''
        Yields the FULL current issue set for one asset, paging transparently.

        Do not add incremental filters (updated-after etc.) here without
        re-reading the retirement rule in the README: a submitted scan that
        carries only a subset of an asset's issues retires the absent ones.
        Change detection belongs in the NeedsUpdate gate, at asset granularity.
        '''
        yield from self._get_paged(f"assets/{asset_id}/issues", params={"limit": PAGE_SIZE})

    # -- transport ------------------------------------------------------------

    def _get(self, path: str, params: dict = None) -> dict:
        ''' One GET, with every failure translated to a SourceExceptions subclass. '''
        url = self.base_url + path
        try:
            response = requests.get(url, params=params, headers=self.headers,
                                    timeout=REQUEST_TIMEOUT_SECS)
        except requests.exceptions.Timeout as e:
            raise SourceTransientException(f"Request to {path} timed out.") from e
        except requests.exceptions.RequestException as e:
            raise SourceTransientException(f"Request to {path} failed: {e}") from e
        if response.status_code in (401, 403):
            raise SourceAuthException(
                f"Source rejected credentials on {path} (HTTP {response.status_code}).")
        if response.status_code == 429:
            raise SourceRateLimitException(f"Source is rate limiting (HTTP 429 on {path}).")
        if response.status_code >= 500:
            raise SourceTransientException(
                f"Source server error on {path} (HTTP {response.status_code}).")
        if response.status_code >= 400:
            raise SourceFatalException(
                f"Request to {path} rejected (HTTP {response.status_code}): "
                f"{response.text[:200]}")
        try:
            return response.json()
        except ValueError as e:
            raise SourceFatalException(
                f"Non-JSON response from {path} - check that BaseUrl points at the API, "
                "not the UI host.") from e

    def _get_paged(self, path: str, params: dict = None):
        '''
        Pages through a collection endpoint.  Written for `links.next`-style
        paging; adapt the loop condition for cursor/offset/page-number APIs.
        '''
        data = self._get(path, params=params)
        yield from data.get("data", [])
        while data.get("links", {}).get("next"):
            data = self._get(data["links"]["next"])
            yield from data.get("data", [])


# ===========================================================================
# MockTemplateClient - no-vendor stand-in for the no-op template run
# ===========================================================================

class MockTemplateClient(TemplateClient):
    '''
    Same surface as TemplateClient, canned payloads.

    The payloads double as documentation of the shape the template's mapping
    functions expect.  Replace field names in TemplateAdapter to match your
    vendor rather than reshaping your vendor's data to match these.
    '''

    MOCK_ASSETS = [
        {
            "id": "mock-asset-001",
            "name": "Example App One",
            "version": "main",
            "updated_at": "2026-08-01T12:00:00.000Z",
            "gui_url": "https://vendor.example.com/assets/mock-asset-001"
        },
        {
            "id": "mock-asset-002",
            "name": "Example App Two",
            "version": "release/2.0",
            "updated_at": "2026-08-15T09:30:00.000Z",
            "gui_url": "https://vendor.example.com/assets/mock-asset-002"
        },
    ]

    MOCK_ISSUES = {
        "mock-asset-001": [
            {
                "id": "mock-issue-001",
                "title": "Example vulnerability in dependency",
                "severity": "high",
                "status": "open",
                "created_at": "2026-07-01T00:00:00.000Z",
                "updated_at": "2026-08-01T12:00:00.000Z",
                "cve_ids": ["CVE-2026-0001"],
                "description": "An example finding used by the template's mock run.",
                "recommendation": "Upgrade the dependency.",
                "location": "package.json"
            },
        ],
        "mock-asset-002": [
            {
                "id": "mock-issue-002",
                "title": "Example resolved vulnerability",
                "severity": "medium",
                "status": "resolved",
                "created_at": "2026-06-15T00:00:00.000Z",
                "updated_at": "2026-08-15T09:30:00.000Z",
                "resolved_at": "2026-08-15T09:30:00.000Z",
                "cve_ids": [],
                "description": "A closed example finding - exercises the RemovedDate path.",
                "recommendation": None,
                "location": "src/example.py"
            },
        ],
    }

    def __init__(self, settings=None, source_name: str = None):
        # Deliberately does not call super().__init__ - no config or HTTP needed.
        self._source_name = source_name
        self.base_url = "https://vendor.example.com/"
        self.headers = {}

    def get_assets_generator(self):
        logging.info("[MockTemplateClient] Yielding %s mock asset(s).", len(self.MOCK_ASSETS))
        yield from self.MOCK_ASSETS

    def get_asset(self, asset_id: str) -> dict:
        for asset in self.MOCK_ASSETS:
            if asset["id"] == asset_id:
                return asset
        raise SourceTransientException(f"Mock asset '{asset_id}' not found.")

    def get_issues_generator(self, asset_id: str):
        yield from self.MOCK_ISSUES.get(asset_id, [])
