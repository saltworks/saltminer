"""Orchestration: context.json + template.docx → outdir/<name>.docx [+ .pdf]."""

from __future__ import annotations

import json
import os
import sys
import tempfile
from dataclasses import dataclass, field

from docx import Document
from docxtpl import DocxTemplate

from smreport import postprocess
from smreport.context import DEFAULT_MARKDOWN_FIELDS, prepare_context
from smreport.pdf import PdfError, convert_to_pdf


class RenderError(Exception):
    pass


@dataclass
class RenderOptions:
    context_path: str
    template_path: str
    outdir: str
    name: str | None = None
    formats: tuple[str, ...] = ("docx",)
    image_max_width: int = 216
    image_max_height: int = 288
    static_image_alt_text: str = "StaticImage"
    font_substitutions: dict = field(default_factory=dict)
    markdown_fields: list[str] = field(default_factory=lambda: list(DEFAULT_MARKDOWN_FIELDS))
    value_colors: dict = field(default_factory=dict)
    allow_remote_images: bool = True
    soffice: str = "soffice"
    soffice_timeout: int = 600


def log(msg: str):
    print(msg, file=sys.stderr, flush=True)


def load_context(path: str) -> dict:
    with open(path, "r", encoding="utf-8-sig") as fh:
        data = json.load(fh)
    if not isinstance(data, dict):
        raise RenderError("context.json must contain a JSON object at the top level")
    return data


def render(opts: RenderOptions) -> dict[str, str]:
    """Render and return ``{"docx": path, "pdf": path}`` for the formats produced."""
    if not os.path.isfile(opts.context_path):
        raise RenderError(f"context file not found: {opts.context_path}")
    if not os.path.isfile(opts.template_path):
        raise RenderError(f"template not found: {opts.template_path}")
    formats = tuple(f.strip().lower() for f in opts.formats if f.strip())
    unknown = [f for f in formats if f not in ("docx", "pdf")]
    if unknown:
        raise RenderError(f"unsupported format(s): {', '.join(unknown)}")
    if not formats:
        formats = ("docx",)

    name = opts.name or os.path.splitext(os.path.basename(opts.template_path))[0]
    os.makedirs(opts.outdir, exist_ok=True)
    docx_out = os.path.join(opts.outdir, f"{name}.docx")

    raw = load_context(opts.context_path)
    prepared = prepare_context(raw, opts.markdown_fields, opts.value_colors)

    log(f"rendering template {os.path.basename(opts.template_path)} -> {docx_out}")
    with tempfile.TemporaryDirectory(prefix="smreport-") as work:
        stage1 = os.path.join(work, "stage1.docx")
        try:
            tpl = DocxTemplate(opts.template_path)
            tpl.render(prepared.context, autoescape=True)
            tpl.save(stage1)
        except Exception as ex:  # noqa: BLE001 - jinja/docxtpl errors are all "template failed"
            raise RenderError(f"template render failed: {type(ex).__name__}: {ex}") from ex

        document = Document(stage1)
        postprocess.run_all(
            document, prepared,
            base_dir=os.path.dirname(os.path.abspath(opts.context_path)),
            allow_remote_images=opts.allow_remote_images,
            image_max_width=opts.image_max_width,
            image_max_height=opts.image_max_height,
            static_image_alt_text=opts.static_image_alt_text,
            warn=lambda m: log(f"warning: {m}"),
        )
        document.save(docx_out)

    produced = {"docx": docx_out}
    if "pdf" in formats:
        log("converting to PDF with LibreOffice")
        try:
            produced["pdf"] = convert_to_pdf(
                docx_out, opts.outdir, soffice=opts.soffice, timeout=opts.soffice_timeout,
                substitutions=opts.font_substitutions, log=log,
            )
        except PdfError as ex:
            raise RenderError(f"pdf conversion failed: {ex}") from ex

    missing = [f for f in formats if f not in produced or not os.path.isfile(produced[f])]
    if missing:
        raise RenderError(f"output missing for format(s): {', '.join(missing)}")
    for fmt in formats:
        log(f"wrote {produced[fmt]}")
    return produced
