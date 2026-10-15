from app.services.settings_service import get_settings_by_section, update_settings


def test_get_settings_by_section_returns_properties(app, mock_es):
    mock_es.search.return_value = {
        "hits": {
            "hits": [
                {
                    "_id": "1",
                    "_source": {
                        "id": "1",
                        "section": "general",
                        "subsection": "",
                        "property": "orgName",
                        "value": "SaltMiner Security",
                        "value_type": "string",
                        "label": "Organization Name",
                        "description": "Your organization name",
                    },
                },
                {
                    "_id": "2",
                    "_source": {
                        "id": "2",
                        "section": "general",
                        "subsection": "",
                        "property": "darkMode",
                        "value": "false",
                        "value_type": "boolean",
                        "label": "Dark Mode",
                        "description": "Enable dark mode interface",
                    },
                },
            ]
        }
    }

    with app.app_context():
        result = get_settings_by_section("general")

    assert len(result) == 2
    assert result[0]["property"] == "orgName"
    assert result[0]["value"] == "SaltMiner Security"
    assert result[1]["property"] == "darkMode"
    assert result[1]["value"] == "false"


def test_get_settings_by_section_empty(app, mock_es):
    mock_es.search.return_value = {"hits": {"hits": []}}

    with app.app_context():
        result = get_settings_by_section("nonexistent")

    assert result == []


def test_update_settings_upserts_documents(app, mock_es):
    updates = [
        {"property": "orgName", "value": "New Name"},
        {"property": "darkMode", "value": "true"},
    ]

    with app.app_context():
        update_settings("general", "", updates)

    assert mock_es.bulk.called
    bulk_body = mock_es.bulk.call_args[1]["operations"]
    assert len(bulk_body) == 4  # 2 update actions + 2 docs
