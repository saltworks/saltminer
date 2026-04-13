#!/bin/bash
export SALTMINER_2_CONFIG_PATH=/etc/saltworks/saltminer-2.5.0
cd /usr/share/saltworks/saltminer-2.5.0
source /usr/share/saltworks/.venv/bin/activate
fl=/tmp/sm-agentservice.lock
touch -a "$fl"
flock -n "$fl" python3 -m RunAgentService "$@"
deactivate
