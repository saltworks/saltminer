#!/bin/ash
handle_error() {
  echo "Error on line $1"
  echo "This does not affect SaltMiner container start."
  # Errors shouldn't prevent other containers from starting.
  exit 0
}

trap 'handle_error $LINENO' ERR

CONFIG_FILE=/appsettings.json

echo 'Transforming configuration file'
echo 'UiApiConfig.KibanaBaseUrl Before'
jq '.UiApiConfig.KibanaBaseUrl' $CONFIG_FILE

jq --arg kbu "$KIBANA_URL" \
  '.UiApiConfig.KibanaBaseUrl = $kbu' \
  $CONFIG_FILE \
  > "/scripts/$CONFIG_FILE.new"

echo 'UiApiConfig.KibanaBaseUrl After'
jq '.UiApiConfig.KibanaBaseUrl' "/scripts/$CONFIG_FILE.new"

cp "/scripts/$CONFIG_FILE.new" /etc/saltworks/saltminer-3.0.0/ui-api/appsettings.json
