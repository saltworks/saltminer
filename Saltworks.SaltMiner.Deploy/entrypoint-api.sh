#!/bin/sh
set -e

APP_ROOT="${APP_ROOT:-/opt/saltworks/saltminer}"
CONFIG_DIR="${SALTMINER_CONFIG_PATH:-${APP_ROOT}/config}"
LOGS_DIR="${APP_ROOT}/logs"
DATA_DIR="${APP_ROOT}/app/api/data"

# Ensure the bind-mounted directories the api writes to exist.  On a fresh
# deployment these mount points may be empty; the api itself seeds the datastore
# contents at runtime (see DataApi Program.cs), so here we only guarantee the
# top-level directories are present before handing off to dotnet.
for dir in "$CONFIG_DIR" "$LOGS_DIR" "$DATA_DIR"; do
  if [ ! -d "$dir" ]; then
    echo "Creating missing directory ${dir}..."
    mkdir -p "$dir"
  fi
done

# Start the data api (as previously defined directly in the Dockerfile entrypoint).
exec /usr/bin/dotnet "${APP_ROOT}/app/api/Saltworks.SaltMiner.DataApi.dll"
