from flask import Blueprint, request, send_file
from app.services.report_templates_service import (
    validate_template_filename,
    validate_docx_content,
    list_templates,
    get_template_path,
    template_exists,
    save_template,
    delete_template,
    MAX_FILE_SIZE,
)
from app.utils.responses import success_response, error_response

report_templates_bp = Blueprint("report_templates", __name__, url_prefix="/smuiapi4/report-templates")


@report_templates_bp.route("", methods=["GET"])
def list_all():
    try:
        result = list_templates()
        return success_response(result)
    except Exception as e:
        return error_response(str(e), code="FS_ERROR")


@report_templates_bp.route("/<filename>", methods=["GET"])
def download(filename):
    try:
        filepath, error = get_template_path(filename)
        if error:
            code = "NOT_FOUND" if "not found" in error.lower() else "INVALID_FILENAME"
            status = 404 if "not found" in error.lower() else 400
            return error_response(error, code=code, status_code=status)
        return send_file(filepath, as_attachment=True, download_name=filename)
    except Exception as e:
        return error_response(str(e), code="FS_ERROR")


@report_templates_bp.route("", methods=["POST"])
def upload():
    try:
        if 'file' not in request.files:
            return error_response("No file provided", code="NO_FILE", status_code=400)

        file = request.files['file']
        if not file.filename:
            return error_response("No filename provided", code="NO_FILE", status_code=400)

        filename = file.filename
        validation_error = validate_template_filename(filename)
        if validation_error:
            return error_response(validation_error, code="INVALID_FILENAME", status_code=400)

        if template_exists(filename):
            return error_response(
                f"Template '{filename}' already exists. Delete it first to replace.",
                code="DUPLICATE_TEMPLATE",
                status_code=409,
            )

        file_data = file.read()

        if len(file_data) > MAX_FILE_SIZE:
            return error_response(
                f"File exceeds maximum size of {MAX_FILE_SIZE // (1024*1024)}MB",
                code="FILE_TOO_LARGE",
                status_code=413,
            )

        content_error = validate_docx_content(file_data)
        if content_error:
            return error_response(content_error, code="INVALID_CONTENT", status_code=400)

        save_template(filename, file_data)
        return success_response({"uploaded": filename}, status_code=201)
    except Exception as e:
        return error_response(str(e), code="FS_ERROR")


@report_templates_bp.route("/<filename>", methods=["DELETE"])
def remove(filename):
    try:
        validation_error = validate_template_filename(filename)
        if validation_error:
            return error_response(validation_error, code="INVALID_FILENAME", status_code=400)

        if not template_exists(filename):
            return error_response(f"Template '{filename}' not found", code="NOT_FOUND", status_code=404)

        delete_template(filename)
        return success_response({"deleted": filename})
    except Exception as e:
        return error_response(str(e), code="FS_ERROR")
