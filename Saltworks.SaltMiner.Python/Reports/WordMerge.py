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

# Nested-group mail merge for DOCX templates, on python-docx.
#
# Reproduces the DocIO behaviours the shipped SaltMiner template depends on:
#   * TableStart:<Group> / TableEnd:<Group> regions, each repeated once per record and nested to
#     any depth (ReportProcessor.CreateWordReport, MailMerge.ExecuteNestedGroup), including a
#     region whose markers sit in table rows: the same row when both markers share it, or the
#     rows from the start row to the end row inclusive otherwise.
#   * a group's records come from the record directly around it, so the same group name under two
#     parents resolves to each parent's own list.
#   * a field's value resolves against the innermost record first, then outwards.
#   * the merged text inherits the cached result run's formatting (w:rPr).
#   * every root-level group named Section<digits> is bound to the same root record and rendered
#     once, whatever the document's Word section count.
#
# A group marker split across two tables or between a table and the body, or sitting inside a
# nested table, is not merged. Neither are merge fields in headers and footers. All are reported
# on the MergeResult by name.

from __future__ import annotations

import copy
import logging
import re
from collections import Counter
from dataclasses import dataclass, field as dc_field
from typing import Callable

import docx
from docx.oxml import OxmlElement
from docx.oxml.ns import qn

from Reports.WordFields import (
    GROUP_MARKER,
    P,
    R,
    RPR,
    group_marker,
    iter_merge_fields,
    make_run,
    parse_field_name,
    replace_field,
    row_markers,
    row_paragraphs,
)

W14 = "http://schemas.microsoft.com/office/word/2010/wordml"
_SCRUB_ATTRS = (f"{{{W14}}}paraId", f"{{{W14}}}textId")
_BOOKMARKS = (qn("w:bookmarkStart"), qn("w:bookmarkEnd"))
_TC = qn("w:tc")
_TR = qn("w:tr")
_TBL = qn("w:tbl")
_PPR = qn("w:pPr")
_SECTPR = qn("w:sectPr")
_FLDSIMPLE = qn("w:fldSimple")
_INSTR_ATTR = qn("w:instr")

ROOT_GROUP = "Section1"
SECTION_ROOT = re.compile(r"^Section\d+$")


@dataclass(frozen=True)
class FieldContext:
    """What a renderer is given for one merge field."""

    name: str
    value: str
    run_properties: object | None
    paragraph_properties: object | None
    in_table_cell: bool


# A renderer returns one of three shapes: a str (inline text, built into a run with the field's
# own cached formatting), a single `w:r` element (spliced in place of just the field's own runs,
# staying inline in the same paragraph, for a renderer that needs to set its own run formatting,
# such as colouring one value without disturbing the rest of the paragraph), or a list of block
# elements to splice in place of the field's whole paragraph.
Renderer = Callable[[FieldContext], "str | object | list"]


def plain_text_renderer(ctx: FieldContext) -> str:
    return ctx.value


@dataclass
class MergeResult:
    """What the merge did, so the caller can assert on it."""

    groups: dict = dc_field(default_factory=dict)
    unmatched: list = dc_field(default_factory=list)
    unsupported_markers: list = dc_field(default_factory=list)
    unsupported_fields: list = dc_field(default_factory=list)
    groups_missing: list = dc_field(default_factory=list)
    fields_merged: int = 0


class TemplateStructureError(ValueError):
    """A mismatched, unclosed or mixed group marker. The message names the marker."""


class SectionRoots(dict):
    """{'Section1': [record]} as stored; any Section<digits> name answers with the same list."""

    def __contains__(self, name) -> bool:
        return super().__contains__(name) or bool(SECTION_ROOT.match(name))

    def __missing__(self, name):
        if SECTION_ROOT.match(name):
            return self[ROOT_GROUP]
        raise KeyError(name)


def bind_roots(record: dict) -> SectionRoots:
    """The root groups a record is merged against: every Section<digits> name resolves to it."""
    return SectionRoots({ROOT_GROUP: [record]})


def _add_once(items: list, item: str) -> None:
    if item not in items:
        items.append(item)


def _parse_table(tbl):
    """A body-level w:tbl -> ({'table': tbl, 'children': [...]}, set()) when its row markers pair
    up by name, or ({'block': tbl}, names) when they do not (requirement 7: reported, not raised).
    """
    rows = [child for child in tbl if child.tag == _TR]
    row_marker_list = [row_markers(row) for row in rows]

    starts: Counter = Counter()
    ends: Counter = Counter()
    for markers in row_marker_list:
        for kind, name in markers:
            (starts if kind == "start" else ends)[name] += 1

    names = set(starts) | set(ends)
    if any(starts[name] != ends[name] for name in names):
        return {"block": tbl}, names

    root: list = []
    stack = [root]
    open_groups: list[str] = []
    for row, markers in zip(rows, row_marker_list):
        for kind, name in markers:
            if kind == "start":
                node = {"group": name, "children": []}
                stack[-1].append(node)
                stack.append(node["children"])
                open_groups.append(name)
        stack[-1].append({"block": row})
        for kind, name in markers:
            if kind == "end":
                if not open_groups or open_groups[-1] != name:
                    closes = f"TableStart:{open_groups[-1]}" if open_groups else "no open group"
                    raise TemplateStructureError(f"TableEnd:{name} does not close {closes}")
                stack.pop()
                open_groups.pop()

    return {"table": tbl, "children": root}, set()


def _parse_tree(blocks: list) -> list:
    """Body blocks -> nested nodes: {'block': el}, {'table': el, 'children': [...]} or
    {'group': name, 'children': [...]}."""
    table_nodes: dict = {}
    orphans: set[str] = set()
    for block in blocks:
        if block.tag == _TBL:
            node, names = _parse_table(block)
            table_nodes[id(block)] = node
            if "block" in node:
                orphans |= names

    root: list = []
    stack = [root]
    open_groups: list[str] = []

    for block in blocks:
        if block.tag == _TBL:
            stack[-1].append(table_nodes[id(block)])
            continue
        try:
            marker = group_marker(block)
        except ValueError as exc:
            raise TemplateStructureError(str(exc)) from exc
        if marker and marker[1] in orphans:
            marker = None
        if marker and marker[0] == "start":
            node = {"group": marker[1], "children": []}
            stack[-1].append(node)
            stack.append(node["children"])
            open_groups.append(marker[1])
        elif marker and marker[0] == "end":
            if not open_groups or open_groups[-1] != marker[1]:
                closes = f"TableStart:{open_groups[-1]}" if open_groups else "no open group"
                raise TemplateStructureError(f"TableEnd:{marker[1]} does not close {closes}")
            stack.pop()
            open_groups.pop()
        else:
            stack[-1].append({"block": block})

    if open_groups:
        raise TemplateStructureError(f"unclosed TableStart:{open_groups[-1]}")
    return root


def _resolve_value(scope: list, name: str):
    """Innermost record first, then outwards. (found, value)."""
    for record in scope:
        if isinstance(record, dict) and name in record:
            return True, record[name]
    return False, None


def _group_records(scope: list, roots: dict, name: str, result: MergeResult) -> list:
    """The records a group repeats over: read from the innermost record only."""
    source = scope[0] if scope else roots
    if not (isinstance(source, dict) and name in source):
        _add_once(result.groups_missing, name)
        return []
    value = source[name]
    if isinstance(value, dict):
        return [value]
    if isinstance(value, (list, tuple)):
        return list(value)
    return []


def _scrub_copy(element) -> None:
    """Drop bookmarks and paragraph ids from a repeated copy so ids stay unique."""
    for node in list(element.iter(*_BOOKMARKS)):
        node.getparent().remove(node)
    for node in element.iter():
        for attr in _SCRUB_ATTRS:
            node.attrib.pop(attr, None)


def _strip_markers(row) -> None:
    """Remove the row's own group marker fields, leaving the rest of each cell's content."""
    for paragraph in row_paragraphs(row):
        for field in iter_merge_fields(paragraph):
            if GROUP_MARKER.match(field.name):
                replace_field(field, [])


def _table_shell(tbl):
    """A copy of `tbl` with its properties and grid but no rows, ready for rendered rows."""
    shell = copy.deepcopy(tbl)
    for row in list(shell.findall(_TR)):
        shell.remove(row)
    return shell


def _apply_blocks(paragraph, blocks: list) -> None:
    """Splice pre-built blocks in for a paragraph whose field asked for them."""
    has_content = any(child.tag != _PPR for child in paragraph)
    parent = paragraph.getparent()
    anchor = paragraph
    for block in blocks:
        anchor.addnext(block)
        anchor = block
    if not has_content:
        parent.remove(paragraph)
    if parent.tag == _TC and len(parent) and parent[-1].tag != P:
        parent.append(OxmlElement("w:p"))


def _fill_fields(element, scope: list, keep_unmatched: bool, renderer, result: MergeResult) -> None:
    paragraphs = [element] if element.tag == P else []
    paragraphs.extend(p for p in element.iter(P) if p is not element)

    for paragraph in paragraphs:
        block_results: list = []
        for field in iter_merge_fields(paragraph):
            # A group marker reaching this point is one the region walk could not see, for
            # example inside a table cell. Blanking it silently would drop a repeating region
            # without a word, so it is reported.
            if GROUP_MARKER.match(field.name):
                _add_once(result.unsupported_markers, field.name)
                replace_field(field, [])
                continue

            found, value = _resolve_value(scope, field.name)
            if not found:
                _add_once(result.unmatched, field.name)
                if not keep_unmatched:
                    replace_field(field, [])
                continue

            result.fields_merged += 1
            source = field.format_source
            rpr = source.find(RPR)
            ppr = paragraph.find(_PPR)
            ctx = FieldContext(
                name=field.name,
                value="" if value is None else str(value),
                run_properties=None if rpr is None else copy.deepcopy(rpr),
                paragraph_properties=None if ppr is None else copy.deepcopy(ppr),
                in_table_cell=paragraph.getparent() is not None and paragraph.getparent().tag == _TC,
            )
            rendered = renderer(ctx)
            if isinstance(rendered, str):
                replace_field(field, [make_run(rendered, source)])
            elif isinstance(rendered, list):
                replace_field(field, [])
                block_results.extend(rendered)
            elif getattr(rendered, "tag", None) == R:
                # A single run, built with its own formatting (for example a colour override):
                # stays inline in place of just the field's own runs, not a paragraph splice.
                replace_field(field, [rendered])
            else:
                raise TypeError(f"renderer for field {field.name} returned {type(rendered).__name__}")

        if block_results:
            _apply_blocks(paragraph, block_results)


def _render(nodes: list, scope: list, roots: dict, keep_unmatched: bool, renderer,
            result: MergeResult, seen: set) -> list:
    out = []
    for node in nodes:
        if "block" in node:
            source = node["block"]
            element = copy.deepcopy(source)
            if id(source) in seen:
                _scrub_copy(element)
            seen.add(id(source))
            if element.tag == _TR:
                _strip_markers(element)
            # A holder gives a top-level paragraph a parent, so a renderer's blocks can be
            # spliced in beside it.
            holder = OxmlElement("w:body")
            holder.append(element)
            _fill_fields(element, scope, keep_unmatched, renderer, result)
            out.extend(list(holder))
            continue
        if "table" in node:
            shell = _table_shell(node["table"])
            for row in _render(node["children"], scope, roots, keep_unmatched, renderer, result, seen):
                shell.append(row)
            if shell.find(_TR) is not None:
                out.append(shell)
            continue
        name = node["group"]
        records = _group_records(scope, roots, name, result)
        result.groups[name] = result.groups.get(name, 0) + len(records)
        for record in records:
            out.extend(_render(node["children"], [record, *scope], roots, keep_unmatched,
                               renderer, result, seen))
    return out


def _scan_unsupported(document, blocks: list, result: MergeResult) -> None:
    """Report what the engine will not merge: header and footer markers, body simple fields."""
    for block in blocks:
        for node in block.iter(_FLDSIMPLE):
            instr = node.get(_INSTR_ATTR) or ""
            if "MERGEFIELD" in instr.upper():
                _add_once(result.unsupported_fields, parse_field_name(instr))

    for rel in document.part.rels.values():
        if rel.is_external or not (rel.reltype.endswith("/header") or rel.reltype.endswith("/footer")):
            continue
        for paragraph in rel.target_part.element.iter(P):
            for field in iter_merge_fields(paragraph):
                if GROUP_MARKER.match(field.name):
                    _add_once(result.unsupported_markers, field.name)


def merge_document(document, roots: dict, *, keep_unmatched: bool = False,
                   renderer: Renderer | None = None) -> MergeResult:
    """Merge `roots` into an opened python-docx Document, in place."""
    renderer = renderer or plain_text_renderer
    result = MergeResult()
    body = document.element.body
    blocks = [child for child in body if child.tag != _SECTPR]
    tree = _parse_tree(blocks)
    _scan_unsupported(document, blocks, result)
    rendered = _render(tree, [], roots, keep_unmatched, renderer, result, set())

    for block in blocks:
        body.remove(block)
    sect_pr = body.find(_SECTPR)
    for element in rendered:
        if sect_pr is not None:
            sect_pr.addprevious(element)
        else:
            body.append(element)
    _style_hyperlink_fields(body)
    return result


_HYPERLINK_INSTR = re.compile(r'HYPERLINK\s+"([^"]+)"')


def _style_hyperlink_fields(body) -> None:
    """Underline and colour blue the visible text of every `HYPERLINK "..."` complex field.

    ReportProcessor.cs 257-292 on the base did this after the merge: the run whose text equals the
    field's result text got `UnderlineStyle.Single` and `Color.Blue` (0000FF). An ordinary
    `w:hyperlink` element is not a field code and is left alone, as .NET left it.
    """
    for paragraph in body.iter(qn("w:p")):
        stack: list[dict] = []
        for run in paragraph.findall(qn("w:r")):
            fld = run.find(qn("w:fldChar"))
            kind = None if fld is None else fld.get(qn("w:fldCharType"))
            if kind == "begin":
                stack.append({"instr": "", "separated": False, "runs": []})
            elif kind == "separate" and stack:
                stack[-1]["separated"] = True
            elif kind == "end" and stack:
                state = stack.pop()
                if _HYPERLINK_INSTR.search(state["instr"]):
                    for result_run in state["runs"]:
                        _blue_underline(result_run)
            elif stack:
                if not stack[-1]["separated"]:
                    stack[-1]["instr"] += "".join(i.text or "" for i in run.findall(qn("w:instrText")))
                elif run.find(qn("w:t")) is not None:
                    stack[-1]["runs"].append(run)


def _blue_underline(run) -> None:
    rpr = run.find(qn("w:rPr"))
    if rpr is None:
        rpr = OxmlElement("w:rPr")
        run.insert(0, rpr)
    for tag, attr, value in (("w:color", "w:val", "0000FF"), ("w:u", "w:val", "single")):
        el = rpr.find(qn(tag))
        if el is None:
            el = OxmlElement(tag)
            rpr.append(el)
        el.set(qn(attr), value)


def fill_template(template_path, output_path, record: dict, *, keep_unmatched: bool = False,
                  renderer: Renderer | None = None, on_open=None,
                  before_save=None) -> MergeResult:
    """Fill the Word template at `template_path` from `record` and save it at `output_path`.

    `on_open(document)` runs after the template opens and `before_save(document)` runs before it
    is saved, for a caller that adds pictures to the document part and resizes them (PBI-083).
    """
    document = docx.Document(str(template_path))
    if on_open is not None:
        on_open(document)
    result = merge_document(document, bind_roots(record), keep_unmatched=keep_unmatched,
                            renderer=renderer)
    if before_save is not None:
        before_save(document)
    document.save(str(output_path))
    logging.info("[WordMerge][fill_template] merged %s fields, groups %s", result.fields_merged,
                 result.groups)
    if result.unmatched:
        logging.warning("[WordMerge][fill_template] unmatched fields: %s", result.unmatched)
    if result.unsupported_markers:
        logging.warning("[WordMerge][fill_template] unsupported group markers: %s",
                        result.unsupported_markers)
    if result.unsupported_fields:
        logging.warning("[WordMerge][fill_template] unsupported simple fields: %s",
                        result.unsupported_fields)
    if result.groups_missing:
        logging.warning("[WordMerge][fill_template] groups with no records key: %s",
                        result.groups_missing)
    return result
