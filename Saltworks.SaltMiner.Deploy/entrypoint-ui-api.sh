#!/bin/sh
set -e

APP_ROOT="${APP_ROOT:-/opt/saltworks/saltminer}"
CONFIG_DIR="${SALTMINER_CONFIG_PATH:-${APP_ROOT}/config}"
LOGS_DIR="${APP_ROOT}/logs"
UI_FILES_DIR="${APP_ROOT}/app/ui-files"

# Ensure the bind-mounted directories ui-api reads/writes exist.  On a fresh
# deployment these mount points may be empty (and config/ui-api is not created by
# sm-init), so guarantee the top-level dirs before handing off to dotnet.
for dir in "$CONFIG_DIR" "$LOGS_DIR" "$UI_FILES_DIR"; do
  if [ ! -d "$dir" ]; then
    echo "Creating missing directory ${dir}..."
    mkdir -p "$dir"
  fi
done

# Start the ui api (as previously defined directly in the Dockerfile entrypoint).
exec /usr/bin/dotnet "${APP_ROOT}/app/ui-api/Saltworks.SaltMiner.Ui.Api.dll"
