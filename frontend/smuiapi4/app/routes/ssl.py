from flask import Blueprint, request
from app.services.ssl_service import (
    get_certificate_info,
    validate_cert_format,
    validate_key_format,
    validate_cert_key_match,
    save_certificate,
)
from app.utils.responses import success_response, error_response

ssl_bp = Blueprint("ssl", __name__, url_prefix="/smuiapi4/ssl")


@ssl_bp.route("/certificate", methods=["GET"])
def get_cert_info():
    try:
        result = get_certificate_info()
        return success_response(result)
    except Exception as e:
        return error_response(str(e), code="SSL_ERROR")


@ssl_bp.route("/certificate", methods=["POST"])
def upload_cert():
    try:
        if "cert" not in request.files or "key" not in request.files:
            return error_response(
                "Both certificate (.crt) and key (.key) files are required",
                code="MISSING_FILES",
                status_code=400,
            )

        cert_file = request.files["cert"]
        key_file = request.files["key"]

        if not cert_file.filename or not cert_file.filename.lower().endswith(".crt"):
            return error_response("Certificate file must have .crt extension", code="INVALID_FILENAME", status_code=400)

        if not key_file.filename or not key_file.filename.lower().endswith(".key"):
            return error_response("Key file must have .key extension", code="INVALID_FILENAME", status_code=400)

        cert_data = cert_file.read()
        key_data = key_file.read()

        cert_error = validate_cert_format(cert_data)
        if cert_error:
            return error_response(cert_error, code="INVALID_FORMAT", status_code=400)

        key_error = validate_key_format(key_data)
        if key_error:
            return error_response(key_error, code="INVALID_FORMAT", status_code=400)

        match_error = validate_cert_key_match(cert_data, key_data)
        if match_error:
            return error_response(match_error, code="KEY_MISMATCH", status_code=400)

        save_certificate(cert_data, key_data)

        return success_response({
            "uploaded": True,
            "message": "Certificate uploaded successfully. Run 'docker compose restart nginx' to apply the new certificate.",
        })
    except Exception as e:
        return error_response(str(e), code="SSL_ERROR")
