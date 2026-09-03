from smreport.markdown import parse


def kinds(blocks):
    return [b.kind for b in blocks]


def test_empty():
    assert parse("") == []
    assert parse("   \n") == []


def test_inline_formatting():
    (b,) = parse("plain **bold** *it* ~~gone~~ `code`")
    flags = [(i.text, i.bold, i.italic, i.strike, i.code) for i in b.inlines]
    assert ("bold", True, False, False, False) in flags
    assert ("it", False, True, False, False) in flags
    assert ("gone", False, False, True, False) in flags
    assert ("code", False, False, False, True) in flags


def test_link_and_autolink():
    (b,) = parse("see [OWASP](https://owasp.org) or https://example.com")
    links = [(i.text, i.link) for i in b.inlines if i.link]
    assert ("OWASP", "https://owasp.org") in links
    assert ("https://example.com", "https://example.com") in links


def test_image_inline():
    (b,) = parse("shot ![alt text](images/x.png)")
    img = [i for i in b.inlines if i.kind == "image"]
    assert img and img[0].src == "images/x.png" and img[0].alt == "alt text"


def test_single_newline_splits_paragraph_like_old_renderer():
    blocks = parse("line one\nline two")
    assert kinds(blocks) == ["paragraph", "paragraph"]
    assert blocks[0].inlines[0].text == "line one"


def test_html_br_splits_paragraph():
    blocks = parse("a<br>b<br/>c")
    assert [b.inlines[0].text for b in blocks] == ["a", "b", "c"]


def test_lists_nested_and_ordered():
    blocks = parse("- one\n  - two\n- three\n\n1. n1\n2. n2")
    items = [b for b in blocks if b.kind == "list_item"]
    assert [(b.level, b.ordered) for b in items] == [(0, False), (1, False), (0, False), (0, True), (0, True)]
    # bullet list and ordered list get different ids so numbering restarts
    assert items[0].list_id != items[3].list_id


def test_code_block_and_span():
    blocks = parse("```http\nGET / HTTP/1.1\nHost: x\n```")
    assert blocks[0].kind == "code"
    assert blocks[0].inlines[0].text == "GET / HTTP/1.1\nHost: x"


def test_table():
    (b,) = parse("| h1 | h2 |\n|---|---|\n| a | b |")
    assert b.kind == "table" and b.header and len(b.rows) == 2
    assert b.rows[1][1][0].text == "b"


def test_quote_heading_rule():
    blocks = parse("> quoted\n\n---\n\n## Head\ntext")
    assert kinds(blocks) == ["quote", "hr", "heading", "paragraph"]
    assert blocks[2].level == 2


def test_escapes_are_unescaped():
    (b,) = parse(r"1\. not a list \*not italic\*")
    assert "".join(i.text for i in b.inlines) == "1. not a list *not italic*"
