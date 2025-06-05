#!/bin/bash
cd /usr/share/saltworks/saltminer-3.0.0/agent
export SALTMINER_AGENT_CONFIG_PATH=/etc/saltworks/saltminer-3.0.0/agent
touch -a /tmp/sm-agent-wiz.lock
echo ""
echo "** Remember if the Agent doesn't fire it could be running already in a cron job **"
echo ""

# Run Agent
# direct command example: dotnet Saltworks.SaltMiner.SyncAgent.dll sync -s BlackDuck
if [ -z "$1" ]; then
  echo ""
  echo "You can call this script with parameters to pass to the Agent and fire it off all in one step."
  echo "Use the -h parameter for help.  If no parameters are passed, then agent will run and process all sources."
  echo ""
  echo "No parameters detected, starting default run of agent for all sources..."
  flock -n /tmp/sm-agent-wiz.lock dotnet Saltworks.SaltMiner.SyncAgent.dll
else
  echo ""
  echo "Parameter(s) detected, starting agent..."
  flock -n /tmp/sm-agent-wiz.lock dotnet Saltworks.SaltMiner.SyncAgent.dll "$@"
  echo ""
fi
