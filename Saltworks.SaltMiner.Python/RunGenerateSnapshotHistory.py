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
import time

# Python 3.13 bug: _DeleteDummyThreadOnDel.__del__ crashes during interpreter
# shutdown because its lock is cleared to None before __del__ runs. Patch it
# out — the cleanup is cosmetic and skipping it is safe.
if hasattr(threading, '_DeleteDummyThreadOnDel'):
    threading._DeleteDummyThreadOnDel.__del__ = lambda self: None

from Core.Application import Application
from snapshot import run_snapshot_history

# Usage: python RunGenerateSnapshotHistory.py [source_type] [--rebuild]
#
# No args:     process every discovered (source_type, asset_type) pair. Pairs
#              missing historical data are backfilled from the earliest
#              vulnerability.found_date; pairs already populated just refresh
#              _current.
# source_type: limit processing to that source (e.g. 'FOD' or 'Saltworks.FOD').
# --rebuild:   requires source_type. Deletes the _historical issue and scan
#              indices for that source, then runs the normal flow so it
#              rebuilds from earliest data.

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

args = sys.argv[1:]
rebuild = "--rebuild" in args
args = [a for a in args if a != "--rebuild"]
source_type_arg: str | None = args[0] if args else None

if rebuild and not source_type_arg:
    logging.error("[%s] --rebuild requires a source_type argument", prog)
    sys.exit(1)

worker_count = app.Settings.Get("Snapshots", "HistoryWorkerCount",       4)
page_size    = app.Settings.Get("Snapshots", "HistoryCompositePageSize", 1000)
chunk_size   = app.Settings.Get("Snapshots", "HistorySourceIdChunkSize", 1000)

logging.info("[%s] Starting — source_type=%s, workers=%d, rebuild=%s",
             prog, source_type_arg or "all", worker_count, rebuild)

try:
    start_timer("RunGenerateSnapshotHistory")
    run_snapshot_history(
        app_settings=app.Settings,
        source_type_arg=source_type_arg,
        worker_count=worker_count,
        composite_page_size=page_size,
        source_id_chunk_size=chunk_size,
        rebuild=rebuild,
    )
    end_timer("RunGenerateSnapshotHistory")
except Exception as e:
    logging.critical("[%s] Exception: [%s] %s", prog, type(e).__name__, e)
    raise

logging.info("[%s] Processing complete", prog)