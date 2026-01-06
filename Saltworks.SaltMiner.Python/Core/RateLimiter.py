''' --[auto-generated, do not modify this block]--
*
* Copyright (c) 2025 Saltworks Security, LLC
*
* Use of this software is governed by the Business Source License included
* in the LICENSE file.
*
* Change Date: 2029-12-09
*
* On the date above, in accordance with the Business Source License, use
* of this software will be governed by version 2 or later of the General
* Public License.
*
* ----
'''

import threading
import time
import logging

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
