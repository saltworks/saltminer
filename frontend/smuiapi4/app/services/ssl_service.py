import os
from datetime import datetime, timezone, timedelta
from cryptography import x509
from cryptography.hazmat.primitives import serialization

from app.services.paths_service import ssl_certs_path

CERT_FILENAME = "saltminer.crt"
KEY_FILENAME = "saltminer.key"
EXPIRY_WARNING_DAYS = 30


def get_certificate_info():
    certs_dir = ssl_certs_path()
    cert_path = os.path.join(certs_dir, CERT_FILENAME)
    if not os.path.isfile(cert_path):
        return {"found": False}

    with open(cert_path, "rb") as f:
        cert_data = f.read()

    return parse_certificate(cert_data)


def parse_certificate(cert_data):
    try:
        cert = x509.load_pem_x509_certificate(cert_data)
    except Exception:
        return {"found": True, "error": "Unable to parse certificate"}

    now = datetime.now(timezone.utc)
    valid_to = cert.not_valid_after_utc
    is_expired = now > valid_to
    expiring_soon = not is_expired and (valid_to - now) < timedelta(days=EXPIRY_WARNING_DAYS)

    subject = cert.subject.rfc4514_string()
    issuer = cert.issuer.rfc4514_string()

    try:
        cn = cert.subject.get_attributes_for_oid(x509.oid.NameOID.COMMON_NAME)[0].value
    except (IndexError, Exception):
        cn = subject

    try:
        issuer_cn = cert.issuer.get_attributes_for_oid(x509.oid.NameOID.COMMON_NAME)[0].value
    except (IndexError, Exception):
        issuer_cn = issuer

    return {
        "found": True,
        "subject": cn,
        "subjectFull": subject,
        "issuer": issuer_cn,
        "issuerFull": issuer,
        "validFrom": cert.not_valid_before_utc.isoformat(),
        "validTo": valid_to.isoformat(),
        "isExpired": is_expired,
        "expiringSoon": expiring_soon,
        "serialNumber": format(cert.serial_number, "X"),
    }


def validate_cert_format(cert_data):
    if not cert_data.strip().startswith(b"-----BEGIN CERTIFICATE-----"):
        return "Invalid certificate format: must be PEM encoded (-----BEGIN CERTIFICATE-----)"
    try:
        x509.load_pem_x509_certificate(cert_data)
    except Exception as e:
        return f"Invalid certificate: {str(e)}"
    return None


def validate_key_format(key_data):
    if not key_data.strip().startswith(b"-----BEGIN"):
        return "Invalid key format: must be PEM encoded"
    try:
        serialization.load_pem_private_key(key_data, password=None)
    except Exception as e:
        return f"Invalid private key: {str(e)}"
    return None


def validate_cert_key_match(cert_data, key_data):
    try:
        cert = x509.load_pem_x509_certificate(cert_data)
        key = serialization.load_pem_private_key(key_data, password=None)

        cert_public_key = cert.public_key()
        key_public_key = key.public_key()

        cert_pub_bytes = cert_public_key.public_bytes(
            serialization.Encoding.PEM,
            serialization.PublicFormat.SubjectPublicKeyInfo,
        )
        key_pub_bytes = key_public_key.public_bytes(
            serialization.Encoding.PEM,
            serialization.PublicFormat.SubjectPublicKeyInfo,
        )

        if cert_pub_bytes != key_pub_bytes:
            return "Certificate and key do not match"
    except Exception as e:
        return f"Unable to verify cert/key match: {str(e)}"
    return None


def save_certificate(cert_data, key_data):
    certs_dir = ssl_certs_path()
    os.makedirs(certs_dir, exist_ok=True)
    cert_path = os.path.join(certs_dir, CERT_FILENAME)
    key_path = os.path.join(certs_dir, KEY_FILENAME)

    with open(cert_path, "wb") as f:
        f.write(cert_data)
    with open(key_path, "wb") as f:
        f.write(key_data)
    os.chmod(key_path, 0o600)
