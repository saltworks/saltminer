import os

SCRIPTS_PATH = os.environ.get("CUSTOM_JOBS_PATH", "/opt/saltworks/saltminer/custom-jobs/")


def list_scripts():
    """List files in the custom jobs scripts directory.

    Returns a list of filenames. Empty list if directory doesn't exist.
    """
    if not os.path.isdir(SCRIPTS_PATH):
        return []
    entries = []
    for name in os.listdir(SCRIPTS_PATH):
        full_path = os.path.join(SCRIPTS_PATH, name)
        if os.path.isfile(full_path):
            entries.append(name)
    return sorted(entries)
