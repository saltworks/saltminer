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
