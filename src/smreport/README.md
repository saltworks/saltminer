# smreport — SaltMiner engagement report renderer

Standalone Python renderer that replaces the Syncfusion DocIO merge/render step in the
JobManager.  File-based contract: a JSON context (the serialized report DTO) plus a
Jinja-tagged `.docx` template in, `.docx` / `.pdf` out.  It has no dependency on any
other SaltMiner component.

```
python -m smreport --context <context.json> --template <template.docx> \
    --outdir <dir> --name <basename> [--formats docx,pdf] \
    [--image-max-width N] [--image-max-height N] [--static-image-alt-text S] \
    [--font-substitutions JSON] [--markdown-fields a,b,c] [--value-colors JSON] \
    [--no-remote-images] [--soffice PATH] [--soffice-timeout SEC]
```

Exit codes: `0` all requested formats written, `2` bad arguments/input, `3` render
failure, `4` PDF conversion failure.  Diagnostics go to stderr.

## What it does

1. `docxtpl` renders the template against the context (autoescaped).
2. Post-processing with `python-docx`:
   * markdown fields (`Proof`, `Details`, `Implication`, `Recommendation`, `References`,
     `TestingInstructions`, their `*Text`/`*Imgs` variants, and any names passed via
     `--markdown-fields`) become rich text: bold/italic/strike, bullet and numbered
     lists (nested), code spans/blocks, links, images, tables, quotes, headings.
     Text inherits the font/size/colour of the run that held the tag.
   * every hyperlink is styled blue + underlined.
   * pictures wider/taller than the max box (points) are scaled down, preserving aspect
     ratio; pictures whose alt text equals `--static-image-alt-text` are left alone.
   * `--value-colors {"critical":"Red"}` colours any field whose value matches
     (the old `FieldValueColorCustomizations` setting).
   * newlines in plain string values become line breaks; `null` renders as empty.
3. PDF via `soffice --headless --convert-to pdf`.  Fonts the document asks for are
   checked against fontconfig and missing ones are reported on stderr; configured
   `--font-substitutions` are applied to a scratch copy used only for the PDF.

Markdown images: `data:` URIs, local paths (relative to the context file), and — as a
best-effort fallback — http(s) URLs.  Anything needing authentication must be fetched
by the caller and rewritten to a local path first (the C# shim does this).

## Converting a Syncfusion template

```
python -m smreport.convert SaltworksTemplate.docx SaltworksTemplate-jinja.docx --report
```

`«TableStart:X»`/`«TableEnd:X»` become `{%p for … %}`/`{%p endfor %}`, `«Field»` becomes
`{{ Field }}` (or `{{ var.Field }}` inside a group), the outer `SectionN` group is dropped,
and `«EngagementAttributes|key»` / `«IssueAttributes|key»` map to the
`EngagementAttribute_key` / `IssueAttribute_key` names the DTO actually exposes.
Loop variables: `asset`, `group`, `issue`, `toc`, `detail`.

## Development

```
python -m venv .venv && . .venv/bin/activate
pip install -r requirements.txt -e .
pytest
```

PDF tests are skipped when `soffice` is not installed.
