from flask import Blueprint
from app.auth import _get_sid, _validate_sid
from app.utils.responses import success_response, error_response

auth_bp = Blueprint("auth", __name__, url_prefix="/smuiapi4/auth")


@auth_bp.route("/me", methods=["GET"])
def get_current_user():
    try:
        sid = _get_sid()
        if not sid:
            return success_response({"authenticated": False, "username": ""})

        user = _validate_sid(sid)
        if user is None:
            return success_response({"authenticated": False, "username": ""})

        username = user.get("username", user.get("full_name", "User"))
        return success_response({
            "authenticated": True,
            "username": username,
            "fullName": user.get("full_name", username),
            "email": user.get("email", ""),
            "roles": user.get("roles", []),
        })
    except Exception as e:
        return error_response(str(e), code="AUTH_ERROR")
