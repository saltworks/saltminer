#!/bin/sh
# =============================================================================
# SaltMiner nginx entrypoint script
#
# Execution order:
#   1. Seed /etc/nginx/ files from /etc/nginx/defaults/ if not present on host
#   2. Perform envsubst on nginx.conf for any remaining placeholders
#   3. Hand off to nginx
#
# The seed step must run first. If /etc/nginx is bind-mounted to an empty
# host directory, the config files baked into the image are hidden by the
# mount. Seeding copies them from the defaults bundle at /etc/nginx/defaults/
# before any other step attempts to read them.
#
# Each file is seeded independently so a partially-populated bind mount
# (e.g. customer has provided their own nginx.conf but not the certs) is
# handled correctly — only the missing files are restored.
# =============================================================================

set -e

# -----------------------------------------------------------------------------
# Logging helper
# Prefix all messages with [entrypoint] for easy log filtering.
# -----------------------------------------------------------------------------
log() {
    echo "[entrypoint] $*"
}

# -----------------------------------------------------------------------------
# seed_file <target> <source>
#
# Copies <source> to <target> if <target> does not already exist.
# Preserves any file the customer has placed at <target>.
# Exits with an error if the defaults source file is missing from the image,
# which would indicate a broken image build.
# -----------------------------------------------------------------------------
seed_file() {
    target="$1"
    source="$2"

    if [ ! -f "$target" ]; then
        if [ ! -f "$source" ]; then
            log "ERROR: Default file missing from image: $source"
            log "       This indicates a broken image build. Cannot continue."
            exit 1
        fi
        log "Seeding $target from image defaults..."
        cp "$source" "$target"
        log "Seeded:  $target"
    else
        log "Present: $target (skipping seed)"
    fi
}

# =============================================================================
# Step 1 — Seed /etc/nginx/ files from bundled defaults
#
# Source path: /etc/nginx/defaults/
# This directory is baked into the image at build time and is never bind
# mounted, so its contents are always available regardless of what the
# customer has mounted over /etc/nginx/.
# =============================================================================

DEFAULTS_DIR="/etc/nginx-defaults"
NGINX_DIR="/etc/nginx"

log "Checking $NGINX_DIR for required files..."

# -----------------------------------------------------------------------------
# DEBUG — filesystem dump
# Prints the contents of both /etc/nginx/ and /etc/nginx/defaults/ at runtime
# so we can see exactly what the container has access to when it starts.
# Remove this block once the seeding issue is confirmed resolved.
# -----------------------------------------------------------------------------
log "DEBUG: Contents of $NGINX_DIR:"
ls -la "$NGINX_DIR" 2>&1 | while IFS= read -r line; do log "DEBUG:   $line"; done

log "DEBUG: Contents of $DEFAULTS_DIR:"
if [ -d "$DEFAULTS_DIR" ]; then
    ls -la "$DEFAULTS_DIR" 2>&1 | while IFS= read -r line; do log "DEBUG:   $line"; done
else
    log "DEBUG:   DIRECTORY DOES NOT EXIST"
fi
# -----------------------------------------------------------------------------
# END DEBUG
# -----------------------------------------------------------------------------

# -- nginx.conf ---------------------------------------------------------------
# Main nginx configuration. Contains KIBANA_URL and KIBANA_HOST placeholders
# that are resolved in Step 2 below.
seed_file \
    "$NGINX_DIR/nginx.conf" \
    "$DEFAULTS_DIR/nginx.conf"

# -- saltminer.crt ------------------------------------------------------------
# TLS certificate delivered as part of the SaltMiner product.
# Customers may replace this with their own certificate.
seed_file \
    "$NGINX_DIR/saltminer.crt" \
    "$DEFAULTS_DIR/saltminer.crt"

# -- saltminer.key ------------------------------------------------------------
# TLS private key paired with saltminer.crt.
# Permissions are tightened after seeding to restrict read access.
seed_file \
    "$NGINX_DIR/saltminer.key" \
    "$DEFAULTS_DIR/saltminer.key"

# Ensure the private key is always owner-read-only, whether it was just
# seeded or was already present (customer-provided key should also be locked).
chmod 600 "$NGINX_DIR/saltminer.key"

# =============================================================================
# Step 2 — Substitute environment variables in nginx.conf
#
# Only runs if the config still contains unresolved placeholders. This allows
# the container to restart cleanly after a first run where substitution has
# already been applied to a host-mounted file.
# =============================================================================

CONF="$NGINX_DIR/nginx.conf"

if grep -q '\$KIBANA_URL\|\$KIBANA_HOST' "$CONF"; then
    log "Substituting environment variables in nginx.conf..."

    # Validate that required vars are set before attempting substitution.
    # The := syntax prints a clear error and exits if either var is missing.
    : "${KIBANA_URL:?KIBANA_URL environment variable is required}"
    : "${KIBANA_HOST:?KIBANA_HOST environment variable is required}"

    envsubst '$KIBANA_URL $KIBANA_HOST' < "$CONF" > "$CONF.tmp"
    mv "$CONF.tmp" "$CONF"
    log "Substitution complete."
else
    log "nginx.conf has no placeholders, skipping substitution."
fi

# =============================================================================
# Step 3 — Hand off to nginx
#
# exec replaces this shell process with nginx, making nginx PID 1.
# daemon off keeps nginx in the foreground so Docker can track the process.
# =============================================================================

log "Starting nginx..."
exec nginx -g 'daemon off;'
