# TODO: TEMPORARY DEBUGGING - Remove after testing
'''
Probe script — verifies every ES query the snapshot pipeline will use, against opov.
Connection: ai/scratch/opov.saltminer.io.md (do NOT commit credentials).
Run: SALTMINER_CONFIG_PATH=... python -m Scratch.snapshot_query_probe
'''
import json
import logging
from Core.Application import Application

logging.basicConfig(level=logging.INFO)

def main():
    app = Application()
    es = app.GetElasticClient()
    print("[probe] connected; cluster info:")
    print(json.dumps(es.GetClusterHealth() if hasattr(es, "GetClusterHealth") else {"note": "no health method"}, indent=2, default=str))

if __name__ == "__main__":
    main()
