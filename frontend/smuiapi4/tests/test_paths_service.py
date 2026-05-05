from unittest.mock import patch

from app.services import paths_service
from app.services.paths_service import (
    DEFAULTS,
    custom_jobs_path,
    saltminer_jobs_path,
    ssl_certs_path,
    report_templates_path,
)


# ---------------------------------------------------------------------------
# Defaults
# ---------------------------------------------------------------------------

def test_defaults_have_all_four_keys():
    assert set(DEFAULTS.keys()) == {
        "customJobsPath",
        "saltminerJobsPath",
        "sslCertsPath",
        "reportTemplatesPath",
    }


def test_defaults_match_spec():
    assert DEFAULTS["customJobsPath"] == "/opt/saltworks/saltminer/custom-jobs/"
    assert DEFAULTS["saltminerJobsPath"] == "/opt/saltworks/saltminer/saltminer-jobs/"
    assert DEFAULTS["sslCertsPath"] == "/opt/saltworks/saltminer/ssl/"
    assert DEFAULTS["reportTemplatesPath"] == "/opt/saltworks/saltminer/report-templates/"


# ---------------------------------------------------------------------------
# Read-through behavior
# ---------------------------------------------------------------------------

def test_returns_default_when_es_returns_no_docs():
    with patch("app.services.paths_service.get_settings_by_section", return_value=[]):
        assert custom_jobs_path() == DEFAULTS["customJobsPath"]
        assert ssl_certs_path() == DEFAULTS["sslCertsPath"]
        assert report_templates_path() == DEFAULTS["reportTemplatesPath"]
        assert saltminer_jobs_path() == DEFAULTS["saltminerJobsPath"]


def test_returns_stored_value_when_doc_present():
    docs = [
        {"property": "customJobsPath", "value": "/var/data/cj/"},
        {"property": "sslCertsPath", "value": "/etc/ssl-custom/"},
    ]
    with patch("app.services.paths_service.get_settings_by_section", return_value=docs):
        assert custom_jobs_path() == "/var/data/cj/"
        assert ssl_certs_path() == "/etc/ssl-custom/"
        # Properties not in docs fall back
        assert report_templates_path() == DEFAULTS["reportTemplatesPath"]


def test_empty_string_value_returns_default():
    docs = [{"property": "customJobsPath", "value": ""}]
    with patch("app.services.paths_service.get_settings_by_section", return_value=docs):
        assert custom_jobs_path() == DEFAULTS["customJobsPath"]


def test_whitespace_value_returns_default():
    docs = [{"property": "customJobsPath", "value": "   "}]
    with patch("app.services.paths_service.get_settings_by_section", return_value=docs):
        assert custom_jobs_path() == DEFAULTS["customJobsPath"]


def test_returns_default_when_es_raises():
    with patch(
        "app.services.paths_service.get_settings_by_section",
        side_effect=Exception("ES down"),
    ):
        assert custom_jobs_path() == DEFAULTS["customJobsPath"]
        assert ssl_certs_path() == DEFAULTS["sslCertsPath"]


def test_each_call_hits_es_no_cache():
    docs = [{"property": "customJobsPath", "value": "/first/"}]
    with patch(
        "app.services.paths_service.get_settings_by_section",
        return_value=docs,
    ) as mock_get:
        custom_jobs_path()
        custom_jobs_path()
        custom_jobs_path()
    assert mock_get.call_count == 3


def test_get_settings_by_section_called_with_correct_args():
    with patch(
        "app.services.paths_service.get_settings_by_section",
        return_value=[],
    ) as mock_get:
        custom_jobs_path()
    mock_get.assert_called_with("general", subsection="paths")
