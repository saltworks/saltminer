"""Markdown → a small block/inline model, via mistune's AST.

The model is deliberately tiny: it only has to carry what the DOCX writer can express
(paragraphs, headings, bullet/numbered lists, code, quotes, rules, tables, and inline
bold/italic/strike/code/links/images/breaks).
"""

from __future__ import annotations

import re
from dataclasses import dataclass, field

import mistune

_BR_RE = re.compile(r"<br\s*/?>", re.IGNORECASE)
_TAG_RE = re.compile(r"<[^>]+>")


@dataclass
class Inline:
    kind: str = "text"      # "text" | "br" | "image"
    text: str = ""
    bold: bool = False
    italic: bool = False
    strike: bool = False
    code: bool = False
    link: str | None = None
    src: str | None = None  # images
    alt: str = ""


@dataclass
class Block:
    kind: str                       # paragraph | heading | list_item | code | quote | hr | table
    inlines: list[Inline] = field(default_factory=list)
    level: int = 0                  # heading level / list nesting depth
    ordered: bool = False
    list_id: int = 0                # groups items of one list for numbering restarts
    rows: list[list[list[Inline]]] = field(default_factory=list)  # tables
    header: bool = False


_parser = mistune.create_markdown(renderer=None, plugins=["strikethrough", "table", "url"])


def parse(text: str) -> list[Block]:
    if not text or not text.strip():
        return []
    text = text.replace("\r\n", "\n").replace("\r", "\n")
    tokens = _parser(text)
    conv = _Converter()
    conv.blocks_from(tokens, level=0)
    return _split_paragraph_breaks(conv.blocks)


class _Converter:
    def __init__(self):
        self.blocks: list[Block] = []
        self._list_seq = 0

    # ---- blocks -------------------------------------------------------------
    def blocks_from(self, tokens, level: int, quote=False):
        for tok in tokens:
            t = tok.get("type")
            if t in ("paragraph", "block_text"):
                self._emit(Block("quote" if quote else "paragraph", self.inlines(tok.get("children", []))))
            elif t == "heading":
                self._emit(Block("heading", self.inlines(tok.get("children", [])),
                                 level=int((tok.get("attrs") or {}).get("level", 1))))
            elif t == "list":
                self._list(tok, level)
            elif t == "block_code":
                self._emit(Block("code", [Inline(text=_raw(tok).rstrip("\n"), code=True)]))
            elif t == "block_quote":
                self.blocks_from(tok.get("children", []), level, quote=True)
            elif t == "thematic_break":
                self._emit(Block("hr"))
            elif t == "table":
                self._table(tok)
            elif t == "block_html":
                self._html_block(_raw(tok))
            elif t == "blank_line":
                continue
            elif "children" in tok:
                self._emit(Block("paragraph", self.inlines(tok["children"])))
            elif _raw(tok):
                self._emit(Block("paragraph", [Inline(text=_raw(tok))]))

    def _emit(self, block: Block):
        self.blocks.append(block)

    def _list(self, tok, level):
        attrs = tok.get("attrs") or {}
        ordered = bool(attrs.get("ordered"))
        self._list_seq += 1
        list_id = self._list_seq
        for item in tok.get("children", []):
            first = True
            for child in item.get("children", []):
                ct = child.get("type")
                if ct in ("paragraph", "block_text"):
                    inl = self.inlines(child.get("children", []))
                    if first:
                        self._emit(Block("list_item", inl, level=level, ordered=ordered, list_id=list_id))
                        first = False
                    else:
                        # continuation paragraph inside an item: indent under the item
                        self._emit(Block("list_item", inl, level=level, ordered=ordered, list_id=-1))
                elif ct == "list":
                    if first:  # item with no text of its own
                        self._emit(Block("list_item", [], level=level, ordered=ordered, list_id=list_id))
                        first = False
                    self._list(child, level + 1)
                else:
                    if first:
                        self._emit(Block("list_item", [], level=level, ordered=ordered, list_id=list_id))
                        first = False
                    self.blocks_from([child], level + 1)

    def _table(self, tok):
        rows = []
        header = False
        for section in tok.get("children", []):
            st = section.get("type")
            if st == "table_head":
                rows.append([self.inlines(c.get("children", [])) for c in section.get("children", [])])
                header = True
            elif st == "table_body":
                for row in section.get("children", []):
                    rows.append([self.inlines(c.get("children", [])) for c in row.get("children", [])])
        if rows:
            self._emit(Block("table", rows=rows, header=header))

    def _html_block(self, raw: str):
        parts = _BR_RE.split(raw)
        inl: list[Inline] = []
        for i, part in enumerate(parts):
            if i:
                inl.append(Inline(kind="br"))
            txt = _TAG_RE.sub("", part).strip()
            if txt:
                inl.append(Inline(text=txt))
        if inl:
            self._emit(Block("paragraph", inl))

    # ---- inlines ------------------------------------------------------------
    def inlines(self, tokens, bold=False, italic=False, strike=False, link=None) -> list[Inline]:
        out: list[Inline] = []
        for tok in tokens:
            t = tok.get("type")
            if t == "text":
                out.append(Inline(text=_raw(tok), bold=bold, italic=italic, strike=strike, link=link))
            elif t == "strong":
                out += self.inlines(tok.get("children", []), True, italic, strike, link)
            elif t == "emphasis":
                out += self.inlines(tok.get("children", []), bold, True, strike, link)
            elif t == "strikethrough":
                out += self.inlines(tok.get("children", []), bold, italic, True, link)
            elif t == "codespan":
                out.append(Inline(text=_raw(tok), code=True, bold=bold, italic=italic, link=link))
            elif t == "link":
                url = (tok.get("attrs") or {}).get("url", "")
                children = tok.get("children") or [{"type": "text", "raw": url}]
                out += self.inlines(children, bold, italic, strike, url or link)
            elif t == "image":
                url = (tok.get("attrs") or {}).get("url", "")
                alt = "".join(_raw(c) for c in tok.get("children", []) if c.get("type") == "text")
                out.append(Inline(kind="image", src=url, alt=alt))
            elif t in ("softbreak", "linebreak"):
                out.append(Inline(kind="br"))
            elif t == "inline_html":
                raw = _raw(tok)
                if _BR_RE.fullmatch(raw.strip()):
                    out.append(Inline(kind="br"))
                else:
                    txt = _TAG_RE.sub("", raw)
                    if txt:
                        out.append(Inline(text=txt, bold=bold, italic=italic, strike=strike, link=link))
            elif "children" in tok:
                out += self.inlines(tok["children"], bold, italic, strike, link)
            elif _raw(tok):
                out.append(Inline(text=_raw(tok), bold=bold, italic=italic, strike=strike, link=link))
        return out


def _raw(tok) -> str:
    return tok.get("raw") or tok.get("text") or ""


def _split_paragraph_breaks(blocks: list[Block]) -> list[Block]:
    """Plain paragraphs split into separate paragraphs at line breaks.

    Mirrors the old renderer, which turned every ``\\n`` / ``<br>`` in a value into a
    new paragraph.  Other block kinds keep the break as a soft line break.
    """
    out: list[Block] = []
    for b in blocks:
        if b.kind not in ("paragraph", "heading") or not any(i.kind == "br" for i in b.inlines):
            out.append(b)
            continue
        cur: list[Inline] = []
        for inl in b.inlines:
            if inl.kind == "br":
                out.append(Block(b.kind, cur, level=b.level))
                cur = []
            else:
                cur.append(inl)
        out.append(Block(b.kind, cur, level=b.level))
    # drop empty paragraphs produced by trailing breaks, keep at least one block
    cleaned = [b for b in out if b.kind != "paragraph" or b.inlines]
    return cleaned or out[:1]
