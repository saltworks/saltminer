#!/bin/bash
cd /opt/saltworks/saltminer/app/agent

# Default lockfile, use a per-source lockfile for sync runs so different
# sources (the value following -s) don't block each other.
LOCKFILE=/tmp/sm-agent.lock
if [ "$1" == "sync" ]; then
  prev=""
  for arg in "$@"; do
    if [ "$prev" == "-s" ]; then
      LOCKFILE="/tmp/sm-agent-${arg}.lock"
      break
    fi
    prev="$arg"
  done
fi

touch -a "$LOCKFILE"
echo ""
echo "** Remember if the Agent doesn't fire it could be running already in a cron job **"
echo ""

# Run Agent
# direct command example: dotnet Saltworks.SaltMiner.SyncAgent.dll sync -s BlackDuck
if [ -z "$1" ]; then
  echo ""
  echo "You must call this script with parameters to pass to the Agent."
  echo "Use the -h parameter for help."
  echo ""
  echo "No parameters detected, canceling run..."
  exit 1
else
  echo ""
  echo "Parameter(s) detected, starting agent..."
  flock -n "$LOCKFILE" dotnet Saltworks.SaltMiner.SyncAgent.dll "$@"
  echo ""
fi


