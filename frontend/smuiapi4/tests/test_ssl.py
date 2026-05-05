import datetime
import io
from unittest.mock import patch, MagicMock

import pytest
from cryptography import x509
from cryptography.hazmat.primitives import hashes, serialization
from cryptography.hazmat.primitives.asymmetric import rsa
from cryptography.x509.oid import NameOID

from app.services.ssl_service import (
    get_certificate_info,
    validate_cert_format,
    validate_key_format,
    validate_cert_key_match,
)


# ---------------------------------------------------------------------------
# Helpers
# ---------------------------------------------------------------------------

def make_test_cert_and_key():
    key = rsa.generate_private_key(public_exponent=65537, key_size=2048)
    subject = issuer = x509.Name([x509.NameAttribute(NameOID.COMMON_NAME, "test.local")])
    cert = (
        x509.CertificateBuilder()
        .subject_name(subject)
        .issuer_name(issuer)
        .public_key(key.public_key())
        .serial_number(x509.random_serial_number())
        .not_valid_before(datetime.datetime.now(datetime.timezone.utc))
        .not_valid_after(datetime.datetime.now(datetime.timezone.utc) + datetime.timedelta(days=365))
        .sign(key, hashes.SHA256())
    )
    cert_pem = cert.public_bytes(serialization.Encoding.PEM)
    key_pem = key.private_bytes(
        serialization.Encoding.PEM,
        serialization.PrivateFormat.TraditionalOpenSSL,
        serialization.NoEncryption(),
    )
    return cert_pem, key_pem


# ---------------------------------------------------------------------------
# Service tests
# ---------------------------------------------------------------------------

class TestGetCertificateInfo:
    def test_get_certificate_info_no_cert(self):
        with patch("app.services.ssl_service.os.path.isfile", return_value=False):
            result = get_certificate_info()
        assert result == {"found": False}


class TestValidateCertFormat:
    def test_validate_cert_format_valid(self):
        cert_pem, _ = make_test_cert_and_key()
        error = validate_cert_format(cert_pem)
        assert error is None

    def test_validate_cert_format_invalid(self):
        error = validate_cert_format(b"not a cert")
        assert error is not None
        assert isinstance(error, str)
        assert len(error) > 0


class TestValidateKeyFormat:
    def test_validate_key_format_valid(self):
        _, key_pem = make_test_cert_and_key()
        error = validate_key_format(key_pem)
        assert error is None

    def test_validate_key_format_invalid(self):
        error = validate_key_format(b"not a key")
        assert error is not None
        assert isinstance(error, str)
        assert len(error) > 0


class TestValidateCertKeyMatch:
    def test_validate_cert_key_match_valid(self):
        cert_pem, key_pem = make_test_cert_and_key()
        error = validate_cert_key_match(cert_pem, key_pem)
        assert error is None

    def test_validate_cert_key_match_mismatch(self):
        cert_pem, _ = make_test_cert_and_key()
        _, other_key_pem = make_test_cert_and_key()
        error = validate_cert_key_match(cert_pem, other_key_pem)
        assert error is not None
        assert "match" in error.lower() or "mismatch" in error.lower()


# ---------------------------------------------------------------------------
# Route tests
# ---------------------------------------------------------------------------

class TestSSLRoutes:
    def test_route_get_certificate(self, client):
        with patch("app.routes.ssl.get_certificate_info", return_value={"found": False}):
            response = client.get("/smuiapi4/ssl/certificate")
        assert response.status_code == 200
        data = response.get_json()
        assert data["error"] is None
        assert data["data"] == {"found": False}

    def test_route_upload_certificate(self, client):
        cert_pem, key_pem = make_test_cert_and_key()

        with (
            patch("app.routes.ssl.validate_cert_format", return_value=None),
            patch("app.routes.ssl.validate_key_format", return_value=None),
            patch("app.routes.ssl.validate_cert_key_match", return_value=None),
            patch("app.routes.ssl.save_certificate") as mock_save,
        ):
            response = client.post(
                "/smuiapi4/ssl/certificate",
                data={
                    "cert": (io.BytesIO(cert_pem), "saltminer.crt"),
                    "key": (io.BytesIO(key_pem), "saltminer.key"),
                },
                content_type="multipart/form-data",
            )

        assert response.status_code == 200
        data = response.get_json()
        assert data["error"] is None
        assert data["data"]["uploaded"] is True
        mock_save.assert_called_once()

    def test_route_upload_missing_files(self, client):
        response = client.post(
            "/smuiapi4/ssl/certificate",
            data={},
            content_type="multipart/form-data",
        )
        assert response.status_code == 400
        data = response.get_json()
        assert data["error"]["code"] == "MISSING_FILES"

    def test_route_upload_invalid_cert(self, client):
        cert_pem, key_pem = make_test_cert_and_key()

        with patch("app.routes.ssl.validate_cert_format", return_value="Invalid certificate format: must be PEM encoded"):
            response = client.post(
                "/smuiapi4/ssl/certificate",
                data={
                    "cert": (io.BytesIO(cert_pem), "saltminer.crt"),
                    "key": (io.BytesIO(key_pem), "saltminer.key"),
                },
                content_type="multipart/form-data",
            )

        assert response.status_code == 400
        data = response.get_json()
        assert data["error"]["code"] == "INVALID_FORMAT"

    def test_route_upload_key_mismatch(self, client):
        cert_pem, key_pem = make_test_cert_and_key()

        with (
            patch("app.routes.ssl.validate_cert_format", return_value=None),
            patch("app.routes.ssl.validate_key_format", return_value=None),
            patch("app.routes.ssl.validate_cert_key_match", return_value="Certificate and key do not match"),
        ):
            response = client.post(
                "/smuiapi4/ssl/certificate",
                data={
                    "cert": (io.BytesIO(cert_pem), "saltminer.crt"),
                    "key": (io.BytesIO(key_pem), "saltminer.key"),
                },
                content_type="multipart/form-data",
            )

        assert response.status_code == 400
        data = response.get_json()
        assert data["error"]["code"] == "KEY_MISMATCH"
