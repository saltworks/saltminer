#!/bin/sh
set -e

APP_ROOT="${APP_ROOT:-/opt/saltworks/saltminer}"
SCRIPTS_DIR="${APP_ROOT}/scripts"
DEFAULT_SCRIPTS_DIR="${APP_ROOT}/app/default-scripts"
PY_DIR="${APP_ROOT}/app/python"
PY_DEFAULTS_DIR="${PY_DIR}/Defaults"
PY_CONFIG_DIR="${SALTMINER_CONFIG_PATH:-${APP_ROOT}/config}/python"

# Seed a destination directory from a baked-in defaults directory.  On a fresh
# deployment the (bind-mounted) destination will be missing or empty, so copy in
# the defaults.  If it already has contents, leave host-customized files untouched.
seed_dir() {
  src="$1"
  dest="$2"
  if [ -d "$dest" ] && [ -n "$(ls -A "$dest" 2>/dev/null)" ]; then
    echo "Contents found in ${dest}, leaving existing files in place."
  else
    echo "No contents found in ${dest}, copying in defaults from ${src}..."
    mkdir -p "$dest"
    cp -a "$src/." "$dest/"
  fi
}

# Seed default scripts (bind-mounted from the host).
seed_dir "$DEFAULT_SCRIPTS_DIR" "$SCRIPTS_DIR"

# Seed the default python configuration.  The python Application class only locates
# existing config (see Core/Application.py __InitConfig) - it does not create one on
# first run, so an empty config mount must be seeded here.
if [ -d "${PY_DEFAULTS_DIR}/Config" ]; then
  seed_dir "${PY_DEFAULTS_DIR}/Config" "$PY_CONFIG_DIR"
fi

# Seed the python custom/mapping dirs (bind-mounted from the host).  The Dockerfile
# moves these out of the python app dir into Defaults at build time - seed every dir
# found there (except Config, handled above) into its expected runtime path.
for src in "$PY_DEFAULTS_DIR"/*/; do
  [ -d "$src" ] || continue
  name="$(basename "$src")"
  [ "$name" = "Config" ] && continue
  seed_dir "$src" "${PY_DIR}/${name}"
done

# Start the service manager (as previously defined directly in the Dockerfile entrypoint).
exec /usr/bin/dotnet "${APP_ROOT}/app/servicemanager/Saltworks.SaltMiner.ServiceManager.dll"
