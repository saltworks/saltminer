#!/bin/bash
LOCKFILE=/tmp/riskroller.lock
touch -a "$LOCKFILE"
cd /opt/saltworks/saltminer/app/python
flock -n "$LOCKFILE" python3 RunRiskRollup.py