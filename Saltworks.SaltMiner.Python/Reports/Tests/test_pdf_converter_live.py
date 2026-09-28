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

# Integration test against the real, built `-pdf` JobManager image. Skipped unless
# SM_SOFFICE_IMAGE names it (for example jobmanager-3.6-pdf, or a local
# `docker build --target os-pdf .dockerfiles/Dockerfile-jobmanager` tag). docker_soffice.sh
# stands in for `soffice` on PATH, so PdfConverter.convert_to_pdf runs unmodified against the
# real converter inside that image.
#
#   docker build --target os-pdf -f .dockerfiles/Dockerfile-jobmanager -t jobmanager-pdf-local .
#   SM_SOFFICE_IMAGE=jobmanager-pdf-local \
#   SM_REPORT_SPIKE_DOCX=<tracker checkout>/Spikes/PBI-025-report-merge/output/engagement-report.docx \
#   python -m pytest Reports/Tests/test_pdf_converter_live.py -q -s
#
# AC-9's reference figures (page count, character count, embedded fonts) are the spike's own
# measurement of Spikes/PBI-025-report-merge/output/engagement-report.pdf
# (FINDINGS.md, 2026-09-22): 8 pages, 4,670 characters, five embedded families.

import os
import shutil
import subprocess
import tempfile
import unittest
from pathlib import Path

from docx import Document

from Reports import PdfConverter as pdf_converter
from Reports.Tests import synthetic as syn

SM_SOFFICE_IMAGE = os.environ.get("SM_SOFFICE_IMAGE", "")
SM_REPORT_SPIKE_DOCX = os.environ.get("SM_REPORT_SPIKE_DOCX", "")
DOCKER_SOFFICE = Path(__file__).parent / "docker_soffice.sh"

REFERENCE_PAGE_COUNT = 8
REFERENCE_CHAR_COUNT = 4670
REFERENCE_FONTS = {"Carlito", "Carlito-Bold", "DejaVuSerif", "LiberationMono", "OpenSymbol"}

_POPPLER_IMAGE = "sm-pdf-converter-test-poppler"


def _poppler(pdf_path: Path, *args: str) -> str:
    """Run a poppler-utils command against `pdf_path` in a throwaway container; the host running
    this test has no poppler (`which pdftotext` is empty on the reference workstation)."""
    result = subprocess.run(
        ["docker", "run", "--rm", "-v", f"{pdf_path.parent}:/work", _POPPLER_IMAGE,
         *args, f"/work/{pdf_path.name}"],
        capture_output=True, text=True, check=True,
    )
    return result.stdout


def _pdfinfo_pages(pdf_path: Path) -> int:
    info = _poppler(pdf_path, "pdfinfo")
    line = next(l for l in info.splitlines() if l.startswith("Pages:"))
    return int(line.split()[-1])


def _pdftotext_plain(pdf_path: Path) -> str:
    """No `-layout`: matches the PBI's own `pdftotext <out> - | wc -c` / `grep -c .` measurement
    for AC-1 and AC-9. `-layout` pads for column alignment and inflates the character count."""
    result = subprocess.run(
        ["docker", "run", "--rm", "-v", f"{pdf_path.parent}:/work", _POPPLER_IMAGE,
         "pdftotext", f"/work/{pdf_path.name}", "-"],
        capture_output=True, text=True, check=True,
    )
    return result.stdout


def _pdftotext_layout(pdf_path: Path, first: int, last: int) -> str:
    """`-layout`, per page: AC-14's own wording (`pdftotext -layout -f n -l n`)."""
    result = subprocess.run(
        ["docker", "run", "--rm", "-v", f"{pdf_path.parent}:/work", _POPPLER_IMAGE,
         "pdftotext", "-layout", "-f", str(first), "-l", str(last), f"/work/{pdf_path.name}", "-"],
        capture_output=True, text=True, check=True,
    )
    return result.stdout


def _pdffonts_families(pdf_path: Path) -> set:
    output = _poppler(pdf_path, "pdffonts")
    lines = [line for line in output.splitlines()[2:] if line.strip()]
    names = {line.split()[0] for line in lines}
    # A subsetted embedded font is named "ABCDEF+FamilyName" (six-letter tag, per the PDF spec);
    # strip it so this compares family names, the same names pdffonts printed on the reference.
    return {name.split("+", 1)[1] if len(name) > 7 and name[6] == "+" else name for name in names}


def _build_toc_fixture() -> Document:
    doc = Document()
    syn.add_wrong_toc_fixture(doc, [
        ("_Toc1", "Heading One", 9),
        ("_Toc2", "Heading Two", 9),
        ("_Toc3", "Heading Three", 9),
    ])
    doc.add_page_break()
    syn.add_heading_with_bookmark(doc, 1, "_Toc1", "Heading One", level=1)
    doc.add_paragraph("Body text for section one.")
    doc.add_page_break()
    syn.add_heading_with_bookmark(doc, 2, "_Toc2", "Heading Two", level=1)
    doc.add_paragraph("Body text for section two.")
    doc.add_page_break()
    doc.add_paragraph("Filler page to push the next heading to page 5.")
    doc.add_page_break()
    syn.add_heading_with_bookmark(doc, 3, "_Toc3", "Heading Three", level=1)
    doc.add_paragraph("Body text for section three.")
    return doc


@unittest.skipUnless(SM_SOFFICE_IMAGE, "SM_SOFFICE_IMAGE is not set")
class LiveConversion(unittest.TestCase):
    @classmethod
    def setUpClass(cls):
        subprocess.run(
            ["docker", "build", "-t", _POPPLER_IMAGE, "-"],
            input="FROM alpine:3.22\nRUN apk add --no-cache poppler-utils\n",
            text=True, capture_output=True, check=True,
        )
        cls._orig_tempdir = tempfile.tempdir
        cls._orig_path = os.environ.get("PATH", "")
        cls.root = Path(tempfile.mkdtemp(prefix="pdfconv-live-"))
        tempfile.tempdir = str(cls.root)
        cls.bin_dir = cls.root / "bin"
        cls.bin_dir.mkdir()
        stub = cls.bin_dir / "soffice"
        shutil.copy(DOCKER_SOFFICE, stub)
        stub.chmod(0o755)
        os.environ["PATH"] = str(cls.bin_dir) + os.pathsep + cls._orig_path
        os.environ["SM_SOFFICE_MOUNT_ROOT"] = str(cls.root)

    @classmethod
    def tearDownClass(cls):
        tempfile.tempdir = cls._orig_tempdir
        os.environ["PATH"] = cls._orig_path
        os.environ.pop("SM_SOFFICE_MOUNT_ROOT", None)
        shutil.rmtree(cls.root, ignore_errors=True)

    @unittest.skipUnless(SM_REPORT_SPIKE_DOCX, "SM_REPORT_SPIKE_DOCX is not set")
    def test_spike_reference_conversion(self):
        """AC-1: a real conversion produces a non-empty, paginated PDF. AC-9: it matches the
        spike's reference on fonts, page count, and character count within 1 percent."""
        output_dir = self.root / "ac1-out"
        result = pdf_converter.convert_to_pdf(Path(SM_REPORT_SPIKE_DOCX), output_dir)
        self.assertTrue(result.exists())

        pages = _pdfinfo_pages(result)
        self.assertGreater(pages, 0)
        self.assertEqual(pages, REFERENCE_PAGE_COUNT)

        text = _pdftotext_plain(result)
        non_whitespace = len(text.strip())
        self.assertGreater(non_whitespace, 0)
        char_count = len(text)
        self.assertLessEqual(abs(char_count - REFERENCE_CHAR_COUNT) / REFERENCE_CHAR_COUNT, 0.01,
                              f"got {char_count} characters, reference is {REFERENCE_CHAR_COUNT}")

        self.assertEqual(_pdffonts_families(result), REFERENCE_FONTS)

    def test_toc_fixture_pages_refresh(self):
        """AC-14: the deliberately wrong cached TOC pages (9, 9, 9) come back as the headings'
        real pages (2, 3, 5); a control run through plain --convert-to proves the fixture would
        still read 9 without the refresh this item adds."""
        fixture_path = self.root / "toc-fixture.docx"
        _build_toc_fixture().save(fixture_path)

        control_dir = self.root / "control-out"
        control_dir.mkdir()
        subprocess.run(
            ["soffice", "--headless", "--norestore",
             f"-env:UserInstallation={(self.root / 'control-profile').as_uri()}",
             "--convert-to", "pdf", "--outdir", str(control_dir), str(fixture_path)],
            check=True, capture_output=True,
        )
        control_toc = _pdftotext_layout(control_dir / "toc-fixture.pdf", 1, 1)
        for heading in ("Heading One", "Heading Two", "Heading Three"):
            self.assertRegex(control_toc, rf"{heading}[.\s]*9\b",
                              "plain --convert-to must not refresh the TOC")

        output_dir = self.root / "ac14-out"
        result = pdf_converter.convert_to_pdf(fixture_path, output_dir)
        self.assertEqual(_pdfinfo_pages(result), 5)
        toc_text = _pdftotext_layout(result, 1, 1)
        for heading, page in (("Heading One", 2), ("Heading Two", 3), ("Heading Three", 5)):
            self.assertRegex(toc_text, rf"{heading}[.\s]*{page}\b",
                              f"TOC entry for {heading!r} must read page {page}")
            page_text = _pdftotext_layout(result, page, page)
            self.assertIn(heading, page_text, f"{heading!r} must actually be on page {page}")


if __name__ == "__main__":
    unittest.main()
