#!/bin/sh
set -e

CONF=/etc/nginx/nginx.conf

# Check if the config still contains variable placeholders
if grep -q '\$KIBANA_URL\|\$KIBANA_HOST' "$CONF"; then
  echo "[entrypoint] Substituting environment variables in nginx.conf..."

  # Validate that required vars are set
  : "${KIBANA_URL:?KIBANA_URL environment variable is required}"
  : "${KIBANA_HOST:?KIBANA_HOST environment variable is required}"

  envsubst '$KIBANA_URL $KIBANA_HOST' < "$CONF" > "$CONF.tmp"
  mv "$CONF.tmp" "$CONF"
  echo "[entrypoint] Substitution complete."
else
  echo "[entrypoint] nginx.conf has no placeholders, skipping substitution."
fi

# Hand off to the official nginx entrypoint
exec nginx -g 'daemon off;'