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

# Word complex-field reading and rewriting, on top of python-docx.
#
# A DOCX merge field is not one element. It is a run sequence:
#
#     <w:r><w:fldChar w:fldCharType="begin"/></w:r>
#     <w:r><w:instrText> MERGEFIELD  Severity </w:instrText></w:r>
#     <w:r><w:fldChar w:fldCharType="separate"/></w:r>
#     <w:r><w:rPr>..</w:rPr><w:t>&#171;Severity&#187;</w:t></w:r>   <- cached result
#     <w:r><w:fldChar w:fldCharType="end"/></w:r>
#
# Everything in this module works on that sequence. Reading it gives the field name; rewriting it
# means putting runs in the result run's place and deleting the field runs, which is what leaves
# no MERGEFIELD instruction behind.

from __future__ import annotations

import copy
import re
from dataclasses import dataclass, field as dc_field

from docx.oxml import OxmlElement
from docx.oxml.ns import qn

P = qn("w:p")
R = qn("w:r")
RPR = qn("w:rPr")
FLDCHAR = qn("w:fldChar")
FLDCHARTYPE = qn("w:fldCharType")
INSTRTEXT = qn("w:instrText")
XML_SPACE = "{http://www.w3.org/XML/1998/namespace}space"

# Field switches such as \* MERGEFORMAT are not part of the name.
_SWITCHES = re.compile(r"\s+\\[*#@!].*$", re.DOTALL)

GROUP_MARKER = re.compile(r"^Table(Start|End):(.+)$")


@dataclass
class MergeField:
    """One MERGEFIELD complex field, as a set of runs to be replaced."""

    name: str
    begin: object
    instr: list = dc_field(default_factory=list)
    separate: object = None
    result: list = dc_field(default_factory=list)
    end: object = None

    @property
    def format_source(self):
        """The run whose formatting the merged text inherits."""
        return self.result[0] if self.result else self.begin

    def all_runs(self) -> list:
        runs = [self.begin, *self.instr, *self.result]
        if self.separate is not None:
            runs.append(self.separate)
        if self.end is not None:
            runs.append(self.end)
        return runs


def parse_field_name(instr_text: str) -> str:
    """' MERGEFIELD  Severity \\* MERGEFORMAT ' -> 'Severity'."""
    text = instr_text.strip()
    if text.upper().startswith("MERGEFIELD"):
        text = text[len("MERGEFIELD"):]
    text = _SWITCHES.sub("", text).strip()
    if len(text) > 1 and text[0] == '"' and text[-1] == '"':
        text = text[1:-1]
    return text.strip()


def _fldchar_type(run) -> str | None:
    fld = run.find(FLDCHAR)
    return None if fld is None else fld.get(FLDCHARTYPE)


def iter_merge_fields(paragraph_el) -> list[MergeField]:
    """Every MERGEFIELD complex field in one paragraph, in document order.

    Nested fields are handled with a stack, so a field sitting inside another field's result does
    not corrupt the outer one.
    """
    stack: list[dict] = []
    found: list[MergeField] = []

    for run in paragraph_el.iter(R):
        kind = _fldchar_type(run)
        if kind == "begin":
            stack.append({"begin": run, "instr": [], "separate": None, "result": [], "in_instr": True})
            continue
        if kind == "separate":
            if stack:
                stack[-1]["separate"] = run
                stack[-1]["in_instr"] = False
            continue
        if kind == "end":
            if not stack:
                continue
            raw = stack.pop()
            instr_text = "".join(node.text or "" for r in raw["instr"] for node in r.iter(INSTRTEXT))
            if "MERGEFIELD" in instr_text.upper():
                found.append(
                    MergeField(
                        name=parse_field_name(instr_text),
                        begin=raw["begin"],
                        instr=raw["instr"],
                        separate=raw["separate"],
                        result=raw["result"],
                        end=run,
                    )
                )
            continue
        if stack:
            stack[-1]["instr" if stack[-1]["in_instr"] else "result"].append(run)

    return found


def group_marker(block_el):
    """('start'|'end', group name) when a block is a lone group marker, otherwise None.

    Raises ValueError when a paragraph mixes a marker with another field, which would otherwise
    merge silently wrong. The message names the marker.
    """
    if block_el.tag != P:
        return None
    fields = iter_merge_fields(block_el)
    hits = [(f, GROUP_MARKER.match(f.name)) for f in fields]
    hits = [(f, m) for f, m in hits if m]
    if not hits:
        return None
    if len(fields) != 1:
        raise ValueError(
            f"paragraph mixes group marker {hits[0][0].name} with other fields: "
            f"{[f.name for f in fields]}"
        )
    match = hits[0][1]
    return ("start" if match.group(1) == "Start" else "end"), match.group(2)


def make_run(text: str, rpr_source=None):
    """A new w:r carrying `text`, wearing the w:rPr of `rpr_source` when one is given.

    Newlines become <w:br/>, which is how Word represents a line break inside a paragraph.
    """
    run = OxmlElement("w:r")
    if rpr_source is not None:
        rpr = rpr_source if rpr_source.tag == RPR else rpr_source.find(RPR)
        if rpr is not None:
            run.append(copy.deepcopy(rpr))

    for index, part in enumerate(str(text).split("\n")):
        if index:
            run.append(OxmlElement("w:br"))
        if part:
            t = OxmlElement("w:t")
            t.set(XML_SPACE, "preserve")
            t.text = part
            run.append(t)
    return run


def replace_field(field: MergeField, runs: list) -> None:
    """Put `runs` where the field began and delete every run the field owned."""
    parent = field.begin.getparent()
    at = parent.index(field.begin)
    for offset, run in enumerate(runs):
        parent.insert(at + offset, run)
    for run in field.all_runs():
        owner = run.getparent()
        if owner is not None:
            owner.remove(run)
