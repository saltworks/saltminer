import json
from app.services.integration_service import (
    get_available_adapters,
    get_adapter_template,
    get_configured_integrations,
    get_instance_settings,
    update_instance_settings,
    delete_instance,
    create_instance,
    instance_exists,
)


CHECKMARX_AVAILABLE_HITS = [
    {
        "_id": "integrations-available_Checkmarx__metadata",
        "_source": {
            "id": "integrations-available_Checkmarx__metadata",
            "section": "integrations-available",
            "subsection": "Checkmarx",
            "property": "_metadata",
            "value": json.dumps({"description": "SAST platform"}),
            "value_type": "json",
            "label": "Checkmarx",
            "description": "SAST platform",
        },
    },
    {
        "_id": "integrations-available_Checkmarx_icon",
        "_source": {
            "id": "integrations-available_Checkmarx_icon",
            "section": "integrations-available",
            "subsection": "Checkmarx",
            "property": "icon",
            "value": "/smui4/icons/integrations/checkmarx.svg",
            "value_type": "string",
            "label": "Icon",
            "description": "",
        },
    },
    {
        "_id": "integrations-available_Checkmarx_baseUrl",
        "_source": {
            "id": "integrations-available_Checkmarx_baseUrl",
            "section": "integrations-available",
            "subsection": "Checkmarx",
            "property": "baseUrl",
            "value": "",
            "value_type": "string",
            "label": "Base URL",
            "description": "API endpoint",
        },
    },
    {
        "_id": "integrations-available_Checkmarx_clientSecret",
        "_source": {
            "id": "integrations-available_Checkmarx_clientSecret",
            "section": "integrations-available",
            "subsection": "Checkmarx",
            "property": "clientSecret",
            "value": "",
            "value_type": "string",
            "label": "Client Secret",
            "description": "",
        },
    },
]


def test_get_available_adapters(app, mock_es_integration):
    mock_es_integration.search.return_value = {"hits": {"hits": CHECKMARX_AVAILABLE_HITS}}

    with app.app_context():
        result = get_available_adapters()

    assert len(result) == 1
    adapter = result[0]
    assert adapter["name"] == "Checkmarx"
    assert adapter["description"] == "SAST platform"
    assert adapter["icon"] == "/smui4/icons/integrations/checkmarx.svg"
    assert len(adapter["fields"]) == 2  # baseUrl and clientSecret (excludes _metadata and icon)


def test_get_available_adapters_empty(app, mock_es_integration):
    mock_es_integration.search.return_value = {"hits": {"hits": []}}

    with app.app_context():
        result = get_available_adapters()

    assert result == []


def test_get_adapter_template(app, mock_es_integration):
    mock_es_integration.search.return_value = {"hits": {"hits": CHECKMARX_AVAILABLE_HITS}}

    with app.app_context():
        result = get_adapter_template("Checkmarx")

    assert result["adapter"] == "Checkmarx"
    assert len(result["fields"]) == 2
    field_names = [f["property"] for f in result["fields"]]
    assert "baseUrl" in field_names
    assert "clientSecret" in field_names


def test_get_configured_integrations(app, mock_es_integration):
    mock_es_integration.search.return_value = {
        "hits": {
            "hits": [
                {
                    "_id": "integrations-configured_CX Prod_adapterName",
                    "_source": {
                        "id": "integrations-configured_CX Prod_adapterName",
                        "section": "integrations-configured",
                        "subsection": "CX Prod",
                        "property": "adapterName",
                        "value": "Checkmarx",
                        "value_type": "string",
                        "label": "Adapter Name",
                        "description": "",
                    },
                },
                {
                    "_id": "integrations-configured_CX Prod_baseUrl",
                    "_source": {
                        "id": "integrations-configured_CX Prod_baseUrl",
                        "section": "integrations-configured",
                        "subsection": "CX Prod",
                        "property": "baseUrl",
                        "value": "https://api.checkmarx.com",
                        "value_type": "string",
                        "label": "Base URL",
                        "description": "",
                    },
                },
            ]
        }
    }

    with app.app_context():
        result = get_configured_integrations()

    assert len(result) == 1
    assert result[0]["instance"] == "CX Prod"
    assert len(result[0]["properties"]) == 2


def test_get_configured_integrations_empty(app, mock_es_integration):
    mock_es_integration.search.return_value = {"hits": {"hits": []}}

    with app.app_context():
        result = get_configured_integrations()

    assert result == []


def test_get_instance_settings(app, mock_es_integration):
    mock_es_integration.search.return_value = {
        "hits": {
            "hits": [
                {
                    "_id": "integrations-configured_CX Prod_baseUrl",
                    "_source": {
                        "section": "integrations-configured",
                        "subsection": "CX Prod",
                        "property": "baseUrl",
                        "value": "https://api.checkmarx.com",
                        "value_type": "string",
                        "label": "Base URL",
                        "description": "",
                    },
                },
            ]
        }
    }

    with app.app_context():
        result = get_instance_settings("CX Prod")

    assert result["instance"] == "CX Prod"
    assert len(result["properties"]) == 1


def test_update_instance_settings(app, mock_es_integration):
    updates = [
        {"property": "baseUrl", "value": "https://new.url.com"},
    ]

    with app.app_context():
        update_instance_settings("CX Prod", updates)

    assert mock_es_integration.bulk.called
    bulk_body = mock_es_integration.bulk.call_args[1]["operations"]
    assert len(bulk_body) == 2  # 1 action + 1 doc


def test_delete_instance(app, mock_es_integration):
    mock_es_integration.search.return_value = {
        "hits": {"hits": [{"_id": "integrations-configured_CX Prod_baseUrl", "_source": {}}]}
    }

    with app.app_context():
        delete_instance("CX Prod")

    assert mock_es_integration.bulk.called


def test_instance_exists_true(app, mock_es_integration):
    mock_es_integration.count.return_value = {"count": 3}

    with app.app_context():
        result = instance_exists("CX Prod")

    assert result is True


def test_instance_exists_false(app, mock_es_integration):
    mock_es_integration.count.return_value = {"count": 0}

    with app.app_context():
        result = instance_exists("CX Prod")

    assert result is False


def test_create_instance(app, mock_es_integration):
    # Mock: template lookup returns Checkmarx fields
    mock_es_integration.search.return_value = {"hits": {"hits": CHECKMARX_AVAILABLE_HITS}}
    # Mock: instance doesn't exist yet
    mock_es_integration.count.return_value = {"count": 0}

    with app.app_context():
        result = create_instance("Checkmarx", "CX Prod")

    assert mock_es_integration.bulk.called
    bulk_body = mock_es_integration.bulk.call_args[1]["operations"]
    # 2 template fields + adapterName + enabled + runEveryHours + startingAt = 6 docs = 12 operations
    assert len(bulk_body) == 12

    # Verify adapterName is in the bulk operations
    docs = [bulk_body[i] for i in range(1, len(bulk_body), 2)]
    adapter_doc = next(d for d in docs if d["doc"]["property"] == "adapterName")
    assert adapter_doc["doc"]["value"] == "Checkmarx"
    assert adapter_doc["doc"]["section"] == "integrations-configured"
    assert adapter_doc["doc"]["subsection"] == "CX Prod"
