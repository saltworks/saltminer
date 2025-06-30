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

import requests

from Core.Application import Application
from Core.SscClient import SscClient
from Core.FodClient import FodClient

app = Application(skipCleanFiles=True)
logging.debug("Testing elastic connection...")
report = { "Elasticsearch": "Failed", "SSC": "Failed", "FOD": "Failed" }
try:
    es = app.GetElasticClient()
    if es.PingServer():
        report['Elasticsearch'] = "OK"
except requests.exceptions.ConnectionError as e:
    logging.error("Error connecting to elasticsearch server: connection failed or timed out")
except Exception as e:
    logging.error(f"Error connecting to elasticsearch server: [{type(e).__name__}] {e}")

logging.debug("Testing SSC connection...")
try:
    ok, msg = SscClient.TestConnection(app.Settings, "SSC1")
    if ok:
        report['SSC'] = "OK"
    else:
        logging.warning(f"SSC connection failed: {msg}")
except requests.exceptions.ConnectionError as e:
    logging.error("Error connecting to SSC: connection failed or timed out")
except Exception as e:
    logging.error(f"Error connecting to SSC:: [{type(e).__name__}] {e}")

logging.info("Testing FOD connection...")
try:
    ok, msg = FodClient.TestConnection(app.Settings, "FOD1")
    if ok:
        report['FOD'] = "OK"
    else:
        logging.warning(f"FOD connection failed: {msg}")
except requests.exceptions.ConnectionError as e:
    msg = "Error connecting to FOD: connection failed or timed out"
    logging.exception()
except Exception as e:
    msg = "Error connecting to FOD: {}".format(e)
if msg.startswith("Error"):
    logging.error(msg)
else:
    logging.info(msg)

print("****************************\n** Results:")
for k in report.keys():
    print(f"** {k}: {report[k]}")
print("****************************")
