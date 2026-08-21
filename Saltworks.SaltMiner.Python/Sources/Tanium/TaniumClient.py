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


# ---------------------------------------------------------------------------
# field sets
# ---------------------------------------------------------------------------
#
# The query is composed from two tiers rather than frozen as one string.
#
# CORE is Stability 3 - always requested, never conditional.  EXTENSION is
# Stability 1.1/1.2 (Experimental / Release Candidate) and is requested only
# after introspection confirms the running schema still exposes it.
#
# This matters because GraphQL validates the whole document: naming one field
# the server does not have fails the entire query, not that field.  Three of
# the fields this integration already relies on are experimental, so without
# the guard a single vendor rename stops all collection.

ENDPOINT_TYPE = "Endpoint"
FINDING_TYPE  = "EndpointComplianceCveFinding"

CORE_ENDPOINT_FIELDS = [
    "id", "name", "computerID", "systemUUID", "serialNumber", "domainName",
    "namespace", "ipAddress", "ipAddresses", "macAddresses", "manufacturer",
    "model", "chassisType", "isVirtual", "eidLastSeen"
]

CORE_FINDING_FIELDS = [
    "cveId", "cveYear", "cvssScore", "cvssScoreV3", "severity", "severityV3",
    "summary", "firstFound", "absoluteFirstFoundDate", "lastFound"
]

# Experimental fields already in use by the mapping.  Moved here from the
# mandatory set - they were never stable, they were just written down as if
# they were.  Keep this list minimal; widening it is a mapping decision.
EXTENSION_ENDPOINT_FIELDS = ["entityProviderName", "entityProviderType"]
EXTENSION_FINDING_FIELDS  = ["detectedProducts"]

# Candidates for the census probe only (--probe-extended).  Presence in the
# schema is not evidence the tenant populates them; that is what the probe
# measures.  Nothing here is mapped, and nothing here should be mapped until
# the probe says it carries data.
PROBE_ENDPOINT_FIELDS = []
PROBE_FINDING_FIELDS = [
    "excepted", "remediation", "cwes",
    "epssScore", "epssPercentile", "isCisaKev", "maxMaturity",
    "cvssScoreV4", "cvssSeverityV4", "cvssVectorV4", "cvssTemporalScoreV3",
    "detectedCPEs", "affectedProducts", "cpes",
    "scanType"
]


# `sort` is typed [EndpointFieldSort!].  A single object is legal under GraphQL
# input coercion, so the un-listed form below is correct as written.
#
# SortOrder is `asc` / `desc`, lower case.  Enum values are case sensitive and
# `ASC` does not exist in this schema.
ENDPOINTS_QUERY_TEMPLATE = '''
query TaniumCveFindings($first: Int = 100, $after: Cursor, $allNamespaces: Boolean = true) {
  endpoints(
    first: $first
    after: $after
    source: { tds: { allNamespaces: $allNamespaces } }
    sort: { path: "id", order: asc }
  ) {
    totalRecords
    pageInfo { hasNextPage endCursor }
    edges {
      cursor
      node {
%(endpoint_fields)s
        compliance {
          # Do not add a `filter` here without also setting restrictOwner: false.
          # FieldFilter.restrictOwner defaults to true, which drops every endpoint
          # with no matching finding.  Clean endpoints would vanish from the page
          # set, read as absent, and mass-close every finding recorded against them.
          cveFindings {
%(finding_fields)s
          }
        }
      }
    }
  }
}
'''

# Fleet-size baseline.  Deliberately selects no nodes - the previous version ran
# the full endpoints query at first: 1 and pulled an entire node and compliance
# block to read one integer.
TOTAL_RECORDS_QUERY = '''
query TaniumTotalRecords($allNamespaces: Boolean = true) {
  endpoints(first: 1, source: { tds: { allNamespaces: $allNamespaces } }) {
    totalRecords
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
      type { name kind ofType { name kind ofType { name kind ofType { name kind } } } }
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

# Best effort only.  Requires the token to carry `Token - View`.  myAPITokens
# returns APITokenQueryPayload { tokens: [APIToken!], error: SystemError },
# not a bare token list - verified by introspection against a live gateway.
MY_API_TOKENS_QUERY = '''
query TaniumMyApiTokens {
  myAPITokens {
    tokens {
      id
      expiration
      trustedIPAddresses
    }
  }
}
'''

# Schema maximum for the `first` argument on `endpoints`.  Note the schema's own
# default for `first` is 20; the query variable declares 100 and a variable
# default wins over the argument default, so 100 is what an unspecified call
# sends.  Do not "fix" one to match the other.
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

    Auth is the `session` header carrying the API token.  Authorization: Bearer
    returns 401 against this API; there is no scheme to negotiate.
    '''

    def __init__(self, settings):
        self.base_url        = settings.GetSource("Tanium", "Base_Url")
        self.token           = settings.GetSource("Tanium", "API_Key")
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

        # Resolved extension fields, populated once per run by introspection.
        self.__extended = False
        self.__resolved_endpoint_ext = None
        self.__resolved_finding_ext = None
        self.__dropped = {}
        self.__query = None

    # -- configuration helpers ------------------------------------------------

    @staticmethod
    def __AsBool(value, default):
        if value is None or value == "":
            return default
        if isinstance(value, bool):
            return value
        return str(value).strip().lower() in ("true", "1", "yes", "y")

    def BuildHeaders(self):
        ''' Tanium authenticates with the API token in a `session` header. '''
        return {
            "Content-Type": "application/json",
            "session": self.token
        }

    def __ClampFirst(self, first):
        first = int(first or self.page_size)
        if first < 1:
            raise ValueError(f"Parameter first must be at least 1, got {first}.")
        if first > MAX_FIRST:
            logging.warning("[Tanium Client] first=%s exceeds schema maximum %s, clamping.", first, MAX_FIRST)
            return MAX_FIRST
        return first

    # -- transport ------------------------------------------------------------

    def Post(self, query, variables=None):
        '''
        Executes one GraphQL request and returns the `data` block.

        Retries HTTP 429 and 5xx with exponential backoff, 3 attempts, then raises.
        Connection failures and timeouts are retried on the same budget; they are
        transient in the same way and carry no response body to inspect.

        A GraphQL `errors` payload is never retried.  Those are deterministic.

        Note this method does not touch cursor bookkeeping.  The idle clock is
        per-cursor, so only the paged query is allowed to reset it.
        '''
        if self.__started is None:
            self.__started = time.monotonic()

        payload = { "query": query, "variables": variables or {} }
        headers = self.BuildHeaders()
        attempts = 3
        last_error = None

        for attempt in range(1, attempts + 1):
            try:
                self.__requests += 1
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
                    f"Tanium returned HTTP {response.status_code} with a non-JSON body. "
                    f"This usually means Base_Url points at the host rather than the "
                    f"GraphQL endpoint path: {response.text[:500]}") from e

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

    # -- query composition ----------------------------------------------------

    def EnableExtendedFields(self, enabled=True):
        '''
        Widens the extension tier to the full probe list.  Invalidates any
        resolved field set so the next request re-introspects.
        '''
        if enabled != self.__extended:
            self.__extended = enabled
            self.__resolved_endpoint_ext = None
            self.__resolved_finding_ext = None
            self.__dropped = {}
            self.__query = None

    @staticmethod
    def __BaseTypeKind(field):
        '''
        Unwraps NON_NULL / LIST wrappers and returns the innermost kind.

        Presence in the schema is not enough to safely request a field: an OBJECT
        field requires a sub-selection, and naming it bare fails validation for the
        whole document.  Extension fields are composed blind, so anything that is
        not a leaf gets dropped rather than guessed at.
        '''
        node = field.get("type") or {}
        for _ in range(4):
            kind = node.get("kind")
            if kind not in ("NON_NULL", "LIST"):
                return kind
            node = node.get("ofType") or {}
        return node.get("kind")

    def __SchemaLeafFields(self, type_name):
        '''
        Returns the set of scalar/enum field names on one type, or None if the
        schema has no such type.
        '''
        result = self.IntrospectType(type_name)
        if result is None:
            return None
        leaves = set()
        for field in (result.get("fields") or []):
            if self.__BaseTypeKind(field) in ("SCALAR", "ENUM"):
                leaves.add(field.get("name"))
        return leaves

    def __ResolveOne(self, type_name, wanted):
        present = self.__SchemaLeafFields(type_name)
        if present is None:
            logging.warning(
                "[Tanium Client] Schema has no type %r; dropping all %s extension field(s).",
                type_name, len(wanted))
            return [], list(wanted)
        keep = [f for f in wanted if f in present]
        drop = [f for f in wanted if f not in present]
        return keep, drop

    def ResolveFields(self, force=False):
        '''
        Introspects both types once and caches which extension fields the running
        schema actually exposes as leaves.  One request per type, at startup.

        Returns (endpoint_ext, finding_ext).
        '''
        if self.__resolved_finding_ext is not None and not force:
            return self.__resolved_endpoint_ext, self.__resolved_finding_ext

        wanted_ep = list(EXTENSION_ENDPOINT_FIELDS)
        wanted_fi = list(EXTENSION_FINDING_FIELDS)
        if self.__extended:
            wanted_ep += [f for f in PROBE_ENDPOINT_FIELDS if f not in wanted_ep]
            wanted_fi += [f for f in PROBE_FINDING_FIELDS if f not in wanted_fi]

        keep_ep, drop_ep = self.__ResolveOne(ENDPOINT_TYPE, wanted_ep)
        keep_fi, drop_fi = self.__ResolveOne(FINDING_TYPE, wanted_fi)

        self.__resolved_endpoint_ext = keep_ep
        self.__resolved_finding_ext = keep_fi
        self.__dropped = { ENDPOINT_TYPE: drop_ep, FINDING_TYPE: drop_fi }
        self.__query = None

        logging.info("[Tanium Client] Extension fields kept: %s=%s %s=%s",
                     ENDPOINT_TYPE, keep_ep, FINDING_TYPE, keep_fi)
        for type_name, dropped in self.__dropped.items():
            if dropped:
                logging.info(
                    "[Tanium Client] Dropped %s extension field(s) on %s - absent from the "
                    "running schema or not a leaf type: %s", len(dropped), type_name, dropped)
        return keep_ep, keep_fi

    def BuildEndpointsQuery(self):
        ''' Composes the paged query from core plus resolved extension fields. '''
        if self.__query is not None:
            return self.__query
        keep_ep, keep_fi = self.ResolveFields()
        endpoint_fields = CORE_ENDPOINT_FIELDS + list(keep_ep)
        finding_fields  = CORE_FINDING_FIELDS + list(keep_fi)
        self.__query = ENDPOINTS_QUERY_TEMPLATE % {
            "endpoint_fields": "\n".join(f"        {f}" for f in endpoint_fields),
            "finding_fields":  "\n".join(f"            {f}" for f in finding_fields)
        }
        return self.__query

    @property
    def RequestedFindingFields(self):
        ''' Core plus resolved extension finding fields, in query order. '''
        return CORE_FINDING_FIELDS + list(self.__resolved_finding_ext or [])

    @property
    def RequestedEndpointFields(self):
        return CORE_ENDPOINT_FIELDS + list(self.__resolved_endpoint_ext or [])

    @property
    def DroppedFields(self):
        ''' {type_name: [field, ...]} of extension fields the schema did not expose. '''
        return dict(self.__dropped)

    @property
    def ExtendedEnabled(self):
        return self.__extended

    # -- queries --------------------------------------------------------------

    def GetTotalRecords(self):
        ''' Cheap fleet-size baseline.  Selects no nodes. '''
        data = self.Post(TOTAL_RECORDS_QUERY, { "allNamespaces": self.all_namespaces })
        return (data.get("endpoints") or {}).get("totalRecords")

    def GetEndpointsPage(self, after=None, first=None):
        '''
        Returns the raw `endpoints` dict for a single page, unmodified.

        Nothing is defaulted, coalesced, or flattened.  The distinction between
        a missing compliance block and an empty one is the thing the runner exists
        to measure, and it does not survive a .get(x, {}) chain.
        '''
        data = self.Post(self.BuildEndpointsQuery(), {
            "first": self.__ClampFirst(first),
            "after": after,
            "allNamespaces": self.all_namespaces
        })
        # The idle window is per-cursor, so the clock advances here and nowhere
        # else.  Stamped after the response so introspection or token lookups
        # interleaved with a walk cannot mask an expired cursor.
        self.__last_request_at = time.monotonic()
        return data.get("endpoints")

    def GetEndpointsGenerator(self, first=None, max_pages=None):
        '''
        Yields each raw `endpoints` page dict (totalRecords, pageInfo, edges)
        as it comes back, across the full cursor walk. Does not unpack edges
        into individual nodes - that's a caller concern, not this method's.
        Different callers want different granularity (raw page vs one node
        at a time), so walking the cursor is the only thing this method is
        responsible for.

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

            self.__nodes += len(endpoints.get("edges") or [])
            yield endpoints

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

    def GetMyApiTokens(self):
        '''
        Best effort token metadata.  Returns None on any failure.

        The token carries an expiration and a trustedIPAddresses CIDR list.  A
        scheduled runner starts failing when either lapses, and the failure looks
        like an auth error rather than an expiry, so preflight surfaces both when
        the token has permission to read them.
        '''
        try:
            data = self.Post(MY_API_TOKENS_QUERY, {})
            return (data.get("myAPITokens") or {}).get("tokens")
        except TaniumException as e:
            logging.info("[Tanium Client] Token metadata unavailable (needs 'Token - View'): [%s] %s",
                         type(e).__name__, e)
            return None

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
