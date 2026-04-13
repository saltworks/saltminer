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
from datetime import datetime, timezone

from Core.Application import Application
from Sources.Tenable.TenableAdapter import TenableAdapter
from Sources.Snyk.SnykAdapter import SnykAdapter
from Sources.Seeker.SeekerAdapter import SeekerAdapter
from Sources.GHAS.GHASAdapter import GHASAdapter
from Sources.CoverityOnPolaris.COPAdapter import COPAdapter
from Sources.Axonius.AxoniusAdapter import AxoniusAdapter


prm_source = None
if len(sys.argv) > 1:
    prm_source = sys.argv[1]

prm_first_load = True
if len(sys.argv) > 2:
    prm_first_load = sys.argv[2].lower() == "true"

prm_logging_instance = None
if len(sys.argv) > 3:
    prm_logging_instance = sys.argv[3]

hlp_msg = f"Usage: {sys.argv[0]} [source] [first_load] [logging_instance]\n\n:source: Required. Source name to run (Tenable, SNYK, Seeker, etc.)\n:first_load: Optional. Set to 'true' if this is the first load of the adapter (default: true)\n:logging_instance: Optional. Custom logging instance name for this run (default: None)"
if prm_source is None:
    print(hlp_msg)
    sys.exit(1)

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


app = Application(loggingCustomTag=prm_logging_instance)
logging.info("%s starting - %s", prm_source, datetime.now(timezone.utc).isoformat())
start_timer(prm_source)

if prm_source.lower() == "tenable":
    adapter = TenableAdapter(app)
elif prm_source.lower() == "snyk":
    adapter = SnykAdapter(app)
elif prm_source.lower() == "seeker":
    adapter = SeekerAdapter(app)
elif prm_source.lower() == "ghas":
    adapter = GHASAdapter(app)
elif prm_source.lower() == "cop":
    adapter = COPAdapter(app)
elif prm_source.lower() == "axonius":
    adapter = AxoniusAdapter(app)
elif prm_source.lower() in ['ssc', 'fod']:
    print(f"Source '{prm_source}' is supported but requires different calls. Please use RunSync, RunAgentService scripts for these sources.")
    sys.exit(1)
else:
    print(f"Unknown/unsupported source: {prm_source}")
    sys.exit(1)

adapter.run_sync(first_load=prm_first_load)
end_timer(prm_source)