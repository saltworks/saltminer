#!/bin/bash
cd /opt/saltworks/saltminer/python
fl=/tmp/sm-agentservice.lock
touch -a "$fl"
flock -n "$fl" python3 -m RunAgentService "$@"
