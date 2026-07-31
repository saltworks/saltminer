#!/bin/bash
cd /opt/saltworks/saltminer/app/python
fl=/tmp/sm-agentservice.lock
touch -a "$fl"
flock -n "$fl" python3 RunAgentService.py "$@"
