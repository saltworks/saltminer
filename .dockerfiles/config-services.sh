#!/bin/bash
handle_error() {
  echo "Error on line $1"
  echo "This does not affect SaltMiner container start."
  # Errors shouldn't prevent other containers from starting.
  exit 0
}

trap 'handle_error $LINENO' ERR

CONFIG_FILE=/etc/saltworks/saltminer-2.5.0/Elastic.json

# Extract the scheme.
scheme="$(echo $ELASTIC_URL | grep :// | sed -e's,^\(.*://\).*,\1,g')"
# Remove scheme from input URL.
url="$(echo ${ELASTIC_URL/$scheme/})"
# Parse user if it exists.
user="$(echo $url | grep @ | cut -d@ -f1)"
# Extract the host and port.
hostport="$(echo ${url/$user@/} | cut -d/ -f1)"

host="$(echo $hostport | sed -e 's,:.*,,g')"

port=`echo $hostport | grep : | cut -d: -f2`
# Parse path if present.
path="$(echo $url | grep / | cut -d/ -f2-)"

echo "url: $url"
echo "  scheme: $scheme"
echo "  user: $user"
echo "  host: $host"
echo "  port: $port"
echo "  path: $path"

if [ "${port,,}" == "" ]; then
  echo 'No port explicitly specified.'
  if [ "${scheme,,}" == 'https://' ]; then
    echo "Scheme is $scheme so setting port to 443."
    port=443
  elif [ "${scheme,,}" == 'http://' ]; then
    echo "Scheme is $scheme so setting port to 80."
    port=443
  else
    echo "Something is very wrong here. Scheme is $scheme but only http or https is supported."
    handle_error $LINENO
  fi
fi

echo 'Existing scheme: '
jq '.Scheme' $CONFIG_FILE
echo 'Existing host: '
jq '.Host' $CONFIG_FILE
echo 'Existing port: '
jq '.Port' $CONFIG_FILE

jq --arg s "$scheme" \
  --arg h "$host" \
  --arg p "$port" \
  '.ApiConfig.ElasticHttpScheme = $s | .ApiConfig.ElasticHost = $h | .ApiConfig.ElasticPort = $p' \
  $CONFIG_FILE \
  > "$CONFIG_FILE.new"

mv $CONFIG_FILE "$CONFIG_FILE.old"
mv "$CONFIG_FILE.new" $CONFIG_FILE
