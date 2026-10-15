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

# Word -> PDF conversion via headless LibreOffice (`soffice`), the licence-compatible converter
# ruled in PBI-049. The converter is a separately shipped binary, invoked over `subprocess`, never
# imported: `pandoc` is GPL and `docx2pdf` needs Microsoft Word.
#
# Conversion runs a small Basic macro rather than a plain `--convert-to`, because `--convert-to`
# never refreshes a document's fields or table-of-contents page numbers (requirement 12). The
# macro loads the document hidden, runs the field and index refresh twice (a TOC refresh can move
# the headings that follow it onto other pages, so one pass is not enough), exports to PDF, and
# writes any Basic error to a file Python reads back.
#
# Dispatching that macro takes two soffice calls, not one. A single call against a profile
# pre-seeded with the macro before LibreOffice's own first start never runs it: the first start
# sets OfficeRestartInProgress and restarts internally, the command-line macro argument is not
# re-dispatched after that restart, and first-start initialisation also rewrites script.xlb to
# list only the profile's own default module, unregistering whatever was seeded ahead of time.
# So the profile is built first, with --terminate_after_init and no macro argument, and only
# once it exists is the macro module written and script.xlb updated to list it.

from __future__ import annotations

import logging
import os
import shutil
import signal
import subprocess
import tempfile
import time
from pathlib import Path

DEFAULT_TIMEOUT_SECONDS = 120
DEFAULT_ATTACHMENT_TYPE = "Word"
_VALID_ATTACHMENT_TYPES = ("Word", "Pdf", "All")
_PDF_TAG_EXAMPLE = "jobmanager-3.6-pdf"


class ConverterError(RuntimeError):
    """Base for every PDF conversion failure. Never raised directly."""


class ConverterUnavailable(ConverterError):
    """`soffice` is not on PATH, or the OS could not exec it."""

    def __init__(self, executable: str = "soffice"):
        self.executable = executable
        super().__init__(f"{executable} is not available on PATH")


class ConverterFailed(ConverterError):
    """`soffice` ran but produced no PDF: non-zero exit, exit 0 with no PDF, or a timeout."""

    def __init__(self, source: Path, *, returncode: int | None = None, stderr: str = "",
                 detail: str = "", timed_out: bool = False, timeout: float | None = None):
        self.source = source
        self.returncode = returncode
        self.stderr = stderr
        self.detail = detail
        self.timed_out = timed_out
        if timed_out:
            message = f"soffice failed converting {source.name}: timed out after {timeout} s"
        else:
            message = f"soffice failed converting {source.name}: exit code {returncode}; stderr: {stderr}"
        if detail:
            message += f"; macro: {detail}"
        super().__init__(message)


# Overwrites the profile's own first-start script.xlb (which lists only its default `Module1`)
# to also list the seeded `SaltMinerPdf` module. Written only after the profile-init call has
# already created this file; writing it any earlier is the failure mode the module docstring
# describes.
_STANDARD_SCRIPT_XLB = '''<?xml version="1.0" encoding="UTF-8"?>
<!DOCTYPE library:library PUBLIC "-//OpenOffice.org//DTD OfficeDocument 1.0//EN" "library.dtd">
<library:library xmlns:library="http://openoffice.org/2000/library" library:name="Standard" library:readonly="false" library:passwordprotected="false">
 <library:element library:name="Module1"/>
 <library:element library:name="SaltMinerPdf"/>
</library:library>
'''

# `Export` loads the docx hidden, refreshes fields and every document index (running the whole
# refresh pass twice, since regenerating a TOC can move the headings after it onto other pages),
# exports to PDF under the `writer_pdf_Export` filter, and terminates. `On Error` writes the Basic
# error and line number to sErr and terminates rather than leaving soffice hung on a dialog.
_SALTMINER_PDF_XBA = '''<?xml version="1.0" encoding="UTF-8"?>
<!DOCTYPE script:module PUBLIC "-//OpenOffice.org//DTD OfficeDocument 1.0//EN" "module.dtd">
<script:module xmlns:script="http://openoffice.org/2000/script" script:name="SaltMinerPdf" script:language="StarBasic" script:moduleType="normal">Sub Export(sIn As String, sOut As String, sErr As String)
    On Error Goto ErrorHandler

    Dim oDesktop As Object
    Dim oDoc As Object
    Dim oLoadArgs(0) As New com.sun.star.beans.PropertyValue
    oLoadArgs(0).Name = &quot;Hidden&quot;
    oLoadArgs(0).Value = True

    oDesktop = createUnoService(&quot;com.sun.star.frame.Desktop&quot;)
    oDoc = oDesktop.loadComponentFromURL(sIn, &quot;_blank&quot;, 0, oLoadArgs())

    UpdateAll(oDoc)
    UpdateAll(oDoc)

    Dim oPdfArgs(0) As New com.sun.star.beans.PropertyValue
    oPdfArgs(0).Name = &quot;FilterName&quot;
    oPdfArgs(0).Value = &quot;writer_pdf_Export&quot;
    oDoc.storeToURL(sOut, oPdfArgs())

    oDoc.close(True)
    StarDesktop.terminate()
    Exit Sub

ErrorHandler:
    Dim iFile As Integer
    iFile = FreeFile
    Open sErr For Output As #iFile
    Print #iFile, &quot;Error &quot; &amp; Err &amp; &quot;: &quot; &amp; Error$ &amp; &quot; (line &quot; &amp; Erl &amp; &quot;)&quot;
    Close #iFile
    StarDesktop.terminate()
End Sub

Sub UpdateAll(oDoc As Object)
    Dim oIndexes As Object
    Dim i As Integer

    oDoc.getTextFields().refresh()

    oIndexes = oDoc.getDocumentIndexes()
    For i = 0 To oIndexes.Count - 1
        oIndexes.getByIndex(i).update()
    Next i

    oDoc.getTextFields().refresh()
End Sub
</script:module>
'''


def _seed_saltminer_module(profile_dir: Path) -> None:
    """Write the macro module and register it, into a profile the caller has already initialised."""
    standard_dir = profile_dir / "user" / "basic" / "Standard"
    (standard_dir / "SaltMinerPdf.xba").write_text(_SALTMINER_PDF_XBA, encoding="utf-8")
    (standard_dir / "script.xlb").write_text(_STANDARD_SCRIPT_XLB, encoding="utf-8")


def _soffice_base_argv(soffice: str, profile_dir: Path) -> list[str]:
    return [
        soffice, "--headless", "--norestore", "--nodefault",
        f"-env:UserInstallation={profile_dir.as_uri()}",
    ]


def _run_soffice(argv: list[str], env: dict, timeout: float) -> tuple[int, str]:
    """Run one soffice call, returning (returncode, decoded stderr).

    Raises `ConverterUnavailable` if the OS could not exec it, or lets `subprocess.TimeoutExpired`
    propagate (with the process group already killed) for the caller to turn into a
    `ConverterFailed(timed_out=True)` carrying its own `source`.
    """
    try:
        process = subprocess.Popen(argv, stdout=subprocess.PIPE, stderr=subprocess.PIPE,
                                    env=env, start_new_session=True)
    except (FileNotFoundError, PermissionError) as exc:
        raise ConverterUnavailable(argv[0]) from exc

    try:
        _, stderr_bytes = process.communicate(timeout=timeout)
    except subprocess.TimeoutExpired:
        os.killpg(process.pid, signal.SIGKILL)
        process.communicate()
        raise
    return process.returncode, stderr_bytes.decode(errors="replace")


def convert_to_pdf(docx_path: Path, output_dir: Path,
                    timeout: float = DEFAULT_TIMEOUT_SECONDS) -> Path:
    """Convert `docx_path` to a PDF written under `output_dir`, returning its path.

    Two soffice calls under one `timeout` budget: initialise a fresh profile, then seed and
    dispatch the refresh-and-export macro (see the module docstring for why one call cannot do
    this). Raises `ConverterUnavailable` when `soffice` is not on PATH, `ConverterFailed` when
    either call exits non-zero, profile initialisation leaves no script.xlb, no PDF is produced,
    or the combined budget is exceeded. Never lets `FileNotFoundError` or `CalledProcessError`
    escape unwrapped.
    """
    docx_path = Path(docx_path)
    output_dir = Path(output_dir)
    soffice = shutil.which("soffice")
    if soffice is None:
        raise ConverterUnavailable("soffice")

    started = time.monotonic()
    root = Path(tempfile.mkdtemp(prefix="sm-pdf-"))
    try:
        profile_dir = root / "profile"
        out_dir = root / "out"
        error_path = root / "error.txt"
        out_dir.mkdir()
        staged_docx = root / "in.docx"
        shutil.copyfile(docx_path, staged_docx)

        env = dict(os.environ)
        env["HOME"] = str(root)
        env["TMPDIR"] = str(root)

        try:
            profile_rc, profile_stderr = _run_soffice(
                _soffice_base_argv(soffice, profile_dir) + ["--terminate_after_init"],
                env, timeout,
            )
        except subprocess.TimeoutExpired as exc:
            raise ConverterFailed(docx_path, timed_out=True, timeout=timeout) from exc

        script_xlb = profile_dir / "user" / "basic" / "Standard" / "script.xlb"
        if profile_rc != 0 or not script_xlb.exists():
            raise ConverterFailed(docx_path, returncode=profile_rc, stderr=profile_stderr,
                                   detail="profile initialisation failed")

        _seed_saltminer_module(profile_dir)

        produced_path = out_dir / "in.pdf"
        macro = (
            f'macro:///Standard.SaltMinerPdf.Export("{staged_docx.as_uri()}",'
            f'"{produced_path.as_uri()}","{error_path.as_uri()}")'
        )
        remaining = timeout - (time.monotonic() - started)
        if remaining <= 0:
            raise ConverterFailed(docx_path, timed_out=True, timeout=timeout)

        try:
            macro_rc, macro_stderr = _run_soffice(
                _soffice_base_argv(soffice, profile_dir) + [macro], env, remaining,
            )
        except subprocess.TimeoutExpired as exc:
            raise ConverterFailed(docx_path, timed_out=True, timeout=timeout) from exc

        if macro_rc != 0 or not produced_path.exists():
            detail = error_path.read_text(encoding="utf-8", errors="replace") if error_path.exists() else ""
            raise ConverterFailed(docx_path, returncode=macro_rc, stderr=macro_stderr, detail=detail)

        final_path = output_dir / f"{docx_path.stem}.pdf"
        output_dir.mkdir(parents=True, exist_ok=True)
        shutil.move(str(produced_path), str(final_path))
        logging.info("[PdfConverter][convert_to_pdf] converted %s in %.1f s", docx_path.name,
                     time.monotonic() - started)
        return final_path
    finally:
        shutil.rmtree(root, ignore_errors=True)


def write_attachments(docx_path: Path, output_dir: Path, attachment_type: str = DEFAULT_ATTACHMENT_TYPE,
                       timeout: float = DEFAULT_TIMEOUT_SECONDS) -> list[Path]:
    """Write the report attachments `ReportAttachmentType` asks for, returning the paths written.

    `Word` copies `docx_path` into `output_dir` and never invokes the converter. `Pdf` converts
    only. `All` converts first, then copies, so a conversion failure leaves no `.docx` behind.
    Matching is case-insensitive, as `ReportProcessor.cs` compares it (`StringComparison.
    OrdinalIgnoreCase`); `None` or empty means `Word`, the shipped default.
    """
    docx_path = Path(docx_path)
    output_dir = Path(output_dir)
    normalized = (attachment_type or DEFAULT_ATTACHMENT_TYPE).strip().lower()

    written: list[Path] = []
    if normalized == "pdf":
        written.append(convert_to_pdf(docx_path, output_dir, timeout=timeout))
    elif normalized == "all":
        written.append(convert_to_pdf(docx_path, output_dir, timeout=timeout))
        written.append(_copy_docx(docx_path, output_dir))
    elif normalized == "word":
        written.append(_copy_docx(docx_path, output_dir))
    else:
        raise ValueError(
            f"ReportAttachmentType must be one of {_VALID_ATTACHMENT_TYPES}, got {attachment_type!r}"
        )
    return written


def _copy_docx(docx_path: Path, output_dir: Path) -> Path:
    output_dir.mkdir(parents=True, exist_ok=True)
    final_path = output_dir / docx_path.name
    shutil.copyfile(docx_path, final_path)
    return final_path


def check_pdf_converter(attachment_type: str) -> bool:
    """Whether this image variant can satisfy `attachment_type`, logging a named error if not.

    `Word` needs no converter and always returns `True`. `Pdf` and `All` return `True` when
    `soffice` is on PATH, or `False` and log one error naming the `-pdf` tag when it is not. An
    unrecognised value logs one error naming the three valid values and returns `False`. Never
    raises: the caller (PBI-051, at container start) must be free to carry on serving Word
    reports even when PDF is unavailable.
    """
    normalized = (attachment_type or DEFAULT_ATTACHMENT_TYPE).strip().lower()
    if normalized == "word":
        return True
    if normalized not in ("pdf", "all"):
        logging.error(
            "[PdfConverter][check_pdf_converter] ReportAttachmentType is %r but must be one of %s",
            attachment_type, _VALID_ATTACHMENT_TYPES,
        )
        return False
    if shutil.which("soffice") is not None:
        return True
    logging.error(
        "[PdfConverter][check_pdf_converter] ReportAttachmentType is %s but soffice is not on the "
        "path: this deployment runs the plain JobManager image variant. Set "
        "SM_JOBMANAGER_IMAGE_VERSION to the -pdf tag, for example %s, for PDF reports. Word reports "
        "are unaffected.", attachment_type, _PDF_TAG_EXAMPLE,
    )
    return False
