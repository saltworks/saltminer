import time
import logging
import sys
from datetime import datetime, timezone

from Core.Application import Application
from Sources.SNYK.SnykAdapter import SnykAdapter

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
prc_key = "Snyk Adapter"
logging.info("%s starting - %s", prc_key, datetime.now(timezone.utc).isoformat())
start_timer(prc_key)
snyk_adapter = SnykAdapter(app.Settings)
snyk_adapter.run_sync(first_load=first_load)
end_timer(prc_key)




