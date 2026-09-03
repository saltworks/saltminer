from smreport.context import LINE_BREAK, SENTINEL_RE, prepare_context, resolve_color


def test_markdown_fields_are_tagged_at_any_depth():
    raw = {"Proof": "x", "IssueDetails": [{"Details": "**b**", "Name": "n"}]}
    prep = prepare_context(raw)
    assert SENTINEL_RE.fullmatch(prep.context["Proof"])
    assert SENTINEL_RE.fullmatch(prep.context["IssueDetails"][0]["Details"])
    assert prep.context["IssueDetails"][0]["Name"] == "n"
    assert {e.kind for e in prep.entries.values()} == {"md"}


def test_value_colors_and_none_and_newlines():
    raw = {"Severity": "Critical", "Note": None, "Summary": "a\r\nb\nc"}
    prep = prepare_context(raw, value_colors={"critical": "Red"})
    sev = prep.context["Severity"]
    assert SENTINEL_RE.fullmatch(sev)
    entry = prep.entries[int(SENTINEL_RE.fullmatch(sev).group(1))]
    assert entry.kind == "color" and entry.color == "FF0000" and entry.text == "Critical"
    assert prep.context["Note"] == ""
    assert prep.context["Summary"] == f"a{LINE_BREAK}b{LINE_BREAK}c"


def test_resolve_color():
    assert resolve_color("Red") == "FF0000"
    assert resolve_color("#c00000") == "C00000"
    assert resolve_color("NoSuchColor") is None
