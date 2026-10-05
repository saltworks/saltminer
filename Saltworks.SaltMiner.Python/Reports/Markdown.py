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

# Markdown -> Word block elements, for a merge field whose value is markdown-typed.
#
# Replaces Syncfusion's markdown importer (`WordDocument.Open(stream, FormatType.Markdown)`,
# `ReportProcessor.cs:978-980`) with `markdown-it-py` in CommonMark mode, MIT licensed.
#
# CommonMark already gives the two behaviours the .NET path applies as text pre-passes before
# handing the value to Syncfusion (`ReportProcessor.cs:968,971`): a lone newline inside a
# paragraph parses as a `softbreak` token, which this module renders as a Word line break the
# same as an explicit `<br>`; and a backslash before punctuation is consumed by the parser's own
# escape handling, so `\*text\*` reaches this module as the literal text `*text*` with no
# emphasis token. Neither needs a textual rewrite of the source string first.
#
# Covers the subset the report's markdown fields carry: bold, italic, strikethrough, inline
# code, links, bullet and numbered lists, headings, block quotes, fenced code, line breaks, and
# a table rendered as one paragraph per row with cells joined by " | " and a bold header row
# (readable, not a real Word table; FINDINGS.md section 7.2 carries that gap).

from __future__ import annotations

import copy
from dataclasses import dataclass, field as dc_field

from docx.oxml import OxmlElement
from docx.oxml.ns import qn
from markdown_it import MarkdownIt

from Reports.WordFields import RPR, make_run

_MD = MarkdownIt("commonmark").enable("strikethrough").enable("table")

_PPR = qn("w:pPr")


@dataclass
class Span:
    """A run of text with the inline formatting markdown asked for."""

    text: str
    bold: bool = False
    italic: bool = False
    strike: bool = False
    mono: bool = False
    link: str | None = None
    image: tuple | None = None  # (src, alt) when this span stands for a markdown image


@dataclass
class Block:
    """One paragraph's worth of markdown output."""

    kind: str = "p"  # p | li | heading | quote | code | tablerow
    level: int = 0  # heading level, or list nesting depth
    prefix: str = ""  # bullet or number, rendered as literal text
    spans: list = dc_field(default_factory=list)


def _inline_spans(token, *, bold: bool = False, italic: bool = False) -> list[Span]:
    spans: list[Span] = []
    bold_depth = 1 if bold else 0
    italic_depth = 1 if italic else 0
    strike_depth = 0
    link: str | None = None
    # A literal `<br>` already stands for one line break. A source newline immediately after it
    # (the shape a WYSIWYG markdown editor emits, `<br>\n`) is not a second one: the .NET path's
    # own pre-pass only adds a break before a newline that is not already preceded by `<br>`
    # (`ReportProcessor.cs:968`, `(?<!<br>)\n`), so an explicit `<br>` followed by a raw newline
    # collapses to the one break the `<br>` already carries.
    suppress_next_break = False

    for child in token.children or []:
        kind = child.type
        if kind == "text":
            if child.content:
                spans.append(Span(child.content, bool(bold_depth), bool(italic_depth),
                                   bool(strike_depth), False, link))
            suppress_next_break = False
        elif kind == "code_inline":
            spans.append(Span(child.content, bool(bold_depth), bool(italic_depth),
                               bool(strike_depth), True, link))
            suppress_next_break = False
        elif kind == "strong_open":
            bold_depth += 1
            suppress_next_break = False
        elif kind == "strong_close":
            bold_depth = max(0, bold_depth - 1)
            suppress_next_break = False
        elif kind == "em_open":
            italic_depth += 1
            suppress_next_break = False
        elif kind == "em_close":
            italic_depth = max(0, italic_depth - 1)
            suppress_next_break = False
        elif kind == "s_open":
            strike_depth += 1
            suppress_next_break = False
        elif kind == "s_close":
            strike_depth = max(0, strike_depth - 1)
            suppress_next_break = False
        elif kind == "link_open":
            link = child.attrGet("href")
            suppress_next_break = False
        elif kind == "link_close":
            link = None
            suppress_next_break = False
        elif kind in ("softbreak", "hardbreak"):
            if suppress_next_break:
                suppress_next_break = False
                continue
            spans.append(Span("\n", bool(bold_depth), bool(italic_depth), bool(strike_depth),
                               False, link))
        elif kind == "html_inline":
            # The markdown editor emits <br> for carriage returns.
            if child.content.lower().replace(" ", "").rstrip("/>").endswith("<br"):
                spans.append(Span("\n", bool(bold_depth), bool(italic_depth), bool(strike_depth),
                                   False, link))
                suppress_next_break = True
            else:
                suppress_next_break = False
        elif kind == "image":
            alt = child.content or child.attrGet("alt") or "image"
            spans.append(Span(f"[{alt}]", bool(bold_depth), True, bool(strike_depth), False, None,
                              image=(child.attrGet("src") or "", alt)))
            suppress_next_break = False
    return spans


def markdown_blocks(text: str) -> list[Block]:
    """Parse markdown into block paragraphs with inline spans."""
    if text is None:
        return []
    if not str(text).strip():
        return []

    blocks: list[Block] = []
    list_stack: list[dict] = []
    in_quote = 0
    pending_li = False
    in_header_row = False
    cell_index = 0

    for token in _MD.parse(str(text)):
        kind = token.type

        if kind in ("bullet_list_open", "ordered_list_open"):
            list_stack.append(
                {"ordered": kind == "ordered_list_open", "n": int(token.attrGet("start") or 1)}
            )
        elif kind in ("bullet_list_close", "ordered_list_close"):
            if list_stack:
                list_stack.pop()
        elif kind == "list_item_open":
            pending_li = True
        elif kind == "blockquote_open":
            in_quote += 1
        elif kind == "blockquote_close":
            in_quote = max(0, in_quote - 1)
        elif kind == "heading_open":
            blocks.append(Block(kind="heading", level=int(token.tag[1:] or 1)))
        elif kind == "paragraph_open":
            if pending_li and list_stack:
                top = list_stack[-1]
                # .NET's markdown import wrote no number for an ordered item, because the template
                # ships no numbering definition (ruled 2026-10-05, PBI-051 AC-11), so only a bullet
                # item carries a visible mark.
                prefix = "" if top["ordered"] else "• "
                if top["ordered"]:
                    top["n"] += 1
                blocks.append(Block(kind="li", level=len(list_stack), prefix=prefix))
                pending_li = False
            elif in_quote:
                blocks.append(Block(kind="quote"))
            else:
                blocks.append(Block(kind="p"))
        elif kind == "table_open":
            in_header_row = False
        elif kind == "thead_open":
            in_header_row = True
        elif kind == "thead_close":
            in_header_row = False
        elif kind == "tr_open":
            blocks.append(Block(kind="tablerow"))
            cell_index = 0
        elif kind in ("th_open", "td_open"):
            # Count cells rather than test whether the row has spans yet: a row whose first
            # cell is empty has no spans at the second cell, and testing for spans would drop
            # that separator and shift every later column left.
            if blocks and blocks[-1].kind == "tablerow" and cell_index:
                blocks[-1].spans.append(Span(" | "))
            cell_index += 1
        elif kind == "inline":
            if not blocks:
                blocks.append(Block(kind="p"))
            is_heading = blocks[-1].kind == "heading"
            is_quote = blocks[-1].kind == "quote"
            spans = _inline_spans(token, bold=is_heading, italic=is_quote)
            if in_header_row:
                for span in spans:
                    span.bold = True
            blocks[-1].spans.extend(spans)
        elif kind in ("fence", "code_block"):
            for line in token.content.rstrip("\n").split("\n"):
                blocks.append(Block(kind="code", spans=[Span(line, mono=True)]))
        elif kind == "hr":
            blocks.append(Block(kind="p", spans=[Span("-" * 40)]))

    return [b for b in blocks if b.spans or b.prefix]


def _run_for_span(span: Span, rpr_source) -> object:
    """A `w:r` carrying `span.text`, wearing `rpr_source`'s `w:rPr` plus the span's own marks."""
    run = make_run(span.text, rpr_source)
    rpr = run.find(RPR)
    if rpr is None:
        rpr = OxmlElement("w:rPr")
        run.insert(0, rpr)

    def toggle(tag: str) -> None:
        if rpr.find(qn(tag)) is None:
            rpr.append(OxmlElement(tag))

    if span.bold:
        toggle("w:b")
    if span.italic:
        toggle("w:i")
    if span.strike:
        toggle("w:strike")
    if span.mono:
        fonts = rpr.find(qn("w:rFonts"))
        if fonts is None:
            fonts = OxmlElement("w:rFonts")
            rpr.append(fonts)
        for attr in ("w:ascii", "w:hAnsi", "w:cs"):
            fonts.set(qn(attr), "Courier New")
    if span.link:
        underline = rpr.find(qn("w:u"))
        if underline is None:
            underline = OxmlElement("w:u")
            rpr.append(underline)
        underline.set(qn("w:val"), "single")
        existing = rpr.find(qn("w:color"))
        if existing is not None:
            rpr.remove(existing)
        color_el = OxmlElement("w:color")
        color_el.set(qn("w:val"), "0563C1")
        rpr.append(color_el)
    return run


def _block_spans(block: Block) -> list[Span]:
    """A block's spans, with its list bullet or number as a leading span."""
    if not block.prefix:
        return list(block.spans)
    return [Span(block.prefix), *block.spans]


def _block_to_paragraph(block: Block, rpr_source, ppr_source, images=None) -> object:
    paragraph = OxmlElement("w:p")
    ppr = copy.deepcopy(ppr_source) if ppr_source is not None else None
    if block.kind == "li":
        # No numbering definition ships with the template for an ordered list, so a real
        # w:numPr is not universally correct; the "ListParagraph" style the template does define
        # is the style-based half of "numbering or style marks them as list items". The bullet or
        # "N. " prefix text carries the visible mark and tells bullet from ordered apart.
        ppr = ppr if ppr is not None else OxmlElement("w:pPr")
        existing_style = ppr.find(qn("w:pStyle"))
        if existing_style is not None:
            ppr.remove(existing_style)
        style = OxmlElement("w:pStyle")
        style.set(qn("w:val"), "ListParagraph")
        ppr.insert(0, style)
    if ppr is not None:
        paragraph.append(ppr)
    for span in _block_spans(block):
        if span.image is not None and images is not None:
            picture = images.run_for(span.image[0], span.image[1], rpr_source)
            if picture is not None:
                paragraph.append(picture)
                continue
        if span.text:
            paragraph.append(_run_for_span(span, rpr_source))
    return paragraph


def render_markdown(text: str, rpr_source=None, ppr_source=None, images=None) -> list:
    """Markdown text -> a list of `w:p` elements, one per block, ready to splice in.

    `rpr_source` is the merge field's cached result run, copied onto every run this builds so
    paragraph-level formatting from the template survives. `ppr_source` is the field's own
    paragraph properties, copied onto every paragraph this builds. `images`, when given, is an
    `Images.ImageEmbedder`: each markdown image becomes an inline picture, or stays the italic
    `[alt]` text when it cannot be resolved.
    """
    blocks = markdown_blocks(text)
    return [_block_to_paragraph(block, rpr_source, ppr_source, images) for block in blocks]
