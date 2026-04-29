import pytest
from unittest.mock import MagicMock, patch
from app import create_app


@pytest.fixture
def app():
    app = create_app({
        "TESTING": True,
        "ES_HOST": "https://localhost:9200",
        "ES_USER": "elastic",
        "ES_PASSWORD": "test",
        "ES_VERIFY_CERTS": False,
    })
    yield app


@pytest.fixture
def client(app):
    return app.test_client()


@pytest.fixture
def mock_es():
    with patch("app.services.settings_service.get_es_client") as mock:
        es = MagicMock()
        mock.return_value = es
        yield es


@pytest.fixture
def mock_es_integration():
    with patch("app.services.integration_service.get_es_client") as mock:
        es = MagicMock()
        mock.return_value = es
        yield es


@pytest.fixture
def mock_es_scanning():
    with patch("app.services.scanning_service.get_es_client") as mock:
        es = MagicMock()
        mock.return_value = es
        yield es


