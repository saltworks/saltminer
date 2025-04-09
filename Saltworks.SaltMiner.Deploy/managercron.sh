#!/bin/bash
cd /usr/share/saltworks/saltminer-3.0.0/manager
export SALTMINER_MANAGER_CONFIG_PATH=/etc/saltworks/saltminer-3.0.0/manager
touch -a /tmp/sm-manager.lock
echo ""
echo "** Remember if the Manager doesn't fire it could be running already in a cron job **"
echo ""

# Run Manager
if [ -z "$1" ]; then
  echo "You can call this script with parameters to pass to the Manager and fire it off all in one step."
  echo "Use the -h parameter for help.  If no parameters are passed, then a default Manager run will start (queue processor)."
  echo ""
  echo "No parameters detected, starting default Manager, processing a max of 100 queued scans..."
  flock -n /tmp/sm-manager.lock dotnet Saltworks.SaltMiner.Manager.dll queue -n 100
else
  echo ""
  echo "Parameter(s) detected, starting Manager with passed parameters..."
  echo ""
  flock -n /tmp/sm-manager.lock dotnet Saltworks.SaltMiner.Manager.dll "$@"
fi
