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

# Unit tests against a stub `soffice`: a small Python script placed first on PATH that records its
# argv, pulls the macro's output URI out of the `macro:///...Export(...)` argument, and, per
# SM_STUB_SOFFICE_MODE, writes an empty PDF there, exits 1 printing "boom", exits 0 writing
# nothing, or sleeps past the caller's timeout. No real LibreOffice runs here; that is
# test_pdf_converter_live.py, against the built `-pdf` image.

import os
import shutil
import tempfile
import unittest
from pathlib import Path

from Reports import PdfConverter as pdf_converter

_STUB_SOFFICE = '''#!/usr/bin/env python3
import os
import re
import sys
import time
from urllib.parse import unquote, urlparse

argv_file = os.environ.get("SM_STUB_SOFFICE_ARGV_FILE")
if argv_file:
    with open(argv_file, "w") as f:
        f.write("\\n".join(sys.argv[1:]))

mode = os.environ.get("SM_STUB_SOFFICE_MODE", "success")
macro_arg = next((a for a in sys.argv if a.startswith("macro:///")), "")
uris = re.findall(r'"([^"]*)"', macro_arg)
out_uri = uris[1] if len(uris) > 1 else None

if mode == "hang":
    time.sleep(30)
    sys.exit(0)
if mode == "fail":
    sys.stderr.write("boom")
    sys.exit(1)
if mode == "empty":
    sys.exit(0)

if out_uri:
    out_path = unquote(urlparse(out_uri).path)
    with open(out_path, "wb") as f:
        f.write(b"%PDF-1.4 stub\\n")
sys.exit(0)
'''


class _StubSofficeTestCase(unittest.TestCase):
    def setUp(self):
        self._orig_tempdir = tempfile.tempdir
        self._orig_path = os.environ.get("PATH", "")
        self.work_dir = Path(tempfile.mkdtemp(prefix="pdfconv-work-"))
        # PdfConverter.convert_to_pdf parents its own throwaway directory on tempfile.tempdir, so
        # pointing that at a directory this test controls lets AC-5 assert it is empty afterward.
        self.watched_dir = Path(tempfile.mkdtemp(prefix="pdfconv-watched-"))
        tempfile.tempdir = str(self.watched_dir)
        self.bin_dir = self.work_dir / "bin"
        self.bin_dir.mkdir()
        self.output_dir = self.work_dir / "output"
        self.docx_path = self.work_dir / "sample.docx"
        self.docx_path.write_bytes(b"PK\x03\x04 stub docx contents")
        self.argv_file = self.work_dir / "argv.txt"

    def tearDown(self):
        tempfile.tempdir = self._orig_tempdir
        os.environ["PATH"] = self._orig_path
        os.environ.pop("SM_STUB_SOFFICE_MODE", None)
        os.environ.pop("SM_STUB_SOFFICE_ARGV_FILE", None)
        shutil.rmtree(self.work_dir, ignore_errors=True)
        shutil.rmtree(self.watched_dir, ignore_errors=True)

    def _install_stub(self, mode: str) -> None:
        stub = self.bin_dir / "soffice"
        stub.write_text(_STUB_SOFFICE)
        stub.chmod(0o755)
        os.environ["PATH"] = str(self.bin_dir) + os.pathsep + self._orig_path
        os.environ["SM_STUB_SOFFICE_MODE"] = mode
        os.environ["SM_STUB_SOFFICE_ARGV_FILE"] = str(self.argv_file)

    def _no_soffice_on_path(self) -> None:
        # An empty directory, not the real PATH: guarantees no soffice is found regardless of
        # what happens to be installed on the machine running the test.
        os.environ["PATH"] = str(self.bin_dir)


class ConvertToPdf(_StubSofficeTestCase):
    def test_success_writes_pdf_named_after_the_source(self):
        self._install_stub("success")
        result = pdf_converter.convert_to_pdf(self.docx_path, self.output_dir)
        self.assertEqual(result, self.output_dir / "sample.pdf")
        self.assertTrue(result.exists())

    def test_soffice_absent_raises_unavailable(self):
        self._no_soffice_on_path()
        with self.assertRaises(pdf_converter.ConverterUnavailable):
            pdf_converter.convert_to_pdf(self.docx_path, self.output_dir)

    def test_nonzero_exit_raises_failed_carrying_code_and_stderr(self):
        self._install_stub("fail")
        with self.assertRaises(pdf_converter.ConverterFailed) as ctx:
            pdf_converter.convert_to_pdf(self.docx_path, self.output_dir)
        self.assertIn("boom", str(ctx.exception))
        self.assertIn("1", str(ctx.exception))
        self.assertEqual(ctx.exception.returncode, 1)
        self.assertIn("boom", ctx.exception.stderr)

    def test_zero_exit_with_no_pdf_raises_failed(self):
        self._install_stub("empty")
        with self.assertRaises(pdf_converter.ConverterFailed) as ctx:
            pdf_converter.convert_to_pdf(self.docx_path, self.output_dir)
        self.assertEqual(ctx.exception.returncode, 0)

    def test_timeout_raises_failed_timed_out(self):
        self._install_stub("hang")
        with self.assertRaises(pdf_converter.ConverterFailed) as ctx:
            pdf_converter.convert_to_pdf(self.docx_path, self.output_dir, timeout=1)
        self.assertTrue(ctx.exception.timed_out)

    def test_no_leftover_directory_after_success_or_failure(self):
        self._install_stub("success")
        pdf_converter.convert_to_pdf(self.docx_path, self.output_dir)
        self._install_stub("fail")
        with self.assertRaises(pdf_converter.ConverterFailed):
            pdf_converter.convert_to_pdf(self.docx_path, self.output_dir)
        self.assertEqual(os.listdir(self.watched_dir), [])


class WriteAttachments(_StubSofficeTestCase):
    def test_word_writes_only_docx_and_never_calls_the_converter(self):
        self._install_stub("success")
        written = pdf_converter.write_attachments(self.docx_path, self.output_dir, "Word")
        self.assertEqual(written, [self.output_dir / "sample.docx"])
        self.assertTrue((self.output_dir / "sample.docx").exists())
        self.assertFalse((self.output_dir / "sample.pdf").exists())
        self.assertFalse(self.argv_file.exists(), "the stub soffice must not have been invoked")

    def test_pdf_writes_only_pdf(self):
        self._install_stub("success")
        written = pdf_converter.write_attachments(self.docx_path, self.output_dir, "Pdf")
        self.assertEqual(written, [self.output_dir / "sample.pdf"])
        self.assertTrue((self.output_dir / "sample.pdf").exists())
        self.assertFalse((self.output_dir / "sample.docx").exists())

    def test_all_writes_both(self):
        self._install_stub("success")
        written = pdf_converter.write_attachments(self.docx_path, self.output_dir, "All")
        self.assertEqual(written, [self.output_dir / "sample.pdf", self.output_dir / "sample.docx"])
        self.assertTrue((self.output_dir / "sample.pdf").exists())
        self.assertTrue((self.output_dir / "sample.docx").exists())

    def test_default_and_case_insensitive(self):
        self._install_stub("success")
        self.assertEqual(pdf_converter.write_attachments(self.docx_path, self.output_dir, None),
                          [self.output_dir / "sample.docx"])
        shutil.rmtree(self.output_dir)
        self.assertEqual(pdf_converter.write_attachments(self.docx_path, self.output_dir, "PDF"),
                          [self.output_dir / "sample.pdf"])

    def test_unrecognised_type_raises_value_error(self):
        with self.assertRaises(ValueError):
            pdf_converter.write_attachments(self.docx_path, self.output_dir, "Excel")


class CheckPdfConverter(_StubSofficeTestCase):
    def test_pdf_without_converter_logs_one_error_naming_pdf_tag_and_returns_false(self):
        self._no_soffice_on_path()
        with self.assertLogs(level="ERROR") as logs:
            result = pdf_converter.check_pdf_converter("Pdf")
        self.assertFalse(result)
        self.assertEqual(len(logs.records), 1)
        self.assertIn("-pdf", logs.records[0].getMessage())

    def test_all_without_converter_logs_one_error_naming_pdf_tag_and_returns_false(self):
        self._no_soffice_on_path()
        with self.assertLogs(level="ERROR") as logs:
            result = pdf_converter.check_pdf_converter("All")
        self.assertFalse(result)
        self.assertEqual(len(logs.records), 1)
        self.assertIn("-pdf", logs.records[0].getMessage())

    def test_word_logs_nothing_and_returns_true(self):
        self._no_soffice_on_path()
        with self.assertNoLogs(level="ERROR"):
            result = pdf_converter.check_pdf_converter("Word")
        self.assertTrue(result)

    def test_pdf_with_converter_present_logs_nothing_and_returns_true(self):
        self._install_stub("success")
        with self.assertNoLogs(level="ERROR"):
            result = pdf_converter.check_pdf_converter("Pdf")
        self.assertTrue(result)

    def test_unrecognised_type_logs_one_error_and_returns_false(self):
        with self.assertLogs(level="ERROR") as logs:
            result = pdf_converter.check_pdf_converter("Excel")
        self.assertFalse(result)
        self.assertEqual(len(logs.records), 1)


if __name__ == "__main__":
    unittest.main()
