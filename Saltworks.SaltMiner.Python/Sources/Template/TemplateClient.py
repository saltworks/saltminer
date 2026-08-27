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
Vendor API client for a source adapter, plus the adapter's exception taxonomy.

===========================================================================
WHAT THIS FILE IS
===========================================================================
ALL HTTP, auth, and paging against the source lives here and nowhere else -
the adapter, loader, and worker never import requests.  Four invariants hold
no matter which vendor you point this at, and you break them at your peril:

- Every collection method is a GENERATOR, so memory stays constant regardless
  of result size.  Never materialize a page set into a list.
- Every failure leaves this module as a SourceExceptions subclass (taxonomy at
  the BOTTOM of this file).  Transient vs fatal is the distinction that
  matters operationally: transient may succeed on retry, fatal never will.
  Callers act on the kind of failure without knowing anything about the vendor.
- get_issues_generator() yields the FULL current issue set for its asset.  A
  partial set retires the issues it leaves out - the warning on that method is
  not optional reading.
- Only RequestTimeoutSecs and PageSize are config.  Everything else that
  varies by vendor - endpoints, auth scheme, paging style, retry tuning - is
  code you edit here.

===========================================================================
STEP 1: PICK YOUR ACQUISITION SHAPE
===========================================================================
Before you write a single line, decide which shape your vendor is.  Then
DELETE the methods the other shape does not use - do not leave them sitting
there unimplemented for the next developer to trip over.

--- Shape A: issues-only -------------------------------------------------
The vendor exposes ONE findings endpoint, and each finding payload carries the
asset identity inline (project name, repo, host, whatever the vendor calls
it).  There is nothing to fetch separately.

    Keep:   get_issues_generator() - unscoped, or scoped by whatever grouping
            the vendor does offer.  The adapter derives assets from the issue
            payloads themselves.
    DELETE: get_asset() and get_scans_generator().  Also delete
            get_assets_generator() if the vendor has no asset listing at all.

--- Shape B: separate pulls ----------------------------------------------
The vendor requires distinct calls for assets, scans, and issues.  This is the
shipped template, and the shape MockTemplateClient demonstrates, because it is
the superset.

    Keep:   all of get_assets_generator(), get_asset(), get_scans_generator(),
            get_issues_generator(), each mapped to its own endpoint.

get_asset() additionally carries a `# DELETE-IF-BATCH:` marker.  A batch-model
adapter carries the full asset payload through the loader and never refetches
by id, so that method is dead weight there even in Shape B.

===========================================================================
STEP 2: FILL-IN CHECKLIST
===========================================================================
Work top to bottom.  The quoted names are the `# -- ... --` section banners in
this file; every site you must edit is marked `# WRITE:`.

 1. `-- config --`: nothing to write unless your vendor needs extra config
    keys.  If it does, add them here AND to Config/Sources/SourceTemplate.json
    next to RequestTimeoutSecs and PageSize.
 2. `-- auth --`: pick "static" or "jwt" for auth_scheme, write the header
    construction (static) or the token endpoint and its payload/response field
    names (`-- auth: JWT worked example --`), and delete the scheme you did
    not pick.
 3. `-- vendor calls --`: replace every endpoint path and paging parameter
    name with the vendor's, and delete the methods your shape (Step 1) does
    not use.
 4. `-- transport --`: adapt _get_paged() to the vendor's paging style, and
    _rate_limit_wait() to the vendor's throttling headers.
 5. `MockTemplateClient`: replace the canned payloads with real ones captured
    from your vendor, field names and all.  The adapter's mapping functions
    are written against these payloads, so they are the contract between this
    file and the adapter - not just test data.
 6. Run the mock path end-to-end before pointing at a live vendor:
    `python Sources/Template/TemplateAdapter.py` from the repo root does the
    mock dry run - mappings validated through the real DTOs, nothing sent.
'''

import logging
import time

import requests

from Utility.SaltminerExceptions import SaltminerException


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

    # Per-vendor tuning, deliberately NOT config: these describe how the
    # vendor's API behaves, not how this deployment is run.  Only the request
    # timeout and the page size are config keys.  Retune these by editing the
    # file when you learn what your vendor actually tolerates.
    RATE_LIMIT_MAX_RETRIES = 3
    RATE_LIMIT_BACKOFF_SECS = 2
    RATE_LIMIT_MAX_SLEEP_SECS = 60
    TOKEN_EXPIRY_SKEW_SECS = 60

    def __init__(self, settings, source_name: str):
        # -- config -----------------------------------------------------------
        self._source_name = source_name
        self.base_url = settings.GetSource(source_name, "BaseUrl", None)
        if not self.base_url:
            raise SourceConfigException(
                f"BaseUrl is not set in the '{source_name}' source config.")
        if not self.base_url.endswith("/"):
            self.base_url += "/"
        # WRITE: add any further config keys your vendor needs here, and to
        # Config/Sources/SourceTemplate.json.  Defaults belong in this call so
        # an unmodified config still runs.
        self.request_timeout_secs = settings.GetSource(source_name, "RequestTimeoutSecs", 30)
        self.page_size = settings.GetSource(source_name, "PageSize", 100)

        # -- auth -------------------------------------------------------------
        # WRITE: keep ONE scheme and delete the other, here and below.
        #   "static" - the credential never expires; the header is built once.
        #              This is the out-of-the-box behavior.
        #   "jwt"    - the vendor issues short-lived tokens; see the worked
        #              example under `-- auth: JWT worked example --`.
        self.auth_scheme = "static"
        self._api_key = settings.GetSource(source_name, "ApiKey", None)
        self._token = None
        self._token_expires_at = 0.0
        # WRITE: the vendor's header scheme (Bearer, token, X-Api-Key, session
        # cookie, ...).  Under the "jwt" scheme _ensure_token overwrites
        # Authorization on every refresh, so what you set here is a placeholder.
        self.headers = {
            "Authorization": f"token {self._api_key}",
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
        # WRITE: the vendor's "list projects/endpoints/apps" path, and the
        # vendor's page-size parameter name in place of "limit".
        yield from self._get_paged("assets", params={"limit": self.page_size})

    def get_asset(self, asset_id: str) -> dict:
        '''
        Fetches one asset by id.  Used by the worker, which carries only the
        id through its work items and re-fetches the payload itself.

        Shape A adapters delete this method - their asset identity arrives
        inline on the issue payloads.
        '''
        # DELETE-IF-BATCH: a batch adapter carries the full asset payload
        # through the loader and never refetches by id.  Delete this method.
        # WRITE: the vendor's single-asset path.
        data = self._get(f"assets/{asset_id}")
        if not data:
            raise SourceTransientException(
                f"Asset '{asset_id}' returned no data on refetch - it may have been "
                "removed between enumeration and processing.")
        return data

    def get_scans_generator(self, asset_id: str):
        '''
        Yields the source-side scans/assessments for one asset, paging
        transparently.

        Used by Shape B adapters that record one scan document per source-side
        scan, so the SaltMiner scan history mirrors the vendor's.  Shape A
        adapters - and Shape B adapters that record a single scan per sync run
        instead - delete this method.
        '''
        # WRITE: the vendor's scan/assessment/run listing path for one asset,
        # and the vendor's page-size parameter name in place of "limit".
        yield from self._get_paged(f"assets/{asset_id}/scans",
                                   params={"limit": self.page_size})

    def get_issues_generator(self, asset_id: str):
        '''
        Yields the FULL current issue set for one asset, paging transparently.

        Do not add incremental filters (updated-after etc.) here without
        re-reading the retirement rule in the README.  Downstream retirement
        compares the set this method delivers against what SaltMiner already
        holds for the asset, so a partial pull silently retires every issue
        absent from it - a filter that looks like an optimization here deletes
        real findings there.
        Change detection belongs in the NeedsUpdate gate, at asset granularity.
        '''
        # WRITE: the vendor's findings/issues path, the vendor's page-size
        # parameter name in place of "limit", and - for Shape A - drop the
        # asset_id argument if the vendor's findings endpoint is unscoped.
        yield from self._get_paged(f"assets/{asset_id}/issues",
                                   params={"limit": self.page_size})

    # -- auth: JWT worked example ---------------------------------------------
    #
    # The ALTERNATIVE to the static header built in __init__, for vendors that
    # issue expiring tokens.  Live only when auth_scheme == "jwt"; the static
    # path never enters this code.  Two mechanisms, deliberately:
    #   proactive - _get calls _ensure_token() before every request, which
    #               refreshes once the held token enters the skew window.
    #               This is the primary mechanism.
    #   reactive  - a 401 mid-run forces one refresh and retries the request
    #               exactly once, covering a token that aged out between the
    #               check and the use.  A second 401 means the credentials are
    #               actually bad and raises SourceAuthException.
    # WRITE: keep this if your vendor issues expiring tokens and set
    # auth_scheme = "jwt"; otherwise delete this method and leave the static
    # header in place.

    def _ensure_token(self, force: bool = False):
        '''
        Fetches a token when none is held, when the held one is inside the
        expiry skew window, or when `force` (the reactive 401 path).

        Deliberately does NOT go through _get: _get calls this method, so
        routing the token request back through it would recurse.
        '''
        if not force and self._token and \
                time.time() < self._token_expires_at - self.TOKEN_EXPIRY_SKEW_SECS:
            return
        # WRITE: the vendor's token endpoint, its credential payload, and the
        # response field names (`access_token` / `expires_in` below).
        url = self.base_url + "auth/token"
        try:
            response = requests.post(url, json={"api_key": self._api_key},
                                     headers={"Content-Type": "application/json"},
                                     timeout=self.request_timeout_secs)
        except requests.exceptions.Timeout as e:
            raise SourceTransientException("Token request timed out.") from e
        except requests.exceptions.RequestException as e:
            raise SourceTransientException(f"Token request failed: {e}") from e
        if response.status_code >= 400:
            raise SourceAuthException(
                f"Source rejected the token request (HTTP {response.status_code}) - "
                "check the configured credential.")
        try:
            payload = response.json()
        except ValueError as e:
            raise SourceFatalException(
                "Non-JSON response from the token endpoint - check that BaseUrl points "
                "at the API, not the UI host.") from e
        self._token = payload.get("access_token")
        if not self._token:
            raise SourceAuthException(
                "Token endpoint answered without an access_token - check the response "
                "field names against the vendor's docs.")
        self._token_expires_at = time.time() + float(payload.get("expires_in", 3600))
        self.headers["Authorization"] = f"Bearer {self._token}"

    # -- transport ------------------------------------------------------------

    def _get(self, path: str, params: dict = None, _auth_retried: bool = False) -> dict:
        '''
        One GET, with every failure translated to a SourceExceptions subclass.

        Handles rate limiting in place (see _rate_limit_wait) and, under the
        JWT scheme, token refresh.  Callers see the same contract either way:
        a 429 that survives every retry still surfaces as
        SourceRateLimitException, and a real auth failure as SourceAuthException.
        '''
        url = self.base_url + path
        if self.auth_scheme == "jwt":
            self._ensure_token()
        attempt = 0
        while True:
            try:
                response = requests.get(url, params=params, headers=self.headers,
                                        timeout=self.request_timeout_secs)
            except requests.exceptions.Timeout as e:
                raise SourceTransientException(f"Request to {path} timed out.") from e
            except requests.exceptions.RequestException as e:
                raise SourceTransientException(f"Request to {path} failed: {e}") from e
            if response.status_code == 401 and self.auth_scheme == "jwt" and not _auth_retried:
                # Reactive refresh: the token aged out between check and use.
                logging.warning("[TemplateClient] 401 on %s under the JWT scheme - "
                                "refreshing the token and retrying once.", path)
                self._ensure_token(force=True)
                return self._get(path, params=params, _auth_retried=True)
            if response.status_code in (401, 403):
                raise SourceAuthException(
                    f"Source rejected credentials on {path} (HTTP {response.status_code}).")
            if response.status_code == 429:
                if attempt >= self.RATE_LIMIT_MAX_RETRIES:
                    raise SourceRateLimitException(
                        f"Source is rate limiting (HTTP 429 on {path}) - still throttled "
                        f"after {self.RATE_LIMIT_MAX_RETRIES} retries.")
                wait_secs = self._rate_limit_wait(response, attempt)
                logging.warning("[TemplateClient] Rate limited on %s - waiting %ss before "
                                "retry %s of %s.", path, wait_secs, attempt + 1,
                                self.RATE_LIMIT_MAX_RETRIES)
                time.sleep(wait_secs)
                attempt += 1
                continue
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

    def _rate_limit_wait(self, response, attempt: int) -> float:
        '''
        How long to wait before retrying a throttled request.

        A worked example, not a universal one: this reads the `Retry-After`
        seconds form and falls back to exponential backoff.  Vendors differ -
        some send `Retry-After` as an HTTP date, some send an
        `X-RateLimit-Reset` epoch timestamp, some throttle with a 503 instead
        of a 429.  Adapt the header parsing below; keep the structure
        (honor the vendor's hint, else back off, always cap).
        '''
        # WRITE: the vendor's throttling header(s).
        retry_after = response.headers.get("Retry-After")
        if retry_after:
            try:
                return min(float(retry_after), self.RATE_LIMIT_MAX_SLEEP_SECS)
            except (TypeError, ValueError):
                logging.warning("[TemplateClient] Unparsed Retry-After value '%s' - "
                                "falling back to backoff.", retry_after)
        return min(self.RATE_LIMIT_BACKOFF_SECS * (2 ** attempt),
                   self.RATE_LIMIT_MAX_SLEEP_SECS)

    def _get_paged(self, path: str, params: dict = None):
        '''
        Pages through a collection endpoint.  Written for `links.next`-style
        paging; adapt the loop condition for cursor/offset/page-number APIs.
        '''
        # WRITE: the vendor's paging style - the envelope key holding the rows
        # ("data" below) and how the next page is requested.
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
    Same surface as TemplateClient, canned payloads.  Demonstrates Shape B
    (assets + scans + issues) because it is the superset; a Shape A copy
    drops the methods it does not keep from both classes together.

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

    MOCK_SCANS = {
        "mock-asset-001": [
            {
                "id": "mock-scan-001",
                "scan_type": "sca",
                "status": "complete",
                "started_at": "2026-08-01T11:45:00.000Z",
                "finished_at": "2026-08-01T12:00:00.000Z",
                "issue_count": 1
            },
        ],
        "mock-asset-002": [
            {
                "id": "mock-scan-002",
                "scan_type": "sast",
                "status": "complete",
                "started_at": "2026-08-15T09:15:00.000Z",
                "finished_at": "2026-08-15T09:30:00.000Z",
                "issue_count": 1
            },
        ],
    }

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
        # The attributes below exist only to keep the surface identical.
        self._source_name = source_name
        self.base_url = "https://vendor.example.com/"
        self.headers = {}
        self.request_timeout_secs = 30
        self.page_size = 100

    def get_assets_generator(self):
        logging.info("[MockTemplateClient] Yielding %s mock asset(s).", len(self.MOCK_ASSETS))
        yield from self.MOCK_ASSETS

    def get_asset(self, asset_id: str) -> dict:
        for asset in self.MOCK_ASSETS:
            if asset["id"] == asset_id:
                return asset
        raise SourceTransientException(f"Mock asset '{asset_id}' not found.")

    def get_scans_generator(self, asset_id: str):
        yield from self.MOCK_SCANS.get(asset_id, [])

    def get_issues_generator(self, asset_id: str):
        yield from self.MOCK_ISSUES.get(asset_id, [])


# ===========================================================================
# exception taxonomy
# ===========================================================================
#
# The taxonomy lives in this file because every other module in the adapter
# imports it from here; it sits at the bottom so the client - the code you
# actually edit - is the first thing on screen.  (Python resolves these names
# at raise time, after the module has fully loaded, so the classes above
# referencing them is safe.)
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
    ''' A work item failed end-to-end. '''
    pass
