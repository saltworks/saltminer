"""
GHASClient.py
=============
Async GitHub REST API client for the SaltMiner GHAS Source Adapter.

Responsibilities:
- Auth: PAT and GitHub App (auto-detected from key format)
- Async pagination generators for Code Scanning, Secret Scanning, Dependabot
- Repo inventory and GHAS enablement checks
- SARIF document retrieval
- Rate-limit backoff and retry logic

No SaltMiner dependencies. Fully testable in isolation.
"""

import asyncio
import json
import logging
import time
from typing import AsyncGenerator, Optional

import aiohttp
import jwt
from cryptography.hazmat.primitives import serialization
from cryptography.hazmat.backends import default_backend
from datetime import datetime, timezone, timedelta

logger = logging.getLogger(__name__)

# GitHub API pagination limit
PAGE_SIZE = 100

# ── Rate-limit defaults (grounded in GitHub's published limits) ─────────────
#
# GitHub's documented secondary rate limits (docs.github.com, "Rate limits for
# the REST API"):
#   - No more than 900 points/minute to a single REST endpoint. GET = 1 point,
#     so ~900 GET/min == ~15 req/s is the *documented ceiling* for one endpoint.
#   - No more than 100 concurrent requests (shared REST + GraphQL).
#   - No more than 90s of CPU time per 60s real time. Code-scanning alert
#     queries are comparatively expensive server-side, so this CPU rule trips
#     for code-scanning *below* the 900/min request ceiling — which is why a
#     high-concurrency fan-out fails on code-scanning while dependabot/secret
#     scanning survive the same request rate.
#   - The secondary limit is DYNAMIC ("may change based on current load or risk
#     factors") and may fire "for undisclosed reasons". So a fixed request rate
#     can only ever be a conservative target, never a guarantee — the authoritative
#     signal is the 403/429 "secondary rate limit" response itself plus its
#     Retry-After / x-ratelimit-reset headers.
#
# Design (layered):
#   1. PACE requests to the expensive code-scanning endpoint well under the
#      documented ceiling (token-bucket min-interval). This is the primary
#      avoidance mechanism and matches GitHub's own advice to "make requests
#      serially ... implement a queue system".
#   2. Cap CONCURRENCY on code-scanning low (separate from the global limit),
#      because concurrency interacts with the CPU-time rule.
#   3. On a secondary-limit signal, BACK OFF using GitHub's own headers
#      (Retry-After → x-ratelimit-reset → >=60s then exponential), and after a
#      bounded number of attempts FAIL LOUD as a distinct rate-limit error —
#      never silently skip the scope (which previously masqueraded as "engine
#      inaccessible" and preserved stale/empty data).
#
# All of these are overridable per-instance via config (see GHASClient.__init__).
# Defaults are deliberately conservative: code-scanning paced to ~5 req/s
# (200ms min interval), which is ~1/3 of the documented 900/min ceiling, with a
# concurrency ceiling of 3. Tune upward in config if your org tolerates it.

DEFAULT_CODE_SCANNING_MIN_INTERVAL_MS = 200   # ~5 req/s to the code-scanning endpoint
DEFAULT_CODE_SCANNING_CONCURRENCY = 3         # max concurrent code-scanning requests
DEFAULT_OTHER_ENGINE_MIN_INTERVAL_MS = 0      # no pacing on cheaper endpoints by default

# Re-baseline (cold full-pull) mode defaults — avoidance-first + deep-retry
# backstop, engaged only when the state file is empty. Slower but designed to
# complete code-scanning in a single pass without tripping the secondary limit.
DEFAULT_REBASELINE_CS_MIN_INTERVAL_MS = 330   # ~3 req/s — gentler than steady-state
DEFAULT_REBASELINE_CS_CONCURRENCY = 2         # very low concurrency on the expensive endpoint
DEFAULT_REBASELINE_SECONDARY_MAX_ATTEMPTS = 10  # deep retry backstop (vs 5 normally)

# Secondary-rate-limit retry policy.
#   SECONDARY_MAX_ATTEMPTS — how many times to wait-and-retry a single request
#     that keeps hitting the secondary limit before giving up and raising
#     GHASRateLimitError for that scope (the scope is retried on the next run).
#   SECONDARY_FALLBACK_BACKOFF — used only when GitHub sends NO Retry-After and
#     NO usable x-ratelimit-reset (a documented-but-undocumented gap: GitHub
#     does not always send Retry-After on secondary limits). Per GitHub's
#     guidance: wait >=60s, then increase exponentially.
SECONDARY_MAX_ATTEMPTS = 5
SECONDARY_FALLBACK_BACKOFF = [60, 120, 240, 480, 900]  # seconds; last value repeats if needed

# Legacy name retained for compatibility with any external references; the
# fallback schedule above supersedes it for secondary-limit handling.
SECONDARY_BACKOFF = SECONDARY_FALLBACK_BACKOFF

REQUEST_TIMEOUT = aiohttp.ClientTimeout(total=60, connect=10)


class GHASAuthError(Exception):
    """Raised when authentication fails and cannot be recovered."""


class GHASRateLimitError(Exception):
    """
    Raised when a request to a GHAS alert endpoint repeatedly hits GitHub's
    SECONDARY rate limit and exhausts the retry budget (SECONDARY_MAX_ATTEMPTS).

    This is deliberately DISTINCT from GHASEngineInaccessibleError. The two
    look identical on the wire (both surface as HTTP 403) but mean opposite
    things and demand opposite handling:

      - GHASEngineInaccessibleError (403 "Resource not accessible" / 404):
        a *permission/enablement* condition. The scope is skipped and prior
        SaltMiner data is preserved. Stable across runs.

      - GHASRateLimitError (403/429 "secondary rate limit"):
        a *load* condition. The data IS accessible — we simply asked too fast.
        Skipping-and-preserving here is wrong twice over: it hides a tuning
        problem as if it were a permissions problem, and (because the scope
        keeps its stale/absent watermark) it re-attempts as a full first-sync
        every run, re-hammering the endpoint and re-tripping the limit.

    Historical note: before this class existed, a secondary-limit 403 that
    survived backoff was caught by the `status in (403, 404)` branch and
    converted into GHASEngineInaccessibleError — so an entire org's worth of
    code-scanning scopes were reported as "engine inaccessible" (implying a PAT
    permissions problem) when the real cause was the adapter overrunning the
    code-scanning secondary rate limit under high concurrency. This class makes
    that failure mode visible and separately counted in the run summary.

    The carried `scope` (e.g. "org/repo/code_scanning") lets the adapter list
    rate-limited scopes explicitly in the end-of-run summary.
    """

    def __init__(self, message: str, scope: Optional[str] = None):
        super().__init__(message)
        self.scope = scope


class GHASEngineInaccessibleError(Exception):
    """
    Raised when a per-repo alert endpoint returns 403 or 404 for a
    *permission/enablement* reason — NOT a rate-limit reason and NOT an
    archived repo.

    Two related but separate conditions are handled elsewhere and must not
    reach this exception:
      - SECONDARY RATE LIMIT (403/429 with a "secondary rate limit" body):
        raised as GHASRateLimitError after the retry budget is exhausted.
        See that class. A rate-limit 403 means the data IS accessible; we
        asked too fast. Treating it as "inaccessible" is the bug this split
        was created to fix.
      - ARCHIVED REPOS: detected up front from the repo's `archived` flag and
        diverted to the tombstone/purge path by the adapter BEFORE any alert
        fetch. An archived repo therefore never reaches an alert request and
        never produces one of these 403/404s.

    With those removed, the 403/404 that *does* reach here is a genuine
    permission/enablement condition:
      - 403 "Resource not accessible by personal access token": the PAT can
        talk to GitHub but lacks access to this specific alert endpoint.
        Causes: fine-grained PAT missing the engine's alert-read permission;
        classic PAT missing SSO authorization on a SAML-enforced org;
        fine-grained PAT awaiting org admin approval; PAT repo-selection scope
        excludes this repo.
      - 404 "Not Found": the alert endpoint doesn't exist for this PAT —
        usually the engine has never been enabled on this repo.

    From the adapter's perspective both are operationally equivalent: we can't
    see alerts here, and we don't know whether the absence is real (engine off)
    or apparent (PAT scope). The conservative response is to skip the scope
    WITHOUT advancing state and WITHOUT firing the FIX-001 replacement or
    ENH-004 heartbeat paths, preserving whatever SaltMiner already holds.

    Why we don't fire the replacement path on this:
      Treating "engine inaccessible" as "alerts all closed → empty replacement"
      would destroy previously-queued alerts on the first transient access blip.
      Operators who want to clean up a permanently-disabled scope can remove its
      watermark from the state file (→ first-sync next run).

    Diagnostic guidance:
      If many scopes are skipped via THIS exception (not GHASRateLimitError) on
      one run, suspect a systemic PAT permission/SSO/approval problem. If the
      skips are instead GHASRateLimitError and concentrated on code_scanning,
      suspect secondary rate limiting, not permissions — lower the code-scanning
      pacing/concurrency in config.
    """


class GHASClient:
    """
    Async GitHub REST API client for GHAS data collection.

    The client reads its configuration via settings.GetSource(source_name, ...).
    The source_name parameter identifies which configured instance this client
    belongs to (e.g. "ghas1", "ghas2"), enabling multiple GHAS instances to
    run from a single deployment.

    Usage:
        client = GHASClient(settings, "ghas1")
        async with client:
            repos = await client.get_repos_async()
            async for alert in client.get_alerts_async("org/repo", "code_scanning"):
                process(alert)
    """

    def __init__(self, settings, source_name: str = "GHAS"):
        self._source_name = source_name
        self._base_url = settings.GetSource(source_name, "BaseUrl").rstrip("/")
        self._org = settings.GetSource(source_name, "Org")
        api_key = settings.GetSource(source_name, "ApiKey") or ""
        app_id = settings.GetSource(source_name, "AppId") or 0

        self._auth_mode = self._detect_auth_mode(api_key)
        self._pat = api_key if self._auth_mode == "pat" else None
        self._app_id = int(app_id) if self._auth_mode == "app" else None
        self._app_private_key = self._load_pem_key(api_key) if self._auth_mode == "app" else None

        # GitHub App installation token cache
        self._installation_token: Optional[str] = None
        self._token_expiry: Optional[datetime] = None
        self._token_lock = asyncio.Lock()

        # ── Rate-limit / pacing configuration (all optional, defaulted) ─────
        # Code-scanning is the expensive, secondary-limit-prone endpoint, so it
        # gets its own pacing and concurrency knobs distinct from the other
        # engines. All keys are optional; absent → conservative defaults, so
        # existing configs run unchanged.
        #
        # IMPORTANT: settings.GetSource() RAISES ApplicationConfigurationException
        # when a key is entirely absent from the config (it does NOT return
        # None). So every optional key must be read through _opt_setting(), which
        # catches that and falls back to the default. Using a bare GetSource()
        # for a key the operator hasn't added would crash the whole instance —
        # which is exactly what happened on first deploy.
        cs_interval_ms = self._opt_setting(
            settings, source_name, "CodeScanningMinIntervalMs",
            DEFAULT_CODE_SCANNING_MIN_INTERVAL_MS)
        other_interval_ms = self._opt_setting(
            settings, source_name, "OtherEngineMinIntervalMs",
            DEFAULT_OTHER_ENGINE_MIN_INTERVAL_MS)
        cs_concurrency = self._opt_setting(
            settings, source_name, "CodeScanningConcurrencyLimit",
            DEFAULT_CODE_SCANNING_CONCURRENCY)

        # Per-engine minimum interval between requests (seconds). Enforced by a
        # simple monotonic-clock pacer guarded by a lock per engine, so it works
        # correctly regardless of how many coroutines share the client.
        self._engine_min_interval = {
            "code_scanning": max(0.0, float(cs_interval_ms) / 1000.0),
            "secret_scanning": max(0.0, float(other_interval_ms) / 1000.0),
            "dependabot": max(0.0, float(other_interval_ms) / 1000.0),
        }
        # Pacer state: last-request monotonic timestamp + a lock, per engine.
        self._pace_locks = {eng: asyncio.Lock() for eng in self._engine_min_interval}
        self._pace_last = {eng: 0.0 for eng in self._engine_min_interval}

        # Dedicated concurrency gate for code-scanning requests, layered UNDER
        # the adapter's overall semaphore. Even if the adapter dispatches many
        # code-scanning scopes at once, no more than this many code-scanning
        # HTTP requests are in flight simultaneously. Other engines are not
        # gated here (they use the adapter's global semaphore only).
        self._code_scanning_concurrency = max(1, int(cs_concurrency))
        self._code_scanning_gate = asyncio.Semaphore(self._code_scanning_concurrency)

        # Secondary-limit retry budget for normal (incremental) runs.
        self._secondary_max_attempts = SECONDARY_MAX_ATTEMPTS

        # ── Re-baseline (cold full-pull) mode values ────────────────────────
        # A re-baseline run pulls every scope as a first-sync full fetch, which
        # is the highest-volume / most rate-limit-prone scenario (e.g. ~179
        # code-scanning repos at once). When the adapter detects an empty state
        # file it calls enable_rebaseline_mode(), which tightens code-scanning
        # concurrency and pacing (avoidance-first) AND raises the retry budget
        # (deep-retry backstop) so the run tries to complete code-scanning in a
        # single pass. These engage ONLY on empty-state runs; normal
        # incremental runs keep the faster everyday values above.
        #
        # All overridable per-instance via config; absent → defaults below.
        self._rebaseline_cs_interval_ms = self._opt_setting(
            settings, source_name, "RebaselineCodeScanningMinIntervalMs",
            DEFAULT_REBASELINE_CS_MIN_INTERVAL_MS)
        self._rebaseline_cs_concurrency = self._opt_setting(
            settings, source_name, "RebaselineCodeScanningConcurrencyLimit",
            DEFAULT_REBASELINE_CS_CONCURRENCY)
        self._rebaseline_max_attempts = self._opt_setting(
            settings, source_name, "RebaselineSecondaryMaxAttempts",
            DEFAULT_REBASELINE_SECONDARY_MAX_ATTEMPTS)

        logger.info(
            "[%s] Rate-limit config: code_scanning pacing=%.0fms concurrency=%d; "
            "other-engine pacing=%.0fms. Re-baseline mode (if state empty): "
            "code_scanning pacing=%.0fms concurrency=%d, retry budget=%d.",
            source_name,
            self._engine_min_interval["code_scanning"] * 1000.0,
            self._code_scanning_concurrency,
            self._engine_min_interval["dependabot"] * 1000.0,
            float(self._rebaseline_cs_interval_ms),
            int(self._rebaseline_cs_concurrency),
            int(self._rebaseline_max_attempts),
        )

        # aiohttp session — created on first use or via async context manager
        self._session: Optional[aiohttp.ClientSession] = None

    # ── Context manager ────────────────────────────────────────────────────

    async def __aenter__(self):
        await self._ensure_session()
        return self

    async def __aexit__(self, *_):
        await self.close()

    async def close(self):
        if self._session and not self._session.closed:
            await self._session.close()
            self._session = None

    @staticmethod
    def _opt_setting(settings, source_name: str, key: str, default):
        """
        Read an OPTIONAL source config key, returning `default` if the key is
        absent OR present-but-empty.

        Necessary because ApplicationSettings.GetSource() RAISES
        ApplicationConfigurationException for a key that does not exist in the
        config file (it does not return None). A bare GetSource() on a new
        optional key therefore crashes any instance whose config predates that
        key — which is not acceptable for backward-compatible optional tuning
        knobs. This wrapper makes "absent" behave as "use the default".

        We catch broadly (any exception from the lookup) because the specific
        exception type lives in Core.ApplicationExceptions and we don't want a
        hard import dependency just to read an optional setting; the only
        outcomes here are "got a value" or "fall back to default", and a
        malformed-but-present value is handled by the caller's int()/float()
        coercion.
        """
        try:
            val = settings.GetSource(source_name, key)
        except Exception:
            return default
        if val is None or val == "":
            return default
        return val

    async def _ensure_session(self):
        if self._session is None or self._session.closed:
            self._session = aiohttp.ClientSession(timeout=REQUEST_TIMEOUT)

    def enable_rebaseline_mode(self):
        """
        Switch the client into re-baseline (cold full-pull) rate-limit posture:
        tighter code-scanning concurrency + wider pacing (avoidance-first) and a
        larger secondary-limit retry budget (deep-retry backstop), so a full
        first-sync of every scope tries to complete code-scanning in one pass.

        Called by the adapter ONCE at startup when it detects an empty state
        file (the is_rebaseline snapshot). Idempotent and safe to call before
        any requests are issued. Has no effect on normal incremental runs.
        """
        self._engine_min_interval["code_scanning"] = max(
            0.0, float(self._rebaseline_cs_interval_ms) / 1000.0
        )
        self._code_scanning_concurrency = max(1, int(self._rebaseline_cs_concurrency))
        # Re-create the gate at the tighter size. Safe here because no requests
        # are in flight yet (called before dispatch).
        self._code_scanning_gate = asyncio.Semaphore(self._code_scanning_concurrency)
        self._secondary_max_attempts = int(self._rebaseline_max_attempts)
        logger.warning(
            "[%s] RE-BASELINE MODE engaged (empty state file detected): "
            "code_scanning pacing=%.0fms, concurrency=%d, secondary-retry budget=%d. "
            "Code-scanning will pull slower but aims to complete in a single pass.",
            self._source_name,
            self._engine_min_interval["code_scanning"] * 1000.0,
            self._code_scanning_concurrency,
            self._secondary_max_attempts,
        )

    # ── Auth detection ─────────────────────────────────────────────────────

    @staticmethod
    def _detect_auth_mode(api_key: str) -> str:
        if api_key.startswith(("ghp_", "gho_", "ghs_", "github_pat_")):
            return "pat"
        if "-----BEGIN" in api_key:
            return "app"
        raise ValueError(
            "ApiKey format not recognised. Expected a PAT starting with ghp_/gho_/ghs_/github_pat_, "
            "or a PEM-encoded GitHub App private key containing '-----BEGIN'."
        )

    @staticmethod
    def _load_pem_key(pem_string: str):
        """Load RSA private key from PEM string (handles both PKCS#1 and PKCS#8)."""
        try:
            return serialization.load_pem_private_key(
                pem_string.encode("utf-8"),
                password=None,
                backend=default_backend(),
            )
        except Exception as exc:
            raise GHASAuthError(f"Failed to load GitHub App private key: {exc}") from exc

    # ── Token management ───────────────────────────────────────────────────

    async def _get_token(self) -> str:
        """Return a valid bearer token (PAT or refreshed App installation token)."""
        if self._auth_mode == "pat":
            return self._pat

        async with self._token_lock:
            # Check again inside lock (another coroutine may have refreshed)
            if self._token_expiry and datetime.now(timezone.utc) < self._token_expiry - timedelta(minutes=5):
                return self._installation_token
            await self._refresh_app_token()
            return self._installation_token

    async def _refresh_app_token(self):
        """Generate a JWT, discover the org installation, and obtain an installation token."""
        logger.info("Refreshing GitHub App installation token for org '%s'.", self._org)

        jwt_token = self._generate_app_jwt()
        installation_id = await self._get_installation_id(jwt_token)
        token, expiry = await self._get_installation_token(jwt_token, installation_id)

        self._installation_token = token
        self._token_expiry = expiry
        logger.debug("GitHub App token refreshed. Expires: %s", expiry.isoformat())

    def _generate_app_jwt(self) -> str:
        now = int(time.time())
        payload = {
            "iat": now - 60,   # 60-second clock skew buffer
            "exp": now + 540,  # 9 minutes (max 10)
            "iss": self._app_id,
        }
        private_key_pem = self._app_private_key.private_bytes(
            encoding=serialization.Encoding.PEM,
            format=serialization.PrivateFormat.TraditionalOpenSSL,
            encryption_algorithm=serialization.NoEncryption(),
        )
        return jwt.encode(payload, private_key_pem, algorithm="RS256")

    async def _get_installation_id(self, jwt_token: str) -> int:
        url = f"{self._base_url}/app/installations"
        headers = {"Authorization": f"Bearer {jwt_token}", "Accept": "application/vnd.github+json"}
        async with self._session.get(url, headers=headers) as resp:
            await self._raise_for_status(resp, url)
            installations = await resp.json()
        for inst in installations:
            if inst.get("account", {}).get("login", "").lower() == self._org.lower():
                return inst["id"]
        raise GHASAuthError(
            f"GitHub App is not installed on organisation '{self._org}'. "
            f"Found installations: {[i.get('account', {}).get('login') for i in installations]}"
        )

    async def _get_installation_token(self, jwt_token: str, installation_id: int):
        url = f"{self._base_url}/app/installations/{installation_id}/access_tokens"
        headers = {"Authorization": f"Bearer {jwt_token}", "Accept": "application/vnd.github+json"}
        async with self._session.post(url, headers=headers) as resp:
            await self._raise_for_status(resp, url)
            data = await resp.json()
        token = data["token"]
        expiry = datetime.fromisoformat(data["expires_at"].replace("Z", "+00:00"))
        return token, expiry

    # ── HTTP helpers ───────────────────────────────────────────────────────

    async def _headers(self) -> dict:
        token = await self._get_token()
        return {
            "Authorization": f"Bearer {token}",
            "Accept": "application/vnd.github+json",
            "X-GitHub-Api-Version": "2022-11-28",
        }

    async def _headers_sarif(self) -> dict:
        h = await self._headers()
        h["Accept"] = "application/sarif+json"
        return h

    @staticmethod
    async def _raise_for_status(resp: aiohttp.ClientResponse, url: str):
        if resp.status < 400:
            return
        body = ""
        try:
            body = await resp.text()
        except Exception:
            pass
        raise aiohttp.ClientResponseError(
            resp.request_info, resp.history,
            status=resp.status,
            message=f"HTTP {resp.status} for {url}: {body[:200]}",
        )

    @staticmethod
    def _parse_next_link(link_header: Optional[str]) -> Optional[str]:
        """Parse GitHub Link header and return the 'next' URL if present."""
        if not link_header:
            return None
        for part in link_header.split(","):
            parts = part.strip().split(";")
            if len(parts) == 2 and 'rel="next"' in parts[1]:
                return parts[0].strip().strip("<>")
        return None

    async def _pace(self, engine: Optional[str]):
        """
        Enforce the configured minimum interval between requests for a given
        engine (token-bucket style, monotonic clock). No-op when the engine has
        no pacing configured (interval 0) or engine is None (non-engine calls
        like repo inventory and App-token requests are not paced here).

        This is the primary secondary-rate-limit AVOIDANCE mechanism: by
        spacing code-scanning requests we stay well under GitHub's ~900
        points/min single-endpoint ceiling and, more importantly, under the
        opaque CPU-time secondary limit that code-scanning trips first.
        """
        if not engine:
            return
        interval = self._engine_min_interval.get(engine, 0.0)
        if interval <= 0.0:
            return
        lock = self._pace_locks[engine]
        async with lock:
            now = time.monotonic()
            wait = (self._pace_last[engine] + interval) - now
            if wait > 0:
                await asyncio.sleep(wait)
                now = time.monotonic()
            self._pace_last[engine] = now

    @staticmethod
    def _is_secondary_rate_limit(status: int, body: str, resp_headers=None) -> bool:
        """
        Identify a GitHub SECONDARY rate-limit (throttle) response.

        GitHub signals a throttle as 403 or 429. The signal can appear in EITHER
        the body OR the headers, and the two do not always co-occur:

          - BODY signal: a message mentioning a secondary rate limit / abuse
            detection (the classic, documented case).
          - HEADER signal: a `Retry-After` header, or `x-ratelimit-remaining: 0`.
            GitHub does NOT send Retry-After on permission 403s — its presence
            on a 403 is therefore a throttle signal, not a permission signal.
            Likewise, remaining==0 means the budget is exhausted (throttling by
            definition), regardless of body text.

        Matching on headers as well as body is the fix for the under-reporting
        bug: code-scanning throttle 403s frequently arrive with throttle headers
        but a body that lacks the magic phrase (or an empty/HTML body). Body-only
        matching mis-filed those as GHASEngineInaccessibleError, masking
        systematic data loss as a permissions problem.

        A genuine permission 403 ("Resource not accessible by personal access
        token") carries NONE of these signals and so still returns False,
        preserving correct GHASEngineInaccessibleError classification.
        """
        if status not in (403, 429):
            return False

        b = (body or "").lower()
        if (
            "secondary rate limit" in b
            or "exceeded a secondary rate limit" in b
            or "you have triggered an abuse detection" in b   # legacy phrasing
        ):
            return True

        # Header-based throttle signals (body did not match or was empty).
        if resp_headers is not None:
            if resp_headers.get("Retry-After") is not None:
                return True
            remaining = resp_headers.get("x-ratelimit-remaining")
            if remaining is not None and str(remaining).isdigit() and int(remaining) == 0:
                return True

        return False

    @staticmethod
    def _compute_rate_limit_wait(resp_headers, attempt: int) -> float:
        """
        Decide how long to wait before retrying a secondary-rate-limited
        request, following GitHub's documented guidance and tolerating the
        known gap where Retry-After is sometimes absent on secondary limits:

          1. Retry-After header present  → wait that many seconds.
          2. else x-ratelimit-remaining == 0 → wait until x-ratelimit-reset.
          3. else (no usable headers)    → fall back to >=60s, increasing
             exponentially per SECONDARY_FALLBACK_BACKOFF.

        A small fixed cushion is added so we resume just after the window.
        """
        cushion = 2.0

        retry_after = resp_headers.get("Retry-After")
        if retry_after is not None:
            try:
                return max(0.0, float(int(retry_after))) + cushion
            except (ValueError, TypeError):
                pass

        remaining = resp_headers.get("x-ratelimit-remaining")
        reset = resp_headers.get("x-ratelimit-reset")
        if remaining is not None and reset is not None:
            try:
                if int(remaining) == 0:
                    wait = int(reset) - int(time.time())
                    return max(0.0, float(wait)) + cushion
            except (ValueError, TypeError):
                pass

        # No usable headers — fall back to the documented "wait >=60s then
        # exponential" schedule. Clamp the index to the last entry so further
        # attempts keep waiting the maximum rather than indexing out of range.
        idx = min(attempt, len(SECONDARY_FALLBACK_BACKOFF) - 1)
        return float(SECONDARY_FALLBACK_BACKOFF[idx]) + cushion

    async def _get_response_with_retry(
        self,
        url: str,
        params: dict = None,
        headers: dict = None,
        engine: Optional[str] = None,
        scope: Optional[str] = None,
    ) -> aiohttp.ClientResponse:
        """
        GET with engine-aware pacing, secondary-rate-limit backoff, and primary
        rate-limit handling. Returns the raw response so callers can read the
        Link header for pagination.

        engine: which GHAS engine this request is for (drives pacing and, for
                code_scanning, the dedicated concurrency gate). None for
                non-engine calls (repo inventory, App token, analyses, SARIF).
        scope:  "org/repo/engine" for diagnostics, carried into
                GHASRateLimitError if the secondary limit can't be beaten.

        Distinguishes two 403 meanings that previously collapsed together:
          - secondary rate limit  → retry with header-driven backoff; after
            SECONDARY_MAX_ATTEMPTS raise GHASRateLimitError (NOT a permission
            error, NOT a silent skip).
          - anything else (incl. permission 403) → raised via _raise_for_status
            for the caller to classify (the alert generators turn a permission
            403/404 into GHASEngineInaccessibleError).
        """
        await self._ensure_session()
        h = headers or await self._headers()
        secondary_attempts = 0

        # Code-scanning requests pass through a dedicated low-concurrency gate
        # layered under the adapter's global semaphore, so the expensive
        # endpoint never has more than CodeScanningConcurrencyLimit requests in
        # flight regardless of how many scopes the adapter dispatched at once.
        gate = self._code_scanning_gate if engine == "code_scanning" else None
        if gate is not None:
            await gate.acquire()
        try:
            while True:
                # Pace BEFORE each attempt (including retries) so backoff and
                # pacing compose correctly.
                await self._pace(engine)

                resp = await self._session.get(url, headers=h, params=params)

                # Primary rate limit: 429 with Retry-After, OR remaining==0.
                if resp.status == 429 and not self._peek_secondary(resp):
                    retry_after = int(resp.headers.get("Retry-After", 60))
                    await resp.release()
                    await asyncio.sleep(retry_after + 2)
                    h = headers or await self._headers()
                    continue

                remaining = resp.headers.get("x-ratelimit-remaining")
                if (
                    remaining is not None
                    and remaining.isdigit()
                    and int(remaining) == 0
                    and resp.status >= 400
                ):
                    # Primary budget exhausted. Wait until reset.
                    reset_ts = int(resp.headers.get("x-ratelimit-reset", time.time() + 60))
                    await resp.release()
                    wait = max(reset_ts - int(time.time()), 0) + 2
                    logger.warning(
                        "Primary rate limit hit (remaining=0). Waiting %ds before retry. scope=%s",
                        wait, scope or url,
                    )
                    await asyncio.sleep(wait)
                    h = headers or await self._headers()
                    continue

                # Secondary rate limit: 403/429 identified by body OR headers.
                if resp.status in (403, 429):
                    body = await resp.text()
                    if self._is_secondary_rate_limit(resp.status, body, resp.headers):
                        if secondary_attempts >= self._secondary_max_attempts:
                            await resp.release()
                            raise GHASRateLimitError(
                                f"Secondary rate limit not cleared after "
                                f"{self._secondary_max_attempts} attempts for {scope or url}.",
                                scope=scope,
                            )
                        wait = self._compute_rate_limit_wait(resp.headers, secondary_attempts)
                        await resp.release()
                        secondary_attempts += 1
                        logger.warning(
                            "Secondary rate limit on %s (attempt %d/%d) — waiting %.0fs. "
                            "If concentrated on code_scanning, lower CodeScanningConcurrencyLimit "
                            "or raise CodeScanningMinIntervalMs.",
                            scope or url, secondary_attempts, self._secondary_max_attempts, wait,
                        )
                        await asyncio.sleep(wait)
                        h = headers or await self._headers()
                        continue
                    # Not a throttle 403/429 → treated as a genuine error
                    # (e.g. permission 403) and raised so the caller can
                    # classify 403/404 → GHASEngineInaccessibleError.
                    #
                    # Log the full 403 fingerprint BEFORE classifying so an
                    # inaccessible verdict can never again be silent. If these
                    # lines show throttle headers (Retry-After / remaining==0),
                    # the request was throttled and _is_secondary_rate_limit
                    # should have caught it — investigate before trusting the
                    # INACCESSIBLE count.
                    logger.warning(
                        "Classifying HTTP %d as INACCESSIBLE for %s — "
                        "no throttle signal found. Retry-After=%s "
                        "x-ratelimit-remaining=%s x-ratelimit-reset=%s "
                        "body[:120]=%r",
                        resp.status, scope or url,
                        resp.headers.get("Retry-After"),
                        resp.headers.get("x-ratelimit-remaining"),
                        resp.headers.get("x-ratelimit-reset"),
                        (body or "")[:120],
                    )
                    raise aiohttp.ClientResponseError(
                        resp.request_info, resp.history,
                        status=resp.status, message=body[:200],
                    )

                await self._raise_for_status(resp, url)
                return resp
        finally:
            if gate is not None:
                gate.release()

    @staticmethod
    def _peek_secondary(resp: aiohttp.ClientResponse) -> bool:
        """
        Best-effort check (header-only, no body read) for whether a 429 is a
        secondary-limit response, used to route 429s. GitHub does not provide a
        definitive header, so this conservatively returns False and lets the
        body-based _is_secondary_rate_limit make the real determination in the
        403/429 branch. Kept as a seam for future header-based signals.
        """
        return False

    # ── Repository inventory ───────────────────────────────────────────────

    async def get_repos_async(self) -> list:
        """Return all repositories in the configured org accessible to the PAT.

        Note: the returned set depends on the PAT's permissions. A
        misconfigured PAT (missing org/repo scopes, missing SSO authorization,
        fine-grained repo-selection limit) may return fewer repos than the
        org actually contains. The full repo name list is emitted at DEBUG
        level so operators can verify the visible set matches expectations
        without having to grep GitHub manually.
        """
        await self._ensure_session()
        repos = []
        url = f"{self._base_url}/orgs/{self._org}/repos"
        params = {"per_page": PAGE_SIZE, "type": "all"}

        while url:
            resp = await self._get_response_with_retry(url, params=params)
            async with resp:
                data = await resp.json()
                link = resp.headers.get("Link")
            repos.extend(data)
            url = self._parse_next_link(link)
            params = None  # params are encoded in next URL

        logger.info("Discovered %d repositories in org '%s'.", len(repos), self._org)
        if logger.isEnabledFor(logging.DEBUG):
            names = sorted(r.get("full_name", "?") for r in repos)
            logger.debug("Visible repositories: %s", ", ".join(names))
        return repos

    # NOTE: A previous version of this client included get_repo_enablement_async()
    # that read GitHub's repo-level security_and_analysis block to pre-filter
    # which engines were enabled per repo. That field is only returned to tokens
    # with administrative permissions, so it gave false negatives for tokens
    # with the documented read-only alert permissions. The pre-check has been
    # removed; engine availability is now determined authoritatively by 404
    # responses from the per-engine alert endpoints, handled below.

    # ── Change detection ───────────────────────────────────────────────────

    async def get_latest_alert_timestamp_async(self, full_name: str, engine: str) -> Optional[str]:
        """
        Return the most recent alert.updated_at across ALL alert states for
        this engine, or None if the engine returns no alerts. Used for
        Phase 1 change detection.

        Phase 1 must query across all states (open + closed). A state transition
        out of open (dismissal, fix, resolution) bumps the alert's updated_at
        and must trigger Phase 2 so the open set in SaltMiner can be refreshed
        via ReplaceIssues=True. If Phase 1 narrowed to open-only, dismissals
        would be invisible and dismissed alerts would linger in SaltMiner.

        For most engines this costs one API call. For secret_scanning, GitHub
        rejects comma-separated state parameters, so we issue one call per
        state ("open", "resolved") and take the max of the per-state results.

        Error handling:
          - 403 or 404 → raises GHASEngineInaccessibleError. The adapter
            catches this and skips the scope without state changes (see
            the exception's docstring for rationale).
          - Other ClientResponseError (5xx, transient network issues, real
            auth failures on other endpoints) → re-raised. The adapter's
            outer except logs and the gather summary counts it as a failure.
        """
        endpoint = self._engine_endpoint(full_name, engine)
        latest: Optional[str] = None

        for state in self._engine_state_queries(engine, purpose="all"):
            params = {"per_page": 1, "sort": "updated", "direction": "desc", "state": state}
            try:
                resp = await self._get_response_with_retry(
                    endpoint, params=params,
                    engine=engine, scope=f"{full_name}/{engine}",
                )
                async with resp:
                    data = await resp.json()
            except aiohttp.ClientResponseError as exc:
                if exc.status in (403, 404):
                    # Permission/enablement 403 or 404 only. Secondary
                    # rate-limit 403s never reach here — they are retried inside
                    # _get_response_with_retry and, if unbeatable, raised as
                    # GHASRateLimitError (a different exception type). The
                    # status distinction for diagnosis:
                    #   403 → PAT permission scope likely insufficient, OR
                    #         SSO authorization missing, OR fine-grained PAT
                    #         admin-approval missing.
                    #   404 → Engine has never been enabled on this repo, OR
                    #         the repo is inaccessible to this PAT entirely.
                    raise GHASEngineInaccessibleError(
                        f"HTTP {exc.status} on Phase 1 fetch for {full_name}/{engine} "
                        f"(state={state})"
                    ) from exc
                # Real error (5xx, etc.) — surface to the adapter so it counts
                # as a failure rather than a silent skip.
                logger.error(
                    "Phase 1 fetch failed for %s/%s (state=%s): %s — aborting scope.",
                    full_name, engine, state, exc,
                )
                raise

            if data:
                ts = data[0].get("updated_at")
                if ts and (latest is None or ts > latest):
                    latest = ts

        return latest

    # ── Alert generators ───────────────────────────────────────────────────

    async def get_alerts_async(self, full_name: str, engine: str) -> AsyncGenerator[dict, None]:
        """
        Async generator yielding currently-open alerts for a given repo and
        engine. Handles pagination transparently. Yields raw GitHub API alert
        objects.

        FIX-001: Phase 2 fetch is narrowed to state=open for every engine. The
        customer-visible Open count in SaltMiner is drawn exclusively from
        currently-open GHAS alerts; closed-state alerts (dismissed/fixed/
        resolved/auto_dismissed) are never queued. Removal is handled via
        ReplaceIssues=True on the Scan document — alerts that have transitioned
        out of open are simply absent from the replacement set, and SaltMiner
        removes them by absence.

        For code_scanning and dependabot this is one paginated query. For
        secret_scanning, the GitHub API still requires a state parameter on
        the request, but for "open" we issue exactly one paginated query.

        Error handling:
          - 403 or 404 → raises GHASEngineInaccessibleError. The adapter
            catches this and skips the scope without state changes (see
            the exception's docstring for rationale).
          - Other ClientResponseError → re-raised. The adapter's outer
            except logs and the gather summary counts it as a failure.
        """
        endpoint = self._engine_endpoint(full_name, engine)

        for state in self._engine_state_queries(engine, purpose="open"):
            url = endpoint
            params = {
                "per_page": PAGE_SIZE,
                "sort": "updated",
                "direction": "desc",
                "state": state,
            }

            while url:
                try:
                    resp = await self._get_response_with_retry(
                        url, params=params,
                        engine=engine, scope=f"{full_name}/{engine}",
                    )
                    async with resp:
                        alerts = await resp.json()
                        link = resp.headers.get("Link")
                except aiohttp.ClientResponseError as exc:
                    if exc.status in (403, 404):
                        # Permission/enablement 403 or 404 (NOT a secondary
                        # rate-limit 403 — those are handled inside
                        # _get_response_with_retry and raised as
                        # GHASRateLimitError, which is a different exception
                        # type and bypasses this block entirely). See the
                        # matching block in get_latest_alert_timestamp_async.
                        raise GHASEngineInaccessibleError(
                            f"HTTP {exc.status} on Phase 2 fetch for {full_name}/{engine} "
                            f"(state={state})"
                        ) from exc
                    logger.error(
                        "Phase 2 fetch failed for %s/%s (state=%s): %s — aborting scope.",
                        full_name, engine, state, exc,
                    )
                    raise

                for alert in alerts:
                    yield alert

                url = self._parse_next_link(link)
                params = None  # params are encoded in the next URL

    @staticmethod
    def _engine_endpoint(full_name: str, engine: str) -> str:
        endpoints = {
            "code_scanning": f"https://api.github.com/repos/{full_name}/code-scanning/alerts",
            "secret_scanning": f"https://api.github.com/repos/{full_name}/secret-scanning/alerts",
            "dependabot": f"https://api.github.com/repos/{full_name}/dependabot/alerts",
        }
        if engine not in endpoints:
            raise ValueError(f"Unknown engine '{engine}'. Expected one of: {list(endpoints)}")
        return endpoints[engine]

    @staticmethod
    def _engine_state_queries(engine: str, purpose: str = "all") -> list:
        """
        Return the list of `state` query values to issue for a given engine
        and purpose.

        purpose:
          "open" — Phase 2 narrow fetch (FIX-001). Returns ["open"] for every
                   engine. The customer-visible Open count in SaltMiner must
                   match GHAS's Open tab, so closed-state alerts are never
                   fetched into the issue replacement set.
          "all"  — Phase 1 change detection. Returns the full per-engine
                   state set so that state transitions out of open
                   (dismissal, fix, resolution) bump the alert's updated_at
                   and trigger Phase 2.

        GitHub's code-scanning and dependabot endpoints accept a comma-
        separated list of states in a single request, so the "all" path
        returns a one-element list wrapping the CSV. Secret scanning rejects
        comma-separated states (HTTP 400 — "State needs to be either 'open'
        or 'resolved'") and must be queried once per state; callers iterate
        and merge results. For "open" purpose the secret_scanning path
        returns ["open"] (still a single call).
        """
        if purpose == "open":
            return ["open"]
        if purpose != "all":
            raise ValueError(
                f"Unknown purpose '{purpose}'. Expected 'open' or 'all'."
            )
        if engine == "secret_scanning":
            return ["open", "resolved"]
        csv = {
            "code_scanning": "open,dismissed,fixed",
            "dependabot": "open,dismissed,fixed,auto_dismissed",
        }.get(engine, "open")
        return [csv]

    # ── SARIF ──────────────────────────────────────────────────────────────

    async def get_analyses_async(self, full_name: str) -> AsyncGenerator[dict, None]:
        """Yield Code Scanning analyses metadata records for a repository.

        403 and 404 are both treated as "no analyses available" (engine not
        accessible) and end the generator cleanly without logging an error.
        Callers that need scan-date evidence already handle the no-analyses
        outcome gracefully (heartbeat skips, replacement uses now() fallback).
        """
        url = f"{self._base_url}/repos/{full_name}/code-scanning/analyses"
        params = {"per_page": PAGE_SIZE}

        while url:
            try:
                resp = await self._get_response_with_retry(url, params=params)
                async with resp:
                    analyses = await resp.json()
                    link = resp.headers.get("Link")
            except aiohttp.ClientResponseError as exc:
                if exc.status in (403, 404):
                    logger.debug(
                        "Analyses endpoint inaccessible for %s (HTTP %d) — no analyses.",
                        full_name, exc.status,
                    )
                    return
                logger.error("Failed fetching analyses for %s: %s", full_name, exc)
                return

            for analysis in analyses:
                yield analysis

            url = self._parse_next_link(link)
            params = None

    async def get_sarif_async(self, full_name: str, analysis_id: int) -> Optional[dict]:
        """
        Fetch the raw SARIF document for a specific analysis.
        Returns None if the document is no longer retained by GitHub or the
        endpoint is inaccessible (403/404).
        """
        url = f"{self._base_url}/repos/{full_name}/code-scanning/analyses/{analysis_id}"
        headers = await self._headers_sarif()
        try:
            resp = await self._get_response_with_retry(url, headers=headers)
            async with resp:
                text = await resp.text()
            return json.loads(text)
        except aiohttp.ClientResponseError as exc:
            if exc.status in (403, 404):
                logger.info(
                    "SARIF document for analysis %d on %s not available (HTTP %d).",
                    analysis_id, full_name, exc.status,
                )
                return None
            logger.error("Failed fetching SARIF %d for %s: %s", analysis_id, full_name, exc)
            return None
        except json.JSONDecodeError as exc:
            logger.error("SARIF response for analysis %d is not valid JSON: %s", analysis_id, exc)
            return None
