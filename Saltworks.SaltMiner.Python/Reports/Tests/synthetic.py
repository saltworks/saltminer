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
# Helpers that build small synthetic Word documents and read text back out of them.

from docx.oxml import OxmlElement
from docx.oxml.ns import qn


def _fld_run(kind):
    run = OxmlElement("w:r")
    fld = OxmlElement("w:fldChar")
    fld.set(qn("w:fldCharType"), kind)
    run.append(fld)
    return run


def _instr_run(name):
    run = OxmlElement("w:r")
    instr = OxmlElement("w:instrText")
    instr.text = f" MERGEFIELD  {name} "
    run.append(instr)
    return run


def _text_run(text):
    run = OxmlElement("w:r")
    t = OxmlElement("w:t")
    t.text = text
    run.append(t)
    return run


def add_field(paragraph, name, result_text=None, inner_name=None):
    """Append the five-run complex field for `name`. `inner_name` nests a second field in the result."""
    element = paragraph._p
    element.append(_fld_run("begin"))
    element.append(_instr_run(name))
    element.append(_fld_run("separate"))
    if inner_name:
        add_field(paragraph, inner_name, result_text=f"«{inner_name}»")
    else:
        element.append(_text_run(result_text or f"«{name}»"))
    element.append(_fld_run("end"))
    return element


def paragraph_text(paragraph_el):
    return "".join(t.text or "" for t in paragraph_el.iter(qn("w:t")))
