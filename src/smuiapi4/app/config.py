import os


class Config:
    # Only ES connection details come from environment variables.
    # All other settings are stored in the sys_config ES index.
    ES_HOST = os.environ.get("ES_HOST", "https://localhost:9200")
    ES_USER = os.environ.get("ES_USER", "elastic")
    ES_PASSWORD = os.environ.get("ES_PASSWORD", "changeme")
    ES_VERIFY_CERTS = os.environ.get("ES_VERIFY_CERTS", "false").lower() == "true"

    # Kibana URL for auth validation (/internal/security/me)
    KIBANA_URL = os.environ.get("KIBANA_URL", "")
