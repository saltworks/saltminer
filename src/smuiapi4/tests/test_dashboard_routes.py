def test_get_executive_dashboard(client):
    response = client.get("/smuiapi4/dashboards/executive")

    assert response.status_code == 200
    data = response.get_json()
    assert data["error"] is None
    assert len(data["data"]["kpis"]) == 4
    assert len(data["data"]["topIssues"]) == 4
    assert len(data["data"]["recentActivity"]) == 4


def test_get_development_dashboard(client):
    response = client.get("/smuiapi4/dashboards/development")

    assert response.status_code == 200
    data = response.get_json()
    assert data["error"] is None
    assert len(data["data"]["kpis"]) == 4


def test_get_security_dashboard(client):
    response = client.get("/smuiapi4/dashboards/security")

    assert response.status_code == 200
    data = response.get_json()
    assert data["error"] is None
    assert len(data["data"]["kpis"]) == 4


def test_get_operations_dashboard(client):
    response = client.get("/smuiapi4/dashboards/operations")

    assert response.status_code == 200
    data = response.get_json()
    assert data["error"] is None
    assert len(data["data"]["kpis"]) == 4
