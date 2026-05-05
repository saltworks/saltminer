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


def test_get_paths_settings(client, mock_es):
    mock_es.search.return_value = {
        "hits": {
            "hits": [
                {
                    "_id": "general_paths_customJobsPath",
                    "_source": {
                        "id": "general_paths_customJobsPath",
                        "section": "general",
                        "subsection": "paths",
                        "property": "customJobsPath",
                        "value": "/var/data/cj/",
                        "value_type": "string",
                        "label": "Custom Jobs Path",
                        "description": "",
                    },
                },
            ]
        }
    }

    response = client.get("/smuiapi4/settings/general/paths")

    assert response.status_code == 200
    data = response.get_json()
    assert data["error"] is None
    assert len(data["data"]) == 1
    assert data["data"][0]["property"] == "customJobsPath"
    assert data["data"][0]["value"] == "/var/data/cj/"


def test_put_paths_settings(client, mock_es):
    response = client.put(
        "/smuiapi4/settings/general/paths",
        json=[
            {"property": "customJobsPath", "value": "/var/data/cj/", "value_type": "string", "label": "Custom Jobs Path"},
            {"property": "sslCertsPath", "value": "", "value_type": "string", "label": "SSL Certs Path"},
        ],
    )

    assert response.status_code == 200
    assert mock_es.bulk.called
    bulk_kwargs = mock_es.bulk.call_args.kwargs
    operations = bulk_kwargs["operations"]
    doc_entries = [op for op in operations if "doc" in op]
    assert all(d["doc"]["section"] == "general" for d in doc_entries)
    assert all(d["doc"]["subsection"] == "paths" for d in doc_entries)
    properties = {d["doc"]["property"] for d in doc_entries}
    assert properties == {"customJobsPath", "sslCertsPath"}


def test_put_paths_settings_handles_es_error(client, mock_es):
    mock_es.bulk.side_effect = Exception("Connection refused")

    response = client.put(
        "/smuiapi4/settings/general/paths",
        json=[{"property": "customJobsPath", "value": "/foo/"}],
    )

    assert response.status_code == 500
    data = response.get_json()
    assert data["error"]["code"] == "ES_ERROR"
