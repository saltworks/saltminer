#!/bin/bash
export SALTMINER_CONFIG_PATH=/etc/saltworks/saltminer-2.5.0
cd /usr/share/saltworks/saltminer-2.5.0
fl=/tmp/snapshots.lock
touch -a "$fl"

# Run 2.5 Agent (python)
flock -n "$fl" python3 -m Custom.RunTouchLog
flock -n "$fl" python3 RunGenerateSnapshotHistory.py
