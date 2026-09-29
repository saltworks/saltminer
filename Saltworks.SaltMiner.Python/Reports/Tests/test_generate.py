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

# Supporting coverage for Reports.Generate (PBI-051), not a criterion on its own (design section 5):
# runs `report` end to end on the fixture transport and the shipped default template, checks the
# settings shim satisfies a real Core.DataClient.DataClient, and checks the `report`/`check` exit
# code contract ReportGenerator.cs (the .NET side) relies on.

import json
import os
import tempfile
import unittest
from pathlib import Path
from unittest import mock

from Core.DataClient import DataClient
from Reports import Generate
from Reports.EngagementData import DataApiSource
from Reports.Tests import data_api_fixture as fx

# Saltworks.SaltMiner.Python/Reports/Tests -> the repository root is three levels up from Python.
_HERE = os.path.dirname(os.path.abspath(__file__))
TEMPLATE = os.path.normpath(os.path.join(
    _HERE, "..", "..", "..", "Saltworks.SaltMiner.JobManager", "Saltworks.SaltMiner.JobManager",
    "TemplateDefaults", "Saltworks", "SaltworksTemplate.docx"))


class ShimSatisfiesDataClient(unittest.TestCase):
    def test_shim_constructs_a_real_data_client(self):
        app = Generate._build_app_shim({"data_api": {"url": "http://data-api:5000"}}, "test-key")
        client = DataClient(app)
        try:
            self.assertEqual(client.client.BaseUrl, "http://data-api:5000/")
        finally:
            client.close()


class ReportEndToEnd(unittest.TestCase):
    def _write_request(self, tmp_dir: str, engagement_id: str) -> Path:
        output_dir = Path(tmp_dir) / "out"
        output_dir.mkdir()
        request = {
            "engagement_id": engagement_id,
            "template_path": TEMPLATE,
            "output_dir": str(output_dir),
            "result_path": str(Path(tmp_dir) / "result.json"),
            "report_name": "Report-test",
            "attachment_type": "Word",
            "data_api": {"url": "http://data-api:5000", "verify_ssl": True, "timeout_sec": 10},
            "settings": {},
            "field_value_colors": {},
        }
        request_path = Path(tmp_dir) / "request.json"
        request_path.write_text(json.dumps(request), encoding="utf-8")
        return request_path

    def test_report_mode_writes_one_docx_and_a_result_file(self):
        with tempfile.TemporaryDirectory() as tmp_dir, \
             mock.patch.dict(os.environ, {Generate.DATA_API_KEY_ENV_VAR: "test-key"}), \
             mock.patch.object(Generate, "DataApiSource",
                              lambda client: DataApiSource(None, transport=fx.FakeTransport())):
            request_path = self._write_request(tmp_dir, fx.ENGAGEMENT_ID)
            exit_code = Generate._handle_report(
                Generate.build_parser().parse_args(["report", "--request", str(request_path)]))

            self.assertEqual(exit_code, Generate.EXIT_OK)
            result = json.loads((Path(tmp_dir) / "result.json").read_text(encoding="utf-8"))
            self.assertEqual(len(result["files"]), 1)
            self.assertTrue(result["files"][0].endswith("Report-test.docx"))
            self.assertGreater(result["fields_merged"], 0)

    def test_report_mode_exit_1_on_an_unknown_engagement(self):
        with tempfile.TemporaryDirectory() as tmp_dir, \
             mock.patch.dict(os.environ, {Generate.DATA_API_KEY_ENV_VAR: "test-key"}), \
             mock.patch.object(Generate, "DataApiSource",
                              lambda client: DataApiSource(None, transport=fx.FakeTransport())):
            request_path = self._write_request(tmp_dir, "no-such-engagement")
            exit_code = Generate._handle_report(
                Generate.build_parser().parse_args(["report", "--request", str(request_path)]))

            self.assertEqual(exit_code, Generate.EXIT_REPORT_FAILED)


class CheckMode(unittest.TestCase):
    def test_check_mode_exit_0_on_pass(self):
        with mock.patch.object(Generate, "check_pdf_converter", lambda attachment_type: True):
            exit_code = Generate._handle_check(
                Generate.build_parser().parse_args(["check", "--attachment-type", "Pdf"]))
            self.assertEqual(exit_code, Generate.EXIT_OK)

    def test_check_mode_exit_3_on_fail(self):
        with mock.patch.object(Generate, "check_pdf_converter", lambda attachment_type: False):
            exit_code = Generate._handle_check(
                Generate.build_parser().parse_args(["check", "--attachment-type", "Pdf"]))
            self.assertEqual(exit_code, Generate.EXIT_CHECK_FAILED)


if __name__ == "__main__":
    unittest.main()
