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
seeker_adapter = SeekerAdapter(app.Settings)
seeker_adapter.run_sync()

end_timer(prc_key)


