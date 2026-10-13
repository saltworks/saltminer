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

import asyncio
import os
import unittest
from unittest.mock import MagicMock

from Sources.GHAS.GHASAdapter import GHASAdapter

module = os.path.splitext(os.path.basename(__file__))[0]


class _State:
    def __init__(self):
        self.watermarks = {"acme/old-repo/code_scanning": "2026-01-01T00:00:00Z"}

    def get_watermark(self, full_name, engine):
        return self.watermarks.get(f"{full_name}/{engine}")

    def clear_watermark(self, full_name, engine):
        self.watermarks.pop(f"{full_name}/{engine}", None)

    async def save_async(self):
        return None


class _Writer:
    batch_size = 100

    def search_scope_issues(self, search):
        return iter([])

    def bulk_update(self, docs):
        return None


class ArchivedReproTests(unittest.TestCase):
    """PBI-102 reproduction: an archived repository's findings must not be deleted.

    The delete is triggered by a scan queued with ReplaceIssues=True, which the
    adapter requests through map_scan(..., replace=True). On the 3.4.0 base this
    test fails because the archived path makes that call.
    """

    def test_archived_never_requests_replace(self):
        a = GHASAdapter.__new__(GHASAdapter)
        a.instance = "ghas1"
        a._is_rebaseline = False
        a._state = _State()
        a._state_lock = asyncio.Lock()
        a._queue_lock = asyncio.Lock()
        a._archived_suppressed_scopes = []
        a._archived_purged_scopes = []
        a._write_failed_scopes = []
        a._issue_writer = _Writer()
        a._data_client = MagicMock()
        scan = MagicMock()
        scan.model_dump_json.return_value = "{}"
        a.map_scan = MagicMock(return_value=scan)
        a.map_asset = MagicMock(return_value=scan)

        method = getattr(a, "_suppress_archived_async", None) or a._tombstone_archived_async
        try:
            asyncio.run(method({"id": 4242, "full_name": "acme/old-repo", "archived": True}, "code_scanning"))
        except Exception:
            pass

        for call in a.map_scan.call_args_list:
            self.assertFalse(call.kwargs.get("replace", False),
                             "archived path requested replace=True, which hard-deletes the scope's findings")
        print(f"[TEST SUCCESS] {module}:test_archived_never_requests_replace")


if __name__ == '__main__':
    unittest.main()
