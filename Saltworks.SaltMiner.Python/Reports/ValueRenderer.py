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

# The value renderer PBI-047's merge engine calls through its `Renderer` hook
# (`Reports/WordMerge.py`, `FieldContext` and `Renderer`): markdown for a field whose name is in
# the markdown set, coloured plain text for a field whose value matches the field-value colour
# configuration, plain text otherwise.
#
# A coloured value returns a single `w:r`, the inline-run shape `Renderer` accepts alongside
# `str` and `list`: the merge engine splices it in place of just the field's own runs, so a field
# that shares its paragraph with other static text (most of them do; only ten paragraphs in the
# shipped template do not) keeps that text in place, exactly like the .NET original's in-place
# `CharacterFormat.TextColor` assignment (`ReportProcessor.cs:924-938`). A `list` block splice
# would replace the whole paragraph instead, dropping any of that shared text.

from __future__ import annotations

from dataclasses import dataclass, field as dc_field
from typing import Callable, Iterable

from docx.oxml import OxmlElement
from docx.oxml.ns import qn

from Reports.Colors import resolve_color
from Reports.Markdown import render_markdown
from Reports.WordFields import RPR, make_run
from Reports.WordMerge import FieldContext


@dataclass
class ValueRenderResult:
    """What the value renderer did, so the caller can assert on it or fold it into a job log."""

    markdown_fields_rendered: int = 0
    colored_values: int = 0
    unknown_colors: list = dc_field(default_factory=list)
    warnings: list = dc_field(default_factory=list)  # the image embedder's own list


def _add_once(items: list, item: str) -> None:
    if item not in items:
        items.append(item)


def _colored_run(text: str, rpr_source, hex_color: str):
    run = make_run(text, rpr_source)
    rpr = run.find(RPR)
    if rpr is None:
        rpr = OxmlElement("w:rPr")
        run.insert(0, rpr)
    existing = rpr.find(qn("w:color"))
    if existing is not None:
        rpr.remove(existing)
    color_el = OxmlElement("w:color")
    color_el.set(qn("w:val"), hex_color)
    rpr.append(color_el)
    return run


def make_value_renderer(
    markdown_fields: Iterable[str],
    field_value_colors: dict,
    images=None,
) -> tuple[Callable[[FieldContext], "str | object | list"], ValueRenderResult]:
    """A `Renderer` for PBI-047's merge engine, and the result object it fills in as it runs.

    `markdown_fields` names the fields whose value is markdown. `field_value_colors` is the
    caller's `JobManagerConfig.FieldValueColorCustomizations`, in the shape that setting has:
    keyed by the lowercased merged VALUE, not the field name, valued by a
    `System.Drawing.KnownColor` name (`ReportProcessor.cs:924-938`). No deployment ships a
    default for that setting today (`JobManagerConfig.cs:82` defaults it to `{}`), so this
    function reads only whatever `field_value_colors` it is given; it defines none itself.
    """
    markdown_names = frozenset(markdown_fields)
    result = ValueRenderResult()
    if images is not None:
        result.warnings = images.warnings

    def render(ctx: FieldContext):
        if ctx.name in markdown_names:
            blocks = render_markdown(ctx.value, ctx.run_properties, ctx.paragraph_properties,
                                     images)
            if blocks:
                result.markdown_fields_rendered += 1
            return blocks

        wanted = field_value_colors.get(ctx.value.strip().lower()) if ctx.value else None
        if not wanted:
            return ctx.value

        hex_color = resolve_color(wanted)
        if hex_color is None:
            _add_once(result.unknown_colors, wanted)
            return ctx.value

        result.colored_values += 1
        return _colored_run(ctx.value, ctx.run_properties, hex_color)

    return render, result
