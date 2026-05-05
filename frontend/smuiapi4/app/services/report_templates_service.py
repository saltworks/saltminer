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
