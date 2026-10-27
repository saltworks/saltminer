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

import json
import os
import tempfile
import unittest
import unittest.mock
import urllib.parse

from Core.ApplicationSettings import ApplicationSettings
from Core.FodClient import FodClient
from Core.RateLimiter import ApiThrottle

# Unique to this module: throttles are cached per ClientId for the whole process, so a ClientId another
# test used would bring that test's throttle (and its 2 s buffer) along.
CLIENT_ID = "pbi131-rate-limit-log-client"
SECRET = "pbi131-rate-limit-log-secret"
DATA_URL = "/api/v3/releases"


class _FakeResponse(object):
    def __init__(self, status=200, body=None, headers=None):
        self.status_code = status
        self.headers = headers or {}
        self.reason = "OK" if status < 400 else "Error"
        self.ok = status < 400
        self.text = json.dumps(body or {})
        self._body = body or {}

    def json(self):
        return self._body


class RateLimiterLogTests(unittest.TestCase):
    ''' PBI-131 unit tests: the 429 line names the source and the key, never the secret.  No live FOD needed. '''

    def setUp(self):
        self.tmp = tempfile.TemporaryDirectory()
        self.addCleanup(self.tmp.cleanup)
        ApiThrottle._instances.clear()
        self.data_answers = [_FakeResponse(429, headers={"X-Rate-Limit-Reset": "0"}), _FakeResponse(200, {"items": []})]
        patcher = unittest.mock.patch("Core.FodClient.requests.request", side_effect=self._request)
        patcher.start()
        self.addCleanup(patcher.stop)

    def _request(self, method, url, **kwargs):
        if url.endswith("/oauth/token"):
            self.assertEqual(urllib.parse.parse_qs(kwargs["data"])["client_id"], [CLIENT_ID])
            return _FakeResponse(200, {"access_token": "tok"})
        return self.data_answers.pop(0)

    def _client(self, source_name:str="FOD1") -> FodClient:
        main = os.path.join(self.tmp.name, "Main.json")
        with open(main, "w") as f:
            json.dump({}, f)
        sdir = os.path.join(self.tmp.name, "Sources")
        os.makedirs(sdir, exist_ok=True)
        path = os.path.join(sdir, f"{source_name}.json")
        with open(path, "w") as f:
            json.dump({"Source": "FOD", "SourceName": source_name, "BaseUrl": "https://fod.test",
                       "ClientId": CLIENT_ID, "ClientSecret": SECRET, "RateLimitBufferSeconds": 0}, f)
        return FodClient(ApplicationSettings([main], [path], keyFile=os.path.join(self.tmp.name, "test.key")), source_name)

    # ---- AC-5 ----------------------------------------------------------------------------
    def test_429_names_source_and_client(self):
        """AC-5: a 429 logs one WARNING with the source name and the ClientId, and no record holds the secret."""
        c = self._client("FOD1")
        with self.assertLogs("Core.FodClient", level="INFO") as logs:
            self.assertEqual(c.Get(DATA_URL).Status, 200)
        warnings = [r.getMessage() for r in logs.records if r.levelname == "WARNING"]
        self.assertEqual(len(warnings), 1)
        self.assertIn("Rate limited (429)", warnings[0])
        self.assertIn("source 'FOD1'", warnings[0])
        self.assertIn(f"ClientId '{CLIENT_ID}'", warnings[0])
        for record in logs.records:
            self.assertNotIn(SECRET, record.getMessage())

    def test_throttle_without_label_logs_as_before(self):
        """A caller that passes no label gets the line it always had."""
        throttle = ApiThrottle(buffer_secs=0)
        with self.assertLogs(level="WARNING") as logs:
            throttle.observe("/x", 429, {"X-Rate-Limit-Reset": "0"})
        self.assertEqual(logs.records[0].getMessage(),
                         "Rate limited (429) on '/x' - retrying in 0.0s (server reset: 0s, 0 client(s) sharing this key)")

    def test_label_goes_into_the_line(self):
        throttle = ApiThrottle(buffer_secs=0)
        with self.assertLogs(level="WARNING") as logs:
            throttle.observe("/x", 429, {"X-Rate-Limit-Reset": "0"}, label="source 'S', ClientId 'C'")
        self.assertIn("on '/x' for source 'S', ClientId 'C' - retrying", logs.records[0].getMessage())


if __name__ == "__main__":
    unittest.main()
