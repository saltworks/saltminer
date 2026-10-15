from flask import jsonify


def success_response(data, status_code=200, warning=None):
    body = {"data": data, "error": None}
    if warning:
        body["warning"] = warning
    return jsonify(body), status_code


def error_response(message, code="UNKNOWN_ERROR", status_code=500):
    return jsonify({"data": None, "error": {"code": code, "message": message}}), status_code
