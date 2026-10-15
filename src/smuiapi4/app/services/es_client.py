from elasticsearch import Elasticsearch
from flask import current_app


_es_client = None


def get_es_client():
    global _es_client
    if _es_client is None:
        config = current_app.config
        _es_client = Elasticsearch(
            config["ES_HOST"],
            basic_auth=(config["ES_USER"], config["ES_PASSWORD"]),
            verify_certs=config["ES_VERIFY_CERTS"],
        )
    return _es_client


def reset_es_client():
    """Reset the client — used in testing."""
    global _es_client
    _es_client = None
