"""``python -m smreport`` — the file-based renderer contract.

Exit codes: 0 success, 2 bad arguments/input, 3 render failure, 4 PDF conversion failure.
"""

from __future__ import annotations

import argparse
import json
import sys

from smreport import __version__
from smreport.context import DEFAULT_MARKDOWN_FIELDS
from smreport.render import RenderError, RenderOptions, render


def _json_arg(value: str) -> dict:
    if not value:
        return {}
    try:
        data = json.loads(value)
    except json.JSONDecodeError as ex:
        raise argparse.ArgumentTypeError(f"invalid JSON: {ex}") from ex
    if not isinstance(data, dict):
        raise argparse.ArgumentTypeError("expected a JSON object")
    return data


def build_parser() -> argparse.ArgumentParser:
    p = argparse.ArgumentParser(prog="smreport", description="SaltMiner engagement report renderer")
    p.add_argument("--version", action="version", version=f"smreport {__version__}")
    p.add_argument("--context", required=True, help="path to context.json (serialized report DTO)")
    p.add_argument("--template", required=True, help="path to the Jinja-tagged .docx template")
    p.add_argument("--outdir", required=True, help="directory to write outputs into")
    p.add_argument("--name", help="output basename (default: template filename without extension)")
    p.add_argument("--formats", default="docx", help="comma-separated: docx,pdf (default docx)")
    p.add_argument("--image-max-width", type=int, default=216, help="points (default 216 = 3in)")
    p.add_argument("--image-max-height", type=int, default=288, help="points (default 288 = 4in)")
    p.add_argument("--static-image-alt-text", default="StaticImage",
                   help="images whose alt text equals this are never resized")
    p.add_argument("--font-substitutions", type=_json_arg, default={},
                   help='JSON: {"Calibri":"Carlito"} or {"Calibri":{"Font":"Carlito",...}}')
    p.add_argument("--markdown-fields", default=",".join(DEFAULT_MARKDOWN_FIELDS),
                   help="comma-separated field names rendered as markdown")
    p.add_argument("--value-colors", type=_json_arg, default={},
                   help='JSON: {"critical":"Red"} — colour any field whose value matches')
    p.add_argument("--no-remote-images", action="store_true", help="do not fetch http(s) markdown images")
    p.add_argument("--soffice", default="soffice", help="LibreOffice executable (default: soffice)")
    p.add_argument("--soffice-timeout", type=int, default=600, help="seconds (default 600)")
    return p


def main(argv=None) -> int:
    args = build_parser().parse_args(argv)
    opts = RenderOptions(
        context_path=args.context,
        template_path=args.template,
        outdir=args.outdir,
        name=args.name,
        formats=tuple(args.formats.split(",")),
        image_max_width=args.image_max_width,
        image_max_height=args.image_max_height,
        static_image_alt_text=args.static_image_alt_text,
        font_substitutions=args.font_substitutions,
        markdown_fields=[f.strip() for f in args.markdown_fields.split(",") if f.strip()],
        value_colors=args.value_colors,
        allow_remote_images=not args.no_remote_images,
        soffice=args.soffice,
        soffice_timeout=args.soffice_timeout,
    )
    try:
        render(opts)
    except RenderError as ex:
        print(f"smreport: error: {ex}", file=sys.stderr, flush=True)
        msg = str(ex)
        if msg.startswith("pdf conversion"):
            return 4
        if "not found" in msg or "unsupported format" in msg or "context.json" in msg:
            return 2
        return 3
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
