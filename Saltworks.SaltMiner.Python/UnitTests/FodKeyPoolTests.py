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

import concurrent.futures
import json
import os
import tempfile
import threading
import unittest
import urllib.parse
from collections import Counter
from unittest import mock

from Core.ApplicationExceptions import ApplicationConfigurationException
from Core.ApplicationSettings import ApplicationSettings
from Core.FodClient import (FodClient, FodClientAuthenticationException,
                            FodClientConfigurationException)
from Core.RateLimiter import ApiThrottle

DATA_URL = "/api/v3/releases"


class _FakeResponse(object):
    ''' Just enough of requests.Response for FodClient. '''
    def __init__(self, status=200, body=None):
        self.status_code = status
        self.headers = {}
        self.reason = "OK" if status < 400 else "Error"
        self.ok = status < 400
        self.text = json.dumps(body if body is not None else {})
        self._body = body if body is not None else {}

    def json(self):
        return self._body


class _FakeFod(object):
    '''
    Stands in for requests.request.  A POST to /oauth/token answers by the client_id in the body
    (a token "tok-<ClientId>-<n>", or a failure when `login_failures[ClientId]` still has a count
    left); any other call is recorded with the ClientId its bearer token belongs to.
    '''
    def __init__(self):
        self.lock = threading.Lock()
        self.login_failures = {}      # ClientId -> failed logins still to return
        self.login_count = Counter()  # ClientId -> logins attempted
        self.login_secrets = {}       # ClientId -> secret last posted
        self.data_calls = []          # (bearer ClientId, bearer token, status answered)
        self.unauthorized_once = set()  # ClientIds whose first data call is answered 401
        self.always_unauthorized = set()
        self.on_data = None           # optional hook(bearerClientId, token)

    def __call__(self, method, url, **kwargs):
        if url.endswith("/oauth/token"):
            form = urllib.parse.parse_qs(kwargs["data"])
            cid = form["client_id"][0]
            with self.lock:
                self.login_count[cid] += 1
                self.login_secrets[cid] = form["client_secret"][0]
                if self.login_failures.get(cid, 0) > 0:
                    self.login_failures[cid] -= 1
                    return _FakeResponse(400, {"error": "invalid_client"})
                return _FakeResponse(200, {"access_token": f"tok-{cid}-{self.login_count[cid]}"})
        token = kwargs["headers"]["Authorization"].split(" ", 1)[1]
        cid = token[len("tok-"):].rsplit("-", 1)[0]
        if self.on_data:
            self.on_data(cid, token)
        status = 200
        with self.lock:
            if cid in self.always_unauthorized:
                status = 401
            elif cid in self.unauthorized_once:
                self.unauthorized_once.discard(cid)
                status = 401
            self.data_calls.append((cid, token, status))
        return _FakeResponse(status, {"items": []})

    def tokens_sent(self):
        return [t for _, t, _ in self.data_calls]


class FodKeyPoolTests(unittest.TestCase):
    ''' PBI-130 unit tests: the API key pool inside one FOD source.  No live FOD or Elasticsearch needed. '''

    def setUp(self):
        self.tmp = tempfile.TemporaryDirectory()
        self.addCleanup(self.tmp.cleanup)
        ApiThrottle._instances.clear()
        FodClient._RotationCounters.clear()
        self.fake = _FakeFod()
        patcher = mock.patch("Core.FodClient.requests.request", side_effect=self.fake)
        patcher.start()
        self.addCleanup(patcher.stop)

    def _settings(self, **source):
        ''' Writes a Main config and a FOD source file under the temp dir and loads them. '''
        main = os.path.join(self.tmp.name, "Main.json")
        with open(main, "w") as f:
            json.dump({}, f)
        sdir = os.path.join(self.tmp.name, "Sources")
        os.makedirs(sdir, exist_ok=True)
        data = {"Source": "FOD", "SourceName": "FOD1", "BaseUrl": "https://fod.test"}
        data.update(source)
        self.source_file = os.path.join(sdir, "Fod1.json")
        with open(self.source_file, "w") as f:
            json.dump(data, f)
        return ApplicationSettings([main], [self.source_file], keyFile=os.path.join(self.tmp.name, "test.key"))

    @staticmethod
    def _pool(*ids):
        return [{"ClientId": i, "ClientSecret": f"secret-{i}"} for i in ids]

    def _client(self, **source):
        return FodClient(self._settings(**source), "FOD1")

    # ---- AC-1 ----------------------------------------------------------------------------
    def test_single_pair(self):
        """AC-1: a source with only ClientId and ClientSecret holds one key and one throttle."""
        c = self._client(ClientId="solo", ClientSecret="secret-solo")
        self.assertEqual(len(c.Keys), 1)
        self.assertIs(c.Keys[0].Throttle, ApiThrottle.for_key("solo"))
        self.assertEqual(c.Get(DATA_URL).Status, 200)
        self.assertEqual(self.fake.login_secrets["solo"], "secret-solo")

    def test_single_pair_with_empty_key_list(self):
        """The shipped example file (ApiKeys empty) behaves as a single pair."""
        c = self._client(ClientId="solo", ClientSecret="secret-solo", ApiKeys=[])
        self.assertEqual([k.ClientId for k in c.Keys], ["solo"])

    # ---- AC-2 ----------------------------------------------------------------------------
    def test_three_keys(self):
        """AC-2: three key pairs give three tokens and three distinct throttles, one per ClientId."""
        c = self._client(ApiKeys=self._pool("kA", "kB", "kC"))
        self.assertEqual([k.ClientId for k in c.Keys], ["kA", "kB", "kC"])
        self.assertTrue(all(k.Token for k in c.Keys))
        self.assertEqual(len({id(k.Throttle) for k in c.Keys}), 3)
        for k in c.Keys:
            self.assertIs(k.Throttle, ApiThrottle.for_key(k.ClientId))

    def test_top_level_pair_joins_the_pool_first(self):
        c = self._client(ClientId="top", ClientSecret="secret-top", ApiKeys=self._pool("kA"))
        self.assertEqual([k.ClientId for k in c.Keys], ["top", "kA"])

    def test_duplicate_client_id_ignored_with_warning(self):
        s = self._settings(ClientId="kA", ClientSecret="secret-kA", ApiKeys=self._pool("kA", "kB"))
        with self.assertLogs("Core.FodClient", level="WARNING") as logs:
            c = FodClient(s, "FOD1")
        self.assertEqual([k.ClientId for k in c.Keys], ["kA", "kB"])
        self.assertTrue(any("kA" in line for line in logs.output))

    def test_no_keys_raises(self):
        with self.assertRaises(FodClientConfigurationException):
            self._client()

    def test_bad_entry_raises_without_secret(self):
        for bad in ([{"ClientId": "kA"}], ["text"], [{"ClientSecret": "secret-x"}]):
            with self.subTest(bad=bad):
                with self.assertRaises(ApplicationConfigurationException) as ctx:
                    self._client(ApiKeys=bad)
                self.assertNotIn("secret-x", str(ctx.exception))

    # ---- AC-3 ----------------------------------------------------------------------------
    def test_rotation_under_throttle(self):
        """AC-3: 60 calls through three keys; every key carries some; none passes 10 in a fixed window."""
        c = self._client(ApiKeys=self._pool("kA", "kB", "kC"))
        admitted = threading.local()
        records = []
        records_lock = threading.Lock()

        # Each throttle's gate is wrapped at the point it admits a call, which RateLimiter.acquire
        # runs while it holds its own lock.  Reading second_start there, not after acquire()
        # returns, means another thread starting a new window cannot change what is recorded.
        for key in c.Keys:
            gate = key.Throttle._gate
            original = gate._increment_counters

            def admit(original=original, gate=gate, cid=key.ClientId):
                original()
                admitted.value = (cid, gate.second_start)
            gate._increment_counters = admit

        def on_data(bearer_cid, _token):
            value = getattr(admitted, "value", None)
            self.assertIsNotNone(value, "a call was sent without passing a throttle gate")
            admitted.value = None
            with records_lock:
                records.append((bearer_cid, value[0], value[1]))

        self.fake.on_data = on_data
        with concurrent.futures.ThreadPoolExecutor(max_workers=4) as pool:
            statuses = list(pool.map(lambda _: c.Get(DATA_URL).Status, range(60)))

        self.assertEqual(statuses, [200] * 60)
        self.assertEqual(len(records), 60)
        for bearer, admitter, _window in records:
            self.assertEqual(bearer, admitter, "a call went out on a different key from the one whose gate admitted it")
        self.assertEqual({r[0] for r in records}, {"kA", "kB", "kC"})
        per_window = Counter((admitter, window) for _, admitter, window in records)
        self.assertLessEqual(max(per_window.values()), 10)

    # ---- AC-4 ----------------------------------------------------------------------------
    def test_401_renews_one_key(self):
        """AC-4: a 401 renews only the key that was used, and the call is retried once."""
        c = self._client(ApiKeys=self._pool("kA", "kB", "kC"))
        before = {k.ClientId: k.Token for k in c.Keys}
        self.fake.unauthorized_once.add("kB")
        for _ in range(6):
            self.assertEqual(c.Get(DATA_URL).Status, 200)
        self.assertEqual(self.fake.login_count["kB"], 2)
        self.assertEqual(self.fake.login_count["kA"], 1)
        self.assertEqual(self.fake.login_count["kC"], 1)
        kb_calls = [(t, s) for cid, t, s in self.fake.data_calls if cid == "kB" and (s == 401 or t == "tok-kB-2")]
        self.assertEqual(kb_calls[0], ("tok-kB-1", 401))
        self.assertEqual(kb_calls[1], ("tok-kB-2", 200))
        self.assertEqual(len(self.fake.data_calls), 7, "6 calls plus exactly one retry")
        after = {k.ClientId: k.Token for k in c.Keys}
        self.assertEqual(after["kA"], before["kA"])
        self.assertEqual(after["kC"], before["kC"])
        self.assertEqual(after["kB"], "tok-kB-2")

    def test_401_with_failed_renewal_moves_to_another_key(self):
        c = self._client(ApiKeys=self._pool("kA", "kB"))
        self.fake.always_unauthorized.add("kA")
        self.fake.login_failures["kA"] = 99   # renewal fails too
        with self.assertLogs("Core.FodClient", level="ERROR") as logs:
            statuses = [c.Get(DATA_URL).Status for _ in range(4)]
        self.assertEqual(statuses, [200] * 4)
        self.assertFalse(c.Keys[0].Live)
        self.assertTrue(any("kA" in line for line in logs.output))
        self.assertNotIn("secret-kA", "\n".join(logs.output))

    def test_last_live_key_stays_in_rotation(self):
        """A key that fails renewal while it is the only live key stays live; the 401 goes to the caller."""
        c = self._client(ClientId="solo", ClientSecret="secret-solo")
        self.fake.always_unauthorized.add("solo")
        self.fake.login_failures["solo"] = 99
        self.assertEqual(c.Get(DATA_URL).Status, 401)
        self.assertTrue(c.Keys[0].Live)

    # ---- AC-5 ----------------------------------------------------------------------------
    def test_failed_login_named(self):
        """AC-5: a failed login names the ClientId, never the secret, and later calls use the other keys."""
        self.fake.login_failures["kB"] = 99
        s = self._settings(ApiKeys=self._pool("kA", "kB", "kC"))
        with self.assertLogs("Core.FodClient", level="DEBUG") as logs:
            c = FodClient(s, "FOD1")
            for _ in range(20):
                self.assertEqual(c.Get(DATA_URL).Status, 200)
        text = "\n".join(logs.output)
        # The startup line lists every ClientId, so "kB" in the text proves nothing. The failure
        # line itself, the one at ERROR level about the auth failure, must name the failing key.
        failureLines = [r for r in logs.records if r.levelname == "ERROR" and "failure (auth)" in r.getMessage()]
        self.assertTrue(failureLines, "no ERROR line reports the failed login")
        self.assertTrue(any("kB" in r.getMessage() for r in failureLines))
        self.assertFalse(any("kA" in r.getMessage() or "kC" in r.getMessage() for r in failureLines))
        self.assertNotIn("secret-kB", text)
        self.assertNotIn("secret-kA", text)
        self.assertEqual({cid for cid, _, _ in self.fake.data_calls}, {"kA", "kC"})

    def test_debug_request_line_names_the_key(self):
        """AC-7 evidence: each request logs the ClientId it used, so a live run can count calls per key."""
        c = self._client(ApiKeys=self._pool("kA", "kB"))
        with self.assertLogs("Core.FodClient", level="DEBUG") as logs:
            for _ in range(6):
                c.Get(DATA_URL)
        used = {cid for cid in ("kA", "kB") if any(f"called with ClientId '{cid}'" in r.getMessage() for r in logs.records)}
        self.assertEqual(used, {"kA", "kB"})

    def test_all_keys_failing_raises_naming_every_client_id(self):
        for cid in ("kA", "kB"):
            self.fake.login_failures[cid] = 99
        with self.assertRaises(FodClientAuthenticationException) as ctx:
            self._client(ApiKeys=self._pool("kA", "kB"))
        self.assertIn("kA", str(ctx.exception))
        self.assertIn("kB", str(ctx.exception))
        self.assertNotIn("secret-", str(ctx.exception))

    def test_single_pair_login_failure_raises_as_before(self):
        self.fake.login_failures["solo"] = 99
        with self.assertRaises(FodClientAuthenticationException) as ctx:
            self._client(ClientId="solo", ClientSecret="secret-solo")
        self.assertIn("solo", str(ctx.exception))

    def test_repr_hides_secret(self):
        c = self._client(ApiKeys=self._pool("kA"))
        self.assertNotIn("secret-kA", repr(c.Keys[0]))
        self.assertIn("kA", repr(c.Keys[0]))

    # ---- AC-6 ----------------------------------------------------------------------------
    def test_pool_secrets_encrypted(self):
        """AC-6: after load, every ApiKeys secret on disk starts with e$Fernet$ and the client still logs in."""
        s = self._settings(ClientId="", ClientSecret="", ApiKeys=self._pool("kA", "kB", "kC"))
        with open(self.source_file) as f:
            on_disk = json.load(f)
        for entry in on_disk["ApiKeys"]:
            self.assertTrue(entry["ClientSecret"].startswith("e$Fernet$"), entry["ClientId"])
        with open(self.source_file) as f:
            raw = f.read()
        self.assertNotIn("secret-kA", raw)
        c = FodClient(s, "FOD1")
        self.assertEqual(len(c.Keys), 3)
        for cid in ("kA", "kB", "kC"):
            self.assertEqual(self.fake.login_secrets[cid], f"secret-{cid}")

    def test_encrypted_pool_loads_again_and_does_not_double_encrypt(self):
        self._settings(ApiKeys=self._pool("kA"))
        with open(self.source_file) as f:
            first = json.load(f)["ApiKeys"][0]["ClientSecret"]
        s = ApplicationSettings([os.path.join(self.tmp.name, "Main.json")], [self.source_file],
                                keyFile=os.path.join(self.tmp.name, "test.key"))
        with open(self.source_file) as f:
            self.assertEqual(json.load(f)["ApiKeys"][0]["ClientSecret"], first)
        self.assertEqual(s.GetSourceKeyList("FOD1")[0]["ClientSecret"], "secret-kA")

    def test_other_source_types_not_touched(self):
        main = os.path.join(self.tmp.name, "Main.json")
        with open(main, "w") as f:
            json.dump({}, f)
        sf = os.path.join(self.tmp.name, "Other.json")
        with open(sf, "w") as f:
            json.dump({"Source": "SSC", "SourceName": "SSC1", "ApiKeys": self._pool("kA")}, f)
        ApplicationSettings([main], [sf], keyFile=os.path.join(self.tmp.name, "test.key"))
        with open(sf) as f:
            self.assertEqual(json.load(f)["ApiKeys"][0]["ClientSecret"], "secret-kA")

    def test_decrypt_failure_names_entry_not_secret(self):
        s = self._settings(ApiKeys=self._pool("kA"))
        other = ApplicationSettings([os.path.join(self.tmp.name, "Main.json")], [self.source_file],
                                    keyFile=os.path.join(self.tmp.name, "other.key"))
        with self.assertRaises(ApplicationConfigurationException) as ctx:
            other.GetSourceKeyList("FOD1")
        self.assertIn("kA", str(ctx.exception))
        self.assertNotIn("secret-kA", str(ctx.exception))
        self.assertEqual(s.GetSourceKeyList("FOD1")[0]["ClientSecret"], "secret-kA")

    # ---- design tests ----------------------------------------------------------------------
    def test_dead_key_readmitted(self):
        """A key whose login failed gets another attempt after KEY_RETRY_SECS and rejoins the rotation."""
        self.fake.login_failures["kB"] = 1
        s = self._settings(ApiKeys=self._pool("kA", "kB"))
        with mock.patch.object(FodClient, "KEY_RETRY_SECS", 0):
            c = FodClient(s, "FOD1")
            self.assertFalse(c.Keys[1].Live)
            for _ in range(6):
                c.Get(DATA_URL)
        self.assertTrue(c.Keys[1].Live)
        self.assertIn("tok-kB-2", self.fake.tokens_sent())

    def test_dead_key_not_retried_before_its_time(self):
        self.fake.login_failures["kB"] = 1
        c = self._client(ApiKeys=self._pool("kA", "kB"))
        for _ in range(6):
            c.Get(DATA_URL)
        self.assertEqual(self.fake.login_count["kB"], 1)
        self.assertFalse(c.Keys[1].Live)

    def test_connection_partial_pool(self):
        """TestConnection is False, naming the failed ClientId and not its secret, when one pool key fails."""
        self.fake.login_failures["kB"] = 99
        s = self._settings(ApiKeys=self._pool("kA", "kB", "kC"))
        ok, msg = FodClient.TestConnection(s, "FOD1")
        self.assertFalse(ok)
        self.assertIn("1 of 3", msg)
        self.assertIn("kB", msg)
        self.assertNotIn("secret-kB", msg)

    def test_connection_whole_pool_ok(self):
        s = self._settings(ApiKeys=self._pool("kA", "kB"))
        self.assertEqual(FodClient.TestConnection(s, "FOD1"), (True, ""))


if __name__ == "__main__":
    unittest.main()
