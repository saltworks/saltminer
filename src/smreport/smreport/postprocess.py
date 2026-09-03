"""Post-render passes over the python-docx document.

1. sentinel expansion — markdown → rich text, value colours, line breaks
2. hyperlink styling — blue + underline on every hyperlink
3. image resizing — cap to max width/height (points), skipping static images by alt text
"""

from __future__ import annotations

import re
from copy import deepcopy
from typing import Callable

from docx.oxml import OxmlElement
from docx.oxml.ns import nsmap, qn
from docx.shared import Pt

from smreport.context import LINE_BREAK, SENTINEL_RE, Prepared
from smreport.docxwriter import DocxWriter, NumberingHelper, make_break_run, make_run, style_hyperlink_runs
from smreport.markdown import parse as parse_markdown

_SPLIT_RE = re.compile(f"({SENTINEL_RE.pattern}|{LINE_BREAK})")
_MARKERS = ("", LINE_BREAK)


def story_parts(document):
    """(root element, part) for the body and every distinct header/footer."""
    yield document.element.body, document.part
    seen = set()
    for section in document.sections:
        for hf in (section.header, section.first_page_header, section.even_page_header,
                   section.footer, section.first_page_footer, section.even_page_footer):
            try:
                if hf.is_linked_to_previous:
                    continue
                part = hf.part
            except Exception:  # noqa: BLE001
                continue
            if id(part) in seen:
                continue
            seen.add(id(part))
            yield hf._element, part


# ----------------------------------------------------------------- sentinel expansion
def expand_sentinels(document, prepared: Prepared, *, base_dir=None, allow_remote_images=True,
                     warn: Callable[[str], None] = lambda m: None):
    numbering = NumberingHelper(document)
    for root, part in story_parts(document):
        writer = DocxWriter(part, numbering, base_dir=base_dir, allow_remote_images=allow_remote_images, warn=warn)
        for p in list(root.iter(qn("w:p"))):
            if _paragraph_has_marker(p):
                _expand_paragraph(p, prepared, writer)


def _paragraph_has_marker(p) -> bool:
    for t in p.iter(qn("w:t")):
        if t.text and any(m in t.text for m in _MARKERS):
            return True
    return False


def _expand_paragraph(p, prepared: Prepared, writer: DocxWriter):
    base_ppr = p.find(qn("w:pPr"))
    out_paras = []
    cur = _new_paragraph_like(base_ppr)

    for child in list(p):
        if child.tag == qn("w:pPr"):
            continue
        if child.tag != qn("w:r") or not _run_has_marker(child):
            cur.append(deepcopy(child))
            continue
        base_rpr = child.find(qn("w:rPr"))
        for kind, payload in _explode_run(child):
            if kind == "run":
                cur.append(payload)
            elif kind == "br":
                cur.append(make_break_run(base_rpr))
            elif kind == "color":
                entry = prepared.entries[payload]
                cur.append(make_run(entry.text, base_rpr, color=entry.color))
            elif kind == "md":
                entry = prepared.entries[payload]
                blocks = parse_markdown(entry.text)
                elements = writer.block_elements(blocks, base_rpr, base_ppr)
                for i, (bkind, el) in enumerate(elements):
                    if i == 0 and bkind == "paragraph":
                        # first paragraph is spliced inline where the field was
                        for r in list(el):
                            if r.tag != qn("w:pPr"):
                                cur.append(r)
                        continue
                    if _has_content(cur):
                        out_paras.append(cur)
                    if el.tag == qn("w:p"):
                        cur = el
                    else:
                        out_paras.append(el)
                        cur = _new_paragraph_like(base_ppr)
    # keep the trailing paragraph when it has content, when nothing else was emitted, or
    # when it follows a table (a table cell / body must not end with a table)
    if _has_content(cur) or not out_paras or out_paras[-1].tag == qn("w:tbl"):
        out_paras.append(cur)
    parent = p.getparent()
    final = out_paras
    for el in final:
        p.addprevious(el)
    parent.remove(p)


def _new_paragraph_like(base_ppr):
    p = OxmlElement("w:p")
    if base_ppr is not None:
        p.append(deepcopy(base_ppr))
    return p


def _has_content(p) -> bool:
    return any(c.tag != qn("w:pPr") for c in p)


def _run_has_marker(r) -> bool:
    for t in r.findall(qn("w:t")):
        if t.text and any(m in t.text for m in _MARKERS):
            return True
    return False


def _explode_run(r):
    """Yield ("run", w:r) | ("br", None) | ("md", id) | ("color", id) pieces of a run."""
    rpr = r.find(qn("w:rPr"))
    for child in r:
        if child.tag == qn("w:rPr"):
            continue
        if child.tag != qn("w:t") or not child.text or not any(m in child.text for m in _MARKERS):
            nr = make_run("", rpr)
            nr.append(deepcopy(child))
            yield "run", nr
            continue
        for piece in _SPLIT_RE.split(child.text):
            if not piece:
                continue
            if piece == LINE_BREAK:
                yield "br", None
                continue
            m = SENTINEL_RE.fullmatch(piece)
            if m:
                idx = int(m.group(1))
                yield ("md", idx) if _kind_of(idx) == "md" else ("color", idx)
                continue
            if piece.isdigit() and _SPLIT_RE.fullmatch(f"{piece}"):
                # re.split emits the capture group for the sentinel id; skip it
                continue
            yield "run", make_run(piece, rpr)


_KIND_LOOKUP: dict[int, str] = {}


def _kind_of(idx: int) -> str:
    return _KIND_LOOKUP.get(idx, "md")


def _install_kinds(prepared: Prepared):
    _KIND_LOOKUP.clear()
    _KIND_LOOKUP.update({k: v.kind for k, v in prepared.entries.items()})


# ------------------------------------------------------------------- hyperlink style
def style_hyperlinks(document):
    for root, _part in story_parts(document):
        for h in root.iter(qn("w:hyperlink")):
            style_hyperlink_runs(h)


# --------------------------------------------------------------------- image resize
def resize_images(document, max_width_pt: int, max_height_pt: int, static_alt_text: str | None):
    """Scale down any picture larger than the max box, preserving aspect ratio.

    Same rule as the old renderer: whichever dimension overshoots its max by more gets
    pinned to the max and the other dimension is scaled.  Pictures whose alt text equals
    ``static_alt_text`` are left alone.
    """
    max_w, max_h = int(Pt(max_width_pt)), int(Pt(max_height_pt))
    for root, _part in story_parts(document):
        for holder in list(root.iter(qn("wp:inline"))) + list(root.iter(qn("wp:anchor"))):
            docpr = holder.find(qn("wp:docPr"))
            if docpr is not None and static_alt_text and static_alt_text in (
                docpr.get("descr"), docpr.get("title"), docpr.get("name")
            ):
                continue
            extent = holder.find(qn("wp:extent"))
            if extent is None:
                continue
            try:
                cx, cy = int(extent.get("cx")), int(extent.get("cy"))
            except (TypeError, ValueError):
                continue
            if cx <= 0 or cy <= 0:
                continue
            dw, dh = cx - max_w, cy - max_h
            if dw <= 0 and dh <= 0:
                continue
            ratio = cx / cy
            if dw > dh:
                new_w, new_h = max_w, int(max_w / ratio)
            else:
                new_h, new_w = max_h, int(max_h * ratio)
            extent.set("cx", str(new_w))
            extent.set("cy", str(new_h))
            for ext in holder.iter(qn("a:ext")):
                if ext.getparent() is not None and ext.getparent().tag == qn("a:xfrm"):
                    ext.set("cx", str(new_w))
                    ext.set("cy", str(new_h))


def run_all(document, prepared: Prepared, *, base_dir=None, allow_remote_images=True, image_max_width=216,
            image_max_height=288, static_image_alt_text="StaticImage", warn=lambda m: None):
    _install_kinds(prepared)
    expand_sentinels(document, prepared, base_dir=base_dir, allow_remote_images=allow_remote_images, warn=warn)
    style_hyperlinks(document)
    resize_images(document, image_max_width, image_max_height, static_image_alt_text)


__all__ = ["run_all", "expand_sentinels", "style_hyperlinks", "resize_images", "story_parts", "nsmap"]
