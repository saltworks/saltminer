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
import os
import unittest

import docx
from docx.oxml.ns import qn

from Reports.Tests.sample_record import sample_record
from Reports.Tests.synthetic import paragraph_text
from Reports.ValueRenderer import make_value_renderer
from Reports.WordMerge import bind_roots, merge_document

# Saltworks.SaltMiner.Python/Reports/Tests -> the repository root is three levels up from Python.
_HERE = os.path.dirname(os.path.abspath(__file__))
TEMPLATE = os.path.normpath(os.path.join(
    _HERE, "..", "..", "..", "Saltworks.SaltMiner.JobManager", "Saltworks.SaltMiner.JobManager",
    "TemplateDefaults", "Saltworks", "SaltworksTemplate.docx"))

# The seven-name shipped configuration (Spikes/PBI-025-report-merge/fixture.py FIELD_VALUE_COLORS).
SHIPPED_FIELD_VALUE_COLORS = {
    "critical": "Red",
    "high": "OrangeRed",
    "medium": "Goldenrod",
    "low": "Green",
    "information": "Gray",
    "fail": "Red",
    "pass": "Green",
}


def _run_with_text(document, text):
    for run in document.element.body.iter(qn("w:r")):
        if "".join(t.text or "" for t in run.iter(qn("w:t"))) == text:
            return run
    return None


def _runs_with_color(document, hex_value):
    found = []
    for run in document.element.body.iter(qn("w:r")):
        rpr = run.find(qn("w:rPr"))
        color = None if rpr is None else rpr.find(qn("w:color"))
        if color is not None and color.get(qn("w:val")) == hex_value:
            found.append(run)
    return found


class MarkdownField(unittest.TestCase):
    def test_details_field_renders_markdown_in_the_shipped_template(self):
        record = sample_record()
        for issue in record["IssueDetails"]:
            issue["Details"] = "**bold** text\n- one\n- two"

        renderer, colour_result = make_value_renderer(markdown_fields={"Details"},
                                                        field_value_colors={})
        document = docx.Document(TEMPLATE)
        merge_document(document, bind_roots(record), renderer=renderer)

        self.assertEqual(colour_result.markdown_fields_rendered, len(record["IssueDetails"]))

        bold_run = _run_with_text(document, "bold")
        self.assertIsNotNone(bold_run)
        self.assertIsNotNone(bold_run.find(qn("w:rPr")).find(qn("w:b")))

        paragraphs = [paragraph_text(p) for p in document.element.body.iter(qn("w:p"))]
        self.assertTrue(any(t.strip().startswith("• one") for t in paragraphs))
        self.assertTrue(any(t.strip().startswith("• two") for t in paragraphs))


class FieldValueColoring(unittest.TestCase):
    def test_severity_and_teststatus_colored(self):
        record = sample_record()
        record["IssueDetails"][0]["Severity"] = "Critical"
        record["IssueDetails"][0]["TestStatus"] = "Pass"

        renderer, colour_result = make_value_renderer(
            markdown_fields=set(), field_value_colors=SHIPPED_FIELD_VALUE_COLORS)
        document = docx.Document(TEMPLATE)
        merge_document(document, bind_roots(record), renderer=renderer)

        self.assertTrue(_runs_with_color(document, "FF0000"))
        self.assertTrue(_runs_with_color(document, "008000"))
        self.assertEqual(colour_result.colored_values, 2)
        self.assertEqual(colour_result.unknown_colors, [])

    def test_unknown_color_name_is_reported_and_left_plain(self):
        record = sample_record()
        record["IssueDetails"][0]["Severity"] = "Weird"

        renderer, colour_result = make_value_renderer(
            markdown_fields=set(), field_value_colors={"weird": "NotAColour"})
        document = docx.Document(TEMPLATE)
        merge_document(document, bind_roots(record), renderer=renderer)

        self.assertEqual(colour_result.unknown_colors, ["NotAColour"])
        run = _run_with_text(document, "Weird")
        self.assertIsNotNone(run)
        rpr = run.find(qn("w:rPr"))
        self.assertTrue(rpr is None or rpr.find(qn("w:color")) is None)


if __name__ == "__main__":
    unittest.main()
