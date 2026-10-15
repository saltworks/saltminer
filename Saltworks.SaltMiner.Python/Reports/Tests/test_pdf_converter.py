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

# Unit tests against a stub `soffice`: a small Python script placed first on PATH that appends
# each call's argv to a file. Given `--terminate_after_init` (the profile-init call), it writes
# script.xlb listing the profile's own default module, as LibreOffice does, unless
# SM_STUB_SOFFICE_SKIP_PROFILE_INIT is set. Given a `macro:///` argument (the second call), it
# checks the seed landed (SaltMinerPdf.xba exists, script.xlb lists it) and records the result,
# then, per SM_STUB_SOFFICE_MODE, writes an empty PDF, exits 1 printing "boom", exits 0 writing
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
from pathlib import Path
from urllib.parse import unquote, urlparse

argv_file = os.environ.get("SM_STUB_SOFFICE_ARGV_FILE")
if argv_file:
    with open(argv_file, "a") as f:
        f.write("\\t".join(sys.argv[1:]) + "\\n")

profile_uri = next((a.split("=", 1)[1] for a in sys.argv if a.startswith("-env:UserInstallation=")), None)
profile_dir = Path(unquote(urlparse(profile_uri).path)) if profile_uri else None

if "--terminate_after_init" in sys.argv:
    if not os.environ.get("SM_STUB_SOFFICE_SKIP_PROFILE_INIT"):
        standard_dir = profile_dir / "user" / "basic" / "Standard"
        standard_dir.mkdir(parents=True, exist_ok=True)
        (standard_dir / "script.xlb").write_text(
            '<library:library><library:element library:name="Module1"/></library:library>'
        )
    sys.exit(0)

macro_arg = next((a for a in sys.argv if a.startswith("macro:///")), "")

seed_check_file = os.environ.get("SM_STUB_SOFFICE_SEED_CHECK_FILE")
if seed_check_file and profile_dir:
    standard_dir = profile_dir / "user" / "basic" / "Standard"
    xba_ok = (standard_dir / "SaltMinerPdf.xba").exists()
    xlb_path = standard_dir / "script.xlb"
    xlb_text = xlb_path.read_text() if xlb_path.exists() else ""
    with open(seed_check_file, "w") as f:
        f.write("OK" if xba_ok and "SaltMinerPdf" in xlb_text else "MISSING")

mode = os.environ.get("SM_STUB_SOFFICE_MODE", "success")
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
        self.seed_check_file = self.work_dir / "seed_check.txt"

    def tearDown(self):
        tempfile.tempdir = self._orig_tempdir
        os.environ["PATH"] = self._orig_path
        for var in ("SM_STUB_SOFFICE_MODE", "SM_STUB_SOFFICE_ARGV_FILE",
                    "SM_STUB_SOFFICE_SKIP_PROFILE_INIT", "SM_STUB_SOFFICE_SEED_CHECK_FILE"):
            os.environ.pop(var, None)
        shutil.rmtree(self.work_dir, ignore_errors=True)
        shutil.rmtree(self.watched_dir, ignore_errors=True)

    def _install_stub(self, mode: str = "success", *, skip_profile_init: bool = False) -> None:
        stub = self.bin_dir / "soffice"
        stub.write_text(_STUB_SOFFICE)
        stub.chmod(0o755)
        os.environ["PATH"] = str(self.bin_dir) + os.pathsep + self._orig_path
        os.environ["SM_STUB_SOFFICE_MODE"] = mode
        os.environ["SM_STUB_SOFFICE_ARGV_FILE"] = str(self.argv_file)
        os.environ["SM_STUB_SOFFICE_SEED_CHECK_FILE"] = str(self.seed_check_file)
        if skip_profile_init:
            os.environ["SM_STUB_SOFFICE_SKIP_PROFILE_INIT"] = "1"

    def _no_soffice_on_path(self) -> None:
        # An empty directory, not the real PATH: guarantees no soffice is found regardless of
        # what happens to be installed on the machine running the test.
        os.environ["PATH"] = str(self.bin_dir)

    def _argv_calls(self) -> list[str]:
        if not self.argv_file.exists():
            return []
        return self.argv_file.read_text().splitlines()


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

    def test_nonzero_exit_on_macro_call_raises_failed_carrying_code_and_stderr(self):
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

    def test_profile_init_failure_raises_failed_and_never_calls_the_macro(self):
        self._install_stub("success", skip_profile_init=True)
        with self.assertRaises(pdf_converter.ConverterFailed) as ctx:
            pdf_converter.convert_to_pdf(self.docx_path, self.output_dir)
        self.assertIn("profile initialisation failed", ctx.exception.detail)
        calls = self._argv_calls()
        self.assertEqual(len(calls), 1, "the macro call must not run when profile init left no script.xlb")
        self.assertIn("--terminate_after_init", calls[0])

    def test_two_calls_profile_then_macro_seed_lands_between_them(self):
        self._install_stub("success")
        pdf_converter.convert_to_pdf(self.docx_path, self.output_dir)
        calls = self._argv_calls()
        self.assertEqual(len(calls), 2)
        self.assertIn("--terminate_after_init", calls[0])
        self.assertNotIn("macro:///", calls[0])
        self.assertNotIn("--terminate_after_init", calls[1])
        self.assertIn("macro:///", calls[1])
        self.assertEqual(self.seed_check_file.read_text(), "OK",
                          "SaltMinerPdf.xba and script.xlb must both be seeded before the macro call")

    def test_macro_uris_name_the_staged_file_not_the_callers_name(self):
        self._install_stub("success")
        pdf_converter.convert_to_pdf(self.docx_path, self.output_dir)
        macro_call = self._argv_calls()[1]
        self.assertIn("in.docx", macro_call)
        self.assertNotIn("sample.docx", macro_call)


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
