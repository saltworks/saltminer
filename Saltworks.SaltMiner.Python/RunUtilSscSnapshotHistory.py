''' --[auto-generated, do not modify this block]--
*
* Copyright (c) 2025 Saltworks Security, LLC
*
* Use of this software is governed by the Business Source License included
* in the LICENSE file.
*
* Change Date: 2029-06-30
*
* On the date above, in accordance with the Business Source License, use
* of this software will be governed by version 2 or later of the General
* Public License.
*
* ----
'''

import logging
import datetime
import time

from Core.Application import Application
from Sources.SSC.SscScanSnapshotHelper import SscScanSnapshotHelper
from Sources.SSC.SscSnapshotHelper import SscSnapshotHelper

timers = {}

def StartTimer(key):
    timers[key] = time.perf_counter()

def EndTimer(key):
    if key in timers.keys() and timers[key]:
        elapsed = time.perf_counter() - timers[key]
        logging.info(f"{key}: {elapsed}")
        return elapsed
    else:
        raise ValueError(f"Invalid timer key '{key}'")

app = Application()
key = "process"

# Snapshot history first, scan history depends on this being updated first
logging.info("Snapshot history update - %s", datetime.datetime.utcnow().isoformat())
helper = SscSnapshotHelper(app.Settings)
StartTimer(key)
helper.RebuildAllRecords('2000-01-01')
EndTimer(key)

logging.info("Snapshot scan history update - %s", datetime.datetime.utcnow().isoformat())
helper = SscScanSnapshotHelper(app.Settings)
StartTimer(key)
helper.RebuildScanHistory('2000-01-01')
EndTimer(key)

logging.info("Snapshot history processing complete.")
