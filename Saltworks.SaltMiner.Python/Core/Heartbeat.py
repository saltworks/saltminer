''' --[auto-generated, do not modify this block]--
*
* Copyright (c) 2026 Saltworks Security, LLC
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

# Heartbeat - rate-limited progress signal handed to long-running collaborators.

from __future__ import annotations

import time
from typing import Callable


class Heartbeat:
    '''
    Callable wrapper that rate-limits beats to at most one per interval.

    Long-running code (extractors, processors, source API clients) accepts an optional
    zero-arg callable and invokes it as it makes progress, so the agent can tell a slow
    worker from a defunct one.  Wrapping the raw delegate here lets that code beat as
    often as it likes - inside per-issue loops - without paying for the underlying call
    every time.

    Not thread safe by design: one instance belongs to one worker thread.  The worst case
    if it were shared is a skipped or duplicated beat, neither of which matters.
    '''

    def __init__(self, fn:Callable[[], None], min_interval_secs:float=5.0):
        '''
        :fn: zero-arg delegate to invoke (typically Worker.heartbeat)
        :min_interval_secs: minimum seconds between calls to fn.  This interval is added to the
        worst-case gap between beats, so the agent's defunct_worker_timeout_secs must comfortably
        exceed it plus the slowest single API call or bulk insert.
        '''
        if not callable(fn):
            raise TypeError("Heartbeat requires a callable")
        self._fn = fn
        self._interval = min_interval_secs
        self._last = 0.0

    def __call__(self):
        now = time.monotonic()
        if now - self._last >= self._interval:
            self._last = now
            self._fn()


def BeatingSleep(seconds, beat=None, sliceSecs:float=5.0):
    '''
    Sleep, beating as it goes.  A plain time.sleep() in a retry backoff parks the thread for the
    whole delay, which reads to the agent exactly like a worker that has died - this splits the
    wait into slices and signals progress between them, so a retrying worker stays visibly alive
    while a genuinely wedged one still trips the defunct timeout.

    Falls back to a plain sleep when no beat is supplied, so non-agent callers are unaffected.

    :seconds: total time to sleep
    :beat: optional zero-arg callable invoked between slices (typically a class's _Beat)
    :sliceSecs: maximum time to sleep uninterrupted
    '''
    if not seconds or seconds <= 0:
        return
    if beat is None:
        time.sleep(seconds)
        return
    remaining = seconds
    while remaining > 0:
        nap = sliceSecs if remaining > sliceSecs else remaining
        time.sleep(nap)
        remaining -= nap
        beat()
