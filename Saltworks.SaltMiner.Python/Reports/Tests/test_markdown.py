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
import unittest

from docx.oxml.ns import qn

from Reports.Markdown import render_markdown


def _text(run) -> str:
    return "".join(t.text or "" for t in run.iter(qn("w:t")))


def _runs(paragraph) -> list:
    return list(paragraph.iter(qn("w:r")))


def _has_flag(run, tag: str) -> bool:
    rpr = run.find(qn("w:rPr"))
    return rpr is not None and rpr.find(qn(tag)) is not None


class BoldAndLists(unittest.TestCase):
    def test_bold_then_two_list_items(self):
        paragraphs = render_markdown("**bold** text\n- one\n- two")
        self.assertEqual(len(paragraphs), 3)

        first_run = _runs(paragraphs[0])[0]
        self.assertEqual(_text(first_run), "bold")
        self.assertTrue(_has_flag(first_run, "w:b"))

        for paragraph, word in zip(paragraphs[1:], ("one", "two")):
            joined = "".join(_text(r) for r in _runs(paragraph))
            self.assertTrue(joined.strip().startswith("•"), joined)
            self.assertIn(word, joined)
            pstyle = paragraph.find(qn("w:pPr")).find(qn("w:pStyle"))
            self.assertEqual(pstyle.get(qn("w:val")), "ListParagraph")


class OrderedList(unittest.TestCase):
    def test_ordered_items_carry_no_number_and_keep_list_style(self):
        # .NET wrote no number for an ordered item (PBI-051 AC-11, ruled 2026-10-05).
        paragraphs = render_markdown("1. first step\n2. second step")
        self.assertEqual(len(paragraphs), 2)
        for paragraph, word in zip(paragraphs, ("first step", "second step")):
            joined = "".join(_text(r) for r in _runs(paragraph))
            self.assertEqual(joined, word)
            pstyle = paragraph.find(qn("w:pPr")).find(qn("w:pStyle"))
            self.assertEqual(pstyle.get(qn("w:val")), "ListParagraph")


class FencedCode(unittest.TestCase):
    def test_two_lines_courier_new(self):
        paragraphs = render_markdown("```\nline one\nline two\n```")
        self.assertEqual(len(paragraphs), 2)
        for paragraph, expected in zip(paragraphs, ("line one", "line two")):
            run = _runs(paragraph)[0]
            self.assertEqual(_text(run), expected)
            fonts = run.find(qn("w:rPr")).find(qn("w:rFonts"))
            self.assertEqual(fonts.get(qn("w:ascii")), "Courier New")


class BreaksAndEscapes(unittest.TestCase):
    def test_lone_newline_is_one_break(self):
        paragraphs = render_markdown("a\nb")
        self.assertEqual(len(paragraphs), 1)
        runs = _runs(paragraphs[0])
        breaks = [r for r in runs if list(r.iter(qn("w:br")))]
        self.assertEqual(len(breaks), 1)
        self.assertEqual("".join(_text(r) for r in runs), "ab")

    def test_escaped_asterisks_are_literal(self):
        paragraphs = render_markdown(r"\*not emphasis\*")
        self.assertEqual(len(paragraphs), 1)
        run = _runs(paragraphs[0])[0]
        self.assertEqual(_text(run), "*not emphasis*")
        self.assertFalse(_has_flag(run, "w:i"))

    def test_explicit_br_then_newline_is_still_one_break(self):
        # The markdown editor emits this exact shape for a carriage return. A literal <br>
        # already carries one break; the .NET pre-pass this mirrors never adds a second one for
        # the newline straight after it (ReportProcessor.cs:968, `(?<!<br>)\n`).
        paragraphs = render_markdown("line one<br>\nline two")
        self.assertEqual(len(paragraphs), 1)
        runs = _runs(paragraphs[0])
        breaks = [r for r in runs if list(r.iter(qn("w:br")))]
        self.assertEqual(len(breaks), 1)
        self.assertEqual("".join(_text(r) for r in runs), "line oneline two")


class Table(unittest.TestCase):
    def test_two_column_two_row_table(self):
        paragraphs = render_markdown("| A | B |\n| - | - |\n| 1 | 2 |\n| 3 | 4 |")
        self.assertEqual(len(paragraphs), 3)
        for paragraph in paragraphs:
            joined = "".join(_text(r) for r in _runs(paragraph))
            self.assertIn(" | ", joined)
        header_run = _runs(paragraphs[0])[0]
        self.assertTrue(_has_flag(header_run, "w:b"))


class EmptyInput(unittest.TestCase):
    def test_blank_and_none(self):
        self.assertEqual(render_markdown(""), [])
        self.assertEqual(render_markdown(None), [])


if __name__ == "__main__":
    unittest.main()
