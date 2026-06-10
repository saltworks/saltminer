import io
import os
import zipfile
import tempfile
from unittest.mock import patch, MagicMock, mock_open
import pytest


# ---------------------------------------------------------------------------
# Helpers
# ---------------------------------------------------------------------------

def make_fake_docx():
    buf = io.BytesIO()
    with zipfile.ZipFile(buf, 'w') as zf:
        zf.writestr('[Content_Types].xml', '<Types></Types>')
    return buf.getvalue()


# ---------------------------------------------------------------------------
# Service unit tests
# ---------------------------------------------------------------------------

from app.services.report_templates_service import (
    validate_template_filename,
    validate_docx_content,
    list_templates,
    get_template_path,
    template_exists,
    save_template,
    delete_template,
)

# Test fixture: pin the path the service uses so existing assertions still hold.
TEMPLATES_PATH = "/opt/saltworks/saltminer/report-templates/"
PATHS_PATCH = "app.services.report_templates_service.report_templates_path"


class TestValidateTemplateFilename:
    def test_validate_filename_valid(self):
        assert validate_template_filename("Report.docx") is None

    def test_validate_filename_path_traversal(self):
        result = validate_template_filename("../evil.docx")
        assert result is not None
        assert "path traversal" in result.lower()

    def test_validate_filename_bad_extension(self):
        result = validate_template_filename("report.exe")
        assert result is not None
        assert ".docx" in result

    def test_validate_filename_empty(self):
        result = validate_template_filename("")
        assert result is not None
        assert "required" in result.lower()

    def test_validate_filename_slash(self):
        result = validate_template_filename("dir/evil.docx")
        assert result is not None

    def test_validate_filename_backslash(self):
        result = validate_template_filename("dir\\evil.docx")
        assert result is not None

    def test_validate_filename_null_byte(self):
        result = validate_template_filename("evil\x00.docx")
        assert result is not None


class TestValidateDocxContent:
    def test_validate_docx_content_valid(self):
        data = make_fake_docx()
        assert validate_docx_content(data) is None

    def test_validate_docx_content_not_zip(self):
        result = validate_docx_content(b"not a zip file data")
        assert result is not None
        assert "valid" in result.lower()

    def test_validate_docx_content_zip_no_content_types(self):
        buf = io.BytesIO()
        with zipfile.ZipFile(buf, 'w') as zf:
            zf.writestr('word/document.xml', '<w:document></w:document>')
        data = buf.getvalue()
        result = validate_docx_content(data)
        assert result is not None
        assert "ooxml" in result.lower() or "missing" in result.lower() or "valid" in result.lower()

    def test_validate_docx_content_too_small(self):
        result = validate_docx_content(b"PK")
        assert result is not None
        assert "small" in result.lower() or "valid" in result.lower()


class TestListTemplates:
    def test_list_templates_no_dir(self):
        with patch(PATHS_PATCH, return_value=TEMPLATES_PATH), \
             patch("os.path.isdir", return_value=False):
            result = list_templates()
        assert result == []

    def test_list_templates_returns_docx_files(self):
        mock_stat = MagicMock()
        mock_stat.st_size = 1024
        mock_stat.st_mtime = 1700000000.0

        with patch(PATHS_PATCH, return_value=TEMPLATES_PATH), \
             patch("os.path.isdir", return_value=True), \
             patch("os.listdir", return_value=["Report.docx", "notes.txt", "Template.docx"]), \
             patch("os.path.isfile", return_value=True), \
             patch("os.stat", return_value=mock_stat):
            result = list_templates()

        assert len(result) == 2
        names = [r["name"] for r in result]
        assert "Report.docx" in names
        assert "Template.docx" in names
        assert "notes.txt" not in names

    def test_list_templates_sorted_by_name(self):
        mock_stat = MagicMock()
        mock_stat.st_size = 512
        mock_stat.st_mtime = 1700000000.0

        with patch(PATHS_PATCH, return_value=TEMPLATES_PATH), \
             patch("os.path.isdir", return_value=True), \
             patch("os.listdir", return_value=["Zebra.docx", "Alpha.docx", "Mango.docx"]), \
             patch("os.path.isfile", return_value=True), \
             patch("os.stat", return_value=mock_stat):
            result = list_templates()

        assert [r["name"] for r in result] == ["Alpha.docx", "Mango.docx", "Zebra.docx"]


class TestGetTemplatePath:
    def test_get_template_path_valid(self):
        with patch(PATHS_PATCH, return_value=TEMPLATES_PATH), \
             patch("os.path.isfile", return_value=True):
            path, error = get_template_path("Report.docx")
        assert error is None
        assert path == os.path.join(TEMPLATES_PATH, "Report.docx")

    def test_get_template_path_not_found(self):
        with patch(PATHS_PATCH, return_value=TEMPLATES_PATH), \
             patch("os.path.isfile", return_value=False):
            path, error = get_template_path("Missing.docx")
        assert path is None
        assert "not found" in error.lower()

    def test_get_template_path_invalid_name(self):
        path, error = get_template_path("../evil.docx")
        assert path is None
        assert error is not None


class TestSaveTemplate:
    def test_save_template_writes_file(self):
        m = mock_open()
        with patch(PATHS_PATCH, return_value=TEMPLATES_PATH), \
             patch("os.makedirs") as mock_makedirs, \
             patch("builtins.open", m):
            save_template("Report.docx", b"fake content")
        mock_makedirs.assert_called_once_with(TEMPLATES_PATH, exist_ok=True)
        m.assert_called_once_with(os.path.join(TEMPLATES_PATH, "Report.docx"), 'wb')
        m().write.assert_called_once_with(b"fake content")


class TestDeleteTemplate:
    def test_delete_template_existing(self):
        with patch(PATHS_PATCH, return_value=TEMPLATES_PATH), \
             patch("os.path.isfile", return_value=True), \
             patch("os.remove") as mock_remove:
            result = delete_template("Report.docx")
        assert result is True
        mock_remove.assert_called_once_with(os.path.join(TEMPLATES_PATH, "Report.docx"))

    def test_delete_template_not_found(self):
        with patch(PATHS_PATCH, return_value=TEMPLATES_PATH), \
             patch("os.path.isfile", return_value=False):
            result = delete_template("Missing.docx")
        assert result is False


# ---------------------------------------------------------------------------
# Route tests
# ---------------------------------------------------------------------------

SERVICE_MODULE = "app.routes.report_templates"


class TestListRoute:
    def test_route_list_templates(self, client):
        mock_data = [
            {"name": "Alpha.docx", "size": 1024, "lastModified": "2024-01-01T00:00:00+00:00"},
        ]
        with patch(f"{SERVICE_MODULE}.list_templates", return_value=mock_data):
            response = client.get("/smuiapi4/report-templates")
        assert response.status_code == 200
        data = response.get_json()
        assert data["error"] is None
        assert len(data["data"]) == 1
        assert data["data"][0]["name"] == "Alpha.docx"


class TestUploadRoute:
    def test_route_upload_template(self, client):
        fake_docx = make_fake_docx()
        with patch(f"{SERVICE_MODULE}.template_exists", return_value=False), \
             patch(f"{SERVICE_MODULE}.validate_docx_content", return_value=None), \
             patch(f"{SERVICE_MODULE}.save_template") as mock_save:
            response = client.post(
                "/smuiapi4/report-templates",
                data={"file": (io.BytesIO(fake_docx), "Report.docx")},
                content_type="multipart/form-data",
            )
        assert response.status_code == 201
        data = response.get_json()
        assert data["data"]["uploaded"] == "Report.docx"
        mock_save.assert_called_once()

    def test_route_upload_duplicate(self, client):
        fake_docx = make_fake_docx()
        with patch(f"{SERVICE_MODULE}.template_exists", return_value=True):
            response = client.post(
                "/smuiapi4/report-templates",
                data={"file": (io.BytesIO(fake_docx), "Report.docx")},
                content_type="multipart/form-data",
            )
        assert response.status_code == 409
        data = response.get_json()
        assert data["error"]["code"] == "DUPLICATE_TEMPLATE"

    def test_route_upload_invalid_filename(self, client):
        fake_docx = make_fake_docx()
        response = client.post(
            "/smuiapi4/report-templates",
            data={"file": (io.BytesIO(fake_docx), "../evil.docx")},
            content_type="multipart/form-data",
        )
        assert response.status_code == 400
        data = response.get_json()
        assert data["error"]["code"] == "INVALID_FILENAME"

    def test_route_upload_invalid_content(self, client):
        bad_data = b"this is not a docx file at all"
        with patch(f"{SERVICE_MODULE}.template_exists", return_value=False), \
             patch(f"{SERVICE_MODULE}.validate_docx_content", return_value="File is not a valid .docx document (invalid file signature)"):
            response = client.post(
                "/smuiapi4/report-templates",
                data={"file": (io.BytesIO(bad_data), "Report.docx")},
                content_type="multipart/form-data",
            )
        assert response.status_code == 400
        data = response.get_json()
        assert data["error"]["code"] == "INVALID_CONTENT"

    def test_route_upload_no_file(self, client):
        response = client.post(
            "/smuiapi4/report-templates",
            data={},
            content_type="multipart/form-data",
        )
        assert response.status_code == 400
        data = response.get_json()
        assert data["error"]["code"] == "NO_FILE"


class TestDownloadRoute:
    def test_route_download_template(self, client):
        with tempfile.NamedTemporaryFile(suffix=".docx", delete=False) as tmp:
            tmp.write(make_fake_docx())
            tmp_path = tmp.name
        try:
            with patch(f"{SERVICE_MODULE}.get_template_path", return_value=(tmp_path, None)):
                response = client.get("/smuiapi4/report-templates/Report.docx")
            assert response.status_code == 200
        finally:
            os.unlink(tmp_path)

    def test_route_download_not_found(self, client):
        with patch(f"{SERVICE_MODULE}.get_template_path", return_value=(None, "File not found")):
            response = client.get("/smuiapi4/report-templates/Missing.docx")
        assert response.status_code == 404
        data = response.get_json()
        assert data["error"]["code"] == "NOT_FOUND"


class TestDeleteRoute:
    def test_route_delete_template(self, client):
        with patch(f"{SERVICE_MODULE}.template_exists", return_value=True), \
             patch(f"{SERVICE_MODULE}.delete_template") as mock_del:
            response = client.delete("/smuiapi4/report-templates/Report.docx")
        assert response.status_code == 200
        data = response.get_json()
        assert data["data"]["deleted"] == "Report.docx"
        mock_del.assert_called_once_with("Report.docx")

    def test_route_delete_not_found(self, client):
        with patch(f"{SERVICE_MODULE}.template_exists", return_value=False):
            response = client.delete("/smuiapi4/report-templates/Missing.docx")
        assert response.status_code == 404
        data = response.get_json()
        assert data["error"]["code"] == "NOT_FOUND"
