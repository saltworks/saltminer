"""Context preparation: sentinel-tagging of values that need post-render treatment.

docxtpl can only substitute plain text into a run.  Anything richer (markdown → rich
text, per-value colouring, line breaks) is handled after rendering by
:mod:`smreport.postprocess`, which finds these sentinel markers in the rendered runs.
"""

from __future__ import annotations

import re
from dataclasses import dataclass, field

# Private-use-area characters: never appear in real report text and survive docxtpl's
# XML escaping untouched.
SENT_START = ""
SENT_END = ""
LINE_BREAK = ""

SENTINEL_RE = re.compile(f"{SENT_START}(\\d+){SENT_END}")

# Mirrors ReportProcessor.MarkdownFieldsList in the C# shim.
DEFAULT_MARKDOWN_FIELDS = [
    "Proof", "ProofText", "ProofImgs",
    "References", "ReferencesText", "ReferencesImgs",
    "Recommendation", "RecommendationText", "RecommendationImgs",
    "Implication", "ImplicationText", "ImplicationImgs",
    "Details", "DetailsText", "DetailsImgs",
    "TestingInstructions", "TestingInstructionsText", "TestingInstructionsImgs",
]

# System.Drawing.Color names the old FieldValueColorCustomizations setting accepted.
# Values are RRGGBB.  Hex strings ("#C00000" / "C00000") are accepted directly.
KNOWN_COLORS = {
    "black": "000000", "white": "FFFFFF", "red": "FF0000", "darkred": "8B0000",
    "crimson": "DC143C", "firebrick": "B22222", "orange": "FFA500", "darkorange": "FF8C00",
    "orangered": "FF4500", "gold": "FFD700", "yellow": "FFFF00", "green": "008000",
    "darkgreen": "006400", "lime": "00FF00", "limegreen": "32CD32", "forestgreen": "228B22",
    "seagreen": "2E8B57", "blue": "0000FF", "darkblue": "00008B", "navy": "000080",
    "royalblue": "4169E1", "dodgerblue": "1E90FF", "steelblue": "4682B4", "purple": "800080",
    "indigo": "4B0082", "magenta": "FF00FF", "gray": "808080", "grey": "808080",
    "darkgray": "A9A9A9", "dimgray": "696969", "silver": "C0C0C0", "brown": "A52A2A",
    "maroon": "800000", "teal": "008080", "cyan": "00FFFF", "olive": "808000",
}

_HEX_RE = re.compile(r"^#?([0-9A-Fa-f]{6})$")


def resolve_color(name: str) -> str | None:
    """Return RRGGBB for a colour name or hex string, or None when unrecognised."""
    if not name:
        return None
    m = _HEX_RE.match(name.strip())
    if m:
        return m.group(1).upper()
    return KNOWN_COLORS.get(name.strip().lower())


@dataclass
class Entry:
    kind: str            # "md" | "color"
    text: str
    color: str | None = None


@dataclass
class Prepared:
    context: dict
    entries: dict[int, Entry] = field(default_factory=dict)

    def add(self, entry: Entry) -> str:
        idx = len(self.entries)
        self.entries[idx] = entry
        return f"{SENT_START}{idx}{SENT_END}"


def prepare_context(raw: dict, markdown_fields=None, value_colors=None) -> Prepared:
    """Tag markdown fields and colour-matched values with sentinels; normalise scalars.

    * markdown fields (matched by key name at any depth) become ``md`` entries
    * any other string whose lower-cased value matches a ``value_colors`` key becomes a
      ``color`` entry (mirrors FieldValueColorCustomizations)
    * newlines in plain strings become LINE_BREAK sentinels (rendered as ``<w:br/>``)
    * ``None`` becomes ``""`` so the document never shows the word "None"
    """
    md_fields = set(markdown_fields if markdown_fields is not None else DEFAULT_MARKDOWN_FIELDS)
    colors = {}
    for k, v in (value_colors or {}).items():
        rgb = resolve_color(v)
        if rgb:
            colors[k.lower()] = rgb
    prepared = Prepared(context={})

    def walk(value, key=None):
        if value is None:
            return ""
        if isinstance(value, str):
            if key in md_fields:
                return prepared.add(Entry("md", value))
            rgb = colors.get(value.lower()) if colors else None
            if rgb:
                return prepared.add(Entry("color", value, rgb))
            return value.replace("\r\n", "\n").replace("\r", "\n").replace("\n", LINE_BREAK)
        if isinstance(value, dict):
            return {k: walk(v, k) for k, v in value.items()}
        if isinstance(value, list):
            return [walk(v, key) for v in value]
        return value

    prepared.context = walk(raw)
    return prepared
