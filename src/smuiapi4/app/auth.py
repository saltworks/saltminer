import os
import requests
from functools import wraps
from flask import request, current_app, g
from app.utils.responses import error_response


def _get_sid():
    """Get SID from request cookie."""
    return request.cookies.get("sid") or None


def _validate_sid(sid):
    """Validate SID against Kibana's /internal/security/me endpoint.

    Returns user info dict on success, None on failure.
    """
    kibana_url = current_app.config.get("KIBANA_URL", "")
    if not kibana_url:
        # No Kibana URL configured — skip validation (auth not set up yet)
        return {"username": "unknown", "roles": [], "auth_skipped": True}

    kibana_url = kibana_url.rstrip("/")
    try:
        resp = requests.get(
            f"{kibana_url}/internal/security/me",
            cookies={"sid": sid},
            headers={
                "kbn-xsrf": "true",
                "Content-Type": "application/json",
            },
            verify=False,
            timeout=10,
        )
        if resp.status_code == 200:
            return resp.json()
        return None
    except Exception:
        return None


def require_auth(f):
    """Auth decorator — validates SID cookie against Kibana.

    On success, sets g.user with the authenticated user info.
    On failure, returns 401.

    If KIBANA_URL is not configured, auth is skipped (passthrough).
    """
    @wraps(f)
    def decorated(*args, **kwargs):
        sid = _get_sid()
        if not sid:
            kibana_url = current_app.config.get("KIBANA_URL", "")
            if not kibana_url:
                # Auth not configured — passthrough
                g.user = {"username": "unknown", "roles": [], "auth_skipped": True}
                return f(*args, **kwargs)
            return error_response("Authentication required", code="AUTH_REQUIRED", status_code=401)

        user = _validate_sid(sid)
        if user is None:
            return error_response("Invalid or expired session", code="AUTH_FAILED", status_code=401)

        g.user = user
        return f(*args, **kwargs)
    return decorated
