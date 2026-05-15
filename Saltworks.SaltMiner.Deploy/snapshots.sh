#!/bin/bash
export SALTMINER_CONFIG_PATH=/etc/saltworks/saltminer-2.5.0
cd /usr/share/saltworks/saltminer-2.5.0
fl=/tmp/snapshots.lock
touch -a "$fl"

# Run 2.5 Agent (python)
source /usr/share/saltworks/.venv/bin/activate
flock -n "$fl" python3 -m Custom.RunTouchLog
flock -n "$fl" python3 RunGenerateSnapshotHistory.py
deactivate
