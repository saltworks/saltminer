#!/bin/sh
set -e

APP_ROOT="${APP_ROOT:-/opt/saltworks/saltminer}"
CONFIG_DIR="${SALTMINER_CONFIG_PATH:-${APP_ROOT}/config/jobmanager}"
LOGS_DIR="${APP_ROOT}/logs"
UI_FILES_DIR="${APP_ROOT}/app/ui-files"

# Ensure the bind-mounted directories jobmanager reads/writes exist.  On a fresh
# deployment these mount points may be empty, so guarantee the top-level dirs
# before handing off to dotnet.
for dir in "$CONFIG_DIR" "$LOGS_DIR" "$UI_FILES_DIR"; do
  if [ ! -d "$dir" ]; then
    echo "Creating missing directory ${dir}..."
    mkdir -p "$dir"
  fi
done

# Start the job manager (as previously defined directly in the Dockerfile entrypoint).
exec /usr/bin/dotnet "${APP_ROOT}/app/jobmanager/Saltworks.SaltMiner.JobManager.dll"
