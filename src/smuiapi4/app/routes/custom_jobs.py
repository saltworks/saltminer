from flask import Blueprint
import app.services.custom_jobs_service as custom_jobs_service
from app.utils.responses import success_response, error_response

custom_jobs_bp = Blueprint("custom_jobs", __name__, url_prefix="/smuiapi4/custom-jobs")


@custom_jobs_bp.route("/scripts", methods=["GET"])
def list_custom_scripts():
    """List available script files in the custom jobs directory.

    Used by the GUI to populate the command dropdown.
    """
    try:
        result = custom_jobs_service.list_scripts()
        return success_response(result)
    except Exception as e:
        return error_response(str(e), code="FS_ERROR")
