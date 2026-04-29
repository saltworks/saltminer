from app.services.scanning_service import (
    get_all_scanners,
    get_scanner_settings,
    update_scanner_settings,
    delete_scanner,
    get_scanning_jobs,
    get_scan_schedules,
    AVAILABLE_SCANNERS,
)


def test_get_all_scanners_groups_by_scanner(app, mock_es_scanning):
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
                {
                    "_id": "scanning_Nmap_ports",
                    "_source": {
                        "id": "scanning_Nmap_ports",
                        "section": "scanning",
                        "subsection": "Nmap",
                        "property": "ports",
                        "value": "1-1024",
                        "value_type": "string",
                        "label": "Ports",
                        "description": "",
                    },
                },
                {
                    "_id": "scanning_Nessus_apiKey",
                    "_source": {
                        "id": "scanning_Nessus_apiKey",
                        "section": "scanning",
                        "subsection": "Nessus",
                        "property": "apiKey",
                        "value": "abc123",
                        "value_type": "string",
                        "label": "API Key",
                        "description": "",
                    },
                },
            ]
        }
    }

    with app.app_context():
        result = get_all_scanners()

    assert len(result) == 2
    nmap = next(s for s in result if s["scanner"] == "Nmap")
    assert len(nmap["properties"]) == 2
    nessus = next(s for s in result if s["scanner"] == "Nessus")
    assert len(nessus["properties"]) == 1


def test_get_all_scanners_empty(app, mock_es_scanning):
    mock_es_scanning.search.return_value = {"hits": {"hits": []}}

    with app.app_context():
        result = get_all_scanners()

    assert result == []


def test_get_scanner_settings(app, mock_es_scanning):
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

    with app.app_context():
        result = get_scanner_settings("Nmap")

    assert result["scanner"] == "Nmap"
    assert len(result["properties"]) == 1
    assert result["properties"][0]["property"] == "target"


def test_update_scanner_settings(app, mock_es_scanning):
    updates = [
        {"property": "target", "value": "10.0.0.0/8"},
        {"property": "ports", "value": "1-65535", "value_type": "string"},
    ]

    with app.app_context():
        update_scanner_settings("Nmap", updates)

    assert mock_es_scanning.bulk.called
    bulk_body = mock_es_scanning.bulk.call_args[1]["operations"]
    assert len(bulk_body) == 4  # 2 actions + 2 docs


def test_delete_scanner(app, mock_es_scanning):
    mock_es_scanning.search.return_value = {
        "hits": {
            "hits": [
                {"_id": "scanning_Nmap_target", "_source": {}},
                {"_id": "scanning_Nmap_ports", "_source": {}},
            ]
        }
    }

    with app.app_context():
        delete_scanner("Nmap")

    assert mock_es_scanning.bulk.called
    bulk_body = mock_es_scanning.bulk.call_args[1]["operations"]
    assert len(bulk_body) == 2
    assert all(op.get("delete") for op in bulk_body)


def test_get_scanning_jobs(app, mock_es_scanning):
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

    with app.app_context():
        result = get_scanning_jobs()

    assert len(result) == 1
    assert result[0]["name"] == "Weekly Network Scan"
    assert result[0]["scanner"] == "Nmap"
    assert result[0]["status"] == "completed"


def test_get_scan_schedules(app, mock_es_scanning):
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

    with app.app_context():
        result = get_scan_schedules()

    assert len(result) == 1
    assert result[0]["name"] == "Nightly Scan"
    assert result[0]["scanner"] == "Nessus"
    assert result[0]["frequency"] == "daily"
    assert result[0]["timezone"] == "UTC"
    assert result[0]["status"] == "active"


def test_available_scanners():
    assert len(AVAILABLE_SCANNERS) == 6
    names = [s["name"] for s in AVAILABLE_SCANNERS]
    assert "Nmap" in names
    assert "Nessus" in names
    assert "OpenVAS" in names
    assert "Burp Suite" in names
    assert "Nikto" in names
    assert "Metasploit" in names
    for item in AVAILABLE_SCANNERS:
        assert "name" in item
        assert "description" in item
        assert "icon" in item
