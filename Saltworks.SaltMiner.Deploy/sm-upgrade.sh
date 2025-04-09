#!/bin/bash
dist="./dist"
smapp="/usr/share/saltworks"
smapp2="$smapp"/saltminer-2.5.0
smapp3="$smapp"/saltminer-3.0.0
smcfg="/etc/saltworks"
smcfg2="$smcfg"/saltminer-2.5.0
smcfg3="$smcfg"/saltminer-3.0.0
smlog="/var/log/saltworks"
smlog2="$smlog"/saltminer-2.5.0
smlog3="$smlog"/saltminer-3.0.0
os=?

#######################################################################################################################
# Step 0 - resume setup, sudo, distro, intro
#######################################################################################################################
#   Sudo up!
echo "sudo up"
if [[ "$EUID" = 0 ]]; then
  echo "You're root? huh. Ok then, let's go!"
else
  sudo -k # make sure to ask for password on next sudo
  if sudo true; then
    echo "sudo engaged!"
  else
    echo "sudo failed. Try that password again or get someone with sudo permissions to assist. Exiting..."
    exit 1
  fi
fi

#   Determine OS platform
#   NOTE: there are no typos below, apparent misspellings in strings are due to tr deletions
osver=$(cat /etc/*-release | sed -n '/^VERSION_ID="/ {p;q}' | tr -d 'VERSION_ID="')
osname=$(cat /etc/*-release | sed -n '/^NAME="/ {p;q}' | tr -d 'NAME="')
if [ "$osname" == "Ubuntu" ] && [[ "$osver" == 20.04* ]]; then os="U"; fi
if [ "$osname" == "Ubuntu" ] && [[ "$osver" == 22.04* ]]; then os="U"; fi
if [ "$osname" == "Red Hat nterprise Linux" ] && [[ "$osver" == 8* ]]; then os="R8"; fi
if [ "$osname" == "Oracle Linux Server" ] && [[ "$osver" == 8* ]]; then os="R8"; fi

if [ "$os" == "?" ]; then
  echo "This linux distro ($osname - $osver) isn't supported for this install script currently.  Please see Saltworks Support for help."
  exit 1
fi

#######################################################################################################################
# Step 1 - SaltMiner Components
#######################################################################################################################
read -p "Upgrade SaltMiner v3 components (y/n)? [y] " ok
if [ -z "$ok" ]; then ok="y"; fi
if [ "$ok" == y ]; then
  if [ ! -d "$dist" ]; then ok="n"; fi
  if [ ! -d "$dist"/ui-web ]; then ok="n"; fi
  if [ ! -d "$dist"/ui-api ]; then ok="n"; fi
  if [ ! -d "$dist"/api ]; then ok="n"; fi
  if [ ! -d "$dist"/agent ]; then ok="n"; fi
  if [ ! -d "$dist"/manager ]; then ok="n"; fi
  if [ ! -d "$dist"/jobmanager ]; then ok="n"; fi
  if [ ! -d "$dist"/servicemanager ]; then ok="n"; fi
  if [ ! -d "$dist"/data-templates ]; then ok="n"; fi
  if [ ! -d "$dist"/sm25 ]; then ok="n"; fi
  if [ "$ok" != y ]; then
    echo "One or more dist directory is missing, or upgrade not selected."
    exit 1
  fi
  echo "Running SaltMiner v3 upgrade..."
  sudo chsh -s /bin/bash svc-saltminer
  echo "Stopping services"
  sudo systemctl stop kestrel-saltminer-api
  sudo systemctl stop kestrel-saltminer-ui-api
  sudo systemctl stop saltminer-service-manager
  sudo systemctl stop saltminer-job-manager
  
  echo "sm25"
  # TODO: copy expected custom locations to a backup, then trash it all, then put those back
  #sudo rm -rf "$smapp2"/sm25/*
  sudo cp -r "$dist"/sm25/* "$smapp2"
  sudo rm -rf "$smapp2"/Config
  echo "agent"
  sudo rm -rf "$smapp3"/agent/
  sudo cp -r "$dist"/agent "$smapp3"/
  sudo rm "$smapp3"/agent/AgentSettings.json 
  sudo rm -rf "$smapp3"/agent/SourceConfigs/ 
  echo "manager"
  sudo rm -rf "$smapp3"/manager/
  sudo cp -r "$dist"/manager "$smapp3"/
  sudo rm "$smapp3"/manager/ManagerSettings.json
  echo "jobmanager"
  sudo rm -rf "$smapp3"/jobmanager/
  sudo cp -r "$dist"/jobmanager "$smapp3"/
  sudo rm "$smapp3"/jobmanager/JobManagerSettings.json
  echo "api"
  sudo rm -rf "$smapp3"/api/
  sudo cp -r "$dist"/api "$smapp3"/
  echo "api addins"
  # add in selected sys index templates and seeds for API startup
  sudo mkdir "$smapp3"/api/data
  sudo mkdir "$smapp3"/api/data/index-templates
  sudo mkdir "$smapp3"/api/data/seeds
  sudo cp "$dist"/data-templates/index-templates/sys* "$smapp3"/api/data/index-templates/
  sudo cp "$dist"/data-templates/seeds/{Lookup*,Search*,Field*} "$smapp3"/api/data/seeds/
  sudo cp -r "$dist"/data-templates/ingest-pipelines/ "$smapp3"/api/data/
  sudo rm -rf "$smapp3"/api/data/ingest-pipelines/*custom*.json
  sudo rm "$smapp3"/api/appsettings.json
  echo "ui-api"
  sudo rm -rf "$smapp3"/ui-api/
  sudo cp -r "$dist"/ui-api "$smapp3"/
  sudo rm "$smapp3"/ui-api/appsettings.json
  echo "ui-web"
  sudo rm -rf "$smapp3"/ui-web/
  sudo cp -r "$dist"/ui-web "$smapp3"/
  echo "servicemanager"
  sudo rm -rf "$smapp3"/servicemanager/
  sudo cp -r "$dist"/servicemanager "$smapp3"/
  sudo rm "$smapp3"/servicemanager/ServiceManagerSettings.json
  echo "reset file ownership"
  sudo chown -R svc-saltminer:root "$smapp2"
  sudo chown -R svc-saltminer:root "$smapp3"
  echo ""
  echo ""

  echo -e "Updating API keys.\n"
  sudo cp "$smcfg3"/api/appsettings.json "$smcfg3"/api/appsettings.json.BAK
  sudo sed -i "s/:PENTESTAPI1//g" "$smcfg3"/api/appsettings.json

  read -p "Any errors (y/n)? [n] " ok
  printf -v date '%(%Y%m%d)T' -1
  if [ -z "$ok" ]; then ok="n"; fi
  if [ "$ok" == y ]; then
    echo "After troubleshooting, restart API, then check log for upgrade processing, then restart remaining SaltMiner services.  "
    echo "Helpful commands:"
    echo "tail -fn 50 $smlog3/smapi-$date.log"
    exit 1
  fi

  echo "Restarting API"
  sudo systemctl start kestrel-saltminer-api

  echo -e "Check the API log to see if any errors, and to observe the running upgrade.  \nThis would be best done in a second terminal window.  Command to use:"
  echo "tail -f $smlog3/smapi-$date.log"
  echo "Watch for \"Application started. Press Ctrl+C to shut down.\" in the log to indicate API upgrade is complete."
  
  cmd="pip install -r $smapp2/requirements.txt"
  read -p "No errors in API, and upgrade complete (y/n)? [y] " ok
  if [ -z "$ok" ]; then ok="y"; fi
  if [ "$ok" == y ]; then
    echo "After troubleshooting, restart the other SaltMiner services and run pip to install any new requirements."
    echo "Helpful commands:"
    echo "sudo systemctl start kestrel-saltminer-ui-api"
    echo "sudo systemctl start saltminer-service-manager"
    echo "sudo systemctl start saltminer-job-manager"
    echo "$cmd"
  fi
# TODO: add grep error checking, ask before proceeding

  echo "Restarting other SaltMiner services"
  sudo systemctl start kestrel-saltminer-ui-api
  sudo systemctl start saltminer-service-manager
  sudo systemctl start saltminer-job-manager
      
  if [ "$os" == "U" ]; then
    echo "Running pip install for SM25 dependencies"
    sudo su svc-saltminer -c "$cmd"
  else
    echo "Attempting pip install for SM25 dependencies, only supports python 3.9 right now on this OS because of the versioned calls (pip3.9, python3.9)"
    cmd="pip3.9 install -r $smapp2/requirements.txt"
    sudo su svc-saltminer -c "$cmd"
  fi
  
fi

echo ""
echo "Upgrade complete."
echo "Suggested next steps: "
echo "1. Check release notes for any needed configuration updates"
echo "2. Check log files for each service for errors"
echo "3. Open the SaltMiner UI in the browser and verify functional ('hard' refresh may be required to update the UI files)"
echo ""
