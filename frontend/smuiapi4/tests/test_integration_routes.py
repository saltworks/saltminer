import json


CHECKMARX_TEMPLATE_HITS = [
    {
        "_id": "integrations-available_Checkmarx__metadata",
        "_source": {
            "section": "integrations-available", "subsection": "Checkmarx",
            "property": "_metadata",
            "value": json.dumps({"description": "SAST platform"}),
            "value_type": "json", "label": "Checkmarx", "description": "SAST platform",
        },
    },
    {
        "_id": "integrations-available_Checkmarx_icon",
        "_source": {
            "section": "integrations-available", "subsection": "Checkmarx",
            "property": "icon",
            "value": "/smui4/icons/integrations/checkmarx.svg",
            "value_type": "string", "label": "Icon", "description": "",
        },
    },
    {
        "_id": "integrations-available_Checkmarx_baseUrl",
        "_source": {
            "section": "integrations-available", "subsection": "Checkmarx",
            "property": "baseUrl", "value": "", "value_type": "string",
            "label": "Base URL", "description": "",
        },
    },
]


def test_get_available_adapters(client, mock_es_integration):
    mock_es_integration.search.return_value = {"hits": {"hits": CHECKMARX_TEMPLATE_HITS}}

    response = client.get("/smuiapi4/integrations/available")
    assert response.status_code == 200
    data = response.get_json()
    assert data["error"] is None
    assert len(data["data"]) == 1
    assert data["data"][0]["name"] == "Checkmarx"


def test_get_adapter_template(client, mock_es_integration):
    mock_es_integration.search.return_value = {"hits": {"hits": CHECKMARX_TEMPLATE_HITS}}

    response = client.get("/smuiapi4/integrations/available/Checkmarx")
    assert response.status_code == 200
    data = response.get_json()
    assert data["data"]["adapter"] == "Checkmarx"
    assert len(data["data"]["fields"]) == 1  # baseUrl only (excludes _metadata, icon)


def test_get_configured_integrations(client, mock_es_integration):
    mock_es_integration.search.return_value = {
        "hits": {
            "hits": [
                {
                    "_id": "integrations-configured_CX Prod_adapterName",
                    "_source": {
                        "section": "integrations-configured", "subsection": "CX Prod",
                        "property": "adapterName", "value": "Checkmarx",
                        "value_type": "string", "label": "Adapter Name", "description": "",
                    },
                },
            ]
        }
    }

    response = client.get("/smuiapi4/integrations/configured")
    assert response.status_code == 200
    data = response.get_json()
    assert len(data["data"]) == 1
    assert data["data"][0]["instance"] == "CX Prod"


def test_get_instance_settings(client, mock_es_integration):
    mock_es_integration.search.return_value = {
        "hits": {
            "hits": [
                {
                    "_id": "integrations-configured_CX Prod_baseUrl",
                    "_source": {
                        "section": "integrations-configured", "subsection": "CX Prod",
                        "property": "baseUrl", "value": "https://api.checkmarx.com",
                        "value_type": "string", "label": "Base URL", "description": "",
                    },
                },
            ]
        }
    }

    response = client.get("/smuiapi4/integrations/configured/CX%20Prod")
    assert response.status_code == 200
    data = response.get_json()
    assert data["data"]["instance"] == "CX Prod"


def test_put_instance_settings(client, mock_es_integration):
    response = client.put(
        "/smuiapi4/integrations/configured/CX%20Prod",
        json=[{"property": "baseUrl", "value": "https://new.url.com"}],
    )
    assert response.status_code == 200
    assert mock_es_integration.bulk.called


def test_delete_instance(client, mock_es_integration):
    mock_es_integration.search.return_value = {
        "hits": {"hits": [{"_id": "integrations-configured_CX Prod_baseUrl", "_source": {}}]}
    }

    response = client.delete("/smuiapi4/integrations/configured/CX%20Prod")
    assert response.status_code == 200
    assert mock_es_integration.bulk.called


def test_create_instance(client, mock_es_integration):
    mock_es_integration.search.return_value = {"hits": {"hits": CHECKMARX_TEMPLATE_HITS}}
    mock_es_integration.count.return_value = {"count": 0}

    response = client.post(
        "/smuiapi4/integrations/configured",
        json={"adapterName": "Checkmarx", "instanceName": "CX Prod"},
    )
    assert response.status_code == 201
    assert mock_es_integration.bulk.called


def test_create_instance_duplicate_name(client, mock_es_integration):
    mock_es_integration.count.return_value = {"count": 3}

    response = client.post(
        "/smuiapi4/integrations/configured",
        json={"adapterName": "Checkmarx", "instanceName": "CX Prod"},
    )
    assert response.status_code == 409
    data = response.get_json()
    assert data["error"]["code"] == "DUPLICATE_INSTANCE"


def test_get_available_handles_es_error(client, mock_es_integration):
    mock_es_integration.search.side_effect = Exception("Connection refused")

    response = client.get("/smuiapi4/integrations/available")
    assert response.status_code == 500
    assert response.get_json()["error"]["code"] == "ES_ERROR"
