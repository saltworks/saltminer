def test_get_general_settings(client, mock_es):
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
                        "description": "",
                    },
                },
            ]
        }
    }

    response = client.get("/smuiapi4/settings/general")

    assert response.status_code == 200
    data = response.get_json()
    assert data["error"] is None
    assert len(data["data"]) == 1
    assert data["data"][0]["property"] == "orgName"


def test_put_general_settings(client, mock_es):
    response = client.put(
        "/smuiapi4/settings/general",
        json=[{"property": "orgName", "value": "New Org"}],
    )

    assert response.status_code == 200
    assert mock_es.bulk.called


def test_get_settings_handles_es_error(client, mock_es):
    mock_es.search.side_effect = Exception("Connection refused")

    response = client.get("/smuiapi4/settings/general")

    assert response.status_code == 500
    data = response.get_json()
    assert data["error"]["code"] == "ES_ERROR"
