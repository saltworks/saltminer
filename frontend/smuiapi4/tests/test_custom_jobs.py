from unittest.mock import patch

from app.services.custom_jobs_service import list_scripts


PATHS_PATCH = "app.services.custom_jobs_service.custom_jobs_path"
FAKE_PATH = "/fake/custom-jobs/"


# --- Service tests ---

def test_list_scripts_empty_when_directory_missing(app):
    with patch(PATHS_PATCH, return_value=FAKE_PATH), \
         patch("os.path.isdir", return_value=False):
        with app.app_context():
            assert list_scripts() == []


def test_list_scripts_returns_filenames(app):
    with patch(PATHS_PATCH, return_value=FAKE_PATH), \
         patch("os.path.isdir", return_value=True), \
         patch("os.listdir", return_value=["b.py", "a.py", "subdir"]), \
         patch("os.path.isfile", side_effect=lambda p: not p.endswith("subdir")):
        with app.app_context():
            result = list_scripts()

    assert result == ["a.py", "b.py"]


def test_list_scripts_ignores_directories(app):
    with patch(PATHS_PATCH, return_value=FAKE_PATH), \
         patch("os.path.isdir", return_value=True), \
         patch("os.listdir", return_value=["script.py", "nested"]), \
         patch("os.path.isfile", side_effect=lambda p: p.endswith("script.py")):
        with app.app_context():
            result = list_scripts()

    assert result == ["script.py"]


# --- Route tests ---

def test_route_list_scripts(client):
    with patch("app.services.custom_jobs_service.list_scripts", return_value=["foo.py", "bar.py"]):
        response = client.get("/smuiapi4/custom-jobs/scripts")
    assert response.status_code == 200
    data = response.get_json()
    assert data["error"] is None
    assert data["data"] == ["foo.py", "bar.py"]


def test_route_list_scripts_empty(client):
    with patch("app.services.custom_jobs_service.list_scripts", return_value=[]):
        response = client.get("/smuiapi4/custom-jobs/scripts")
    assert response.status_code == 200
    assert response.get_json()["data"] == []


def test_route_list_scripts_handles_error(client):
    with patch("app.services.custom_jobs_service.list_scripts", side_effect=Exception("boom")):
        response = client.get("/smuiapi4/custom-jobs/scripts")
    assert response.status_code == 500
    assert response.get_json()["error"]["code"] == "FS_ERROR"
