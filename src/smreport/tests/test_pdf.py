import os

import pytest

from smreport.pdf import apply_substitutions, document_fonts, normalize_substitutions
from smreport.render import RenderOptions, render
from tests.conftest import soffice_available


def test_normalize_substitutions_accepts_both_shapes():
    assert normalize_substitutions({"Calibri": "Carlito"}) == {"Calibri": "Carlito"}
    assert normalize_substitutions({"Calibri": {"Font": "Carlito", "Bold": "Carlito Bold", "Italic": "x"}}) == {"Calibri": "Carlito"}


def test_apply_substitutions_rewrites_rfonts(jinja_template, tmp_path):
    fonts = document_fonts(jinja_template)
    assert fonts, "template should declare at least one font"
    target = sorted(fonts)[0]
    out = str(tmp_path / "subst.docx")
    applied = apply_substitutions(jinja_template, out, {target: "DejaVu Sans"})
    assert applied == {target: "DejaVu Sans"}
    assert target not in document_fonts(out) and "DejaVu Sans" in document_fonts(out)


@pytest.mark.skipif(not soffice_available(), reason="LibreOffice (soffice) not installed")
def test_pdf_end_to_end(context_file, jinja_template, tmp_path):
    out = render(RenderOptions(context_path=context_file, template_path=jinja_template,
                               outdir=str(tmp_path / "out"), name="report", formats=("docx", "pdf")))
    assert os.path.isfile(out["pdf"]) and os.path.getsize(out["pdf"]) > 1000
    with open(out["pdf"], "rb") as fh:
        assert fh.read(5) == b"%PDF-"
