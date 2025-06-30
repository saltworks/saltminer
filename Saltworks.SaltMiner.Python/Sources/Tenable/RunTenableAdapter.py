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

import time
import logging
import sys
import json
from datetime import datetime, timezone

from Core.Application import Application
from Sources.Tenable.TenableAdapter import TenableAdapter
timers = {}

def start_timer(key):
    timers[key] = time.perf_counter()

def end_timer(key):
    if key in timers:
        elapsed = time.perf_counter() - timers[key]
        print(f"{key} completed in: {elapsed:.2f}s")
        return elapsed
    else:
        raise ValueError(f"Invalid timer key '{key}'")


first_load = True
app = Application()
prc_key = "Tenable Adapter"
logging.info("%s starting - %s", prc_key, datetime.now(timezone.utc).isoformat())
start_timer(prc_key)
tenable_adapter = TenableAdapter(app.Settings)
tenable_adapter.run_sync(first_load=True)

end_timer(prc_key)


