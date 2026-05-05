import logging

from app.services.settings_service import get_settings_by_section, update_settings

logger = logging.getLogger(__name__)

DEFAULTS = {
    "customJobsPath": "/opt/saltworks/saltminer/custom-jobs/",
    "saltminerJobsPath": "/opt/saltworks/saltminer/saltminer-jobs/",
    "sslCertsPath": "/opt/saltworks/saltminer/ssl/",
    "reportTemplatesPath": "/opt/saltworks/saltminer/report-templates/",
}

_METADATA = {
    "customJobsPath": {
        "label": "Custom Jobs Path",
        "description": "Directory containing custom job scripts",
    },
    "saltminerJobsPath": {
        "label": "SaltMiner Jobs Path",
        "description": "Directory containing SaltMiner job scripts",
    },
    "sslCertsPath": {
        "label": "SSL Certificates Path",
        "description": "Directory holding the saltminer.crt and saltminer.key files",
    },
    "reportTemplatesPath": {
        "label": "Report Templates Path",
        "description": "Directory holding uploaded report .docx templates",
    },
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


def seed_defaults_if_missing() -> None:
    """Insert default docs in sys_config for any path properties not yet present.

    Idempotent: existing docs are left untouched so user edits are preserved.
    Other consumers (besides this API) read these settings directly from
    Elastic, so the docs need to exist after first boot.
    """
    try:
        existing = get_settings_by_section("general", subsection="paths")
    except Exception as exc:
        logger.warning("paths_service: seed skipped, could not read sys_config: %s", exc)
        return

    existing_props = {doc.get("property") for doc in existing}
    missing = [
        {
            "property": prop,
            "value": DEFAULTS[prop],
            "value_type": "string",
            "label": _METADATA[prop]["label"],
            "description": _METADATA[prop]["description"],
        }
        for prop in DEFAULTS
        if prop not in existing_props
    ]

    if not missing:
        return

    try:
        update_settings("general", "paths", missing)
        logger.info("paths_service: seeded %d default path setting(s)", len(missing))
    except Exception as exc:
        logger.warning("paths_service: seed write failed: %s", exc)
