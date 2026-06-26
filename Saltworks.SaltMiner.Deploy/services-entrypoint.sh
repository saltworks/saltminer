#!/bin/sh
set -e

APP_ROOT="${APP_ROOT:-/opt/saltworks/saltminer}"
SCRIPTS_DIR="${APP_ROOT}/scripts"
DEFAULT_SCRIPTS_DIR="${APP_ROOT}/app/default-scripts"

# The scripts dir is bind-mounted from the host. On a fresh deployment it will
# be empty, so seed it with the default scripts baked into the image. If it
# already has contents we leave any host-customized scripts untouched.
if [ -d "$SCRIPTS_DIR" ] && [ -n "$(ls -A "$SCRIPTS_DIR" 2>/dev/null)" ]; then
  echo "Scripts found in ${SCRIPTS_DIR}, leaving existing scripts in place."
else
  echo "No scripts found in ${SCRIPTS_DIR}, copying in default scripts..."
  mkdir -p "$SCRIPTS_DIR"
  cp -a "$DEFAULT_SCRIPTS_DIR/." "$SCRIPTS_DIR/"
fi

# Start the service manager (as previously defined directly in the Dockerfile entrypoint).
exec /usr/bin/dotnet "${APP_ROOT}/app/servicemanager/Saltworks.SaltMiner.ServiceManager.dll"
