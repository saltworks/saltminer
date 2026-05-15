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

import logging
import os
import sys
import threading
import datetime
import time

# Python 3.13 bug: _DeleteDummyThreadOnDel.__del__ crashes during interpreter
# shutdown because its lock is cleared to None before __del__ runs. Patch it
# out — the cleanup is cosmetic and skipping it is safe.
if hasattr(threading, '_DeleteDummyThreadOnDel'):
    threading._DeleteDummyThreadOnDel.__del__ = lambda self: None

from Core.Application import Application
from snapshot import run_snapshot_history

# Usage: python RunGenerateSnapshotHistory.py <source_type|all> [start_date YYYY-MM-DD] [mode all|current|historical|daily]
#
# mode all        — rebuild all historical monthly indices AND refresh _current (default)
# mode current    — refresh only _current for the live month
# mode historical — rebuild historical monthly indices only, skip _current
# mode daily      — smart daily job: rebuild last closed month if its index is missing,
#                   then always refresh _current (no start_date needed)

timers = {}
prog = os.path.splitext(os.path.basename(__file__))[0]


def start_timer(key):
    timers[key] = time.perf_counter()


def end_timer(key):
    if key in timers and timers[key]:
        elapsed = time.perf_counter() - timers[key]
        logging.info("[%s] %s completed in %.3f sec", prog, key, elapsed)
        return elapsed
    raise ValueError(f"Invalid timer key '{key}'")


app = Application()

source_type_arg = sys.argv[1] if len(sys.argv) > 1 else "all"

_MODES = {"all", "current", "historical", "daily"}

start_date: datetime.datetime | None = None
mode = "all"

if len(sys.argv) > 2:
    arg2 = sys.argv[2]
    if arg2 in _MODES:
        # mode supplied in position 2 (no start_date)
        mode = arg2
    else:
        try:
            start_date = datetime.datetime.strptime(arg2, "%Y-%m-%d").replace(
                tzinfo=datetime.timezone.utc
            )
        except ValueError:
            logging.error("[%s] Invalid argument '%s' — expected YYYY-MM-DD or a mode (%s)",
                          prog, arg2, "|".join(sorted(_MODES)))
            sys.exit(1)
        if len(sys.argv) > 3:
            mode = sys.argv[3]
worker_count       = app.Settings.Get("Snapshots", "HistoryWorkerCount",        4)
page_size          = app.Settings.Get("Snapshots", "HistoryCompositePageSize",  1000)
chunk_size         = app.Settings.Get("Snapshots", "HistorySourceIdChunkSize",  1000)
default_start_str  = app.Settings.Get("Snapshots", "HistoryStartDate",          "2000-01-01")

if start_date is None and mode not in ("current", "daily"):
    try:
        start_date = datetime.datetime.strptime(default_start_str, "%Y-%m-%d").replace(
            tzinfo=datetime.timezone.utc
        )
    except ValueError:
        logging.warning(
            "[%s] Could not parse HistoryStartDate '%s'; using earliest available data date",
            prog, default_start_str,
        )
        start_date = None

logging.info("[%s] Starting — source_type=%s, start_date=%s, workers=%d, mode=%s",
             prog, source_type_arg, start_date, worker_count, mode)

try:
    start_timer("RunGenerateSnapshotHistory")
    run_snapshot_history(
        app_settings=app.Settings,
        source_type_arg=source_type_arg,
        start_date=start_date,
        worker_count=worker_count,
        composite_page_size=page_size,
        source_id_chunk_size=chunk_size,
        mode=mode,
    )
    end_timer("RunGenerateSnapshotHistory")
except Exception as e:
    logging.critical("[%s] Exception: [%s] %s", prog, type(e).__name__, e)
    raise

logging.info("[%s] Processing complete", prog)
