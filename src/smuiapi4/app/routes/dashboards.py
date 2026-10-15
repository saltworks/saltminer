from flask import Blueprint
from app.services.dashboard_service import (
    get_executive_dashboard,
    get_development_dashboard,
    get_security_dashboard,
    get_operations_dashboard,
)
from app.utils.responses import success_response, error_response

dashboards_bp = Blueprint("dashboards", __name__, url_prefix="/smuiapi4/dashboards")


@dashboards_bp.route("/executive", methods=["GET"])
def executive_dashboard():
    try:
        data = get_executive_dashboard()
        return success_response(data)
    except Exception as e:
        return error_response(str(e), code="DASHBOARD_ERROR")


@dashboards_bp.route("/development", methods=["GET"])
def development_dashboard():
    try:
        data = get_development_dashboard()
        return success_response(data)
    except Exception as e:
        return error_response(str(e), code="DASHBOARD_ERROR")


@dashboards_bp.route("/security", methods=["GET"])
def security_dashboard():
    try:
        data = get_security_dashboard()
        return success_response(data)
    except Exception as e:
        return error_response(str(e), code="DASHBOARD_ERROR")


@dashboards_bp.route("/operations", methods=["GET"])
def operations_dashboard():
    try:
        data = get_operations_dashboard()
        return success_response(data)
    except Exception as e:
        return error_response(str(e), code="DASHBOARD_ERROR")
