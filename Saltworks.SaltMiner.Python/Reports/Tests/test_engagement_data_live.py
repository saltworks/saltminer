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

# Integration test against a live Data API. It is skipped unless SM_REPORT_LIVE_ENGAGEMENT_ID names an
# engagement and the Python config folder resolves (SALTMINER_CONFIG_PATH, ConfigPath.txt or a local Config
# folder) with a DataClient section that carries ApiUrl, ApiKey and ManagerApiKey.
#
#   SM_REPORT_LIVE_ENGAGEMENT_ID=<id> python -m pytest Reports/Tests/test_engagement_data_live.py -q -s
#
# The engagement should have assets, issues, comments and at least one markdown-typed attribute.

import os
import unittest

from Reports.EngagementData import DataApiSource, assemble_engagement, load_report_settings
from Reports.Tests import data_api_fixture as fx

ENGAGEMENT_ID = os.environ.get("SM_REPORT_LIVE_ENGAGEMENT_ID", "")


@unittest.skipUnless(ENGAGEMENT_ID, "SM_REPORT_LIVE_ENGAGEMENT_ID is not set")
class LiveEngagement(unittest.TestCase):
    @classmethod
    def setUpClass(cls):
        try:
            from Core.Application import Application
            from Core.DataClient import DataClient

            cls.app = Application(skipCleanFiles=True)
            cls.client = DataClient(cls.app)
        except (Exception, SystemExit) as exc:
            raise unittest.SkipTest(f"Python config folder or DataClient section does not resolve: {exc}")
        cls.source = DataApiSource(cls.client)

    @classmethod
    def tearDownClass(cls):
        client = getattr(cls, "client", None)
        if client is not None:
            client.close()

    def test_live_issue_count(self):
        record, markdown = assemble_engagement(ENGAGEMENT_ID, self.source, load_report_settings(self.app))
        draft = record["State"].lower() == "draft"
        route = "queueissue/search" if draft else "issue/search"
        body = {
            "Filter": {"AnyMatch": False,
                       "FilterMatches": {"Saltminer.Engagement.Id": ENGAGEMENT_ID},
                       "SubFilter": {"AnyMatch": True, "FilterMatches": {"Vulnerability.IsActive": "true"}}},
            "PagingInfo": {"Page": 1, "Size": 1, "TotalHitsCanBeTruncated": False},
        }
        if not draft:
            body.update({"AssetType": "Pen", "SourceType": "Pentest", "Instance": "PenTest"})
        response = self.source.request("POST", route, body)
        expected = response["pagingInfo"]["totalHits"]
        print(f"\nlive engagement {ENGAGEMENT_ID}: state={record['State']} assets={len(record['AssetTocs'])} "
              f"IssueDetails={len(record['IssueDetails'])} removed={len(record['IssueDetailsRemoved'])} "
              f"issue/search totalHits={expected} markdown_fields={len(markdown)}")
        self.assertEqual(len(record["IssueDetails"]), expected)

    def test_live_response_keys_match_the_fixture(self):
        # The fixture was written from the entity classes; compare each route's top-level keys with it.
        fixture = fx.FakeTransport()
        for route in ("issue/search", "asset/search", "comment/search", "attributedefinition/search"):
            body = {"Filter": {"AnyMatch": False, "FilterMatches": {"Saltminer.Engagement.Id": ENGAGEMENT_ID}
                               if route != "attributedefinition/search" else {}},
                    "PagingInfo": {"Page": 1, "Size": 1}}
            if route in ("issue/search", "asset/search"):
                body.update({"AssetType": "Pen", "SourceType": "Pentest", "Instance": "PenTest"})
            live = self.source.request("POST", route, body)
            recorded = fixture("POST", route, {"Filter": {}, "PagingInfo": {"Page": 1, "Size": 1}})
            self.assertLessEqual(set(recorded), set(live), f"{route}: fixture keys {sorted(recorded)} vs live "
                                                          f"{sorted(live)}")


if __name__ == "__main__":
    unittest.main()
