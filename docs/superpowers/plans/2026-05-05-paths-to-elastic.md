# Migrate File-Path Settings to Elastic — Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Move four file-path settings (`customJobsPath`, `saltminerJobsPath`, `sslCertsPath`, `reportTemplatesPath`) out of env vars and into the `sys_config` Elastic index, with code-level defaults applied on miss, plus a UI card on Settings → General to edit them.

**Architecture:** A new `paths_service` module reads each path from Elastic on every call (no caching) and returns a hard-coded default if the doc is missing or Elastic errors. Existing services replace their env-var module constants with calls to the new module. New `/smuiapi4/settings/general/paths` GET/PUT endpoints expose the values. The Vue General settings view gains a "File Storage Paths" card backed by new composable methods.

**Tech Stack:** Python 3.12, Flask, Elasticsearch 8 client, pytest, Vue 3 (Composition API), Vuetify 3, Vite.

**Related spec:** [docs/superpowers/specs/2026-05-05-paths-to-elastic-design.md](../specs/2026-05-05-paths-to-elastic-design.md)

**Working dir for backend tasks:** `frontend/smuiapi4`. Run pytest from that dir: `cd frontend/smuiapi4 && pytest`.
**Working dir for frontend tasks:** `frontend/smui4`.

---

## Task 1: Create `paths_service` module (TDD)

**Files:**
- Create: `frontend/smuiapi4/app/services/paths_service.py`
- Create: `frontend/smuiapi4/tests/test_paths_service.py`

This module is the single source of truth for path resolution. Each function does one ES lookup per call (no cache). On any failure or empty/missing value, it returns the default.

- [ ] **Step 1.1: Write the failing tests**

Create `frontend/smuiapi4/tests/test_paths_service.py` with this exact content:

```python
from unittest.mock import patch

from app.services import paths_service
from app.services.paths_service import (
    DEFAULTS,
    custom_jobs_path,
    saltminer_jobs_path,
    ssl_certs_path,
    report_templates_path,
)


# ---------------------------------------------------------------------------
# Defaults
# ---------------------------------------------------------------------------

def test_defaults_have_all_four_keys():
    assert set(DEFAULTS.keys()) == {
        "customJobsPath",
        "saltminerJobsPath",
        "sslCertsPath",
        "reportTemplatesPath",
    }


def test_defaults_match_spec():
    assert DEFAULTS["customJobsPath"] == "/opt/saltworks/saltminer/custom-jobs/"
    assert DEFAULTS["saltminerJobsPath"] == "/opt/saltworks/saltminer/saltminer-jobs/"
    assert DEFAULTS["sslCertsPath"] == "/opt/saltworks/saltminer/ssl/"
    assert DEFAULTS["reportTemplatesPath"] == "/opt/saltworks/saltminer/report-templates/"


# ---------------------------------------------------------------------------
# Read-through behavior
# ---------------------------------------------------------------------------

def test_returns_default_when_es_returns_no_docs():
    with patch("app.services.paths_service.get_settings_by_section", return_value=[]):
        assert custom_jobs_path() == DEFAULTS["customJobsPath"]
        assert ssl_certs_path() == DEFAULTS["sslCertsPath"]
        assert report_templates_path() == DEFAULTS["reportTemplatesPath"]
        assert saltminer_jobs_path() == DEFAULTS["saltminerJobsPath"]


def test_returns_stored_value_when_doc_present():
    docs = [
        {"property": "customJobsPath", "value": "/var/data/cj/"},
        {"property": "sslCertsPath", "value": "/etc/ssl-custom/"},
    ]
    with patch("app.services.paths_service.get_settings_by_section", return_value=docs):
        assert custom_jobs_path() == "/var/data/cj/"
        assert ssl_certs_path() == "/etc/ssl-custom/"
        # Properties not in docs fall back
        assert report_templates_path() == DEFAULTS["reportTemplatesPath"]


def test_empty_string_value_returns_default():
    docs = [{"property": "customJobsPath", "value": ""}]
    with patch("app.services.paths_service.get_settings_by_section", return_value=docs):
        assert custom_jobs_path() == DEFAULTS["customJobsPath"]


def test_whitespace_value_returns_default():
    docs = [{"property": "customJobsPath", "value": "   "}]
    with patch("app.services.paths_service.get_settings_by_section", return_value=docs):
        assert custom_jobs_path() == DEFAULTS["customJobsPath"]


def test_returns_default_when_es_raises():
    with patch(
        "app.services.paths_service.get_settings_by_section",
        side_effect=Exception("ES down"),
    ):
        assert custom_jobs_path() == DEFAULTS["customJobsPath"]
        assert ssl_certs_path() == DEFAULTS["sslCertsPath"]


def test_each_call_hits_es_no_cache():
    docs = [{"property": "customJobsPath", "value": "/first/"}]
    with patch(
        "app.services.paths_service.get_settings_by_section",
        return_value=docs,
    ) as mock_get:
        custom_jobs_path()
        custom_jobs_path()
        custom_jobs_path()
    assert mock_get.call_count == 3


def test_get_settings_by_section_called_with_correct_args():
    with patch(
        "app.services.paths_service.get_settings_by_section",
        return_value=[],
    ) as mock_get:
        custom_jobs_path()
    mock_get.assert_called_with("general", subsection="paths")
```

- [ ] **Step 1.2: Run tests to verify they fail**

Run: `cd frontend/smuiapi4 && pytest tests/test_paths_service.py -v`
Expected: FAIL with `ModuleNotFoundError` or `ImportError` (module doesn't exist yet).

- [ ] **Step 1.3: Implement `paths_service.py`**

Create `frontend/smuiapi4/app/services/paths_service.py`:

```python
import logging

from app.services.settings_service import get_settings_by_section

logger = logging.getLogger(__name__)

DEFAULTS = {
    "customJobsPath": "/opt/saltworks/saltminer/custom-jobs/",
    "saltminerJobsPath": "/opt/saltworks/saltminer/saltminer-jobs/",
    "sslCertsPath": "/opt/saltworks/saltminer/ssl/",
    "reportTemplatesPath": "/opt/saltworks/saltminer/report-templates/",
}


def _get(property_name: str) -> str:
    default = DEFAULTS[property_name]
    try:
        docs = get_settings_by_section("general", subsection="paths")
    except Exception as exc:
        logger.warning("paths_service: ES read failed for %s: %s", property_name, exc)
        return default

    for doc in docs:
        if doc.get("property") == property_name:
            value = doc.get("value")
            if isinstance(value, str) and value.strip():
                return value
            return default
    return default


def custom_jobs_path() -> str:
    return _get("customJobsPath")


def saltminer_jobs_path() -> str:
    return _get("saltminerJobsPath")


def ssl_certs_path() -> str:
    return _get("sslCertsPath")


def report_templates_path() -> str:
    return _get("reportTemplatesPath")
```

- [ ] **Step 1.4: Run tests to verify they pass**

Run: `cd frontend/smuiapi4 && pytest tests/test_paths_service.py -v`
Expected: all 8 tests PASS.

- [ ] **Step 1.5: Commit**

```bash
git add frontend/smuiapi4/app/services/paths_service.py frontend/smuiapi4/tests/test_paths_service.py
git commit -m "feat(smuiapi4): add paths_service for ES-backed path settings"
```

---

## Task 2: Refactor `custom_jobs_service` to use `paths_service`

**Files:**
- Modify: `frontend/smuiapi4/app/services/custom_jobs_service.py`
- Modify: `frontend/smuiapi4/tests/test_custom_jobs.py`

Replace the module-level `SCRIPTS_PATH = os.environ.get(...)` with a call to `paths_service.custom_jobs_path()` inside `list_scripts()`.

- [ ] **Step 2.1: Update the test file first**

Replace the entire body of `frontend/smuiapi4/tests/test_custom_jobs.py` with:

```python
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
```

- [ ] **Step 2.2: Run tests to verify they fail**

Run: `cd frontend/smuiapi4 && pytest tests/test_custom_jobs.py -v`
Expected: tests FAIL with `ImportError` for `custom_jobs_path` (because the service doesn't import it yet) or `AttributeError`.

- [ ] **Step 2.3: Update the service**

Replace the entire body of `frontend/smuiapi4/app/services/custom_jobs_service.py` with:

```python
import os

from app.services.paths_service import custom_jobs_path


def list_scripts():
    """List files in the custom jobs scripts directory.

    Returns a list of filenames. Empty list if directory doesn't exist.
    """
    scripts_path = custom_jobs_path()
    if not os.path.isdir(scripts_path):
        return []
    entries = []
    for name in os.listdir(scripts_path):
        full_path = os.path.join(scripts_path, name)
        if os.path.isfile(full_path):
            entries.append(name)
    return sorted(entries)
```

- [ ] **Step 2.4: Run tests to verify they pass**

Run: `cd frontend/smuiapi4 && pytest tests/test_custom_jobs.py -v`
Expected: all 6 tests PASS.

- [ ] **Step 2.5: Commit**

```bash
git add frontend/smuiapi4/app/services/custom_jobs_service.py frontend/smuiapi4/tests/test_custom_jobs.py
git commit -m "refactor(smuiapi4): custom_jobs_service reads path via paths_service"
```

---

## Task 3: Refactor `ssl_service` to use `paths_service`

**Files:**
- Modify: `frontend/smuiapi4/app/services/ssl_service.py`
- Modify: `frontend/smuiapi4/tests/test_ssl.py`

Remove the module-level `SSL_CERTS_PATH = os.environ.get(...)` constant. Each function that needs the path calls `paths_service.ssl_certs_path()` and binds it to a local variable.

- [ ] **Step 3.1: Update the service module**

Replace the entire body of `frontend/smuiapi4/app/services/ssl_service.py` with:

```python
import os
from datetime import datetime, timezone, timedelta
from cryptography import x509
from cryptography.hazmat.primitives import serialization

from app.services.paths_service import ssl_certs_path

CERT_FILENAME = "saltminer.crt"
KEY_FILENAME = "saltminer.key"
EXPIRY_WARNING_DAYS = 30


def get_certificate_info():
    certs_dir = ssl_certs_path()
    cert_path = os.path.join(certs_dir, CERT_FILENAME)
    if not os.path.isfile(cert_path):
        return {"found": False}

    with open(cert_path, "rb") as f:
        cert_data = f.read()

    return parse_certificate(cert_data)


def parse_certificate(cert_data):
    try:
        cert = x509.load_pem_x509_certificate(cert_data)
    except Exception:
        return {"found": True, "error": "Unable to parse certificate"}

    now = datetime.now(timezone.utc)
    valid_to = cert.not_valid_after_utc
    is_expired = now > valid_to
    expiring_soon = not is_expired and (valid_to - now) < timedelta(days=EXPIRY_WARNING_DAYS)

    subject = cert.subject.rfc4514_string()
    issuer = cert.issuer.rfc4514_string()

    try:
        cn = cert.subject.get_attributes_for_oid(x509.oid.NameOID.COMMON_NAME)[0].value
    except (IndexError, Exception):
        cn = subject

    try:
        issuer_cn = cert.issuer.get_attributes_for_oid(x509.oid.NameOID.COMMON_NAME)[0].value
    except (IndexError, Exception):
        issuer_cn = issuer

    return {
        "found": True,
        "subject": cn,
        "subjectFull": subject,
        "issuer": issuer_cn,
        "issuerFull": issuer,
        "validFrom": cert.not_valid_before_utc.isoformat(),
        "validTo": valid_to.isoformat(),
        "isExpired": is_expired,
        "expiringSoon": expiring_soon,
        "serialNumber": format(cert.serial_number, "X"),
    }


def validate_cert_format(cert_data):
    if not cert_data.strip().startswith(b"-----BEGIN CERTIFICATE-----"):
        return "Invalid certificate format: must be PEM encoded (-----BEGIN CERTIFICATE-----)"
    try:
        x509.load_pem_x509_certificate(cert_data)
    except Exception as e:
        return f"Invalid certificate: {str(e)}"
    return None


def validate_key_format(key_data):
    if not key_data.strip().startswith(b"-----BEGIN"):
        return "Invalid key format: must be PEM encoded"
    try:
        serialization.load_pem_private_key(key_data, password=None)
    except Exception as e:
        return f"Invalid private key: {str(e)}"
    return None


def validate_cert_key_match(cert_data, key_data):
    try:
        cert = x509.load_pem_x509_certificate(cert_data)
        key = serialization.load_pem_private_key(key_data, password=None)

        cert_public_key = cert.public_key()
        key_public_key = key.public_key()

        cert_pub_bytes = cert_public_key.public_bytes(
            serialization.Encoding.PEM,
            serialization.PublicFormat.SubjectPublicKeyInfo,
        )
        key_pub_bytes = key_public_key.public_bytes(
            serialization.Encoding.PEM,
            serialization.PublicFormat.SubjectPublicKeyInfo,
        )

        if cert_pub_bytes != key_pub_bytes:
            return "Certificate and key do not match"
    except Exception as e:
        return f"Unable to verify cert/key match: {str(e)}"
    return None


def save_certificate(cert_data, key_data):
    certs_dir = ssl_certs_path()
    os.makedirs(certs_dir, exist_ok=True)
    cert_path = os.path.join(certs_dir, CERT_FILENAME)
    key_path = os.path.join(certs_dir, KEY_FILENAME)

    with open(cert_path, "wb") as f:
        f.write(cert_data)
    with open(key_path, "wb") as f:
        f.write(key_data)
    os.chmod(key_path, 0o600)
```

- [ ] **Step 3.2: Run tests to verify behavior**

Run: `cd frontend/smuiapi4 && pytest tests/test_ssl.py -v`
Expected: most tests PASS, but `test_get_certificate_info_no_cert` may fail because it patches `app.services.ssl_service.os.path.isfile` directly and now the code computes a path via the new function. The test still works because `os.path.isfile` is the gate it patches. Confirm result; if any failure, proceed to 3.3.

- [ ] **Step 3.3: Patch `ssl_certs_path` in the existing no-cert test**

In `frontend/smuiapi4/tests/test_ssl.py`, replace the `TestGetCertificateInfo` class with:

```python
class TestGetCertificateInfo:
    def test_get_certificate_info_no_cert(self):
        with patch("app.services.ssl_service.ssl_certs_path", return_value="/fake/ssl/"), \
             patch("app.services.ssl_service.os.path.isfile", return_value=False):
            result = get_certificate_info()
        assert result == {"found": False}
```

- [ ] **Step 3.4: Run tests to verify they pass**

Run: `cd frontend/smuiapi4 && pytest tests/test_ssl.py -v`
Expected: all SSL tests PASS.

- [ ] **Step 3.5: Commit**

```bash
git add frontend/smuiapi4/app/services/ssl_service.py frontend/smuiapi4/tests/test_ssl.py
git commit -m "refactor(smuiapi4): ssl_service reads path via paths_service"
```

---

## Task 4: Refactor `report_templates_service` to use `paths_service`

**Files:**
- Modify: `frontend/smuiapi4/app/services/report_templates_service.py`
- Modify: `frontend/smuiapi4/tests/test_report_templates.py`

Remove the module-level `TEMPLATES_PATH = os.environ.get(...)` constant. Each function that needs the path calls `paths_service.report_templates_path()`.

The existing test file imports `TEMPLATES_PATH` and uses it in assertions — those need updating.

- [ ] **Step 4.1: Update the service module**

Replace the entire body of `frontend/smuiapi4/app/services/report_templates_service.py` with:

```python
import os
import re
import zipfile
import io
from datetime import datetime, timezone

from app.services.paths_service import report_templates_path

MAX_FILE_SIZE = 25 * 1024 * 1024  # 25MB
INVALID_CHARS_PATTERN = re.compile(r'[/\\~\x00]')


def validate_template_filename(name):
    if not name:
        return "Filename is required"
    if '..' in name:
        return "Invalid filename: path traversal characters not allowed"
    if INVALID_CHARS_PATTERN.search(name):
        return "Invalid filename: path traversal characters not allowed"
    if not name.lower().endswith('.docx'):
        return "Filename must end in .docx"
    return None


def validate_docx_content(file_data):
    """Verify the file is a genuine OOXML document (ZIP with [Content_Types].xml)."""
    if len(file_data) < 4:
        return "File is too small to be a valid document"
    if file_data[:4] != b'PK\x03\x04':
        return "File is not a valid .docx document (invalid file signature)"
    try:
        with zipfile.ZipFile(io.BytesIO(file_data), 'r') as zf:
            if '[Content_Types].xml' not in zf.namelist():
                return "File is not a valid .docx document (missing OOXML structure)"
    except zipfile.BadZipFile:
        return "File is not a valid .docx document (corrupt archive)"
    return None


def list_templates():
    templates_dir = report_templates_path()
    if not os.path.isdir(templates_dir):
        return []
    templates = []
    for name in os.listdir(templates_dir):
        if not name.lower().endswith('.docx'):
            continue
        filepath = os.path.join(templates_dir, name)
        if os.path.isfile(filepath):
            stat = os.stat(filepath)
            templates.append({
                "name": name,
                "size": stat.st_size,
                "lastModified": datetime.fromtimestamp(stat.st_mtime, tz=timezone.utc).isoformat(),
            })
    return sorted(templates, key=lambda t: t["name"])


def get_template_path(filename):
    error = validate_template_filename(filename)
    if error:
        return None, error
    filepath = os.path.join(report_templates_path(), filename)
    if not os.path.isfile(filepath):
        return None, "File not found"
    return filepath, None


def template_exists(filename):
    return os.path.isfile(os.path.join(report_templates_path(), filename))


def save_template(filename, file_data):
    templates_dir = report_templates_path()
    os.makedirs(templates_dir, exist_ok=True)
    filepath = os.path.join(templates_dir, filename)
    with open(filepath, 'wb') as f:
        f.write(file_data)


def delete_template(filename):
    filepath = os.path.join(report_templates_path(), filename)
    if os.path.isfile(filepath):
        os.remove(filepath)
        return True
    return False
```

- [ ] **Step 4.2: Update the test imports and assertions**

In `frontend/smuiapi4/tests/test_report_templates.py`:

Replace the import block (lines 24–33) — find this:

```python
from app.services.report_templates_service import (
    validate_template_filename,
    validate_docx_content,
    list_templates,
    get_template_path,
    template_exists,
    save_template,
    delete_template,
    TEMPLATES_PATH,
)
```

Replace with:

```python
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
```

Then wrap every test that touches the filesystem (the ones currently patching `os.*`) so they also patch `report_templates_path` to return `TEMPLATES_PATH`. Replace each affected test with the version below.

Replace `TestListTemplates.test_list_templates_no_dir`:

```python
    def test_list_templates_no_dir(self):
        with patch(PATHS_PATCH, return_value=TEMPLATES_PATH), \
             patch("os.path.isdir", return_value=False):
            result = list_templates()
        assert result == []
```

Replace `TestListTemplates.test_list_templates_returns_docx_files`:

```python
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
```

Replace `TestListTemplates.test_list_templates_sorted_by_name`:

```python
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
```

Replace `TestGetTemplatePath.test_get_template_path_valid`:

```python
    def test_get_template_path_valid(self):
        with patch(PATHS_PATCH, return_value=TEMPLATES_PATH), \
             patch("os.path.isfile", return_value=True):
            path, error = get_template_path("Report.docx")
        assert error is None
        assert path == os.path.join(TEMPLATES_PATH, "Report.docx")
```

Replace `TestGetTemplatePath.test_get_template_path_not_found`:

```python
    def test_get_template_path_not_found(self):
        with patch(PATHS_PATCH, return_value=TEMPLATES_PATH), \
             patch("os.path.isfile", return_value=False):
            path, error = get_template_path("Missing.docx")
        assert path is None
        assert "not found" in error.lower()
```

(Leave `test_get_template_path_invalid_name` as-is — it short-circuits before path lookup.)

Replace `TestSaveTemplate.test_save_template_writes_file`:

```python
    def test_save_template_writes_file(self):
        m = mock_open()
        with patch(PATHS_PATCH, return_value=TEMPLATES_PATH), \
             patch("os.makedirs") as mock_makedirs, \
             patch("builtins.open", m):
            save_template("Report.docx", b"fake content")
        mock_makedirs.assert_called_once_with(TEMPLATES_PATH, exist_ok=True)
        m.assert_called_once_with(os.path.join(TEMPLATES_PATH, "Report.docx"), 'wb')
        m().write.assert_called_once_with(b"fake content")
```

Replace `TestDeleteTemplate.test_delete_template_existing`:

```python
    def test_delete_template_existing(self):
        with patch(PATHS_PATCH, return_value=TEMPLATES_PATH), \
             patch("os.path.isfile", return_value=True), \
             patch("os.remove") as mock_remove:
            result = delete_template("Report.docx")
        assert result is True
        mock_remove.assert_called_once_with(os.path.join(TEMPLATES_PATH, "Report.docx"))
```

Replace `TestDeleteTemplate.test_delete_template_not_found`:

```python
    def test_delete_template_not_found(self):
        with patch(PATHS_PATCH, return_value=TEMPLATES_PATH), \
             patch("os.path.isfile", return_value=False):
            result = delete_template("Missing.docx")
        assert result is False
```

(Route tests in `TestListRoute`, `TestUploadRoute`, `TestDownloadRoute`, `TestDeleteRoute` patch `app.routes.report_templates.*` directly and don't touch `TEMPLATES_PATH` — leave them unchanged.)

- [ ] **Step 4.3: Run tests to verify they pass**

Run: `cd frontend/smuiapi4 && pytest tests/test_report_templates.py -v`
Expected: all tests PASS.

- [ ] **Step 4.4: Commit**

```bash
git add frontend/smuiapi4/app/services/report_templates_service.py frontend/smuiapi4/tests/test_report_templates.py
git commit -m "refactor(smuiapi4): report_templates_service reads path via paths_service"
```

---

## Task 5: Add `/settings/general/paths` GET and PUT endpoints

**Files:**
- Modify: `frontend/smuiapi4/app/routes/settings.py`
- Modify: `frontend/smuiapi4/tests/test_settings_routes.py`

Mirror the existing `/general` GET/PUT pattern, but for the `paths` subsection.

- [ ] **Step 5.1: Write the failing tests**

Append to `frontend/smuiapi4/tests/test_settings_routes.py`:

```python
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
    # Inspect the bulk call args to confirm subsection=paths
    bulk_kwargs = mock_es.bulk.call_args.kwargs
    operations = bulk_kwargs["operations"]
    # operations alternate {update: {...}}, {doc: {...}}; check the doc payloads
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
```

- [ ] **Step 5.2: Run tests to verify they fail**

Run: `cd frontend/smuiapi4 && pytest tests/test_settings_routes.py -v -k paths`
Expected: FAIL with 404 (routes not registered yet).

- [ ] **Step 5.3: Add the routes**

Append to `frontend/smuiapi4/app/routes/settings.py` (before the `/logs` route at the end, or at end — order doesn't matter for Flask):

```python
@settings_bp.route("/general/paths", methods=["GET"])
def get_paths_settings():
    try:
        properties = get_settings_by_section("general", subsection="paths")
        return success_response(properties)
    except Exception as e:
        return error_response(str(e), code="ES_ERROR")


@settings_bp.route("/general/paths", methods=["PUT"])
def update_paths_settings():
    try:
        updates = request.get_json()
        update_settings("general", "paths", updates)
        return success_response({"updated": len(updates)})
    except Exception as e:
        return error_response(str(e), code="ES_ERROR")
```

- [ ] **Step 5.4: Run tests to verify they pass**

Run: `cd frontend/smuiapi4 && pytest tests/test_settings_routes.py -v`
Expected: all settings route tests PASS (including the three new ones).

- [ ] **Step 5.5: Run the full backend suite to confirm nothing else regressed**

Run: `cd frontend/smuiapi4 && pytest -v`
Expected: all tests PASS.

- [ ] **Step 5.6: Commit**

```bash
git add frontend/smuiapi4/app/routes/settings.py frontend/smuiapi4/tests/test_settings_routes.py
git commit -m "feat(smuiapi4): add /settings/general/paths GET and PUT endpoints"
```

---

## Task 6: Remove path env vars from `.env`

**Files:**
- Modify: `frontend/smuiapi4/.env`

Strip the four migrated path vars; keep the ES + Kibana settings.

- [ ] **Step 6.1: Edit `.env`**

Open `frontend/smuiapi4/.env` and remove these four lines:

```
REPORT_TEMPLATES_PATH=./data/report-templates
CUSTOM_JOBS_PATH=./data/custom-jobs
SALTMINER_JOBS_PATH=./data/saltminer-jobs
SSL_CERTS_PATH=./data/ssl
```

After the edit the file should contain only:

```
ES_HOST=https://qatracking-2ffb33.es.us-central1.gcp.cloud.es.io
ES_USER=elastic
ES_PASSWORD=Pnk1DTfQ0cjrrqvoEpEcGCZN
ES_VERIFY_CERTS=false
KIBANA_URL=https://qatracking-2ffb33.kb.us-central1.gcp.cloud.es.io
```

- [ ] **Step 6.2: Verify nothing else reads these env vars**

Run: `cd frontend/smuiapi4 && grep -rn "CUSTOM_JOBS_PATH\|SALTMINER_JOBS_PATH\|SSL_CERTS_PATH\|REPORT_TEMPLATES_PATH" app tests`
Expected: no output (no remaining references).

- [ ] **Step 6.3: Run the full backend test suite**

Run: `cd frontend/smuiapi4 && pytest -v`
Expected: all tests PASS.

- [ ] **Step 6.4: Commit**

`.env` is normally gitignored. Check before committing:

```bash
git check-ignore -v frontend/smuiapi4/.env
```

If it returns a match (file is ignored), skip `git add` for `.env` — there's nothing to commit. The .env on disk is changed for the running dev process; that's all that's needed. Skip to Task 7.

If `.env` is tracked, commit:

```bash
git add frontend/smuiapi4/.env
git commit -m "chore(smuiapi4): remove migrated path env vars"
```

---

## Task 7: Add path settings to the `useSettings` composable

**Files:**
- Modify: `frontend/smui4/src/composables/useSettings.js`

Add `pathsSettings` ref plus `fetchPathsSettings` and `savePathsSettings`. Mirror the `general/other` style.

- [ ] **Step 7.1: Update the composable**

Replace the entire body of `frontend/smui4/src/composables/useSettings.js` with:

```javascript
import { ref } from 'vue'
import apiClient from '../services/api.js'

export function useSettings() {
  const settings = ref([])
  const otherSettings = ref([])
  const pathsSettings = ref([])
  const loading = ref(false)
  const error = ref(null)

  async function fetchGeneralSettings() {
    loading.value = true
    error.value = null
    try {
      const response = await apiClient.get('/settings/general')
      settings.value = response.data
    } catch (e) {
      error.value = e.message
    } finally {
      loading.value = false
    }
  }

  async function saveGeneralSettings(updates) {
    loading.value = true
    error.value = null
    try {
      await apiClient.put('/settings/general', updates)
      await fetchGeneralSettings()
    } catch (e) {
      error.value = e.message
    } finally {
      loading.value = false
    }
  }

  function getSettingValue(propertyName) {
    const setting = settings.value.find((s) => s.property === propertyName)
    return setting?.value ?? null
  }

  async function fetchOtherSettings() {
    loading.value = true
    error.value = null
    try {
      const response = await apiClient.get('/settings/general/other')
      otherSettings.value = response.data
    } catch (e) {
      error.value = e.message
    } finally {
      loading.value = false
    }
  }

  async function createOtherSetting(data) {
    loading.value = true
    error.value = null
    try {
      await apiClient.post('/settings/general/other', data)
      await fetchOtherSettings()
    } catch (e) {
      error.value = e.message
      throw e
    } finally {
      loading.value = false
    }
  }

  async function updateOtherSetting(propertyName, data) {
    loading.value = true
    error.value = null
    try {
      await apiClient.put(`/settings/general/other/${encodeURIComponent(propertyName)}`, data)
      await fetchOtherSettings()
    } catch (e) {
      error.value = e.message
      throw e
    } finally {
      loading.value = false
    }
  }

  async function deleteOtherSetting(propertyName) {
    loading.value = true
    error.value = null
    try {
      await apiClient.delete(`/settings/general/other/${encodeURIComponent(propertyName)}`)
      await fetchOtherSettings()
    } catch (e) {
      error.value = e.message
      throw e
    } finally {
      loading.value = false
    }
  }

  async function fetchPathsSettings() {
    loading.value = true
    error.value = null
    try {
      const response = await apiClient.get('/settings/general/paths')
      pathsSettings.value = response.data
    } catch (e) {
      error.value = e.message
    } finally {
      loading.value = false
    }
  }

  async function savePathsSettings(updates) {
    loading.value = true
    error.value = null
    try {
      await apiClient.put('/settings/general/paths', updates)
      await fetchPathsSettings()
    } catch (e) {
      error.value = e.message
      throw e
    } finally {
      loading.value = false
    }
  }

  function getPathSettingValue(propertyName) {
    const setting = pathsSettings.value.find((s) => s.property === propertyName)
    return setting?.value ?? ''
  }

  return {
    settings,
    otherSettings,
    pathsSettings,
    loading,
    error,
    fetchGeneralSettings,
    saveGeneralSettings,
    getSettingValue,
    fetchOtherSettings,
    createOtherSetting,
    updateOtherSetting,
    deleteOtherSetting,
    fetchPathsSettings,
    savePathsSettings,
    getPathSettingValue,
  }
}
```

- [ ] **Step 7.2: Commit**

```bash
git add frontend/smui4/src/composables/useSettings.js
git commit -m "feat(smui4): add paths settings to useSettings composable"
```

---

## Task 8: Add "File Storage Paths" card to General settings view

**Files:**
- Modify: `frontend/smui4/src/views/settings/GeneralView.vue`

Add the card after the existing "Additional Settings" card, inside the `general` `v-window-item`. Wire it to the new composable methods.

- [ ] **Step 8.1: Update `<script setup>` section**

In `frontend/smui4/src/views/settings/GeneralView.vue`, find the destructuring of `useSettings()` (currently lines 238–249):

```javascript
const {
  loading,
  error,
  otherSettings,
  fetchGeneralSettings,
  saveGeneralSettings,
  getSettingValue,
  fetchOtherSettings,
  createOtherSetting,
  updateOtherSetting,
  deleteOtherSetting,
} = useSettings()
```

Replace with:

```javascript
const {
  loading,
  error,
  otherSettings,
  pathsSettings,
  fetchGeneralSettings,
  saveGeneralSettings,
  getSettingValue,
  fetchOtherSettings,
  createOtherSetting,
  updateOtherSetting,
  deleteOtherSetting,
  fetchPathsSettings,
  savePathsSettings,
  getPathSettingValue,
} = useSettings()
```

- [ ] **Step 8.2: Add path-form state and defaults constants**

In the same `<script setup>` section, find the existing `form` declaration:

```javascript
const form = ref({
  orgName: '',
  primaryDomain: '',
})
```

Immediately after it, add:

```javascript
const PATH_DEFAULTS = {
  customJobsPath: '/opt/saltworks/saltminer/custom-jobs/',
  saltminerJobsPath: '/opt/saltworks/saltminer/saltminer-jobs/',
  sslCertsPath: '/opt/saltworks/saltminer/ssl/',
  reportTemplatesPath: '/opt/saltworks/saltminer/report-templates/',
}

const PATH_LABELS = {
  customJobsPath: 'Custom Jobs Path',
  saltminerJobsPath: 'SaltMiner Jobs Path',
  sslCertsPath: 'SSL Certificates Path',
  reportTemplatesPath: 'Report Templates Path',
}

const PATH_HINTS = {
  customJobsPath: 'Directory containing custom job scripts',
  saltminerJobsPath: 'Directory containing SaltMiner job scripts',
  sslCertsPath: 'Directory holding the saltminer.crt and saltminer.key files',
  reportTemplatesPath: 'Directory holding uploaded report .docx templates',
}

const pathsForm = ref({
  customJobsPath: '',
  saltminerJobsPath: '',
  sslCertsPath: '',
  reportTemplatesPath: '',
})
```

- [ ] **Step 8.3: Update `onMounted` to fetch paths**

Find the existing `onMounted` block:

```javascript
onMounted(async () => {
  await fetchGeneralSettings()
  form.value.orgName = getSettingValue('orgName') || ''
  form.value.primaryDomain = getSettingValue('primaryDomain') || ''
  await fetchOtherSettings()
})
```

Replace with:

```javascript
onMounted(async () => {
  await fetchGeneralSettings()
  form.value.orgName = getSettingValue('orgName') || ''
  form.value.primaryDomain = getSettingValue('primaryDomain') || ''
  await fetchOtherSettings()
  await fetchPathsSettings()
  for (const key of Object.keys(pathsForm.value)) {
    pathsForm.value[key] = getPathSettingValue(key)
  }
})
```

- [ ] **Step 8.4: Add `savePaths` function**

After the existing `saveOrgSettings` function, add:

```javascript
async function savePaths() {
  const updates = Object.keys(pathsForm.value).map((property) => ({
    property,
    value: (pathsForm.value[property] || '').trim(),
    value_type: 'string',
    label: PATH_LABELS[property],
    description: PATH_HINTS[property],
  }))
  try {
    await savePathsSettings(updates)
    for (const key of Object.keys(pathsForm.value)) {
      pathsForm.value[key] = getPathSettingValue(key)
    }
  } catch (e) {
    // error already set by composable
  }
}
```

- [ ] **Step 8.5: Add the card to the template**

In the template, find the closing `</v-card>` of the "Additional Settings" card (currently around line 94), then the closing `</v-window-item>` for the `general` tab right after it.

The structure currently is:

```html
        <!-- Other Settings -->
        <v-card class="pa-6">
          ...
        </v-card>
      </v-window-item>
```

Insert a new `<v-card>` between the closing `</v-card>` of "Additional Settings" and the closing `</v-window-item>`:

```html
        <!-- Other Settings -->
        <v-card class="pa-6">
          ...
        </v-card>

        <!-- File Storage Paths -->
        <v-card class="pa-6 mt-6">
          <div class="d-flex align-center mb-2">
            <v-icon color="primary" class="mr-2">mdi-folder-cog</v-icon>
            <span class="text-h6">File Storage Paths</span>
          </div>
          <p class="text-body-2 text-medium-emphasis mb-4">
            Server-side directories used by the API. Leave a field blank to use the default.
          </p>

          <v-text-field
            v-for="key in Object.keys(pathsForm)"
            :key="key"
            v-model="pathsForm[key]"
            :label="PATH_LABELS[key]"
            :placeholder="PATH_DEFAULTS[key]"
            :hint="PATH_HINTS[key]"
            persistent-hint
            persistent-placeholder
            class="mb-4"
            :loading="loading"
          />

          <v-btn color="primary" :loading="loading" @click="savePaths">
            Save Changes
          </v-btn>
        </v-card>
      </v-window-item>
```

(Keep the existing "Additional Settings" `<v-card>` exactly as it is; only insert the new card between it and `</v-window-item>`.)

- [ ] **Step 8.6: Manually verify in the browser**

Run the dev server (use whatever command the project already uses; if unsure check `frontend/smui4/package.json`):

```bash
cd frontend/smui4 && npm run dev
```

Make sure the API is also running. Open the app, navigate to **Settings → General**, and confirm:

1. The "File Storage Paths" card appears at the bottom of the General tab.
2. All four fields render with their correct labels and placeholders.
3. With no docs in `sys_config` for `(general, paths)`, the fields are empty and placeholders show the defaults.
4. Type a value into one field, click "Save Changes" — no errors, page refreshes the values.
5. Reload — the saved value is shown.
6. Clear the field, click "Save Changes" — value is saved as empty, and on reload the field is empty again (the backend's default kicks in for actual file ops, which is invisible from the UI).
7. Confirm via DevTools network tab: `PUT /smuiapi4/settings/general/paths` payload includes all four properties.

If any step fails, fix and re-verify before committing.

- [ ] **Step 8.7: Commit**

```bash
git add frontend/smui4/src/views/settings/GeneralView.vue
git commit -m "feat(smui4): add File Storage Paths card to General settings"
```

---

## Final verification

- [ ] **Step F.1: Backend full suite**

Run: `cd frontend/smuiapi4 && pytest -v`
Expected: all tests PASS.

- [ ] **Step F.2: Confirm no env-var leftovers**

Run from repo root: `grep -rn "CUSTOM_JOBS_PATH\|SALTMINER_JOBS_PATH\|SSL_CERTS_PATH\|REPORT_TEMPLATES_PATH" frontend/`
Expected: no output (no remaining references in `frontend/`).

- [ ] **Step F.3: Confirm path service is wired everywhere**

Run from repo root: `grep -rn "from app.services.paths_service" frontend/smuiapi4/app/`
Expected: imports in `custom_jobs_service.py`, `ssl_service.py`, `report_templates_service.py`.

- [ ] **Step F.4: Confirm UI smoke**

Re-do Step 8.6's checklist from a clean reload to be sure nothing was missed.

---

## Notes for the executing engineer

- **TDD discipline:** every backend task writes the test first, runs it, watches it fail, then implements. Don't skip the failing-run step.
- **Empty-string semantics:** the backend treats blank/whitespace as "use default". The UI sends empty strings; that's intentional. Don't change either side to "delete the doc when empty" — that would break the read-once-on-every-call contract because deletion semantics would diverge from blank-string semantics.
- **No caching:** the spec explicitly chose option B. Do not add an `lru_cache` or module-level cache to `paths_service` even though the round-trip count looks high — the volume is low and freshness matters for the UI's edit flow.
- **Docker `mkdir` lines:** keep them in the Dockerfile. They pre-create the default directories so first-boot file ops don't fail.
- **`saltminerJobsPath` has no consumer yet:** the path is exposed end-to-end (defaults, ES, route, UI) but no code reads it. That's by design — when the saltminer jobs service lands, it imports `saltminer_jobs_path` and is done.
