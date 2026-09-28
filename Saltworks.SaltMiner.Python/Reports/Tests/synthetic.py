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


def _instr_text_run(instr):
    run = OxmlElement("w:r")
    node = OxmlElement("w:instrText")
    node.set(qn("xml:space"), "preserve")
    node.text = instr
    run.append(node)
    return run


def _bookmark_start(paragraph, bookmark_id, name):
    node = OxmlElement("w:bookmarkStart")
    node.set(qn("w:id"), str(bookmark_id))
    node.set(qn("w:name"), name)
    paragraph._p.append(node)


def _bookmark_end(paragraph, bookmark_id):
    node = OxmlElement("w:bookmarkEnd")
    node.set(qn("w:id"), str(bookmark_id))
    paragraph._p.append(node)


def add_pageref_field(paragraph, bookmark_name, cached_page):
    """Append a PAGEREF complex field whose cached result is `cached_page`, deliberately wrong
    until an index update recalculates it against `bookmark_name`'s real page."""
    element = paragraph._p
    element.append(_fld_run("begin"))
    element.append(_instr_text_run(f" PAGEREF {bookmark_name} \\h "))
    element.append(_fld_run("separate"))
    element.append(_text_run(str(cached_page)))
    element.append(_fld_run("end"))


def add_wrong_toc_fixture(document, entries):
    """Build a `TOC \\o "1-3" \\h` field, one cached entry per `(bookmark_name, heading_text,
    cached_page)` in `entries`, each entry's PAGEREF field carrying `cached_page` regardless of
    where its heading actually lands. Used by AC-14: the field and index refresh a real PDF
    conversion runs must recalculate every one of these against the heading's true page.
    """
    toc_paragraph = document.add_paragraph()
    element = toc_paragraph._p
    element.append(_fld_run("begin"))
    element.append(_instr_text_run(' TOC \\o "1-3" \\h '))
    element.append(_fld_run("separate"))
    for index, (bookmark_name, heading_text, cached_page) in enumerate(entries):
        if index:
            element.append(OxmlElement("w:r"))
            element[-1].append(OxmlElement("w:br"))
        element.append(_text_run(f"{heading_text}\t"))
        add_pageref_field(toc_paragraph, bookmark_name, cached_page)
    element.append(_fld_run("end"))
    return toc_paragraph


def add_heading_with_bookmark(document, bookmark_id, bookmark_name, heading_text, level=1):
    """A `Heading <level>` paragraph wrapped in a bookmark a PAGEREF field can point to."""
    paragraph = document.add_heading(heading_text, level=level)
    _bookmark_start(paragraph, bookmark_id, bookmark_name)
    # Re-append the run(s) python-docx already created after the bookmark start, and the
    # matching end after them, so the bookmark spans the heading text as Word itself would write it.
    runs = list(paragraph._p.findall(qn("w:r")))
    element = paragraph._p
    bookmark_start = element.findall(qn("w:bookmarkStart"))[-1]
    element.remove(bookmark_start)
    first_run = runs[0] if runs else None
    if first_run is not None:
        first_run.addprevious(bookmark_start)
    else:
        element.append(bookmark_start)
    _bookmark_end(paragraph, bookmark_id)
    return paragraph
