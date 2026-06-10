import json
from app.services.integration_service import (
    update_adapter_template,
    create_adapter,
    adapter_exists,
    delete_adapter_template,
    get_instances_for_adapter,
    propagate_template_to_instances,
)

ICON_PREFIX = "/smui4/icons/integrations/"


def test_adapter_exists_true(app, mock_es_integration):
    mock_es_integration.count.return_value = {"count": 3}

    with app.app_context():
        result = adapter_exists("Checkmarx")

    assert result is True
    query = mock_es_integration.count.call_args[1]["query"]
    assert query["bool"]["must"][0]["term"]["section"] == "integrations-available"


def test_adapter_exists_false(app, mock_es_integration):
    mock_es_integration.count.return_value = {"count": 0}

    with app.app_context():
        result = adapter_exists("NonExistent")

    assert result is False


def test_create_adapter_with_standard_fields(app, mock_es_integration):
    mock_es_integration.count.return_value = {"count": 0}

    with app.app_context():
        create_adapter("NewAdapter", "A new adapter", "newadapter.svg")

    assert mock_es_integration.bulk.called
    bulk_body = mock_es_integration.bulk.call_args[1]["operations"]
    docs = [bulk_body[i] for i in range(1, len(bulk_body), 2)]

    # _metadata + icon + 4 standard fields = 6 docs
    assert len(docs) == 6

    # Check metadata
    meta_doc = next(d for d in docs if d["doc"]["property"] == "_metadata")
    meta_value = json.loads(meta_doc["doc"]["value"])
    assert meta_value["description"] == "A new adapter"

    # Check icon has full path prepended
    icon_doc = next(d for d in docs if d["doc"]["property"] == "icon")
    assert icon_doc["doc"]["value"] == f"{ICON_PREFIX}newadapter.svg"

    # Check standard fields exist
    field_names = [d["doc"]["property"] for d in docs]
    assert "baseUrl" in field_names
    assert "clientId" in field_names
    assert "clientSecret" in field_names
    assert "assetIdAttribute" in field_names


def test_create_adapter_empty_icon_gets_default(app, mock_es_integration):
    mock_es_integration.count.return_value = {"count": 0}

    with app.app_context():
        create_adapter("NewAdapter", "desc", "")

    bulk_body = mock_es_integration.bulk.call_args[1]["operations"]
    docs = [bulk_body[i] for i in range(1, len(bulk_body), 2)]
    icon_doc = next(d for d in docs if d["doc"]["property"] == "icon")
    assert icon_doc["doc"]["value"] == f"{ICON_PREFIX}default.svg"


def test_update_adapter_template(app, mock_es_integration):
    # Mock existing fields for this adapter
    mock_es_integration.search.return_value = {
        "hits": {
            "hits": [
                {
                    "_id": "integrations-available_CX_baseUrl",
                    "_source": {"property": "baseUrl", "section": "integrations-available", "subsection": "CX"},
                },
                {
                    "_id": "integrations-available_CX_oldField",
                    "_source": {"property": "oldField", "section": "integrations-available", "subsection": "CX"},
                },
            ]
        }
    }

    new_fields = [
        {"property": "baseUrl", "value_type": "string", "label": "Base URL", "description": "API endpoint"},
        {"property": "newField", "value_type": "integer", "label": "New Field", "description": ""},
    ]

    with app.app_context():
        update_adapter_template("CX", "Updated desc", "cx.svg", new_fields)

    assert mock_es_integration.bulk.called
    bulk_body = mock_es_integration.bulk.call_args[1]["operations"]

    # Should have: delete oldField + upsert baseUrl + upsert newField + upsert _metadata + upsert icon
    # = 1 delete + 4 upserts (each upsert = 2 ops) = 1 + 8 = 9
    actions = [bulk_body[i] for i in range(0, len(bulk_body), 2) if "delete" in bulk_body[i]]
    assert len(actions) == 1  # oldField deleted


def test_delete_adapter_template_no_instances(app, mock_es_integration):
    # No configured instances
    mock_es_integration.search.side_effect = [
        # First call: get_instances_for_adapter → no instances
        {"hits": {"hits": []}},
        # Second call: get all available docs for this adapter
        {
            "hits": {
                "hits": [
                    {"_id": "integrations-available_CX__metadata", "_source": {}},
                    {"_id": "integrations-available_CX_icon", "_source": {}},
                    {"_id": "integrations-available_CX_baseUrl", "_source": {}},
                ]
            }
        },
    ]

    with app.app_context():
        result = delete_adapter_template("CX")

    assert result == {"deleted": True}
    assert mock_es_integration.bulk.called


def test_delete_adapter_template_with_instances_blocked(app, mock_es_integration):
    # Has configured instances
    mock_es_integration.search.return_value = {
        "hits": {
            "hits": [
                {
                    "_id": "integrations-configured_CX Prod_adapterName",
                    "_source": {"property": "adapterName", "value": "CX", "subsection": "CX Prod"},
                },
            ]
        }
    }

    with app.app_context():
        result = delete_adapter_template("CX")

    assert result["blocked"] is True
    assert result["instance_count"] == 1
    assert mock_es_integration.bulk.not_called


def test_get_instances_for_adapter(app, mock_es_integration):
    mock_es_integration.search.return_value = {
        "hits": {
            "hits": [
                {
                    "_id": "integrations-configured_CX Prod_adapterName",
                    "_source": {"property": "adapterName", "value": "CX", "subsection": "CX Prod"},
                },
                {
                    "_id": "integrations-configured_CX Dev_adapterName",
                    "_source": {"property": "adapterName", "value": "CX", "subsection": "CX Dev"},
                },
            ]
        }
    }

    with app.app_context():
        result = get_instances_for_adapter("CX")

    assert result == ["CX Prod", "CX Dev"]


def test_get_instances_for_adapter_none(app, mock_es_integration):
    mock_es_integration.search.return_value = {"hits": {"hits": []}}

    with app.app_context():
        result = get_instances_for_adapter("CX")

    assert result == []


def test_propagate_template_to_instances(app, mock_es_integration):
    # First call: find instances for adapter
    # Second/third calls: get each instance's properties
    mock_es_integration.search.side_effect = [
        # get_instances_for_adapter
        {
            "hits": {
                "hits": [
                    {"_id": "x", "_source": {"property": "adapterName", "value": "CX", "subsection": "CX Prod"}},
                ]
            }
        },
        # get instance properties for CX Prod
        {
            "hits": {
                "hits": [
                    {"_id": "integrations-configured_CX Prod_adapterName", "_source": {"property": "adapterName", "value": "CX"}},
                    {"_id": "integrations-configured_CX Prod_enabled", "_source": {"property": "enabled", "value": "false"}},
                    {"_id": "integrations-configured_CX Prod_baseUrl", "_source": {"property": "baseUrl", "value": "https://old.url"}},
                    {"_id": "integrations-configured_CX Prod_oldField", "_source": {"property": "oldField", "value": "remove me"}},
                ]
            }
        },
    ]

    new_fields = [
        {"property": "baseUrl", "value_type": "string", "label": "Base URL", "description": ""},
        {"property": "newField", "value_type": "string", "label": "New Field", "description": ""},
    ]

    with app.app_context():
        result = propagate_template_to_instances("CX", new_fields)

    assert result["instances_updated"] == 1
    assert mock_es_integration.bulk.called

    bulk_body = mock_es_integration.bulk.call_args[1]["operations"]
    # Should add newField (2 ops) and delete oldField (1 op) = 3 operations
    actions = bulk_body
    has_delete = any(op.get("delete") for op in actions if isinstance(op, dict))
    has_update = any(op.get("update") for op in actions if isinstance(op, dict))
    assert has_delete
    assert has_update


# --- Route tests ---

def test_route_create_adapter(client, mock_es_integration):
    mock_es_integration.count.return_value = {"count": 0}

    response = client.post(
        "/smuiapi4/integrations/available",
        json={"adapterName": "NewAdapter", "description": "New", "icon": "new.svg"},
    )
    assert response.status_code == 201
    assert mock_es_integration.bulk.called


def test_route_create_adapter_duplicate(client, mock_es_integration):
    mock_es_integration.count.return_value = {"count": 3}

    response = client.post(
        "/smuiapi4/integrations/available",
        json={"adapterName": "Existing", "description": "X", "icon": ""},
    )
    assert response.status_code == 409
    assert response.get_json()["error"]["code"] == "DUPLICATE_ADAPTER"


def test_route_update_adapter_template(client, mock_es_integration):
    mock_es_integration.search.return_value = {"hits": {"hits": []}}

    response = client.put(
        "/smuiapi4/integrations/available/CX",
        json={
            "description": "Updated",
            "icon": "cx.svg",
            "fields": [{"property": "baseUrl", "value_type": "string", "label": "URL", "description": ""}],
        },
    )
    assert response.status_code == 200
    assert mock_es_integration.bulk.called


def test_route_delete_adapter_no_instances(client, mock_es_integration):
    mock_es_integration.search.side_effect = [
        {"hits": {"hits": []}},  # get_instances_for_adapter
        {"hits": {"hits": [{"_id": "x", "_source": {}}]}},  # get available docs
    ]

    response = client.delete("/smuiapi4/integrations/available/CX")
    assert response.status_code == 200


def test_route_delete_adapter_blocked(client, mock_es_integration):
    mock_es_integration.search.return_value = {
        "hits": {
            "hits": [{"_id": "x", "_source": {"property": "adapterName", "value": "CX", "subsection": "CX Prod"}}]
        }
    }

    response = client.delete("/smuiapi4/integrations/available/CX")
    assert response.status_code == 409
    assert response.get_json()["error"]["code"] == "ADAPTER_IN_USE"


def test_route_propagate(client, mock_es_integration):
    mock_es_integration.search.side_effect = [
        {"hits": {"hits": []}},  # get_instances_for_adapter → none
    ]

    response = client.post(
        "/smuiapi4/integrations/available/CX/propagate",
        json={"fields": [{"property": "baseUrl", "value_type": "string", "label": "URL", "description": ""}]},
    )
    assert response.status_code == 200
    data = response.get_json()
    assert data["data"]["instances_updated"] == 0
