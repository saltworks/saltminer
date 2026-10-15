#!/bin/bash
cd /opt/saltworks/saltminer/app/python

LOCKFILE=/tmp/python-agent
if [ -n "$1" ]; then
  LOCKFILE="$LOCKFILE-$1.lock"
fi

touch -a "$LOCKFILE"
flock -n "$LOCKFILE" python3 -m Sources.RunPythonAdapter "$@"