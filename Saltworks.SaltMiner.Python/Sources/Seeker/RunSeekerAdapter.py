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

import time
import logging
import sys
import json
from datetime import datetime, timezone

from Core.Application import Application
from Sources.Seeker.SeekerAdapter import SeekerAdapter
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

 
app = Application()
prc_key = "seeker Adapter"
logging.info("%s starting - %s", prc_key, datetime.now(timezone.utc).isoformat())
start_timer(prc_key)
seeker_adapter = SeekerAdapter(app)
seeker_adapter.run_sync()

end_timer(prc_key)


