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
import time

import requests

from Utility.SaltminerExceptions import SaltminerException


class TaniumException(SaltminerException):
    ''' Base for all Tanium client failures. '''
    pass

class TaniumTransportException(TaniumException):
    ''' HTTP layer failure that survived the retry budget. '''
    pass

class TaniumGraphQLException(TaniumException):
    '''
    The response carried a GraphQL `errors` array.  Tanium returns these with
    HTTP 200, so raise_for_status never sees them.
    '''
    def __init__(self, message, errors=None):
        super().__init__(message)
        self.Errors = errors or []

class TaniumCursorExpired(TaniumException):
    '''
    A cursor was, or was about to be, used outside its documented lifetime.
    Raised rather than silently restarting so a truncated walk stays visible.
    '''
    pass


# Query is verbatim from the Tanium Gateway reference, provenance [vendor-docs],
# with allNamespaces lifted from a literal to a variable.  Do not add fields.
# If a field is wanted and its name is unknown, use IntrospectType.
ENDPOINTS_QUERY = '''
query TaniumCveFindings($first: Int = 100, $after: Cursor, $allNamespaces: Boolean = true) {
  endpoints(
    first: $first
    after: $after
    source: { tds: { allNamespaces: $allNamespaces } }
    sort: { path: "id", order: ASC }
  ) {
    totalRecords
    pageInfo { hasNextPage endCursor }
    edges {
      cursor
      node {
        id
        name
        computerID
        systemUUID
        serialNumber
        domainName
        namespace
        ipAddress
        ipAddresses
        macAddresses
        manufacturer
        model
        chassisType
        isVirtual
        entityProviderName
        entityProviderType
        eidLastSeen
        compliance {
          cveFindings {
            cveId
            cvssScoreV3
            severityV3
            summary
            detectedProducts
            firstFound
            absoluteFirstFoundDate
            lastFound
          }
        }
      }
    }
  }
}
'''

TYPE_INTROSPECTION_QUERY = '''
query TaniumIntrospectType($name: String!) {
  __type(name: $name) {
    name
    kind
    fields {
      name
      type { name kind ofType { name kind ofType { name kind } } }
    }
  }
}
'''

SCHEMA_INTROSPECTION_QUERY = '''
query TaniumIntrospectSchema {
  __schema {
    types {
      name
      kind
      fields { name type { name kind ofType { name kind } } }
    }
  }
}
'''

AUTH_SESSION = "session"
AUTH_BEARER = "bearer"

# Schema documented maximum for the `first` argument on `endpoints`.
MAX_FIRST = 5000

# Cursors expire 5 minutes after the most recent request against them and
# 1 hour absolute.  These are the margins we refuse to cross.
CURSOR_IDLE_LIMIT_S = 240
CURSOR_WALK_LIMIT_S = 3300


class TaniumClient:
    '''
    GraphQL client for the Tanium API Gateway.

    Structural shape follows SeekerClient.  Error handling deliberately does not:
    this client raises where SeekerClient logs and breaks, because a partial page
    set that looks complete is the failure mode that matters for this source.
    '''

    def __init__(self, settings):
        self.base_url        = settings.GetSource("Tanium", "Base_Url")
        self.token           = settings.GetSource("Tanium", "API_Key")
        self.auth_header     = self.__NormalizeAuthScheme(settings.GetSource("Tanium", "Auth_Header", None))
        self.page_size       = int(settings.GetSource("Tanium", "Page_Size", None) or 100)
        self.all_namespaces  = self.__AsBool(settings.GetSource("Tanium", "All_Namespaces", None), True)
        self.request_timeout = int(settings.GetSource("Tanium", "Timeout", None) or 60)

        self.__pages = 0
        self.__nodes = 0
        self.__requests = 0
        self.__started = None
        self.__last_response_bytes = 0

        # Cursor lifetime bookkeeping, reset at the start of every walk.
        self.__walk_start = None
        self.__last_request_at = None

    # -- configuration helpers ------------------------------------------------

    @staticmethod
    def __NormalizeAuthScheme(value):
        '''
        The header name is unverified, so it is configurable.  Anything that is
        not recognisably a bearer scheme falls back to `session`.
        '''
        if not value:
            return AUTH_SESSION
        v = str(value).strip().lower()
        if v in ("bearer", "authorization", "authorization: bearer"):
            return AUTH_BEARER
        return AUTH_SESSION

    @staticmethod
    def __AsBool(value, default):
        if value is None or value == "":
            return default
        if isinstance(value, bool):
            return value
        return str(value).strip().lower() in ("true", "1", "yes", "y")

    def BuildHeaders(self, scheme=None):
        '''
        Returns request headers for the given auth scheme, defaulting to the
        configured one.  Exposed so the runner can try both without mutating config.
        '''
        scheme = scheme or self.auth_header
        headers = { "Content-Type": "application/json" }
        if scheme == AUTH_BEARER:
            headers["Authorization"] = f"Bearer {self.token}"
        else:
            headers["session"] = self.token
        return headers

    def __ClampFirst(self, first):
        first = int(first or self.page_size)
        if first < 1:
            raise ValueError(f"Parameter first must be at least 1, got {first}.")
        if first > MAX_FIRST:
            logging.warning("[Tanium Client] first=%s exceeds schema maximum %s, clamping.", first, MAX_FIRST)
            return MAX_FIRST
        return first

    # -- transport ------------------------------------------------------------

    def Post(self, query, variables=None, scheme=None):
        '''
        Executes one GraphQL request and returns the `data` block.

        Retries HTTP 429 and 5xx with exponential backoff, 3 attempts, then raises.
        Connection failures and timeouts are retried on the same budget; they are
        transient in the same way and carry no response body to inspect.

        A GraphQL `errors` payload is never retried.  Those are deterministic.
        '''
        if self.__started is None:
            self.__started = time.monotonic()

        payload = { "query": query, "variables": variables or {} }
        headers = self.BuildHeaders(scheme)
        attempts = 3
        last_error = None

        for attempt in range(1, attempts + 1):
            try:
                self.__requests += 1
                self.__last_request_at = time.monotonic()
                response = requests.post(
                    url=self.base_url,
                    headers=headers,
                    data=json.dumps(payload),
                    timeout=self.request_timeout
                )
            except (requests.exceptions.Timeout, requests.exceptions.ConnectionError) as e:
                last_error = e
                if attempt == attempts:
                    raise TaniumTransportException(
                        f"Tanium request failed after {attempts} attempts: [{type(e).__name__}] {e}") from e
                self.__Backoff(attempt, f"[{type(e).__name__}] {e}")
                continue

            self.__last_response_bytes = len(response.content or b"")

            if response.status_code == 429 or response.status_code >= 500:
                last_error = f"HTTP {response.status_code}: {response.text[:500]}"
                if attempt == attempts:
                    raise TaniumTransportException(
                        f"Tanium request failed after {attempts} attempts: {last_error}")
                self.__Backoff(attempt, last_error)
                continue

            # Any other non-2xx is deterministic - auth, bad request, bad path.
            if not response.ok:
                raise TaniumTransportException(
                    f"Tanium request failed: HTTP {response.status_code}: {response.text[:2000]}")

            try:
                body = response.json()
            except ValueError as e:
                raise TaniumTransportException(
                    f"Tanium returned HTTP {response.status_code} with a non-JSON body: {response.text[:500]}") from e

            # GraphQL errors arrive with HTTP 200.  raise_for_status never sees them.
            errors = body.get("errors")
            if errors:
                raise TaniumGraphQLException(
                    f"Tanium GraphQL returned {len(errors)} error(s): {json.dumps(errors)[:2000]}",
                    errors=errors)

            if "data" not in body:
                raise TaniumGraphQLException("Tanium GraphQL response contained neither 'data' nor 'errors'.")

            return body["data"]

        # Unreachable - every path above either returns or raises.
        raise TaniumTransportException(f"Tanium request failed: {last_error}")

    @staticmethod
    def __Backoff(attempt, reason):
        delay = 2 ** (attempt - 1)
        logging.warning("[Tanium Client] Attempt %s failed (%s), retrying in %ss.", attempt, reason, delay)
        time.sleep(delay)

    # -- cursor lifetime ------------------------------------------------------

    def BeginWalk(self):
        ''' Resets cursor lifetime bookkeeping.  Call at the start of every pager run. '''
        self.__walk_start = time.monotonic()
        self.__last_request_at = None

    def CheckCursorLifetime(self):
        '''
        Raises TaniumCursorExpired if the next request would fall outside the
        cursor lifetime.  Cursors expire 5 minutes after the last request against
        them and 1 hour absolute; we refuse at 4 minutes and 55 minutes.
        '''
        now = time.monotonic()
        if self.__last_request_at is not None:
            idle = now - self.__last_request_at
            if idle > CURSOR_IDLE_LIMIT_S:
                raise TaniumCursorExpired(
                    f"Cursor idle for {idle:.0f}s, over the {CURSOR_IDLE_LIMIT_S}s limit. "
                    "The walk is truncated; restart it deliberately rather than continuing.")
        if self.__walk_start is not None:
            walked = now - self.__walk_start
            if walked > CURSOR_WALK_LIMIT_S:
                raise TaniumCursorExpired(
                    f"Walk has run {walked:.0f}s, over the {CURSOR_WALK_LIMIT_S}s limit. "
                    "The walk is truncated; restart it deliberately rather than continuing.")

    # -- queries --------------------------------------------------------------

    def GetTotalRecords(self):
        ''' Cheap fleet-size baseline.  Pulls one record and reads totalRecords. '''
        data = self.Post(ENDPOINTS_QUERY, {
            "first": 1,
            "after": None,
            "allNamespaces": self.all_namespaces
        })
        return (data.get("endpoints") or {}).get("totalRecords")

    def GetEndpointsPage(self, after=None, first=None, scheme=None):
        '''
        Returns the raw `endpoints` dict for a single page, unmodified.

        Nothing is defaulted, coalesced, or flattened.  The distinction between
        a missing compliance block and an empty one is the thing the runner exists
        to measure, and it does not survive a .get(x, {}) chain.
        '''
        data = self.Post(ENDPOINTS_QUERY, {
            "first": self.__ClampFirst(first),
            "after": after,
            "allNamespaces": self.all_namespaces
        }, scheme=scheme)
        return data.get("endpoints")

    def GetEndpointsGenerator(self, first=None, max_pages=None):
        '''
        Yields raw endpoint `node` dicts across the full cursor walk.

        Raises on any failure rather than breaking the loop.  A partial page set
        that reports success is worse than a visible failure for this source,
        because closure is inferred from absence.
        '''
        self.BeginWalk()
        after = None
        page = 0

        while True:
            if max_pages is not None and page >= max_pages:
                logging.info("[Tanium Client] Stopping at max_pages=%s.", max_pages)
                break

            self.CheckCursorLifetime()
            endpoints = self.GetEndpointsPage(after=after, first=first)
            page += 1
            self.__pages += 1

            if not endpoints:
                raise TaniumGraphQLException("Tanium returned a response with no 'endpoints' block.")

            edges = endpoints.get("edges") or []
            for edge in edges:
                node = edge.get("node")
                if node is None:
                    continue
                self.__nodes += 1
                yield node

            page_info = endpoints.get("pageInfo") or {}
            if not page_info.get("hasNextPage"):
                break

            after = page_info.get("endCursor")
            if not after:
                raise TaniumGraphQLException(
                    "Tanium reported hasNextPage=true with no endCursor. Refusing to loop.")

    def IntrospectType(self, type_name):
        ''' Returns the `__type` block for one named type, or None if the schema has no such type. '''
        data = self.Post(TYPE_INTROSPECTION_QUERY, { "name": type_name })
        return data.get("__type")

    def IntrospectSchema(self):
        '''
        Returns every type in the schema with its fields.

        Used when no type name is supplied.  The nested endpoint types are listed
        in the reference by field name, not type name, so the type names have to be
        read off the schema rather than guessed.
        '''
        data = self.Post(SCHEMA_INTROSPECTION_QUERY, {})
        return data.get("__schema")

    # -- instrumentation ------------------------------------------------------

    @property
    def LastResponseBytes(self):
        ''' Byte length of the most recent response body.  Used by sizing runs. '''
        return self.__last_response_bytes

    @property
    def run_stats(self):
        return {
            "pages": self.__pages,
            "nodes": self.__nodes,
            "requests": self.__requests,
            "elapsed_s": round(time.monotonic() - self.__started, 3) if self.__started else 0.0
        }

    def ResetStats(self):
        ''' Clears counters between modes in a multi-mode run. '''
        self.__pages = 0
        self.__nodes = 0
        self.__requests = 0
        self.__started = None
        self.__last_response_bytes = 0
