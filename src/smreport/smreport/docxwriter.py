"""Block/inline model → WordprocessingML elements (python-docx oxml).

Elements are built detached from any paragraph so the post-processor can splice them
wherever the sentinel was found.  Character formatting starts from the run that held
the merge field (so markdown text inherits the template's font/size/colour) and the
markdown emphasis is layered on top.
"""

from __future__ import annotations

import io
from copy import deepcopy
from typing import Callable

from docx.opc.constants import CONTENT_TYPE as CT
from docx.opc.constants import RELATIONSHIP_TYPE as RT
from docx.opc.packuri import PackURI
from docx.oxml import OxmlElement, parse_xml
from docx.oxml.ns import nsdecls, qn
from docx.parts.numbering import NumberingPart

from smreport.images import ImageError, load_image
from smreport.markdown import Block, Inline

# Schema child order for w:rPr / w:pPr (ECMA-376).  python-docx does not expose these, and
# Word is picky about element order inside property groups.
RPR_ORDER = (
    "w:rStyle", "w:rFonts", "w:b", "w:bCs", "w:i", "w:iCs", "w:caps", "w:smallCaps", "w:strike",
    "w:dstrike", "w:outline", "w:shadow", "w:emboss", "w:imprint", "w:noProof", "w:snapToGrid",
    "w:vanish", "w:webHidden", "w:color", "w:spacing", "w:w", "w:kern", "w:position", "w:sz",
    "w:szCs", "w:highlight", "w:u", "w:effect", "w:bdr", "w:shd", "w:fitText", "w:vertAlign",
    "w:rtl", "w:cs", "w:em", "w:lang", "w:eastAsianLayout", "w:specVanish", "w:oMath",
)
PPR_ORDER = (
    "w:pStyle", "w:keepNext", "w:keepLines", "w:pageBreakBefore", "w:framePr", "w:widowControl",
    "w:numPr", "w:suppressLineNumbers", "w:pBdr", "w:shd", "w:tabs", "w:suppressAutoHyphens",
    "w:kinsoku", "w:wordWrap", "w:overflowPunct", "w:topLinePunct", "w:autoSpaceDE",
    "w:autoSpaceDN", "w:bidi", "w:adjustRightInd", "w:snapToGrid", "w:spacing", "w:ind",
    "w:contextualSpacing", "w:mirrorIndents", "w:suppressOverlap", "w:jc", "w:textDirection",
    "w:textAlignment", "w:textboxTightWrap", "w:outlineLvl", "w:divId", "w:cnfStyle", "w:rPr",
    "w:sectPr", "w:pPrChange",
)

LINK_COLOR = "0000FF"
CODE_FONT = "Consolas"
CODE_SHADE = "F2F2F2"
LIST_INDENT_TWIPS = 720   # 36pt  (matches old renderer: LeftIndent = 36)
LIST_HANGING_TWIPS = 360  # 18pt  (FirstLineIndent = -18)
HEADING_SIZES_HALFPT = {1: 32, 2: 28, 3: 24, 4: 22, 5: 22, 6: 22}


# --------------------------------------------------------------------------- helpers
def set_rpr(rpr, tag: str, **attrs):
    """Replace/insert ``tag`` in a ``w:rPr`` honouring schema order."""
    for old in rpr.findall(qn(tag)):
        rpr.remove(old)
    el = OxmlElement(tag)
    for k, v in attrs.items():
        el.set(qn(k), str(v))
    _insert_ordered(rpr, el, RPR_ORDER, tag)
    return el


def set_ppr(ppr, tag: str, **attrs):
    for old in ppr.findall(qn(tag)):
        ppr.remove(old)
    el = OxmlElement(tag)
    for k, v in attrs.items():
        el.set(qn(k), str(v))
    _insert_ordered(ppr, el, PPR_ORDER, tag)
    return el


def _insert_ordered(parent, el, seq, tag):
    try:
        idx = seq.index(tag)
    except ValueError:
        parent.append(el)
        return
    successors = {qn(t) for t in seq[idx + 1:]}
    for child in parent:
        if child.tag in successors:
            child.addprevious(el)
            return
    parent.append(el)


def make_run(text: str, base_rpr=None, **fmt):
    """A ``w:r`` with a copy of ``base_rpr`` plus the requested formatting."""
    r = OxmlElement("w:r")
    rpr = deepcopy(base_rpr) if base_rpr is not None else OxmlElement("w:rPr")
    if fmt.get("bold"):
        set_rpr(rpr, "w:b")
        set_rpr(rpr, "w:bCs")
    if fmt.get("italic"):
        set_rpr(rpr, "w:i")
        set_rpr(rpr, "w:iCs")
    if fmt.get("strike"):
        set_rpr(rpr, "w:strike")
    if fmt.get("code"):
        set_rpr(rpr, "w:rFonts", **{"w:ascii": CODE_FONT, "w:hAnsi": CODE_FONT, "w:cs": CODE_FONT})
        if fmt.get("shade", True):
            set_rpr(rpr, "w:shd", **{"w:val": "clear", "w:color": "auto", "w:fill": CODE_SHADE})
    if fmt.get("link"):
        set_rpr(rpr, "w:color", **{"w:val": LINK_COLOR})
        set_rpr(rpr, "w:u", **{"w:val": "single"})
    if fmt.get("color"):
        set_rpr(rpr, "w:color", **{"w:val": fmt["color"]})
    if fmt.get("size_halfpt"):
        set_rpr(rpr, "w:sz", **{"w:val": fmt["size_halfpt"]})
        set_rpr(rpr, "w:szCs", **{"w:val": fmt["size_halfpt"]})
    if len(rpr):
        r.append(rpr)
    if text:
        t = OxmlElement("w:t")
        t.text = text
        t.set(qn("xml:space"), "preserve")
        r.append(t)
    return r


def make_break_run(base_rpr=None):
    r = make_run("", base_rpr)
    r.append(OxmlElement("w:br"))
    return r


def style_hyperlink_runs(hyperlink):
    """Blue + underline on every run of a ``w:hyperlink`` (old renderer behaviour)."""
    for r in hyperlink.findall(qn("w:r")):
        rpr = r.find(qn("w:rPr"))
        if rpr is None:
            rpr = OxmlElement("w:rPr")
            r.insert(0, rpr)
        set_rpr(rpr, "w:color", **{"w:val": LINK_COLOR})
        set_rpr(rpr, "w:u", **{"w:val": "single"})


# ------------------------------------------------------------------------ numbering
class NumberingHelper:
    """Creates bullet / decimal numbering definitions on demand (one ``w:num`` per list,
    with start overrides so every list restarts at 1)."""

    def __init__(self, document):
        self._doc = document
        self._el = self._numbering_element()
        self._abstract: dict[bool, int] = {}

    def _numbering_element(self):
        part = self._doc.part
        try:
            np = part.part_related_by(RT.NUMBERING)
        except KeyError:
            el = parse_xml(f"<w:numbering {nsdecls('w')}/>")
            np = NumberingPart(PackURI("/word/numbering.xml"), CT.WML_NUMBERING, el, part.package)
            part.relate_to(np, RT.NUMBERING)
        return np.element

    def _next_id(self, tag: str, attr: str) -> int:
        ids = [int(e.get(qn(attr))) for e in self._el.findall(qn(tag)) if e.get(qn(attr), "").lstrip("-").isdigit()]
        return (max(ids) + 1) if ids else 1

    def _abstract_num(self, ordered: bool) -> int:
        if ordered in self._abstract:
            return self._abstract[ordered]
        aid = self._next_id("w:abstractNum", "w:abstractNumId")
        levels = []
        for lvl in range(9):
            if ordered:
                fmt = ["decimal", "lowerLetter", "lowerRoman"][lvl % 3]
                text = f"%{lvl + 1}."
                font = ""
            else:
                fmt = "bullet"
                text = ["", "o", ""][lvl % 3]
                font = ["Symbol", "Courier New", "Wingdings"][lvl % 3]
            rpr = f'<w:rPr><w:rFonts w:ascii="{font}" w:hAnsi="{font}" w:hint="default"/></w:rPr>' if font else ""
            levels.append(
                f'<w:lvl w:ilvl="{lvl}"><w:start w:val="1"/><w:numFmt w:val="{fmt}"/>'
                f'<w:lvlText w:val="{text}"/><w:lvlJc w:val="left"/>'
                f'<w:pPr><w:ind w:left="{LIST_INDENT_TWIPS * (lvl + 1)}" w:hanging="{LIST_HANGING_TWIPS}"/></w:pPr>{rpr}</w:lvl>'
            )
        xml = (f'<w:abstractNum {nsdecls("w")} w:abstractNumId="{aid}">'
               f'<w:multiLevelType w:val="hybridMultilevel"/>{"".join(levels)}</w:abstractNum>')
        el = parse_xml(xml)
        nums = self._el.findall(qn("w:num"))
        if nums:
            nums[0].addprevious(el)
        else:
            self._el.append(el)
        self._abstract[ordered] = aid
        return aid

    def new_list(self, ordered: bool) -> int:
        aid = self._abstract_num(ordered)
        nid = self._next_id("w:num", "w:numId")
        overrides = "".join(
            f'<w:lvlOverride w:ilvl="{l}"><w:startOverride w:val="1"/></w:lvlOverride>' for l in range(9)
        )
        el = parse_xml(f'<w:num {nsdecls("w")} w:numId="{nid}"><w:abstractNumId w:val="{aid}"/>{overrides}</w:num>')
        self._el.append(el)
        return nid


# --------------------------------------------------------------------------- writer
class DocxWriter:
    def __init__(self, part, numbering: NumberingHelper, *, base_dir: str | None = None,
                 allow_remote_images: bool = True, warn: Callable[[str], None] = lambda m: None):
        self.part = part
        self.numbering = numbering
        self.base_dir = base_dir
        self.allow_remote = allow_remote_images
        self.warn = warn
        self._list_nums: dict[int, int] = {}

    # ---- inline -------------------------------------------------------------
    def inline_children(self, inlines: list[Inline], base_rpr=None, *, br_as_break=True, extra=None) -> list:
        out = []
        extra = extra or {}
        for inl in inlines:
            if inl.kind == "br":
                if br_as_break:
                    out.append(make_break_run(base_rpr))
                continue
            if inl.kind == "image":
                pic = self._picture_run(inl, base_rpr)
                if pic is not None:
                    out.append(pic)
                continue
            fmt = dict(bold=inl.bold, italic=inl.italic, strike=inl.strike, code=inl.code, link=bool(inl.link))
            fmt.update({k: v for k, v in extra.items() if v or k not in fmt})
            run = make_run(inl.text, base_rpr, **fmt)
            if inl.link:
                out.append(self._hyperlink(inl.link, [run]))
            else:
                out.append(run)
        return out

    def _hyperlink(self, url: str, runs: list):
        h = OxmlElement("w:hyperlink")
        try:
            rid = self.part.relate_to(url, RT.HYPERLINK, is_external=True)
            h.set(qn("r:id"), rid)
        except Exception as ex:  # noqa: BLE001 - keep the text even if the URL is unusable
            self.warn(f"hyperlink relationship failed for {url!r}: {ex}")
        h.set(qn("w:history"), "1")
        for r in runs:
            h.append(r)
        return h

    def _picture_run(self, inl: Inline, base_rpr=None):
        try:
            data = load_image(inl.src or "", self.base_dir, self.allow_remote)
            inline = self.part.new_pic_inline(io.BytesIO(data), None, None)
        except (ImageError, Exception) as ex:  # noqa: BLE001 - python-docx raises UnrecognizedImageError etc.
            self.warn(f"image skipped ({inl.src[:80] if inl.src else ''}...): {ex}")
            return None
        if inl.alt:
            inline.docPr.set("descr", inl.alt)
        r = make_run("", base_rpr)
        r.add_drawing(inline)
        return r

    # ---- blocks -------------------------------------------------------------
    def block_elements(self, blocks: list[Block], base_rpr=None, base_ppr=None) -> list:
        """Return ``(block_kind, element)`` pairs; elements are ``w:p`` or ``w:tbl``."""
        out = []
        for b in blocks:
            if b.kind == "table":
                out.append(("table", self._table(b, base_rpr, base_ppr)))
                continue
            p = OxmlElement("w:p")
            ppr = self._ppr(base_ppr)
            p.append(ppr)
            if b.kind == "list_item":
                self._apply_list(ppr, b)
                p.extend(self.inline_children(b.inlines, base_rpr))
            elif b.kind == "heading":
                p.extend(self.inline_children(b.inlines, base_rpr,
                                              extra={"bold": True, "size_halfpt": HEADING_SIZES_HALFPT.get(b.level, 22)}))
            elif b.kind == "code":
                set_ppr(ppr, "w:shd", **{"w:val": "clear", "w:color": "auto", "w:fill": CODE_SHADE})
                lines = (b.inlines[0].text if b.inlines else "").split("\n")
                for i, line in enumerate(lines):
                    if i:
                        p.append(make_break_run(base_rpr))
                    p.append(make_run(line, base_rpr, code=True, shade=False))
            elif b.kind == "quote":
                set_ppr(ppr, "w:ind", **{"w:left": LIST_INDENT_TWIPS})
                p.extend(self.inline_children(b.inlines, base_rpr, extra={"italic": True}))
            elif b.kind == "hr":
                pbdr = set_ppr(ppr, "w:pBdr")
                bottom = OxmlElement("w:bottom")
                for k, v in {"w:val": "single", "w:sz": "6", "w:space": "1", "w:color": "auto"}.items():
                    bottom.set(qn(k), v)
                pbdr.append(bottom)
            else:  # paragraph
                p.extend(self.inline_children(b.inlines, base_rpr))
            out.append((b.kind, p))
        return out

    def _ppr(self, base_ppr):
        """Copy of the host paragraph's properties minus anything list/section related."""
        ppr = deepcopy(base_ppr) if base_ppr is not None else OxmlElement("w:pPr")
        for tag in ("w:numPr", "w:sectPr", "w:pPrChange", "w:rPr"):
            for el in ppr.findall(qn(tag)):
                ppr.remove(el)
        return ppr

    def _apply_list(self, ppr, b: Block):
        if b.list_id < 0:  # continuation paragraph inside an item: indent only
            set_ppr(ppr, "w:ind", **{"w:left": LIST_INDENT_TWIPS * (b.level + 1)})
            return
        num_id = self._list_nums.get(b.list_id)
        if num_id is None:
            num_id = self.numbering.new_list(b.ordered)
            self._list_nums[b.list_id] = num_id
        numpr = set_ppr(ppr, "w:numPr")
        ilvl = OxmlElement("w:ilvl")
        ilvl.set(qn("w:val"), str(min(b.level, 8)))
        nid = OxmlElement("w:numId")
        nid.set(qn("w:val"), str(num_id))
        numpr.append(ilvl)
        numpr.append(nid)
        set_ppr(ppr, "w:ind", **{"w:left": LIST_INDENT_TWIPS * (b.level + 1), "w:hanging": LIST_HANGING_TWIPS})

    def _table(self, b: Block, base_rpr, base_ppr):
        ncols = max(len(r) for r in b.rows)
        tbl = OxmlElement("w:tbl")
        tblpr = OxmlElement("w:tblPr")
        tblw = OxmlElement("w:tblW")
        tblw.set(qn("w:w"), "0")
        tblw.set(qn("w:type"), "auto")
        tblpr.append(tblw)
        borders = OxmlElement("w:tblBorders")
        for side in ("top", "left", "bottom", "right", "insideH", "insideV"):
            el = OxmlElement(f"w:{side}")
            for k, v in {"w:val": "single", "w:sz": "4", "w:space": "0", "w:color": "auto"}.items():
                el.set(qn(k), v)
            borders.append(el)
        tblpr.append(borders)
        tbl.append(tblpr)
        grid = OxmlElement("w:tblGrid")
        for _ in range(ncols):
            grid.append(OxmlElement("w:gridCol"))
        tbl.append(grid)
        for ri, row in enumerate(b.rows):
            tr = OxmlElement("w:tr")
            for ci in range(ncols):
                tc = OxmlElement("w:tc")
                tc.append(OxmlElement("w:tcPr"))
                p = OxmlElement("w:p")
                p.append(self._ppr(base_ppr))
                cell = row[ci] if ci < len(row) else []
                extra = {"bold": True} if (b.header and ri == 0) else {}
                p.extend(self.inline_children(cell, base_rpr, extra=extra))
                tc.append(p)
                tr.append(tc)
            tbl.append(tr)
        return tbl
