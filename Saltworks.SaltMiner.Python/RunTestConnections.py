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
