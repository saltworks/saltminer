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
import tempfile
import unittest
import zipfile

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

# No deployment ships a default FieldValueColorCustomizations (JobManagerConfig.cs:82 is `{}`).
# This is the spike's own reference test input (Spikes/PBI-025-report-merge/fixture.py
# FIELD_VALUE_COLORS), used here as a representative config, not a claim that it is shipped.
REFERENCE_FIELD_VALUE_COLORS = {
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
            issue["Details"] = "**bold** text\n- one\n- two\n- three"

        renderer, colour_result = make_value_renderer(markdown_fields={"Details"},
                                                        field_value_colors={})
        document = docx.Document(TEMPLATE)
        merge_document(document, bind_roots(record), renderer=renderer)

        self.assertEqual(colour_result.markdown_fields_rendered, len(record["IssueDetails"]))

        bold_run = _run_with_text(document, "bold")
        self.assertIsNotNone(bold_run)
        self.assertIsNotNone(bold_run.find(qn("w:rPr")).find(qn("w:b")))

        paragraphs = [paragraph_text(p) for p in document.element.body.iter(qn("w:p"))]
        for word in ("one", "two", "three"):
            self.assertTrue(any(t.strip().startswith(f"• {word}") for t in paragraphs), word)

        with tempfile.TemporaryDirectory() as tmp:
            path = os.path.join(tmp, "out.docx")
            document.save(path)
            with zipfile.ZipFile(path) as z:
                xml = z.read("word/document.xml").decode("utf-8")
        self.assertEqual(xml.count("MERGEFIELD"), 0)


class FieldValueColoring(unittest.TestCase):
    def test_severity_and_teststatus_colored(self):
        record = sample_record()
        record["IssueDetails"][0]["Severity"] = "Critical"
        record["IssueDetails"][0]["TestStatus"] = "Pass"

        renderer, colour_result = make_value_renderer(
            markdown_fields=set(), field_value_colors=REFERENCE_FIELD_VALUE_COLORS)
        document = docx.Document(TEMPLATE)
        merge_document(document, bind_roots(record), renderer=renderer)

        self.assertTrue(_runs_with_color(document, "FF0000"))
        self.assertTrue(_runs_with_color(document, "008000"))
        self.assertEqual(colour_result.colored_values, 2)
        self.assertEqual(colour_result.unknown_colors, [])

    def test_colored_value_keeps_shared_static_text_inline(self):
        # Regression: colouring used to splice a whole new paragraph, dropping any static text
        # ("Product: ") that shared the field's paragraph. Product is not one of the two
        # colour-driven fields the shipped template uses alone (Severity, TestStatus), but
        # colouring matches on VALUE, not field name (ReportProcessor.cs:924-938), so any field
        # can trigger it.
        record = sample_record()
        record["IssueDetails"][0]["Product"] = "High"

        renderer, colour_result = make_value_renderer(
            markdown_fields=set(), field_value_colors={"high": "OrangeRed"})
        document = docx.Document(TEMPLATE)
        merge_document(document, bind_roots(record), renderer=renderer)

        self.assertEqual(colour_result.colored_values, 1)
        paragraphs = [paragraph_text(p) for p in document.element.body.iter(qn("w:p"))]
        self.assertTrue(any(t.strip() == "Product: High" for t in paragraphs), paragraphs)
        self.assertTrue(_runs_with_color(document, "FF4500"))

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
