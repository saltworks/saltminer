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

# Rate limit backoff constants (seconds)
SECONDARY_BACKOFF = [60, 120, 240]
REQUEST_TIMEOUT = aiohttp.ClientTimeout(total=60, connect=10)


class GHASAuthError(Exception):
    """Raised when authentication fails and cannot be recovered."""


class GHASClient:
    """
    Async GitHub REST API client for GHAS data collection.

    Usage:
        client = GHASClient(settings)
        async with client:
            repos = await client.get_repos_async()
            async for alert in client.get_alerts_async("org/repo", "code_scanning"):
                process(alert)
    """

    def __init__(self, settings):
        self._base_url = settings.GetSource("GHAS", "BaseUrl").rstrip("/")
        self._org = settings.GetSource("GHAS", "Org")
        api_key = settings.GetSource("GHAS", "ApiKey") or ""
        app_id = settings.GetSource("GHAS", "AppId") or 0

        self._auth_mode = self._detect_auth_mode(api_key)
        self._pat = api_key if self._auth_mode == "pat" else None
        self._app_id = int(app_id) if self._auth_mode == "app" else None
        self._app_private_key = self._load_pem_key(api_key) if self._auth_mode == "app" else None

        # GitHub App installation token cache
        self._installation_token: Optional[str] = None
        self._token_expiry: Optional[datetime] = None
        self._token_lock = asyncio.Lock()

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

    async def _ensure_session(self):
        if self._session is None or self._session.closed:
            self._session = aiohttp.ClientSession(timeout=REQUEST_TIMEOUT)

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

    async def _raise_for_status(self, resp: aiohttp.ClientResponse, url: str):
        if resp.status < 400:
            return
        body = ""
        try:
            body = await resp.text()
        except Exception as exc:
            logger.debug("Could not read response body for %s (status %d): %s", url, resp.status, exc)
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

    async def _get_with_retry(self, url: str, params: dict = None, headers: dict = None) -> dict:
        """GET with rate-limit backoff. Returns parsed JSON."""
        await self._ensure_session()
        h = headers or await self._headers()
        backoff_idx = 0

        while True:
            try:
                async with self._session.get(url, headers=h, params=params) as resp:

                    # Primary rate limit
                    if resp.status == 429:
                        retry_after = int(resp.headers.get("Retry-After", 60))
                        jitter = 5
                        wait = retry_after + jitter
                        logger.warning("Primary rate limit hit. Sleeping %ss.", wait)
                        await asyncio.sleep(wait)
                        h = headers or await self._headers()
                        continue

                    # Proactive primary rate limit
                    remaining = resp.headers.get("x-ratelimit-remaining")
                    if remaining is not None and int(remaining) == 0:
                        reset_ts = int(resp.headers.get("x-ratelimit-reset", time.time() + 60))
                        wait = max(reset_ts - int(time.time()), 0) + 5
                        logger.warning("Rate limit exhausted. Sleeping %ss until reset.", wait)
                        await asyncio.sleep(wait)
                        h = headers or await self._headers()
                        continue

                    # Secondary rate limit (403 with specific body)
                    if resp.status == 403:
                        body = await resp.text()
                        if "secondary rate limit" in body.lower() or "rate limit exceeded" in body.lower():
                            if backoff_idx >= len(SECONDARY_BACKOFF):
                                raise aiohttp.ClientResponseError(
                                    resp.request_info, resp.history,
                                    status=403, message="Secondary rate limit — max retries exceeded."
                                )
                            wait = SECONDARY_BACKOFF[backoff_idx] + 5
                            logger.warning("Secondary rate limit. Backoff %ss (attempt %d).", wait, backoff_idx + 1)
                            await asyncio.sleep(wait)
                            backoff_idx += 1
                            h = headers or await self._headers()
                            continue
                        # Non-rate-limit 403 — surface as error
                        raise aiohttp.ClientResponseError(
                            resp.request_info, resp.history,
                            status=403, message=f"Forbidden: {body[:200]}"
                        )

                    await self._raise_for_status(resp, url)
                    return await resp.json()

            except aiohttp.ServerConnectionError as exc:
                logger.warning("Connection error for %s: %s — retrying once.", url, exc)
                await asyncio.sleep(10)
                h = headers or await self._headers()
                async with self._session.get(url, headers=h, params=params) as resp2:
                    await self._raise_for_status(resp2, url)
                    return await resp2.json()

    async def _get_response_with_retry(self, url: str, params: dict = None, headers: dict = None) -> aiohttp.ClientResponse:
        """
        Like _get_with_retry but returns the raw response object for Link header access.
        CALLER MUST close the response using 'async with resp:' to avoid connection leaks.
        """
        await self._ensure_session()
        h = headers or await self._headers()
        backoff_idx = 0

        while True:
            resp = await self._session.get(url, headers=h, params=params)

            if resp.status == 429:
                retry_after = int(resp.headers.get("Retry-After", 60))
                await asyncio.sleep(retry_after + 5)
                h = headers or await self._headers()
                continue

            remaining = resp.headers.get("x-ratelimit-remaining")
            if remaining is not None and int(remaining) == 0:
                reset_ts = int(resp.headers.get("x-ratelimit-reset", time.time() + 60))
                wait = max(reset_ts - int(time.time()), 0) + 5
                await asyncio.sleep(wait)
                h = headers or await self._headers()
                continue

            if resp.status == 403:
                body = await resp.text()
                if "secondary rate limit" in body.lower() or "rate limit exceeded" in body.lower():
                    if backoff_idx >= len(SECONDARY_BACKOFF):
                        raise aiohttp.ClientResponseError(
                            resp.request_info, resp.history,
                            status=403, message="Secondary rate limit — max retries exceeded."
                        )
                    await asyncio.sleep(SECONDARY_BACKOFF[backoff_idx] + 5)
                    backoff_idx += 1
                    h = headers or await self._headers()
                    continue
                raise aiohttp.ClientResponseError(resp.request_info, resp.history, status=403, message=body[:200])

            await self._raise_for_status(resp, url)
            return resp

    # ── Repository inventory ───────────────────────────────────────────────

    async def get_repos_async(self) -> list:
        """Return all repositories in the configured org."""
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
        return repos

    async def get_repo_enablement_async(self, full_name: str) -> dict:
        """
        Return a dict of {engine: bool} indicating which GHAS engines are enabled.
        Gracefully returns all-False if the repo is inaccessible.
        """
        url = f"{self._base_url}/repos/{full_name}"
        try:
            data = await self._get_with_retry(url)
        except aiohttp.ClientResponseError as exc:
            if exc.status in (404, 403):
                logger.warning("Cannot access repo %s (HTTP %d) — skipping all engines.", full_name, exc.status)
                return {"code_scanning": False, "secret_scanning": False, "dependabot": False}
            raise

        sa = data.get("security_and_analysis") or {}

        def _enabled(block_name: str) -> bool:
            block = sa.get(block_name) or {}
            return block.get("status") == "enabled"

        return {
            "code_scanning": _enabled("advanced_security"),
            "secret_scanning": _enabled("secret_scanning"),
            "dependabot": _enabled("dependabot_security_updates"),
        }

    # ── Change detection ───────────────────────────────────────────────────

    async def get_latest_alert_timestamp_async(self, full_name: str, engine: str) -> Optional[str]:
        """
        Fetch a single alert sorted by updated_at descending.
        Returns the updated_at string of the most recent alert, or None if no alerts exist.
        Used for Phase 1 change detection — costs exactly one API call.
        """
        endpoint = self._engine_endpoint(full_name, engine)
        params = {"per_page": 1, "sort": "updated", "direction": "desc", "state": self._engine_states(engine)}
        try:
            resp = await self._get_response_with_retry(endpoint, params=params)
            async with resp:
                data = await resp.json()
        except aiohttp.ClientResponseError as exc:
            if exc.status == 404:
                return None  # Engine not enabled
            logger.warning("Error checking latest alert for %s/%s: %s", full_name, engine, exc)
            return None

        if not data:
            return None
        return data[0].get("updated_at")

    # ── Alert generators ───────────────────────────────────────────────────

    async def get_alerts_async(self, full_name: str, engine: str) -> AsyncGenerator[dict, None]:
        """
        Async generator yielding all alerts for a given repo and engine.
        Handles pagination transparently. Yields raw GitHub API alert objects.
        """
        url = self._engine_endpoint(full_name, engine)
        params = {
            "per_page": PAGE_SIZE,
            "sort": "updated",
            "direction": "desc",
            "state": self._engine_states(engine),
        }

        while url:
            try:
                resp = await self._get_response_with_retry(url, params=params)
                async with resp:
                    alerts = await resp.json()
                    link = resp.headers.get("Link")
            except aiohttp.ClientResponseError as exc:
                if exc.status == 404:
                    logger.info("Engine %s not enabled on %s — no alerts.", engine, full_name)
                    return
                logger.error("Failed fetching %s alerts for %s: %s", engine, full_name, exc)
                return

            for alert in alerts:
                yield alert

            url = self._parse_next_link(link)
            params = None

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
    def _engine_states(engine: str) -> str:
        """Comma-separated list of alert states to fetch for each engine."""
        states = {
            "code_scanning": "open,dismissed,fixed",
            "secret_scanning": "open,resolved",
            "dependabot": "open,dismissed,fixed,auto_dismissed",
        }
        return states.get(engine, "open")

    # ── SARIF ──────────────────────────────────────────────────────────────

    async def get_analyses_async(self, full_name: str) -> AsyncGenerator[dict, None]:
        """Yield Code Scanning analyses metadata records for a repository."""
        url = f"{self._base_url}/repos/{full_name}/code-scanning/analyses"
        params = {"per_page": PAGE_SIZE}

        while url:
            try:
                resp = await self._get_response_with_retry(url, params=params)
                async with resp:
                    analyses = await resp.json()
                    link = resp.headers.get("Link")
            except aiohttp.ClientResponseError as exc:
                if exc.status == 404:
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
        Returns None if the document is no longer retained by GitHub.
        """
        url = f"{self._base_url}/repos/{full_name}/code-scanning/analyses/{analysis_id}"
        headers = await self._headers_sarif()
        try:
            resp = await self._get_response_with_retry(url, headers=headers)
            async with resp:
                text = await resp.text()
            return json.loads(text)
        except aiohttp.ClientResponseError as exc:
            if exc.status == 404:
                logger.info("SARIF document for analysis %d on %s no longer retained.", analysis_id, full_name)
                return None
            logger.error("Failed fetching SARIF %d for %s: %s", analysis_id, full_name, exc)
            return None
        except json.JSONDecodeError as exc:
            logger.error("SARIF response for analysis %d is not valid JSON: %s", analysis_id, exc)
            return None
