"""Convert a Syncfusion mail-merge template into a docxtpl (Jinja) template.

    python -m smreport.convert in.docx out.docx [--report]

Rules (tag substitution only — no document redesign):

* ``«TableStart:X»`` / ``«TableEnd:X»`` → ``{%p for <var> in <expr> %}`` / ``{%p endfor %}``
  when the marker is alone in its paragraph (``{%tr ... %}`` when the start and end sit in
  different rows of the same table; plain ``{% ... %}`` when inline with other text).
* the outermost ``SectionN`` group is the document itself: its markers are removed.
* ``«Field»`` → ``{{ Field }}`` at the top level, ``{{ <var>.Field }}`` inside a group.
* ``«EngagementAttributes|key»`` / ``«IssueAttributes|key»`` → ``EngagementAttribute_key`` /
  ``IssueAttribute_key`` — the property names the DTO actually exposes.
* the run formatting of the field's displayed text is kept on the new tag run.
"""

from __future__ import annotations

import argparse
import re
import sys
from copy import deepcopy
from dataclasses import dataclass

from docx import Document
from docx.oxml import OxmlElement
from docx.oxml.ns import qn

from smreport.postprocess import story_parts

GROUP_VARS = {
    "AssetTocs": "asset",
    "IssueTocs": "toc",
    "SeverityGroups": "group",
    "Issues": "issue",
    "IssueDetails": "detail",
    "IssueDetailsRemoved": "detail",
    "IssueDetailsAll": "detail",
    "IssueSummary": "item",
    "IssueSummaryRemoved": "item",
    "IssueSummaryAll": "item",
}
_SECTION_RE = re.compile(r"^Section\d+$", re.IGNORECASE)
_MERGEFIELD_RE = re.compile(r"MERGEFIELD\s+(?:\"([^\"]+)\"|(\S+))", re.IGNORECASE)
_ATTR_RE = re.compile(r"^(EngagementAttribute|IssueAttribute)s?\|(.+)$")


@dataclass
class Field:
    paragraph: object
    start: int          # index of first child element of the field within the paragraph
    end: int            # index of last child element (inclusive)
    name: str
    rpr: object         # rPr of the displayed result run (may be None)


@dataclass
class Conversion:
    name: str
    tag: str
    placement: str      # "paragraph" | "row" | "inline" | "removed"


def field_expression(name: str, stack: list[tuple[str, str]]) -> str:
    m = _ATTR_RE.match(name)
    if m:
        name = f"{m.group(1)}_{m.group(2)}"
    name = name.replace("|", "_")
    if stack:
        return f"{stack[-1][1]}.{name}"
    return name


def group_expression(group: str, stack: list[tuple[str, str]]) -> tuple[str, str]:
    var = GROUP_VARS.get(group, "item")
    if any(v == var for _, v in stack):
        var = f"{var}{len(stack)}"
    expr = f"{stack[-1][1]}.{group}" if stack else group
    return var, expr


def _scan_fields(p) -> list[Field]:
    """Complex fields (fldChar begin…end) and fldSimple elements directly under ``p``."""
    fields: list[Field] = []
    children = list(p)
    depth = 0
    start = None
    instr = ""
    rpr = None
    in_result = False
    for i, child in enumerate(children):
        if child.tag == qn("w:fldSimple"):
            m = _MERGEFIELD_RE.search(child.get(qn("w:instr"), ""))
            if m:
                r = child.find(qn("w:r"))
                fields.append(Field(p, i, i, m.group(1) or m.group(2), r.find(qn("w:rPr")) if r is not None else None))
            continue
        if child.tag != qn("w:r"):
            continue
        for fc in child.findall(qn("w:fldChar")):
            t = fc.get(qn("w:fldCharType"))
            if t == "begin":
                depth += 1
                if depth == 1:
                    start, instr, rpr, in_result = i, "", None, False
            elif t == "separate" and depth == 1:
                in_result = True
            elif t == "end":
                depth -= 1
                if depth == 0 and start is not None:
                    m = _MERGEFIELD_RE.search(instr)
                    if m:
                        fields.append(Field(p, start, i, m.group(1) or m.group(2), rpr))
                    start = None
        if depth >= 1 and start is not None:
            for it in child.findall(qn("w:instrText")):
                instr += it.text or ""
            if in_result and rpr is None and child.find(qn("w:t")) is not None:
                rpr = child.find(qn("w:rPr"))
    return fields


def _paragraph_text(p) -> str:
    return "".join(t.text or "" for t in p.iter(qn("w:t")))


def _is_sole_field(p, f: Field) -> bool:
    text = _paragraph_text(p).strip()
    return text in ("", f"«{f.name}»") and p.find(f".//{qn('w:drawing')}") is None


def _enclosing(p, tag: str):
    el = p.getparent()
    while el is not None and el.tag != tag:
        el = el.getparent()
    return el


def _replace_with_run(p, f: Field, text: str):
    children = list(p)
    for el in children[f.start:f.end + 1]:
        p.remove(el)
    r = OxmlElement("w:r")
    if f.rpr is not None:
        r.append(deepcopy(f.rpr))
    if text:
        t = OxmlElement("w:t")
        t.text = text
        t.set(qn("xml:space"), "preserve")
        r.append(t)
    p.insert(f.start, r)


def convert_document(document) -> list[Conversion]:
    report: list[Conversion] = []
    for root, _part in story_parts(document):
        paragraphs = list(root.iter(qn("w:p")))
        scanned = [(p, _scan_fields(p)) for p in paragraphs]
        # first pass: decide {%tr for group markers whose start/end share a table
        group_tables: dict[str, list] = {}
        for p, fields in scanned:
            for f in fields:
                if f.name.startswith(("TableStart:", "TableEnd:")):
                    group_tables.setdefault(f.name.split(":", 1)[1], []).append(_enclosing(p, qn("w:tbl")))
        row_groups = {g for g, tbls in group_tables.items()
                      if len(tbls) == 2 and tbls[0] is not None and tbls[0] is tbls[1]
                      and _enclosing_rows_differ(scanned, g)}
        stack: list[tuple[str, str]] = []
        for p, fields in scanned:
            if not fields:
                continue
            removals = []
            for f in reversed(fields):  # right-to-left so indexes stay valid
                sole = _is_sole_field(p, f) and len(fields) == 1
                if f.name.startswith("TableStart:"):
                    group = f.name.split(":", 1)[1]
                    if not stack and _SECTION_RE.match(group):
                        stack.append((group, ""))
                        removals.append(f)
                        report.append(Conversion(f.name, "", "removed"))
                        continue
                    var, expr = group_expression(group, [s for s in stack if s[1]])
                    stack.append((group, var))
                    prefix = "%tr" if group in row_groups else ("%p" if sole else "%")
                    tag = f"{{{prefix} for {var} in {expr} %}}"
                    _replace_with_run(p, f, tag)
                    report.append(Conversion(f.name, tag, {"%tr": "row", "%p": "paragraph"}.get(prefix, "inline")))
                elif f.name.startswith("TableEnd:"):
                    group = f.name.split(":", 1)[1]
                    if stack and stack[-1][0] == group:
                        _, var = stack.pop()
                    else:
                        var = None
                        print(f"warning: TableEnd:{group} without matching start", file=sys.stderr)
                    if var == "" and not stack:
                        removals.append(f)
                        report.append(Conversion(f.name, "", "removed"))
                        continue
                    prefix = "%tr" if group in row_groups else ("%p" if sole else "%")
                    tag = f"{{{prefix} endfor %}}"
                    _replace_with_run(p, f, tag)
                    report.append(Conversion(f.name, tag, {"%tr": "row", "%p": "paragraph"}.get(prefix, "inline")))
                else:
                    tag = f"{{{{ {field_expression(f.name, [s for s in stack if s[1]])} }}}}"
                    _replace_with_run(p, f, tag)
                    report.append(Conversion(f.name, tag, "inline"))
            for f in removals:
                _remove_field(p, f)
    return report


def _enclosing_rows_differ(scanned, group: str) -> bool:
    rows = []
    for p, fields in scanned:
        for f in fields:
            if f.name in (f"TableStart:{group}", f"TableEnd:{group}"):
                rows.append(_enclosing(p, qn("w:tr")))
    return len(rows) == 2 and rows[0] is not None and rows[0] is not rows[1]


def _remove_field(p, f: Field):
    """Remove a Section marker: the whole paragraph when it holds nothing else."""
    if _is_sole_field(p, f):
        parent = p.getparent()
        if parent is not None and parent.tag == qn("w:tc") and len(parent.findall(qn("w:p"))) == 1:
            _replace_with_run(p, f, "")
        elif parent is not None:
            parent.remove(p)
    else:
        _replace_with_run(p, f, "")


def convert_file(src: str, dest: str) -> list[Conversion]:
    document = Document(src)
    report = convert_document(document)
    document.save(dest)
    return report


def main(argv=None) -> int:
    ap = argparse.ArgumentParser(prog="smreport.convert", description="Syncfusion merge-field template → docxtpl template")
    ap.add_argument("src")
    ap.add_argument("dest")
    ap.add_argument("--report", action="store_true", help="print the field → tag mapping")
    args = ap.parse_args(argv)
    report = convert_file(args.src, args.dest)
    if args.report:
        width = max((len(c.name) for c in report), default=10)
        for c in report:
            print(f"{c.name:<{width}}  {c.placement:<9}  {c.tag}")
    print(f"converted {len(report)} fields -> {args.dest}", file=sys.stderr)
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
