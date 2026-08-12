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

import random
import re
import threading
import time
import logging

from .Heartbeat import BeatingSleep

class RateLimiter:
    """
    Thread-safe rate limiter for API requests.
    Enforces both per-second and per-minute limits.
    
    Usage:
        limiter = RateLimiter(max_per_second=10, max_per_minute=600)
        limiter.acquire()  # Call before each API request
    """
    def __init__(self, max_per_second=10, max_per_minute=600):
        self.max_per_second = max_per_second
        self.max_per_minute = max_per_minute
        self.lock = threading.Lock()
        self._reset_counters()
        
    def _reset_counters(self):
        """Initialize all counters and timers"""
        self.calls_this_second = 0
        self.calls_this_minute = 0
        self.second_start = time.time()
        self.minute_start = time.time()

    def acquire(self):
        """Block until request can be made within rate limits"""
        with self.lock:
            now = time.time()
            self._reset_expired_counters(now)
            self._enforce_second_limit(now)
            self._enforce_minute_limit(now)
            self._increment_counters()

    def _reset_expired_counters(self, now):
        """Reset counters if their time interval has expired"""
        if now - self.second_start >= 1:
            self.calls_this_second = 0
            self.second_start = now
        if now - self.minute_start >= 60:
            self.calls_this_minute = 0
            self.minute_start = now

    def _enforce_second_limit(self, now):
        """Handle per-second rate limiting"""
        if self.calls_this_second >= self.max_per_second:
            sleep_time = 1 - (now - self.second_start)
            if sleep_time > 0:
                logging.debug(f"RateLimiter: Sleeping {sleep_time:.2f}s for per-second limit")
                time.sleep(sleep_time)
            self.calls_this_second = 0
            self.second_start = time.time()

    def _enforce_minute_limit(self, now):
        """Handle per-minute rate limiting"""
        if self.calls_this_minute >= self.max_per_minute:
            sleep_time = 60 - (now - self.minute_start)
            if sleep_time > 0:
                logging.debug(f"RateLimiter: Sleeping {sleep_time:.2f}s for per-minute limit")
                time.sleep(sleep_time)
            self.calls_this_minute = 0
            self.minute_start = time.time()

    def _increment_counters(self):
        """Increment counters after successful acquisition"""
        self.calls_this_second += 1
        self.calls_this_minute += 1


class ApiThrottle:
    """
    Shared, endpoint-aware throttle for APIs that rate limit per endpoint and per credential
    (Fortify on Demand, Wiz, and friends - all use the X-Rate-Limit-* convention).

    Two mechanisms, because either alone is insufficient:

    1. Proactive: a shared RateLimiter gate caps the *aggregate* request rate for a credential.
       Every worker thread's client draws from one budget, which is how the server counts it.
       A per-client limiter would let N workers each stay "under" the limit while together
       exceeding it N-fold.
    2. Reactive: when the server reports throttling (429) or that the endpoint's budget is
       spent (X-Rate-Limit-Remaining: 0), the endpoint is put in a cooldown that *all* threads
       observe until X-Rate-Limit-Reset seconds have passed. Cooldowns are per endpoint because
       the limits are per endpoint - a slow endpoint (FoD's /fpr is 1 per 30s) must not stall
       unrelated calls.

    Cooldown waits are jittered across contending clients so they don't all wake and stampede
    the instant the window resets - which would immediately re-trigger the limit.

    Instances are shared per key (use the credential, not the source name - two source configs
    with the same key share one server-side budget).  Threads within a process coordinate through
    it; separate processes sharing a credential cannot, and rely on the reactive path.

    Usage:
        throttle = ApiThrottle.for_key(client_id, max_per_second=10)
        throttle.register()                       # on client construction
        endpoint = throttle.endpoint_key(url)
        throttle.acquire(endpoint)                # blocks until it is safe to call
        resp = requests.get(url)
        wait = throttle.observe(endpoint, resp.status_code, resp.headers)
        if wait is not None:                      # throttled - sleep and retry the call
            time.sleep(wait)
    """

    THROTTLED_STATUS = 429
    LIMIT_HEADER = 'X-Rate-Limit-Limit'
    REMAINING_HEADER = 'X-Rate-Limit-Remaining'
    RESET_HEADER = 'X-Rate-Limit-Reset'

    _instances = {}
    _instances_lock = threading.Lock()

    @classmethod
    def for_key(cls, key, **kwargs):
        """Returns the throttle shared by every client using this credential/key, creating it if needed."""
        with cls._instances_lock:
            if key not in cls._instances:
                cls._instances[key] = cls(**kwargs)
            return cls._instances[key]

    def __init__(self, max_per_second=10, buffer_secs=2, default_reset_secs=30, max_jitter_secs=5,
                 throttled_status=THROTTLED_STATUS, limit_header=LIMIT_HEADER,
                 remaining_header=REMAINING_HEADER, reset_header=RESET_HEADER, logger=None):
        '''
        max_per_second: aggregate requests/sec allowed across all clients sharing this key
        buffer_secs: added to the server's reset time, so we resume just after the window rolls over
        default_reset_secs: cooldown to use when the server throttles without a usable reset header
        max_jitter_secs: upper bound on the random stagger applied when leaving a cooldown
        throttled_status/[limit|remaining|reset]_header: override for APIs that differ from the X-Rate-Limit-* convention
        '''
        self._gate = RateLimiter(max_per_second=max_per_second, max_per_minute=max_per_second * 60)
        self._buffer_secs = buffer_secs
        self._default_reset_secs = default_reset_secs
        self._max_jitter_secs = max_jitter_secs
        self._throttled_status = throttled_status
        self._limit_header = limit_header
        self._remaining_header = remaining_header
        self._reset_header = reset_header
        self._logger = logger or logging.getLogger(__name__)
        self._lock = threading.Lock()
        self._cooldowns = {}   # endpoint -> epoch time until which no request may be sent
        self._contenders = 0   # live clients sharing this key; scales the wake-up stagger

    def register(self):
        """Count a client as contending for this key's budget."""
        with self._lock:
            self._contenders += 1

    def unregister(self):
        with self._lock:
            self._contenders = max(0, self._contenders - 1)

    @staticmethod
    def endpoint_key(url):
        """
        Normalizes a URL to the endpoint its rate limit is keyed on: no host, no query string,
        and IDs collapsed (/api/v3/releases/1234/scans -> /api/v3/releases/{id}/scans).
        """
        path = re.sub(r'^https?://[^/]+', '', url or '')
        path = path.split('?', 1)[0].split('#', 1)[0]
        return re.sub(r'/\d+(?=/|$)', '/{id}', path) or '/'

    def acquire(self, endpoint, beat=None):
        """
        Blocks until this endpoint is out of cooldown and the shared rate gate allows a request.

        beat: optional zero-arg callable invoked while waiting out a cooldown.  A cooldown can run
        to the server's reset window, which is long enough to look like a dead worker to the agent -
        the beat is per-caller, so this stays shared across clients.
        """
        while True:
            wait = self._cooldown_remaining(endpoint)
            if wait <= 0:
                break
            # Stagger per waiting thread, not per cooldown - a stagger baked into the shared
            # cooldown would still release every thread on the same tick.
            wait += self._stagger()
            self._logger.info("Rate limit cooldown on '%s': waiting %.1fs before next request", endpoint, wait)
            BeatingSleep(wait, beat)
        self._gate.acquire()

    def observe(self, endpoint, status_code, headers):
        """
        Records the outcome of a request.

        Returns the number of seconds to wait before retrying if the request was throttled,
        or None if it was not (in which case the caller proceeds as normal).
        """
        reset = self._header_int(headers, self._reset_header)
        if status_code == self._throttled_status:
            wait = self._start_cooldown(endpoint, reset if reset is not None else self._default_reset_secs)
            self._logger.warning("Rate limited (%s) on '%s' - retrying in %.1fs (server reset: %ss, %d client(s) sharing this key)",
                                 status_code, endpoint, wait, reset if reset is not None else 'not reported', self._contenders)
            return wait
        # Not throttled yet, but out of budget - pause here rather than earn a 429 on the next call.
        remaining = self._header_int(headers, self._remaining_header)
        if remaining is not None and remaining <= 0 and reset:
            wait = self._start_cooldown(endpoint, reset)
            self._logger.info("Rate limit budget exhausted on '%s' (limit %s) - pausing %.1fs until reset",
                              endpoint, self._header_int(headers, self._limit_header), wait)
        return None

    def _start_cooldown(self, endpoint, reset_secs):
        """Holds all clients off this endpoint until its window resets.  Returns the wait in seconds."""
        wait = max(0, reset_secs) + self._buffer_secs
        with self._lock:
            until = time.time() + wait
            # Never shorten a cooldown another thread already set from a later observation
            self._cooldowns[endpoint] = max(self._cooldowns.get(endpoint, 0), until)
        return wait

    def _stagger(self):
        """
        Random hold-off so contending clients don't all resume on the same tick and immediately
        re-trip the limit.  Scaled by how many clients share the key - one client needs none.
        """
        with self._lock:
            contenders = self._contenders
        if contenders <= 1:
            return 0
        return random.uniform(0, min(self._max_jitter_secs, contenders))

    def _cooldown_remaining(self, endpoint):
        with self._lock:
            until = self._cooldowns.get(endpoint)
        return 0 if not until else until - time.time()

    def _header_int(self, headers, name):
        """Header values are strings and may be absent or junk; None means 'not usable'."""
        try:
            value = (headers or {}).get(name)
            return None if value is None else int(value)
        except (ValueError, TypeError, AttributeError):
            self._logger.debug("Could not parse rate limit header '%s'", name)
            return None
