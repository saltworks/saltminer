from flask import Blueprint, request
from app.services.settings_service import get_settings_by_section, update_settings, delete_setting
from app.utils.responses import success_response, error_response

settings_bp = Blueprint("settings", __name__, url_prefix="/smuiapi4/settings")


@settings_bp.route("/general", methods=["GET"])
def get_general_settings():
    try:
        properties = get_settings_by_section("general")
        return success_response(properties)
    except Exception as e:
        return error_response(str(e), code="ES_ERROR")


@settings_bp.route("/general", methods=["PUT"])
def update_general_settings():
    try:
        updates = request.get_json()
        update_settings("general", "", updates)
        return success_response({"updated": len(updates)})
    except Exception as e:
        return error_response(str(e), code="ES_ERROR")


@settings_bp.route("/general/other", methods=["GET"])
def get_other_settings():
    try:
        properties = get_settings_by_section("general", subsection="other")
        return success_response(properties)
    except Exception as e:
        return error_response(str(e), code="ES_ERROR")


@settings_bp.route("/general/other", methods=["POST"])
def create_other_setting():
    try:
        body = request.get_json()
        updates = [body]
        update_settings("general", "other", updates)
        return success_response({"created": body["property"]}, status_code=201)
    except Exception as e:
        return error_response(str(e), code="ES_ERROR")


@settings_bp.route("/general/other/<property_name>", methods=["PUT"])
def update_other_setting(property_name):
    try:
        body = request.get_json()
        body["property"] = property_name
        update_settings("general", "other", [body])
        return success_response({"updated": property_name})
    except Exception as e:
        return error_response(str(e), code="ES_ERROR")


@settings_bp.route("/general/other/<property_name>", methods=["DELETE"])
def delete_other_setting(property_name):
    try:
        delete_setting("general", "other", property_name)
        return success_response({"deleted": property_name})
    except Exception as e:
        return error_response(str(e), code="ES_ERROR")


@settings_bp.route("/general/paths", methods=["GET"])
def get_paths_settings():
    try:
        properties = get_settings_by_section("general", subsection="paths")
        return success_response(properties)
    except Exception as e:
        return error_response(str(e), code="ES_ERROR")


@settings_bp.route("/general/paths", methods=["PUT"])
def update_paths_settings():
    try:
        updates = request.get_json()
        update_settings("general", "paths", updates)
        return success_response({"updated": len(updates)})
    except Exception as e:
        return error_response(str(e), code="ES_ERROR")


@settings_bp.route("/logs", methods=["GET"])
def get_logs():
    try:
        return success_response([])
    except Exception as e:
        return error_response(str(e), code="ES_ERROR")
