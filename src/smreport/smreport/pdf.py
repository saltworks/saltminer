"""PDF conversion via LibreOffice headless, plus font availability / substitution.

LibreOffice silently substitutes fonts it does not have.  To keep the old renderer's
"font X replaced with Y" warnings we (a) list the fonts the document asks for, (b) check
them against fontconfig when ``fc-list`` is available, and (c) apply any configured
substitutions by rewriting ``w:rFonts`` in a scratch copy used only for the PDF.
"""

from __future__ import annotations

import os
import re
import shutil
import subprocess
import sys
import tempfile
import zipfile
from typing import Callable

_RFONTS_TAG_RE = re.compile(r"<w:rFonts\b[^>]*>")
_RFONTS_ATTR_RE = re.compile(r'(w:(?:ascii|hAnsi|cs|eastAsia))="([^"]*)"')
_THEME_FONT_RE = re.compile(r'<a:(?:latin|ea|cs)\s+typeface="([^"]*)"')
_XML_PARTS_RE = re.compile(r"^word/(document|styles|numbering|header\d*|footer\d*|footnotes|endnotes)\.xml$")


class PdfError(Exception):
    pass


def normalize_substitutions(raw) -> dict[str, str]:
    """Accept ``{"Calibri": "Carlito"}`` or the C# shape ``{"Calibri": {"Font": .., "Bold": .., "Italic": ..}}``."""
    out: dict[str, str] = {}
    for k, v in (raw or {}).items():
        if isinstance(v, dict):
            v = v.get("Font") or v.get("font")
        if v:
            out[str(k)] = str(v)
    return out


def document_fonts(docx_path: str) -> set[str]:
    fonts: set[str] = set()
    with zipfile.ZipFile(docx_path) as z:
        for name in z.namelist():
            if _XML_PARTS_RE.match(name):
                xml = z.read(name).decode("utf8", "replace")
                for tag in _RFONTS_TAG_RE.findall(xml):
                    fonts.update(v for _, v in _RFONTS_ATTR_RE.findall(tag) if v)
            elif name.startswith("word/theme/"):
                xml = z.read(name).decode("utf8", "replace")
                fonts.update(v for v in _THEME_FONT_RE.findall(xml) if v)
    return fonts


def installed_font_families() -> set[str] | None:
    fc = shutil.which("fc-list")
    if not fc:
        return None
    try:
        out = subprocess.run([fc, ":", "family"], capture_output=True, text=True, timeout=30, check=False).stdout
    except (OSError, subprocess.SubprocessError):
        return None
    fams: set[str] = set()
    for line in out.splitlines():
        for fam in line.split(","):
            fam = fam.strip()
            if fam:
                fams.add(fam.lower())
    return fams


def apply_substitutions(docx_in: str, docx_out: str, subs: dict[str, str]) -> dict[str, str]:
    """Write a copy with ``w:rFonts`` rewritten per ``subs``; returns what was replaced."""
    lower = {k.lower(): v for k, v in subs.items()}
    applied: dict[str, str] = {}

    def repl(m):
        attr, val = m.group(1), m.group(2)
        new = lower.get(val.lower())
        if new and new != val:
            applied[val] = new
            return f'{attr}="{new}"'
        return m.group(0)

    with zipfile.ZipFile(docx_in) as zin, zipfile.ZipFile(docx_out, "w", zipfile.ZIP_DEFLATED) as zout:
        for item in zin.infolist():
            data = zin.read(item.filename)
            if _XML_PARTS_RE.match(item.filename) or item.filename.startswith("word/theme/"):
                xml = data.decode("utf8")
                xml = _RFONTS_TAG_RE.sub(lambda m: _RFONTS_ATTR_RE.sub(repl, m.group(0)), xml)
                if item.filename.startswith("word/theme/"):
                    def trepl(m):
                        new = lower.get(m.group(1).lower())
                        if new and new != m.group(1):
                            applied[m.group(1)] = new
                            return m.group(0).replace(f'typeface="{m.group(1)}"', f'typeface="{new}"')
                        return m.group(0)
                    xml = _THEME_FONT_RE.sub(trepl, xml)
                data = xml.encode("utf8")
            zout.writestr(item, data)
    return applied


def convert_to_pdf(docx_path: str, outdir: str, *, soffice: str = "soffice", timeout: int = 600,
                   substitutions: dict[str, str] | None = None,
                   log: Callable[[str], None] = lambda m: print(m, file=sys.stderr)) -> str:
    """Convert ``docx_path`` to ``<outdir>/<stem>.pdf``; returns the PDF path."""
    exe = shutil.which(soffice) or soffice
    if not os.path.exists(exe) and not shutil.which(exe):
        raise PdfError(f"LibreOffice executable not found: {soffice!r}")

    subs = normalize_substitutions(substitutions)
    stem = os.path.splitext(os.path.basename(docx_path))[0]
    wanted = document_fonts(docx_path)
    installed = installed_font_families()

    with tempfile.TemporaryDirectory(prefix="smreport-pdf-") as work:
        src = docx_path
        if subs:
            src = os.path.join(work, f"{stem}.docx")
            applied = apply_substitutions(docx_path, src, subs)
            for old, new in sorted(applied.items()):
                log(f"font substitution: '{old}' -> '{new}' (configured)")
            wanted = {subs.get(f, f) for f in wanted}
        if installed is not None:
            for f in sorted(wanted):
                if f.lower() not in installed:
                    log(f"warning: font '{f}' is not installed; LibreOffice will substitute it")
        else:
            log("fontconfig (fc-list) not available; cannot check font availability")

        profile = os.path.join(work, "profile")
        cmd = [exe, "--headless", "--norestore", "--nologo", f"-env:UserInstallation=file://{profile}",
               "--convert-to", "pdf", "--outdir", work, src]
        try:
            proc = subprocess.run(cmd, capture_output=True, text=True, timeout=timeout, check=False)
        except subprocess.TimeoutExpired as ex:
            raise PdfError(f"LibreOffice timed out after {timeout}s") from ex
        except OSError as ex:
            raise PdfError(f"failed to launch LibreOffice: {ex}") from ex
        for line in (proc.stderr or "").splitlines():
            if line.strip():
                log(f"soffice: {line.rstrip()}")
        produced = os.path.join(work, f"{stem}.pdf")
        if proc.returncode != 0 or not os.path.exists(produced):
            tail = (proc.stdout or "")[-500:]
            raise PdfError(f"LibreOffice conversion failed (exit {proc.returncode}). {tail}".strip())
        os.makedirs(outdir, exist_ok=True)
        dest = os.path.join(outdir, f"{stem}.pdf")
        shutil.move(produced, dest)
    return dest
