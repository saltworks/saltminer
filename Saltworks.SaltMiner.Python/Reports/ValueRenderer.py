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
# `Renderer` only carries two shapes back to the merge engine: a `str` (inline text, dropped into
# the field's own paragraph) or a `list` of block elements (spliced in as whole paragraphs,
# replacing the field's paragraph). A coloured value needs its own `w:color` on the run, which
# the inline `str` path cannot carry, since the merge engine builds that run itself from the
# field's original cached formatting. So a coloured value takes the block path too, as a single
# paragraph carrying one coloured run. This only changes what a reader sees when the field
# already stood alone in its own paragraph, which is how the shipped template uses every
# field-value-coloured field (`Severity`, `TestStatus`).

from __future__ import annotations

import copy
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


def _add_once(items: list, item: str) -> None:
    if item not in items:
        items.append(item)


def _colored_paragraph(text: str, rpr_source, ppr_source, hex_color: str):
    paragraph = OxmlElement("w:p")
    if ppr_source is not None:
        paragraph.append(copy.deepcopy(ppr_source))
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
    paragraph.append(run)
    return paragraph


def make_value_renderer(
    markdown_fields: Iterable[str],
    field_value_colors: dict,
) -> tuple[Callable[[FieldContext], "str | list"], ValueRenderResult]:
    """A `Renderer` for PBI-047's merge engine, and the result object it fills in as it runs.

    `markdown_fields` names the fields whose value is markdown. `field_value_colors` is
    `JobManagerConfig.FieldValueColorCustomizations` as shipped: keyed by the lowercased merged
    VALUE, not the field name, valued by a `System.Drawing.KnownColor` name
    (`ReportProcessor.cs:924-938`).
    """
    markdown_names = frozenset(markdown_fields)
    result = ValueRenderResult()

    def render(ctx: FieldContext):
        if ctx.name in markdown_names:
            blocks = render_markdown(ctx.value, ctx.run_properties, ctx.paragraph_properties)
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
        return [_colored_paragraph(ctx.value, ctx.run_properties, ctx.paragraph_properties, hex_color)]

    return render, result
