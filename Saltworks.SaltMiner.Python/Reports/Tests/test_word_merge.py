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
from Reports.Tests.synthetic import add_field, paragraph_text
from Reports.WordMerge import (
    FieldContext,
    TemplateStructureError,
    bind_roots,
    fill_template,
    merge_document,
)

# Saltworks.SaltMiner.Python/Reports/Tests -> the repository root is three levels up from Python.
_HERE = os.path.dirname(os.path.abspath(__file__))
TEMPLATE = os.path.normpath(os.path.join(
    _HERE, "..", "..", "..", "Saltworks.SaltMiner.JobManager", "Saltworks.SaltMiner.JobManager",
    "TemplateDefaults", "Saltworks", "SaltworksTemplate.docx"))


class ShippedTemplate(unittest.TestCase):
    @classmethod
    def setUpClass(cls):
        cls.tmp = tempfile.TemporaryDirectory()
        cls.out = os.path.join(cls.tmp.name, "out.docx")
        cls.result = fill_template(TEMPLATE, cls.out, sample_record())
        cls.document = docx.Document(cls.out)
        with zipfile.ZipFile(cls.out) as z:
            cls.xml = z.read("word/document.xml").decode("utf-8")

    @classmethod
    def tearDownClass(cls):
        cls.tmp.cleanup()

    def _paragraphs(self):
        return [paragraph_text(p) for p in self.document.element.body.iter(qn("w:p"))]

    def test_no_instruction_left(self):
        self.assertEqual(self.xml.count("MERGEFIELD"), 0)
        self.assertEqual(self.xml.count("«"), 0)
        self.assertEqual(self.result.unmatched, [])

    def test_group_counts(self):
        groups = self.result.groups
        self.assertEqual(groups["Section1"], 1)
        self.assertEqual(groups["AssetTocs"], 2)
        self.assertEqual(groups["IssueTocs"], 3)
        self.assertEqual(groups["IssueDetails"], 3)
        self.assertEqual(groups["SeverityGroups"], 6)

    def test_name_resolves_per_region(self):
        paragraphs = self._paragraphs()
        self.assertTrue(any("ENG-NAME" in t for t in paragraphs))
        for asset in ("ASSET-A", "ASSET-B"):
            self.assertTrue(any(t.startswith(asset) for t in paragraphs), asset)
        for issue in ("ISSUE-1", "ISSUE-2", "ISSUE-3"):
            self.assertIn(issue, paragraphs)

    def _size_of(self, text):
        for run in self.document.element.body.iter(qn("w:r")):
            if "".join(t.text or "" for t in run.iter(qn("w:t"))) == text:
                rpr = run.find(qn("w:rPr"))
                size = None if rpr is None else rpr.find(qn("w:sz"))
                return None if size is None else size.get(qn("w:val"))
        self.fail(f"no run with text {text}")

    def test_run_sizes_kept(self):
        self.assertEqual(self._size_of("ENG-NAME"), "44")
        self.assertEqual(self._size_of("ENG-STATE"), "44")
        for text in ("ENG-CUSTOMER", "ENG-TIMESTAMP", "ENG-TESTER"):
            self.assertEqual(self._size_of(text), "28", text)
        self.assertEqual(self._size_of("ISSUE-1-Description"), "24")

    def test_single_sectpr(self):
        body = self.document.element.body
        self.assertEqual(len(list(body.iter(qn("w:sectPr")))), 1)
        source = docx.Document(TEMPLATE).element.body.find(qn("w:sectPr")).find(qn("w:pgSz"))
        output = body.find(qn("w:sectPr")).find(qn("w:pgSz"))
        self.assertEqual(dict(output.attrib), dict(source.attrib))

    def test_repeated_regions_carry_no_duplicate_ids(self):
        ids = [el.get("{http://schemas.microsoft.com/office/word/2010/wordml}paraId")
               for el in self.document.element.body.iter(qn("w:p"))]
        ids = [i for i in ids if i]
        self.assertEqual(len(ids), len(set(ids)))
        bookmark_ids = [el.get(qn("w:id")) for el in self.document.element.body.iter(qn("w:bookmarkStart"))]
        self.assertEqual(len(bookmark_ids), len(set(bookmark_ids)))


def _blank_document():
    document = docx.Document()
    for paragraph in list(document.paragraphs):
        paragraph._p.getparent().remove(paragraph._p)
    return document


class Unsupported(unittest.TestCase):
    def test_marker_in_cell_reported(self):
        document = _blank_document()
        table = document.add_table(rows=1, cols=1)
        add_field(table.cell(0, 0).paragraphs[0], "TableStart:Rows")
        result = merge_document(document, bind_roots({}))
        self.assertIn("TableStart:Rows", result.unsupported_markers)

    def test_unclosed_raises(self):
        document = _blank_document()
        add_field(document.add_paragraph(), "TableStart:X")
        with self.assertRaises(TemplateStructureError) as caught:
            merge_document(document, bind_roots({}))
        self.assertIn("TableStart:X", str(caught.exception))

    def test_mismatched_end_raises(self):
        document = _blank_document()
        add_field(document.add_paragraph(), "TableStart:X")
        add_field(document.add_paragraph(), "TableEnd:Y")
        with self.assertRaises(TemplateStructureError) as caught:
            merge_document(document, bind_roots({}))
        self.assertIn("TableEnd:Y", str(caught.exception))

    def test_simple_field_listed_and_left_in_place(self):
        document = _blank_document()
        paragraph = document.add_paragraph()
        from docx.oxml import OxmlElement
        simple = OxmlElement("w:fldSimple")
        simple.set(qn("w:instr"), " MERGEFIELD  Legacy ")
        paragraph._p.append(simple)
        result = merge_document(document, bind_roots({}))
        self.assertEqual(result.unsupported_fields, ["Legacy"])
        self.assertEqual(len(list(document.element.body.iter(qn("w:fldSimple")))), 1)


class Unmatched(unittest.TestCase):
    def _fill(self, keep):
        record = sample_record()
        del record["Summary"]
        document = docx.Document(TEMPLATE)
        result = merge_document(document, bind_roots(record), keep_unmatched=keep)
        with tempfile.TemporaryDirectory() as tmp:
            path = os.path.join(tmp, "o.docx")
            document.save(path)
            with zipfile.ZipFile(path) as z:
                xml = z.read("word/document.xml").decode("utf-8")
        return result, xml

    def test_keep_and_blank(self):
        result, xml = self._fill(keep=True)
        self.assertIn("MERGEFIELD  Summary", xml)
        self.assertIn("Summary", result.unmatched)
        result, xml = self._fill(keep=False)
        self.assertNotIn("MERGEFIELD", xml)
        self.assertIn("Summary", result.unmatched)


class Renderer(unittest.TestCase):
    def test_stub_renderer_is_given_context_and_its_block_replaces_the_paragraph(self):
        from docx.oxml import OxmlElement
        seen = []

        def stub(ctx: FieldContext):
            seen.append(ctx)
            block = OxmlElement("w:p")
            run = OxmlElement("w:r")
            text = OxmlElement("w:t")
            text.text = f"BLOCK-{ctx.value}"
            run.append(text)
            block.append(run)
            return [block]

        document = _blank_document()
        add_field(document.add_paragraph(), "TableStart:Section1")
        add_field(document.add_paragraph(), "Body")
        add_field(document.add_paragraph(), "TableEnd:Section1")
        merge_document(document, bind_roots({"Body": "x"}), renderer=stub)
        self.assertEqual(seen[0].name, "Body")
        self.assertFalse(seen[0].in_table_cell)
        paragraphs = [paragraph_text(p) for p in document.element.body.iter(qn("w:p"))]
        self.assertEqual(paragraphs, ["BLOCK-x"])

    def test_block_result_in_last_cell_paragraph_keeps_cell_valid(self):
        from docx.oxml import OxmlElement

        def stub(ctx):
            table = OxmlElement("w:tbl")
            return [table]

        document = _blank_document()
        add_field(document.add_paragraph(), "TableStart:Section1")
        table = document.add_table(rows=1, cols=1)
        add_field(table.cell(0, 0).paragraphs[0], "Body")
        add_field(document.add_paragraph(), "TableEnd:Section1")
        merge_document(document, bind_roots({"Body": "x"}), renderer=stub)
        cell = document.element.body.find(qn("w:tbl")).find(qn("w:tr")).find(qn("w:tc"))
        self.assertEqual(cell[-1].tag, qn("w:p"))


class GroupResolution(unittest.TestCase):
    def test_same_group_name_under_two_parents_reads_each_own_list(self):
        document = _blank_document()
        for text in ("TableStart:Section1", "TableStart:A", "TableStart:Items"):
            add_field(document.add_paragraph(), text)
        add_field(document.add_paragraph(), "Label")
        for text in ("TableEnd:Items", "TableEnd:A", "TableStart:B", "TableStart:Items"):
            add_field(document.add_paragraph(), text)
        add_field(document.add_paragraph(), "Label")
        for text in ("TableEnd:Items", "TableEnd:B", "TableEnd:Section1"):
            add_field(document.add_paragraph(), text)
        record = {
            "A": [{"Items": [{"Label": "a1"}, {"Label": "a2"}]}],
            "B": [{"Items": [{"Label": "b1"}]}],
        }
        result = merge_document(document, bind_roots(record))
        texts = [paragraph_text(p) for p in document.element.body.iter(qn("w:p"))]
        self.assertEqual(texts, ["a1", "a2", "b1"])
        self.assertEqual(result.groups["Items"], 3)

    def test_group_key_absent_renders_zero_times_and_is_reported(self):
        document = _blank_document()
        add_field(document.add_paragraph(), "TableStart:Section1")
        add_field(document.add_paragraph(), "TableStart:Gone")
        add_field(document.add_paragraph(), "TableEnd:Gone")
        add_field(document.add_paragraph(), "TableEnd:Section1")
        result = merge_document(document, bind_roots({}))
        self.assertEqual(result.groups_missing, ["Gone"])


if __name__ == "__main__":
    unittest.main()
