from flask import Blueprint, request
from app.services.scanning_service import (
    get_all_scanners,
    get_scanner_settings,
    update_scanner_settings,
    delete_scanner,
    get_scanning_jobs,
    get_scan_schedules,
)
from app.utils.responses import success_response, error_response

scanning_bp = Blueprint("scanning", __name__, url_prefix="/smuiapi4/scanning")


@scanning_bp.route("/jobs", methods=["GET"])
def list_scanning_jobs():
    try:
        result = get_scanning_jobs()
        return success_response(result)
    except Exception as e:
        return error_response(str(e), code="ES_ERROR")


@scanning_bp.route("/scanners", methods=["GET"])
def list_scanners():
    try:
        result = get_all_scanners()
        return success_response(result)
    except Exception as e:
        return error_response(str(e), code="ES_ERROR")


@scanning_bp.route("/scanners/<scanner>", methods=["GET"])
def get_scanner(scanner):
    try:
        result = get_scanner_settings(scanner)
        return success_response(result)
    except Exception as e:
        return error_response(str(e), code="ES_ERROR")


@scanning_bp.route("/scanners/<scanner>", methods=["PUT"])
def update_scanner(scanner):
    try:
        updates = request.get_json()
        update_scanner_settings(scanner, updates)
        return success_response({"updated": len(updates)})
    except Exception as e:
        return error_response(str(e), code="ES_ERROR")


@scanning_bp.route("/scanners/<scanner>", methods=["DELETE"])
def remove_scanner(scanner):
    try:
        delete_scanner(scanner)
        return success_response({"deleted": scanner})
    except Exception as e:
        return error_response(str(e), code="ES_ERROR")


@scanning_bp.route("/schedule", methods=["GET"])
def list_scan_schedules():
    try:
        result = get_scan_schedules()
        return success_response(result)
    except Exception as e:
        return error_response(str(e), code="ES_ERROR")
