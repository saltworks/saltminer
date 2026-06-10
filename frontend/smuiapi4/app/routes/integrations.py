from flask import Blueprint, request
from app.services.integration_service import (
    get_available_adapters,
    get_adapter_template,
    get_configured_integrations,
    get_instance_settings,
    update_instance_settings,
    delete_instance,
    create_instance,
    instance_exists,
    create_adapter,
    adapter_exists,
    update_adapter_template,
    delete_adapter_template,
    get_instances_for_adapter,
    propagate_template_to_instances,
)
from app.utils.responses import success_response, error_response

integrations_bp = Blueprint("integrations", __name__, url_prefix="/smuiapi4/integrations")


@integrations_bp.route("/available", methods=["GET"])
def list_available():
    try:
        result = get_available_adapters()
        return success_response(result)
    except Exception as e:
        return error_response(str(e), code="ES_ERROR")


@integrations_bp.route("/available", methods=["POST"])
def create_available():
    try:
        body = request.get_json()
        adapter_name = body["adapterName"]
        description = body.get("description", "")
        icon = body.get("icon", "")

        if adapter_exists(adapter_name):
            return error_response(
                f"Adapter '{adapter_name}' already exists",
                code="DUPLICATE_ADAPTER",
                status_code=409,
            )

        create_adapter(adapter_name, description, icon)
        return success_response({"created": adapter_name}, status_code=201)
    except Exception as e:
        return error_response(str(e), code="ES_ERROR")


@integrations_bp.route("/available/<adapter>", methods=["PUT"])
def update_available(adapter):
    try:
        body = request.get_json()
        description = body.get("description", "")
        icon = body.get("icon", "")
        fields = body.get("fields", [])
        update_adapter_template(adapter, description, icon, fields)
        return success_response({"updated": True})
    except Exception as e:
        return error_response(str(e), code="ES_ERROR")


@integrations_bp.route("/available/<adapter>", methods=["DELETE"])
def delete_available(adapter):
    try:
        result = delete_adapter_template(adapter)
        if result.get("blocked"):
            return error_response(
                f"Adapter '{adapter}' has {result['instance_count']} configured instance(s) and cannot be deleted",
                code="ADAPTER_IN_USE",
                status_code=409,
            )
        return success_response(result)
    except Exception as e:
        return error_response(str(e), code="ES_ERROR")


@integrations_bp.route("/available/<adapter>/propagate", methods=["POST"])
def propagate_available(adapter):
    try:
        body = request.get_json()
        fields = body.get("fields", [])
        result = propagate_template_to_instances(adapter, fields)
        return success_response(result)
    except Exception as e:
        return error_response(str(e), code="ES_ERROR")


@integrations_bp.route("/available/<adapter>", methods=["GET"])
def get_template(adapter):
    try:
        result = get_adapter_template(adapter)
        return success_response(result)
    except Exception as e:
        return error_response(str(e), code="ES_ERROR")


@integrations_bp.route("/configured", methods=["GET"])
def list_configured():
    try:
        result = get_configured_integrations()
        return success_response(result)
    except Exception as e:
        return error_response(str(e), code="ES_ERROR")


@integrations_bp.route("/configured/<instance>", methods=["GET"])
def get_configured(instance):
    try:
        result = get_instance_settings(instance)
        return success_response(result)
    except Exception as e:
        return error_response(str(e), code="ES_ERROR")


@integrations_bp.route("/configured/<instance>", methods=["PUT"])
def update_configured(instance):
    try:
        updates = request.get_json()
        update_instance_settings(instance, updates)
        return success_response({"updated": len(updates)})
    except Exception as e:
        return error_response(str(e), code="ES_ERROR")


@integrations_bp.route("/configured/<instance>", methods=["DELETE"])
def remove_configured(instance):
    try:
        delete_instance(instance)
        return success_response({"deleted": instance})
    except Exception as e:
        return error_response(str(e), code="ES_ERROR")


@integrations_bp.route("/configured", methods=["POST"])
def create_configured():
    try:
        body = request.get_json()
        adapter_name = body["adapterName"]
        instance_name = body["instanceName"]

        if instance_exists(instance_name):
            return error_response(
                f"Instance '{instance_name}' already exists",
                code="DUPLICATE_INSTANCE",
                status_code=409,
            )

        result = create_instance(adapter_name, instance_name)
        return success_response(result, status_code=201)
    except Exception as e:
        return error_response(str(e), code="ES_ERROR")
