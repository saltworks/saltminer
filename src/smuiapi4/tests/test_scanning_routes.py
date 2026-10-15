def test_get_scanning_jobs(client, mock_es_scanning):
    mock_es_scanning.search.return_value = {
        "hits": {
            "hits": [
                {
                    "_id": "job_001",
                    "_source": {
                        "name": "Weekly Network Scan",
                        "scanner": "Nmap",
                        "nextRun": "2026-04-13T02:00:00Z",
                        "lastRun": "2026-04-06T02:00:00Z",
                        "status": "completed",
                    },
                }
            ]
        }
    }

    response = client.get("/smuiapi4/scanning/jobs")
    assert response.status_code == 200
    data = response.get_json()
    assert data["error"] is None
    assert len(data["data"]) == 1
    assert data["data"][0]["name"] == "Weekly Network Scan"


def test_get_all_scanners(client, mock_es_scanning):
    mock_es_scanning.search.return_value = {
        "hits": {
            "hits": [
                {
                    "_id": "scanning_Nmap_target",
                    "_source": {
                        "id": "scanning_Nmap_target",
                        "section": "scanning",
                        "subsection": "Nmap",
                        "property": "target",
                        "value": "192.168.1.0/24",
                        "value_type": "string",
                        "label": "Target",
                        "description": "",
                    },
                },
            ]
        }
    }

    response = client.get("/smuiapi4/scanning/scanners")
    assert response.status_code == 200
    data = response.get_json()
    assert data["error"] is None
    assert len(data["data"]) == 1
    assert data["data"][0]["scanner"] == "Nmap"


def test_get_scanner_settings(client, mock_es_scanning):
    mock_es_scanning.search.return_value = {
        "hits": {
            "hits": [
                {
                    "_id": "scanning_Nmap_target",
                    "_source": {
                        "id": "scanning_Nmap_target",
                        "section": "scanning",
                        "subsection": "Nmap",
                        "property": "target",
                        "value": "192.168.1.0/24",
                        "value_type": "string",
                        "label": "Target",
                        "description": "",
                    },
                },
            ]
        }
    }

    response = client.get("/smuiapi4/scanning/scanners/Nmap")
    assert response.status_code == 200
    data = response.get_json()
    assert data["data"]["scanner"] == "Nmap"
    assert len(data["data"]["properties"]) == 1


def test_put_scanner_settings(client, mock_es_scanning):
    response = client.put(
        "/smuiapi4/scanning/scanners/Nmap",
        json=[{"property": "target", "value": "10.0.0.0/8"}],
    )
    assert response.status_code == 200
    assert mock_es_scanning.bulk.called


def test_delete_scanner(client, mock_es_scanning):
    mock_es_scanning.search.return_value = {
        "hits": {
            "hits": [
                {"_id": "scanning_Nmap_target", "_source": {}},
            ]
        }
    }

    response = client.delete("/smuiapi4/scanning/scanners/Nmap")
    assert response.status_code == 200
    assert mock_es_scanning.bulk.called


def test_get_scan_schedules(client, mock_es_scanning):
    mock_es_scanning.search.return_value = {
        "hits": {
            "hits": [
                {
                    "_id": "schedule_001",
                    "_source": {
                        "name": "Nightly Scan",
                        "scanner": "Nessus",
                        "frequency": "daily",
                        "nextRunDate": "2026-04-07",
                        "startTime": "02:00",
                        "timezone": "UTC",
                        "status": "active",
                    },
                }
            ]
        }
    }

    response = client.get("/smuiapi4/scanning/schedule")
    assert response.status_code == 200
    data = response.get_json()
    assert data["error"] is None
    assert len(data["data"]) == 1
    assert data["data"][0]["scanner"] == "Nessus"


def test_get_scanners_handles_es_error(client, mock_es_scanning):
    mock_es_scanning.search.side_effect = Exception("Connection refused")

    response = client.get("/smuiapi4/scanning/scanners")
    assert response.status_code == 500
    data = response.get_json()
    assert data["error"]["code"] == "ES_ERROR"
