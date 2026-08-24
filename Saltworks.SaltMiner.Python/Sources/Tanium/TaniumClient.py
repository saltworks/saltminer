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
    '''
    HTTP layer failure that survived the retry budget.

    Carries status_code so page-size classification can tell a 503 (shrink and
    retry) from a 403 (shrinking cannot fix a bad token).  None means the request
    never got a response at all - a timeout or a connection failure.
    '''
    def __init__(self, message, status_code=None):
        super().__init__(message)
        self.StatusCode = status_code


class TaniumPageSizeException(TaniumException):
    '''
    Page-size adaptation gave up: the floor was reached, the per-page retry budget
    was spent, or the projected run no longer fits inside the cursor lifetime.

    Distinct from the failures it wraps so a caller can tell "this instance cannot
    serve this query at any size we are willing to try" from a one-off transport
    blip.
    '''
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
EXTENSION_FINDING_FIELDS  = [
    "detectedProducts",
    # Promoted from the probe tier because the mapping reads them.  Every entry
    # here is a field some mapped value depends on - keep it that way, and move
    # a field back down the moment nothing maps it.
    "remediation",           # -> Vulnerability.Recommendation
    "excepted",              # -> Vulnerability.IsSuppressed
    "scanType",              # -> Saltminer.Source.Analyzer
    "cvssTemporalScoreV3",   # -> Vulnerability.Score.Temporal
    "epssScore",             # -> Attributes
    "epssPercentile",        # -> Attributes
    "isCisaKev",             # -> Attributes
    "maxMaturity",           # -> Attributes
    "detectedCPEs",          # -> Attributes
    "cpes"                   # -> Attributes
]

# Candidates for the census probe only (--probe-extended).  Presence in the
# schema is not evidence the tenant populates them; that is what the probe
# measures.  Nothing here is mapped, and nothing here should be mapped until
# the probe says it carries data.
PROBE_ENDPOINT_FIELDS = []
PROBE_FINDING_FIELDS = [
    # Measured on the lab tenant and not worth mapping yet:
    "cvssScoreV4",       # 12 of 2013 populated
    "cvssSeverityV4",    # "Unscored"
    "cvssVectorV4",      # empty string
    "affectedProducts",  # duplicated detectedProducts in every sampled finding
    "cwes"               # not mapped; candidate for Vulnerability.Classification
]


# `sort` is typed [EndpointFieldSort!].  A single object is legal under GraphQL
# input coercion, so the un-listed form below is correct as written.
#
# SortOrder is `asc` / `desc`, lower case.  Enum values are case sensitive and
# `ASC` does not exist in this schema.
ENDPOINTS_QUERY_TEMPLATE = '''
query TaniumCveFindings($first: Int = 100, $after: Cursor, $allNamespaces: Boolean = true%(filter_var)s) {
  endpoints(
    first: $first
    after: $after
    source: { tds: { allNamespaces: $allNamespaces } }
    sort: { path: "id", order: asc }
%(filter_arg)s  ) {
    totalRecords
    pageInfo { hasNextPage endCursor }
    edges {
      cursor
      node {
%(endpoint_fields)s%(compliance_block)s
      }
    }
  }
}
'''

# The compliance sub-selection, kept separate so ID_ONLY can omit it entirely.
#
# Do not add a `filter` here without also setting restrictOwner: false.
# FieldFilter.restrictOwner defaults to true, which drops every endpoint with no
# matching finding.  Clean endpoints would vanish from the page set, read as
# absent, and mass-close every finding recorded against them.
COMPLIANCE_BLOCK = '''
        compliance {
          cveFindings {
%(finding_fields)s
          }
        }'''


class QueryVariant:
    '''
    Which filter the composed `endpoints` query carries.

    Deliberately orthogonal to `lean` (see BuildEndpointsQuery): filter and
    payload are independent choices, and folding them into one enum would mean a
    new name for every combination - a lean resumed walk being the obvious one
    that a flat enum forgets to provide.
    '''
    PLAIN  = "plain"
    RESUME = "resume"
    BY_ID  = "byid"
    ALL    = (PLAIN, RESUME, BY_ID)


# (variable declaration fragment, argument fragment) per variant.
#
# `filter` is EndpointFieldFilter (Stability 3): path / op / value, op defaulting
# to EQ.  Unlike the FieldFilter on cveFindings it has no restrictOwner, so
# filtering endpoints cannot silently drop clean ones.
#
# The GT variant is what makes a walk resumable without a cursor.  It is only
# correct because `sort` is pinned to id ascending - do not unpin it.
QUERY_FILTERS = {
    QueryVariant.PLAIN:  ("", ""),
    QueryVariant.RESUME: (", $idAfter: String!",
                          '    filter: { path: "id", op: GT, value: $idAfter }\n'),
    QueryVariant.BY_ID:  (", $id: String!",
                          '    filter: { path: "id", op: EQ, value: $id }\n'),
}

# A lean query selects one field.  Anything more is payload the worker is going
# to re-fetch anyway when it pulls the endpoint by id.
LEAN_ENDPOINT_FIELDS = ["id"]


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

# ---------------------------------------------------------------------------
# page-size failure classification
# ---------------------------------------------------------------------------
#
# Shrinking the page only helps when the failure is about how much work one
# request asked for.  Everything else has to abort immediately: retrying a bad
# token or a malformed query at a smaller size just burns the cursor's lifetime
# and buries the real error under five identical ones.
#
# Tanium imposes no rate limiting of its own (vendor confirmed, 2026-08); any
# throttling seen here comes from the hosting infrastructure.

PAGE_RETRYABLE_HTTP = (429, 502, 503, 504)
PAGE_FATAL_HTTP     = (400, 401, 403, 404, 405)

# Substrings that mark a GraphQL error as load- or size-related.  Matched
# case-insensitively against the whole serialized error array.
GRAPHQL_RETRYABLE_HINTS = (
    "timeout", "timed out", "deadline",
    "too large", "response size", "payload size", "result set",
    "query cost", "complexity", "too complex",
    "resource", "exhausted", "overload", "memory", "capacity", "try again"
)

# Substrings that mark it as a query defect.  These are deterministic - the same
# document will fail identically at any page size.
GRAPHQL_FATAL_HINTS = (
    "cannot query field", "unknown argument", "unknown type", "did you mean",
    "expected type", "is not defined", "validation", "syntax error",
    "must not be", "required", "unauthorized", "forbidden", "permission",
    "access denied", "invalid token"
)


def ClassifyPageFailure(exc):
    '''
    Decide whether a failed page request is worth retrying at a smaller size.

    Returns (is_retryable, reason).  `reason` is short and goes into the shrink
    log line, so an operator reading logs can see *why* a size was abandoned.

    Unclassifiable GraphQL errors come back retryable - the caller is expected to
    allow exactly one such attempt and then abort, per the design.  Guessing
    "retryable" once is cheap; guessing "fatal" on a transient error costs the run.
    '''
    if isinstance(exc, TaniumGraphQLException):
        blob = json.dumps(exc.Errors).lower() if exc.Errors else str(exc).lower()
        for hint in GRAPHQL_FATAL_HINTS:
            if hint in blob:
                return False, f"graphql defect ({hint})"
        for hint in GRAPHQL_RETRYABLE_HINTS:
            if hint in blob:
                return True, f"graphql load ({hint})"
        return True, "graphql unclassified"

    if isinstance(exc, TaniumTransportException):
        code = exc.StatusCode
        if code is None:
            return True, "timeout or connection failure"
        if code in PAGE_FATAL_HTTP:
            return False, f"http {code}"
        if code in PAGE_RETRYABLE_HTTP or code >= 500:
            return True, f"http {code}"
        return False, f"http {code}"

    return False, type(exc).__name__


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
        # Adaptive page sizing.  The tolerable size is payload-dependent and cannot
        # be known ahead of time - a size that serves metadata fine will fail once
        # compliance.cveFindings is in the selection - so it is discovered per run.
        # `or default` is wrong for these: 0 is a meaningful value for the retry
        # count and the delay, and `0 or 3` is 3.  Only an absent or blank setting
        # falls back.
        self.page_size_start       = int(self.__Setting(settings, "Page_Size_Start", 500))
        self.page_size_min         = int(self.__Setting(settings, "Page_Size_Min", 25))
        self.max_retries_per_page  = int(self.__Setting(settings, "Max_Retries_Per_Page", 5))
        self.retry_delay_seconds   = float(self.__Setting(settings, "Retry_Delay_Seconds", 3))
        if self.page_size_min < 1:
            raise ValueError(f"Page_Size_Min must be at least 1, got {self.page_size_min}.")
        if self.page_size_start < self.page_size_min:
            raise ValueError(f"Page_Size_Start ({self.page_size_start}) is below "
                             f"Page_Size_Min ({self.page_size_min}).")

        self.__page_size_current = None
        self.__page_size_locked = False
        self.__shrink_events = []
        self.__total_records = None

        self.__walk_start = None
        self.__last_request_at = None
        # Furthest endpoint id yielded, for cursor-free resume.  See CheckpointId.
        self.__checkpoint_id = None

        # Resolved extension fields, populated once per run by introspection.
        self.__extended = False
        self.__resolved_endpoint_ext = None
        self.__resolved_finding_ext = None
        self.__dropped = {}
        self.__queries = {}

    # -- configuration helpers ------------------------------------------------

    @staticmethod
    def __Setting(settings, key, default):
        '''
        Reads one Tanium setting, falling back only when it is genuinely absent.

        Distinct from `value or default` because 0 is a legitimate setting here -
        zero retries, zero delay - and truthiness would silently replace it.
        '''
        value = settings.GetSource("Tanium", key, None)
        if value is None or value == "":
            return default
        return value

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
                        f"Tanium request failed after {attempts} attempts: {last_error}",
                        status_code=response.status_code)
                self.__Backoff(attempt, last_error)
                continue

            # Any other non-2xx is deterministic - auth, bad request, bad path.
            if not response.ok:
                raise TaniumTransportException(
                    f"Tanium request failed: HTTP {response.status_code}: {response.text[:2000]}",
                    status_code=response.status_code)

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
        '''
        Resets cursor lifetime bookkeeping.  Call at the start of every pager run.

        Deliberately does not clear CheckpointId: a resumed walk has to be able to
        report the furthest id reached across all of its segments, not just the last.
        '''
        self.__walk_start = time.monotonic()
        self.__last_request_at = None

    # -- adaptive page sizing -------------------------------------------------

    @property
    def PageSize(self):
        ''' Page size currently in use, or None before a walk has started. '''
        return self.__page_size_current

    @property
    def PageSizeLocked(self) -> bool:
        ''' True once a size has served a page successfully in this run. '''
        return self.__page_size_locked

    @property
    def ShrinkEvents(self):
        ''' [{from, to, reason, after}] for every shrink this run.  Empty is the good case. '''
        return list(self.__shrink_events)

    def __BeginPageSizing(self, first):
        '''
        Resets sizing for a new walk.

        An explicit `first` sets the starting size rather than disabling adaptation:
        a caller asking for 5000 on a lean walk still wants to be rescued if the
        instance cannot serve it, and silently honouring an impossible size would
        just fail the run.
        '''
        start = first if first is not None else self.page_size_start
        self.__page_size_current = self.__ClampFirst(start)
        self.__page_size_locked = False
        self.__shrink_events = []
        self.__total_records = None

    def __ProjectedWalkSeconds(self):
        '''
        Estimated total seconds for the whole walk at the current rate, or None
        while there is not enough information to say.
        '''
        if not self.__total_records or self.__nodes <= 0 or self.__walk_start is None:
            return None
        elapsed = time.monotonic() - self.__walk_start
        if elapsed <= 0:
            return None
        return self.__total_records * (elapsed / self.__nodes)

    def __FetchPageAdaptive(self, after, resume_from, lean):
        '''
        One page, shrinking the size and retrying the same cursor position on any
        load-related failure.

        A failed request returned no data, so the cursor has not advanced - the
        retry re-requests the same `after` with a smaller `first`.  On success the
        size is locked for the rest of the run and never probed back upward;
        re-discovering the ceiling mid-walk would just re-trigger the failure.

        Retries use a short fixed delay, never exponential backoff: the cursor idle
        window is five minutes, and a backoff long enough to matter would expire
        the walk position it is trying to protect.
        '''
        attempts = 0
        unclassified_used = False

        while True:
            size = self.__page_size_current
            try:
                endpoints = self.GetEndpointsPage(after=after, first=size,
                                                  resume_from=resume_from, lean=lean)
                if not self.__page_size_locked:
                    self.__page_size_locked = True
                    logging.info("[Tanium Client] Page size locked at %s for this run.", size)
                return endpoints
            except TaniumCursorExpired:
                raise                                     # not a sizing problem
            except TaniumException as ex:
                retryable, reason = ClassifyPageFailure(ex)

                # An error we cannot classify gets exactly one benefit of the doubt
                # per page position; a second means it is not transient.
                if reason == "graphql unclassified":
                    if unclassified_used:
                        raise TaniumPageSizeException(
                            f"Unclassified GraphQL error recurred at page size {size}; "
                            f"refusing to retry further. Raw error: {ex}") from ex
                    unclassified_used = True

                if not retryable:
                    raise

                attempts += 1
                if attempts > self.max_retries_per_page:
                    raise TaniumPageSizeException(
                        f"Page at cursor {after!r} failed {attempts} time(s) "
                        f"(limit {self.max_retries_per_page}), last size {size}, "
                        f"last reason: {reason}. Aborting rather than retrying blind.") from ex

                if size <= self.page_size_min:
                    raise TaniumPageSizeException(
                        f"Page request failed at the floor size {self.page_size_min} "
                        f"({reason}). This instance cannot serve this query at any size "
                        f"we are willing to try; lower Page_Size_Min only if you know "
                        f"the payload can be split further. Last error: {ex}") from ex

                new_size = max(self.page_size_min, size // 2)
                self.__shrink_events.append({"from": size, "to": new_size,
                                             "reason": reason, "after": after})
                logging.warning("[Tanium Client] Page size %s -> %s at cursor %r (%s). "
                                "Retrying the same position in %ss.",
                                size, new_size, after, reason, self.retry_delay_seconds)
                self.__page_size_current = new_size
                self.__page_size_locked = False

                if new_size <= self.page_size_min:
                    projected = self.__ProjectedWalkSeconds()
                    if projected is not None and projected > CURSOR_WALK_LIMIT_S:
                        raise TaniumPageSizeException(
                            f"At the floor size {self.page_size_min} the walk projects to "
                            f"{projected/60:.0f} minutes, past the {CURSOR_WALK_LIMIT_S/60:.0f} "
                            f"minute cursor lifetime. Collect in segments using the id "
                            f"checkpoint ({self.CheckpointId}) instead of one walk.") from ex

                time.sleep(self.retry_delay_seconds)

    def __UpdateCheckpoint(self, edges):
        '''
        Records the last id on a page as the resume point.  Last, not max - the
        server sorted the page by id ascending and that ordering is authoritative.
        '''
        for edge in reversed(edges or []):
            node = (edge or {}).get("node")
            if node is None:
                continue
            node_id = node.get("id")
            if node_id is not None:
                self.__checkpoint_id = str(node_id)
                return

    @property
    def CheckpointId(self):
        '''
        Highest endpoint id yielded so far, or None before the first page.
        Pass back as GetEndpointsGenerator(resume_from=...) to continue a walk that
        was cut short by cursor expiry, a crash, or a deliberate stop.
        '''
        return self.__checkpoint_id

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
            self.__queries = {}

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
        self.__queries = {}

        logging.info("[Tanium Client] Extension fields kept: %s=%s %s=%s",
                     ENDPOINT_TYPE, keep_ep, FINDING_TYPE, keep_fi)
        for type_name, dropped in self.__dropped.items():
            if dropped:
                logging.info(
                    "[Tanium Client] Dropped %s extension field(s) on %s - absent from the "
                    "running schema or not a leaf type: %s", len(dropped), type_name, dropped)
        return keep_ep, keep_fi

    def BuildEndpointsQuery(self, variant=QueryVariant.PLAIN, lean=False):
        '''
        Composes an `endpoints` query from core plus resolved extension fields.

        Three variants share one template and one field set, so a field added to the
        walk is automatically present in a by-id refetch.  A refetch that returned a
        narrower node than the walk would be worse than no refetch at all - the
        difference would read as the endpoint having lost data.

          PLAIN  - cursor pagination, no filter.  The normal walk.
          RESUME - adds `id GT $idAfter`.  Used for the whole remainder of a resumed
                   walk, not just its first page: the cursors a filtered query returns
                   belong to that filtered result set, so the filter has to stay on.
          BY_ID  - adds `id EQ $id`.  Single endpoint, no walk.

        EndpointFieldFilter carries no restrictOwner, so unlike the FieldFilter on
        cveFindings there is no silent-drop behaviour to defend against here.
        '''
        if variant not in QueryVariant.ALL:
            raise ValueError(f"Unknown query variant '{variant}'.")
        cache_key = (variant, bool(lean))
        cached = self.__queries.get(cache_key)
        if cached is not None:
            return cached

        if lean:
            # No introspection needed: `id` is Stability 3 and always present, so a
            # lean enumeration walk cannot be broken by a vendor field rename.
            endpoint_fields  = list(LEAN_ENDPOINT_FIELDS)
            compliance_block = ""
        else:
            keep_ep, keep_fi = self.ResolveFields()
            endpoint_fields  = CORE_ENDPOINT_FIELDS + list(keep_ep)
            finding_fields   = CORE_FINDING_FIELDS + list(keep_fi)
            compliance_block = COMPLIANCE_BLOCK % {
                "finding_fields": "\n".join(f"            {f}" for f in finding_fields)
            }

        filter_var, filter_arg = QUERY_FILTERS[variant]
        query = ENDPOINTS_QUERY_TEMPLATE % {
            "endpoint_fields":  "\n".join(f"        {f}" for f in endpoint_fields),
            "compliance_block": compliance_block,
            "filter_var": filter_var,
            "filter_arg": filter_arg
        }
        self.__queries[cache_key] = query
        return query

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

    def IterEndpointIds(self, first=None, max_pages=None, resume_from=None):
        '''
        Yields every endpoint id, one at a time, off a lean walk.

        This is call one of the two-call collection design: enumerate cheaply
        here, then let workers fetch each endpoint independently by id.  Selecting
        only `id` is what keeps it cheap - the full node is ~461 KB and the worker
        re-fetches it anyway.

        `first` defaults to MAX_FIRST rather than the configured page size:
        enumeration wants the largest page the schema allows, since the payload
        per row is a single string.
        '''
        for page in self.GetEndpointsGenerator(first=first or MAX_FIRST,
                                               max_pages=max_pages,
                                               resume_from=resume_from,
                                               lean=True):
            for edge in (page.get("edges") or []):
                node = (edge or {}).get("node")
                if node is None:
                    continue
                node_id = node.get("id")
                if node_id is not None:
                    yield str(node_id)

    def GetEndpointById(self, endpoint_id):
        '''
        Returns the raw `endpoints` dict for one endpoint, selected by id equality
        rather than by walking to it.  Same field set as the walk.

        Deliberately does not touch cursor bookkeeping: this is not part of a walk,
        and letting it stamp the idle clock would mask an expiring cursor held by a
        walk running alongside it.

        Returns the envelope, not the node - callers need totalRecords to tell
        "no such endpoint" from "endpoint with no compliance block" apart.
        '''
        if endpoint_id is None or str(endpoint_id) == "":
            raise ValueError("GetEndpointById requires an endpoint id.")
        data = self.Post(self.BuildEndpointsQuery(QueryVariant.BY_ID), {
            "first": 1,
            "after": None,
            "allNamespaces": self.all_namespaces,
            "id": str(endpoint_id)
        })
        return data.get("endpoints")

    def GetEndpointsPage(self, after=None, first=None, resume_from=None, lean=False):
        '''
        Returns the raw `endpoints` dict for a single page, unmodified.

        Nothing is defaulted, coalesced, or flattened.  The distinction between
        a missing compliance block and an empty one is the thing the runner exists
        to measure, and it does not survive a .get(x, {}) chain.

        :resume_from: when set, pages the `id GT` filtered result set instead of the
            unfiltered one.  Must stay set for every page of that walk, including the
            ones that also pass `after` - the cursors belong to the filtered set.
        :lean: select only `id` and omit compliance entirely.  This is the
            enumeration pass that feeds the work queue.
        '''
        variables = {
            "first": self.__ClampFirst(first),
            "after": after,
            "allNamespaces": self.all_namespaces
        }
        if resume_from is None:
            variant = QueryVariant.PLAIN
        else:
            variant = QueryVariant.RESUME
            variables["idAfter"] = str(resume_from)
        data = self.Post(self.BuildEndpointsQuery(variant, lean=lean), variables)
        # The idle window is per-cursor, so the clock advances here and nowhere
        # else.  Stamped after the response so introspection or token lookups
        # interleaved with a walk cannot mask an expired cursor.
        self.__last_request_at = time.monotonic()
        return data.get("endpoints")

    def GetEndpointsGenerator(self, first=None, max_pages=None, resume_from=None, lean=False):
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

        :resume_from: restart after this endpoint id, with no cursor.  Cursors die
            at 5 minutes idle and 1 hour absolute, so a fleet large enough to walk
            past either limit can only be collected in resumable segments.

        CheckpointId is updated after every page.  On TaniumCursorExpired - or any
        other mid-walk failure - it is the id to pass back as resume_from, and the
        walk continues rather than restarting from the top.  Correctness rests on
        the id-ascending sort, which is why the checkpoint is the last edge rather
        than a max() over the page: the server's ordering is authoritative, and a
        lexicographic max over id strings would not necessarily agree with it.
        '''
        self.BeginWalk()
        self.__BeginPageSizing(first)
        after = None
        page = 0

        while True:
            if max_pages is not None and page >= max_pages:
                logging.info("[Tanium Client] Stopping at max_pages=%s.", max_pages)
                break

            self.CheckCursorLifetime()
            endpoints = self.__FetchPageAdaptive(after=after, resume_from=resume_from, lean=lean)
            page += 1
            self.__pages += 1

            if not endpoints:
                raise TaniumGraphQLException("Tanium returned a response with no 'endpoints' block.")

            if endpoints.get("totalRecords") is not None:
                self.__total_records = endpoints["totalRecords"]
            edges = endpoints.get("edges") or []
            self.__nodes += len(edges)
            self.__UpdateCheckpoint(edges)
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
