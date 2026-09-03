"""End-to-end on the shipped Saltworks template (converted on the fly)."""
import os

import pytest

from smreport.convert import convert_file
from smreport.render import RenderOptions, render
from tests.conftest import SHIPPED_TEMPLATE, paragraph_texts, soffice_available


@pytest.mark.skipif(not os.path.exists(SHIPPED_TEMPLATE), reason="shipped template not present")
def test_shipped_template_end_to_end(context_file, tmp_path):
    template = str(tmp_path / "SaltworksTemplate-jinja.docx")
    convert_file(SHIPPED_TEMPLATE, template)
    formats = ("docx", "pdf") if soffice_available() else ("docx",)
    out = render(RenderOptions(context_path=context_file, template_path=template, outdir=str(tmp_path / "out"),
                               name="Acme_eng-0001", formats=formats))
    texts = paragraph_texts(out["docx"])
    joined = "\n".join(texts)
    assert "Acme Web Portal Pentest - Active" in texts
    assert "Created For: Acme Corp" in texts
    assert "Product: Manual" in texts and "Vendor: Saltworks" in texts
    assert "Removed False on " in joined
    assert "Suppressed: False" in texts
    assert "{{" not in joined and "{%" not in joined and "«" not in joined and "" not in joined
    assert texts.count("SQL Injection Total: 1") == 2
    for fmt in formats:
        assert os.path.isfile(out[fmt])
