#!/bin/sh
# =============================================================================
# SaltMiner nginx entrypoint script
#
# Execution order:
#   1. Seed /opt/saltworks/saltminer/nginx/ from image defaults if files
#      are missing — works whether or not a bind mount is present
#   2. Copy all three files from customer dir into /etc/nginx/ live path
#   3. Perform envsubst on nginx.conf for any remaining placeholders
#   4. Hand off to nginx
#
# /etc/nginx/ is never bind mounted so nginx always has its full internal
# directory structure intact. Customers edit files in the Saltworks-owned
# path /opt/saltworks/saltminer/nginx/ which is safe to bind mount without
# affecting nginx internals.
#
# Customer working directory: /opt/saltworks/saltminer/
# Nginx config files:         /opt/saltworks/saltminer/nginx/
# Image defaults bundle:      /etc/nginx-defaults/
# Nginx live path:            /etc/nginx/
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
# Copies <source> to <target> if <target> is missing or empty.
# Used to populate the customer directory on first run, or to restore
# a file the customer has accidentally deleted.
# -----------------------------------------------------------------------------
seed_file() {
    target="$1"
    source="$2"

    if [ ! -s "$target" ]; then
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

# -----------------------------------------------------------------------------
# deploy_file <source> <target>
#
# Copies a file from the customer directory into the live nginx path.
# Always overwrites on every startup so nginx always reflects the current
# state of the customer directory.
# -----------------------------------------------------------------------------
deploy_file() {
    source="$1"
    target="$2"

    log "Deploying $source → $target"
    cp "$source" "$target"
}

DEFAULTS_DIR="/etc/nginx-defaults"
CUSTOMER_DIR="/opt/saltworks/saltminer/nginx"
NGINX_DIR="/etc/nginx"

# =============================================================================
# Step 1 — Seed customer directory from image defaults if files are missing
#
# On first run with an empty bind mount, all three files will be seeded.
# On subsequent runs with customer-edited files, seeding is skipped.
# Individual files can be restored by deleting them from the customer dir
# and restarting the container.
# =============================================================================

log "Checking $CUSTOMER_DIR for required files..."

# -- nginx.conf ---------------------------------------------------------------
# Main nginx configuration. Contains KIBANA_URL and KIBANA_HOST placeholders
# that are resolved in Step 3 below.
seed_file \
    "$CUSTOMER_DIR/nginx.conf" \
    "$DEFAULTS_DIR/nginx.conf"

# -- saltminer.crt ------------------------------------------------------------
# TLS certificate delivered as part of the SaltMiner product.
# Customers may replace this with their own certificate and key.
seed_file \
    "$CUSTOMER_DIR/saltminer.crt" \
    "$DEFAULTS_DIR/saltminer.crt"

# -- saltminer.key ------------------------------------------------------------
# TLS private key paired with saltminer.crt.
seed_file \
    "$CUSTOMER_DIR/saltminer.key" \
    "$DEFAULTS_DIR/saltminer.key"

# Lock down the private key in the customer directory.
chmod 600 "$CUSTOMER_DIR/saltminer.key"

# =============================================================================
# Step 2 — Deploy files from customer directory into live nginx path
#
# Always runs on every startup. nginx always gets a fresh copy of whatever
# is currently in the customer directory, so a restart is all that is needed
# to pick up any customer edits.
# =============================================================================

log "Deploying files to $NGINX_DIR..."

deploy_file "$CUSTOMER_DIR/nginx.conf"    "$NGINX_DIR/nginx.conf"
deploy_file "$CUSTOMER_DIR/saltminer.crt" "$NGINX_DIR/saltminer.crt"
deploy_file "$CUSTOMER_DIR/saltminer.key" "$NGINX_DIR/saltminer.key"

# Lock down the deployed private key in the live nginx path.
chmod 600 "$NGINX_DIR/saltminer.key"

# =============================================================================
# Step 3 — Substitute environment variables in nginx.conf
#
# Only runs if the live nginx.conf still contains unresolved placeholders.
# Operates on the deployed copy in /etc/nginx/ so the customer's source
# file in /opt/saltworks/saltminer/nginx/ is never modified by substitution.
# On container restart the customer file is re-deployed and substitution
# runs again if placeholders are still present.
# =============================================================================

CONF="$NGINX_DIR/nginx.conf"

if grep -q '\$KIBANA_URL\|\$KIBANA_HOST' "$CONF"; then
    log "Substituting environment variables in nginx.conf..."

    # Validate that required vars are set before attempting substitution.
    # The :? syntax prints a clear error and exits if either var is missing.
    : "${KIBANA_URL:?KIBANA_URL environment variable is required}"
    : "${KIBANA_HOST:?KIBANA_HOST environment variable is required}"

    envsubst '$KIBANA_URL $KIBANA_HOST' < "$CONF" > "$CONF.tmp"
    mv "$CONF.tmp" "$CONF"
    log "Substitution complete."
else
    log "nginx.conf has no placeholders, skipping substitution."
fi

# =============================================================================
# Step 4 — Hand off to nginx
#
# exec replaces this shell process with nginx, making nginx PID 1.
# daemon off keeps nginx in the foreground so Docker can track the process.
# =============================================================================

log "Starting nginx..."
exec nginx -g 'daemon off;'
