import logging

from app.services.settings_service import get_settings_by_section

logger = logging.getLogger(__name__)

DEFAULTS = {
    "customJobsPath": "/opt/saltworks/saltminer/custom-jobs/",
    "saltminerJobsPath": "/opt/saltworks/saltminer/saltminer-jobs/",
    "sslCertsPath": "/opt/saltworks/saltminer/ssl/",
    "reportTemplatesPath": "/opt/saltworks/saltminer/report-templates/",
}


def _get(property_name: str) -> str:
    default = DEFAULTS[property_name]
    try:
        docs = get_settings_by_section("general", subsection="paths")
    except Exception as exc:
        logger.warning("paths_service: ES read failed for %s: %s", property_name, exc)
        return default

    for doc in docs:
        if doc.get("property") == property_name:
            value = doc.get("value")
            if isinstance(value, str) and value.strip():
                return value
            return default
    return default


def custom_jobs_path() -> str:
    return _get("customJobsPath")


def saltminer_jobs_path() -> str:
    return _get("saltminerJobsPath")


def ssl_certs_path() -> str:
    return _get("sslCertsPath")


def report_templates_path() -> str:
    return _get("reportTemplatesPath")
