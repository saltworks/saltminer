#!/bin/bash

# INCOMPLETE DRAFT...

# for find and replace in files, use this (reference: https://stackoverflow.com/questions/525592/find-and-replace-inside-a-text-file-from-a-bash-command)
# sed -i -e 's/abc/XYZ/g' /tmp/file.txt

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
pstep=0
os=?

#   Determine OS platform
#   NOTE: there are no typos below, apparent misspellings in strings are due to tr deletions
osver=$(cat /etc/*-release | sed -n '/^VERSION_ID="/ {p;q}' | tr -d 'VERSION_ID="')
osname=$(cat /etc/*-release | sed -n '/^NAME="/ {p;q}' | tr -d 'NAME="')
if [ "$osname" == "Ubuntu" ] && [[ "$osver" == 20.04* ]]; then os="U"; fi
if [ "$osname" == "Ubuntu" ] && [[ "$osver" == 22.04* ]]; then os="U"; fi

if [ "$os" == "?" ]; then
  echo "This linux distro ($osname - $osver) isn't supported for this install script currently.  Please see Saltworks Support for help."
  exit 1
fi

echo "SaltMiner install script for Linux Ubuntu/RHEL 8/OL8"
echo "WARNING: this script should only be run ONCE to completion on a system.  There are no checks for existing installation currently."
echo "NOTE: if you have a license file (license.txt) and place it into the same folder as this script it will be automagically moved and consumed."
 

#######################################################################################################################
# Step 1 - Package installation
#######################################################################################################################
#   Ubuntu
echo "Make sure up to date on stuff before starting"
sudo apt update 
sudo apt upgrade -y
fwinstalled=$(sudo dpkg -l | grep ufw)
if [ -z "$fw" ]; then 
echo "ufw apparently not installed"
else
sudo ufw disable
fi

if [[ "$osver" == 20.04* ]]; then
echo ""
echo "Setup MS repo"
wget https://packages.microsoft.com/config/ubuntu/20.04/packages-microsoft-prod.deb -O packages-microsoft-prod.deb 
sudo dpkg -i packages-microsoft-prod.deb
rm packages-microsoft-prod.deb
fi

echo ""
echo "Installing .NET runtime and unzip"
sudo apt update
sudo apt install -y dotnet-sdk-6.0
sudo apt install -y unzip

echo ""
echo "Installing nginx"
sudo apt install -y nginx
sudo echo "user www-data;" | sudo tee /etc/nginx/nginx.conf >/dev/null
sudo cat nginx.conf | sudo tee -a /etc/nginx/nginx.conf >/dev/null
sudo nginx -s reload

echo ""
echo "Installing pip for python"
sudo apt install -y python3-pip
    
echo ""
echo "Package installation complete."
echo ""
  
#######################################################################################################################
# Step 2 - Install Elasticsearch
#######################################################################################################################
pwd="nope"
kpwd="nope"
esinstalled=0
if [ "$pstep" == 2 ]; then
  read -p "Install Elasticsearch (y/n)? [y] " ok
  if [ -z "$ok" ]; then ok="y"; fi
  if [ "$ok" == y ]; then
    echo ""
    echo "Installing Elasticsearch..."
    echo ""

    # RHEL / OL8 packaging
    if [ "$os" == "R8" ]; then
      # This package is required whether installing a single Elasticsearch node or a cluster.
      echo ""
      echo "Installing yum version lock package if not already installed."
      sudo yum install -y python3-dnf-plugin-versionlock

      # If internet repos not available, follow instructions here to download and install manually:
      # https://www.elastic.co/guide/en/elasticsearch/reference/current/rpm.html#install-rpm

      # Currently installing elasticsearch 8
      # https://www.elastic.co/guide/en/elasticsearch/reference/current/rpm.html#rpm-repo
      sudo rpm --import https://artifacts.elastic.co/GPG-KEY-elasticsearch
      erepo="/etc/yum.repos.d/elasticsearch.repo"
      sudo touch $erepo
      # erepo created with 644 permissions
      sudo chmod 777 $erepo
      sudo echo "[elasticsearch]" > "$erepo"
      sudo echo "name=Elasticsearch repository for 8.x packages" >> "$erepo"
      sudo echo "baseurl=https://artifacts.elastic.co/packages/8.x/yum" >> "$erepo"
      sudo echo "gpgcheck=1" >> "$erepo"
      sudo echo "gpgkey=https://artifacts.elastic.co/GPG-KEY-elasticsearch" >> "$erepo"
      sudo echo "enabled=0" >> "$erepo"
      sudo echo "autorefresh=1" >> "$erepo"
      sudo echo "type=rpm-md" >> "$erepo"
      sudo yum install -y --enablerepo=elasticsearch elasticsearch-8.10.4
      sudo chmod 644 $erepo
      sudo yum versionlock elasticsearch-*
    fi
    # Ubuntu packaging
    if [ "$os" == "U" ]; then
      sudo rm -f /etc/apt/trusted.gpg.d/elasticsearch-keyring.gpg  # remove if exists, ignore if not
      wget -qO - https://artifacts.elastic.co/GPG-KEY-elasticsearch | sudo gpg --dearmor -o /etc/apt/trusted.gpg.d/elasticsearch-keyring.gpg
      echo "deb https://artifacts.elastic.co/packages/8.x/apt stable main" | sudo tee /etc/apt/sources.list.d/elastic-8.x.list
      esinstalled=1
      sudo apt update
      sudo apt -y install elasticsearch=8.10.4
      sudo apt-mark hold elasticsearch
    fi

    # https://www.elastic.co/guide/en/elasticsearch/reference/current/setup-configuration-memory.html#disable-swap-files
    sudo swapoff -a

    sudo systemctl daemon-reload
    sudo systemctl enable elasticsearch.service
    echo "Elasticsearch first start..."
    sudo systemctl start elasticsearch
    pwd=$(sudo /usr/share/elasticsearch/bin/elasticsearch-reset-password -u elastic -bs)
    kpwd=$(sudo /usr/share/elasticsearch/bin/elasticsearch-reset-password -u kibana_system -bs)
    rmupwd=$(sudo /usr/share/elasticsearch/bin/elasticsearch-reset-password -u remote_monitoring_user -bs)

    if test -f "elasticsearch.yml"; then
      sudo cp elasticsearch.yml /etc/elasticsearch/elasticsearch.yml
    fi
    echo ""
    echo "Elasticsearch installed successfully."
    read -p "Next, edit elasticsearch.yml (for local no edits needed).  Press the Enter key to continue"
    if [ "$osname" == "Red Hat nterprise Linux" ]; then
      sudo nano /etc/elasticsearch/elasticsearch.yml
    else
      sudo nano /etc/elasticsearch/elasticsearch.yml
    fi
    echo "Removing default Elasticsearch http ssl secure password"
    sudo /usr/share/elasticsearch/bin/elasticsearch-keystore remove xpack.security.http.ssl.keystore.secure_password

    echo ""
    echo "Restarting Elasticsearch with updated config..."
    sudo systemctl restart elasticsearch
    echo "Security information:"
    echo ""
    echo "elastic user pwd: $pwd"
    echo "kibana_system user pwd: $kpwd"
    echo "remote_monitoring_user pwd (Used with Fleet): $rmupwd" 
    echo ""

    sudo usermod -a -G elasticsearch $USER

    read -p "Elasticsearch installed ok? (y/n)? [y] " ok
    if [ -z "$ok" ]; then ok="y"; fi
    if [ "$ok" != y ]; then
      echo "Re-run this script to resume here after fixing the issues.  Elasticsearch should be running before resuming - answer N to install Elasticsearch.  Record elastic and kibana_system passwords."
      echo ""
      echo "Here's some commands you might need:"
      echo "sudo nano /etc/elasticsearch/elasticsearch.yml"
      echo "sudo /usr/share/elasticsearch/bin/elasticsearch-reset-password -u elastic"
      echo "sudo journalctl -ex -u elasticsearch.service"
      exit 1
    fi
  fi

  pstep=3
  echo "$pstep" > "$plog"
fi

#######################################################################################################################
# Step 3 - Install Kibana
#######################################################################################################################
if [ "$pstep" == 3 ]; then
  read -p "Install Kibana (y/n)? [y] " ok
  if [ -z "$ok" ]; then ok="y"; fi
  if [ "$ok" == y ]; then
    echo ""
    echo "NOTE: If Elasticsearch is being installed on a different server (or cluster), do that before finishing this installation process."
    echo "Installing Kibana..."
    echo ""

    # RHEL / OL8 packaging
    if [ "$os" == "R8" ]; then
      # If internet repos not available, follow instructions here to download and install manually:
      # https://www.elastic.co/guide/en/elasticsearch/reference/current/rpm.html#install-rpm

      # Currently installing kibana 8
      # https://www.elastic.co/guide/en/elasticsearch/reference/current/rpm.html#rpm-repo
      sudo rpm --import https://artifacts.elastic.co/GPG-KEY-elasticsearch
      erepo="/etc/yum.repos.d/kibana.repo"
      sudo touch $erepo
      sudo chmod 777 $erepo
      sudo echo "[kibana]" > "$erepo"
      sudo echo "name=Kibana repository for 8.x packages" >> "$erepo"
      sudo echo "baseurl=https://artifacts.elastic.co/packages/8.x/yum" >> "$erepo"
      sudo echo "gpgcheck=1" >> "$erepo"
      sudo echo "gpgkey=https://artifacts.elastic.co/GPG-KEY-elasticsearch" >> "$erepo"
      sudo echo "enabled=0" >> "$erepo"
      sudo echo "autorefresh=1" >> "$erepo"
      sudo echo "type=rpm-md" >> "$erepo"
      sudo yum install -y --enablerepo=kibana kibana-8.10.4
      sudo chmod 644 $erepo
      sudo yum versionlock kibana-*
    fi
    # Ubuntu packaging
    if [ "$os" == "U" ]; then
      sudo rm -f /etc/apt/trusted.gpg.d/elasticsearch-keyring.gpg  # remove if exists, ignore if not
      wget -qO - https://artifacts.elastic.co/GPG-KEY-elasticsearch | sudo gpg --dearmor -o /etc/apt/trusted.gpg.d/elasticsearch-keyring.gpg
      if [ "$esinstalled" == 0 ]; then
        echo "deb https://artifacts.elastic.co/packages/8.x/apt stable main" | sudo tee -a /etc/apt/sources.list.d/elastic-8.x.list
      fi
      sudo apt update
      sudo apt -y install kibana=8.10.4
      sudo apt-mark hold kibana
      sudo systemctl daemon-reload
    fi

    if test -f "kibana.yml"; then
      sudo cp kibana.yml /etc/kibana/kibana.yml
    fi
    #kcode=$(sudo /usr/share/kibana/bin/kibana-verification-code)

    echo ""
    echo "Kibana installed successfully."
    if [ "$pwd" != "nope" ]; then
      echo "For the next step, kibana_system password is: $kpwd"
    fi
    read -p "Next, edit kibana.yml.  At a minimum enter the kibana_system password.  Press the Enter key to continue."
    if [ "$osname" == "Red Hat nterprise Linux" ]; then
      sudo nano /etc/kibana/kibana.yml
    else
      sudo nano /etc/kibana/kibana.yml
    fi

    kbthemever="8.10.4"
    kbtheme="kibana-theme-${kbthemever}.zip"
    if test -f "$kbtheme"; then
      # version checking is experimental, hard-coded version requirement too.  Ugly...
      elkver=$(sudo dpkg-query --list kibana | tail -n1 | awk '{split($0, a, " "); print a[3]}')
      if [ "$elkver" == "$kbthemever" ]; then
        echo "Found kibana plugin, installing..."
        wd=$(pwd)
        sudo /usr/share/kibana/bin/kibana-plugin install "file://${wd}/${kbtheme}"
        sudo systemctl restart kibana
      else
        echo "Found kibana plugin '$kbtheme', but detected Kibana version ($elkver) does not match.  Will not install."
      fi
    fi

    # Intentionally short-circuit this block with a leading 'x' until properly tested.
    insighttheme="xInsightKibanaTheme-${kbthemever}.zip"
    if test -f "$insighttheme"; then
      # version checking is experimental, hard-coded version requirement too.  Ugly...
      elkver=$(sudo dpkg-query --list kibana | tail -n1 | awk '{split($0, a, " "); print a[3]}')
      if [ "$elkver" == "$kbthemever" ]; then
        echo "Found Insight kibana plugin, installing..."
        wd=$(pwd)
        sudo /usr/share/kibana/bin/kibana-plugin install "file://${wd}/${insighttheme}"
        sudo systemctl restart kibana
      else
        echo "Found kibana plugin '$insighttheme', but detected Kibana version ($elkver) does not match.  Will not install."
      fi
    fi

    echo ""
    echo "Starting Kibana..."
    sudo systemctl enable kibana
    sudo systemctl start kibana

    sudo usermod -a -G kibana $USER

    if [ "$pwd" != "nope" ]; then
      echo "For testing kibana, use elastic password: $pwd"
    fi
    read -p "Kibana installed ok, including testing from browser (firewall not yet enabled)? (y/n)? [y] " ok
    if [ -z "$ok" ]; then ok="y"; fi
    if [ "$ok" != y ]; then
      echo "Re-run this script to resume here after fixing the issue(s).  Kibana should be up and running before continuing - answer N when asked to install it."
      echo ""
      echo "Here's some commands you might need:"
      echo "sudo nano /etc/kibana/kibana.yml"
      echo "sudo tail -n100 /var/log/kibana/kibana.log"
      exit 1
    fi
  fi

  pstep=4
  echo "$pstep" > "$plog"

fi

#######################################################################################################################
# Step 4 - Firewall configuration
#######################################################################################################################
if [ "$pstep" == 4 ]; then
  if [ "$os" == "U" ]; then
    echo "Firewall configuration..."
    read -p "Configure firewall (y/n)? [n] " fw
    if [ -z "$fw" ]; then fw="n"; fi
    if [ "$fw" = y ]; then
      echo "Enabling ssh in firewall rules, and turning it on.  Answer y to the next question from the firewall."
      sudo ufw allow ssh
      sudo ufw enable
      read -p "Open what port (1 of 2)? [80] " port1
      read -p "Open what port (2 of 2)? [None] " port2
      if [ -z "$port1" ]; then
        port1=80
      fi
      sudo ufw allow $port1
      if [ ! -z "$port2" ]; then
        sudo ufw allow $port2
      fi
      read -p "Filter to what IP (i.e. a shared nginx ip: 10.9.100.8)? [None] " ip
      if [ ! -z "$ip" ]; then
        sudo ufw allow proto tcp from $ip to any port $port1
        sudo ufw allow proto tcp from 127.0.0.1 to any port $port1
        if [ ! -z "$port2" ]; then
          sudo ufw allow proto tcp from $ip to any port $port2
          sudo ufw allow proto tcp from 127.0.0.1 to any port $port2
        fi
      fi
    fi
  fi
#  else
#    echo "Firewall configuration for this distro not supported. The following is experimental."
#	  echo "Opening ports 80 and 9200."
#    sudo firewall-cmd --add-service http --permanent
    #sudo firewall-cmd --reload
    #sudo firewall-cmd --list-services
	# 9200 is needed for any initial data loads, Fleet, or multi-node clustering. Feel free to not use this if unnecessary.
#    sudo firewall-cmd --add-service elasticsearch --permanent
#    sudo firewall-cmd --reload
#    sudo firewall-cmd --list-ports
#	  sudo firewall-cmd --list-services

  read -p "Firewall config good (or not supported) - n to handle manually and then resume here (y/n)? [y] " ok
  if [ -z "$ok" ]; then ok="y"; fi
  if [ "$ok" != y ]; then
    echo "Re-run this script to resume here after firewall config fun."
    exit 1
  fi
  pstep=5
  echo "$pstep" > "$plog"
fi

#######################################################################################################################
# Step 5 - SaltMiner Components
#######################################################################################################################
if [ "$pstep" == 5 ]; then
  read -p "Install SaltMiner v3 components using default locations (y/n)? [y] " ok
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
    if [ "$ok" == y ]; then
      sudo useradd -r -m svc-saltminer
      echo "Running SaltMiner v3 installation..."
      sudo chsh -s /bin/bash svc-saltminer
      sudo mkdir "$smcfg"
      sudo mkdir "$smcfg2"
      sudo mkdir "$smcfg3"
      sudo mkdir "$smcfg3"/agent
      sudo mkdir "$smcfg3"/manager
      sudo mkdir "$smcfg3"/api
      sudo mkdir "$smcfg3"/ui-api
      sudo mkdir "$smcfg3"/jobmanager
      sudo mkdir "$smcfg3"/servicemanager
      sudo mkdir "$smlog"
      sudo mkdir "$smlog2"
      sudo mkdir "$smlog3"
      sudo mkdir "$smapp"
      sudo mkdir "$smapp2"
      echo "1"
      sudo cp -r "$dist"/sm25/* "$smapp2"
      echo "2"
      sudo cp -r "$smapp2"/Config/* "$smcfg2"/
      sudo rm -rf "$smapp2"/Config
      echo "3"
      sudo mkdir "$smapp3"
      echo "4"
      sudo cp -r "$dist"/agent "$smapp3"/
      echo "5"
      sudo mv "$smapp3"/agent/AgentSettings.json "$smcfg3"/agent/
      echo "6"
      sudo mv "$smapp3"/agent/SourceConfigs/ "$smcfg3"/agent/
      echo "7"
      sudo cp -r "$dist"/manager "$smapp3"/
      sudo mv "$smapp3"/manager/ManagerSettings.json "$smcfg3"/manager/
      echo "8"
      sudo cp -r "$dist"/jobmanager "$smapp3"/
      sudo mv "$smapp3"/jobmanager/* "$smcfg3"/jobmanager/
      echo "9"
      sudo cp -r "$dist"/api "$smapp3"/
      echo "10"
      sudo mv "$smapp3"/api/appsettings.json "$smcfg3"/api/
      echo "11"
      sudo cp -r "$dist"/ui-api "$smapp3"/
      echo "12"
      sudo mv "$smapp3"/ui-api/appsettings.json "$smcfg3"/ui-api/
      echo "13"
      sudo cp -r "$dist"/ui-web "$smapp3"/
      echo "14"
      sudo cp *.sh "$smapp3"/
      sudo cp *.dev_tools "$smapp2"/
      sudo cp cron.txt "$smapp3"/
      echo "15"
      sudo mkdir "$smapp3"/api/data
      sudo cp -r "$dist"/data-templates/* "$smapp3"/api/data/
      echo "16"
      sudo cp -r "$dist"/servicemanager "$smapp3"/
      sudo mv "$smapp3"/servicemanager/ServiceManagerSettings.json "$smcfg3"/servicemanager/
      echo "17"
      echo "Attempting to copy license.txt - ignore file not found error if license.txt not present..."
      sudo cp license.txt "$smapp3"/api/
      sudo rm "$smapp3"/sm-install.sh
      sudo chmod 755 "$smapp3"/*.sh
      sudo chown -R svc-saltminer:root "$smapp"
      sudo chown -R svc-saltminer:root "$smcfg"
      sudo chown -R svc-saltminer:root "$smlog"
      echo ""
      echo ""
      read -p "Next, edit the API config and enter the elastic password at a minimum.  Press the Enter key..."
      if [ "$osname" == "Red Hat nterprise Linux" ]; then
        sudo nano "$smcfg3"/api/appsettings.json
      else
        sudo nano "$smcfg3"/api/appsettings.json
      fi
      echo ""
      echo ""
      read -p "Next, edit SM25's elastic config and enter the elastic password at a minimum.  Press the Enter key..."
      if [ "$osname" == "Red Hat nterprise Linux" ]; then
        sudo nano "$smcfg2"/Elastic.json
      else
        sudo nano "$smcfg2"/Elastic.json
      fi
    fi

    read -p "Any errors (y/n)? [n] " ok
    if [ -z "$ok" ]; then ok="n"; fi
    if [ "$ok" == y ]; then
      echo "Re-run this script to resume here after troubleshooting.  "
      echo "Recommended: remove /etc/saltworks, /var/log/saltworks, /usr/share/saltworks before resuming."
      echo "The script will reattempt directory creation and file copies upon resume."
      exit 1
    fi

    # Wait for Kibana readiness
    kibana_ready="All services are available"
    while [[ $(curl -u elastic:$pwd http://localhost:5601/api/status) != *"$kibana_ready"* ]]
    do
      echo "Kibana not ready yet. Waiting for 5 seconds before continuing installation."
      sleep 5
    done

    echo -e "\nElasticsearch node connection test."
    curl -u elastic:$pwd http://localhost:9200
    echo -e "\nCluster Health"
    curl -u elastic:$pwd "http://localhost:9200/_cluster/health?pretty&wait_for_status=green"
    # Give ELK stack time to fully start before starting services.
    # 404 errors appear in the smapi log when processing ELK objects. Pausing for 10 seconds prevents this from
    # occurring. This is a temporary solution until something more elegant can be devised.
    sleep 10
    echo ""

    echo "Creating API and other services"
    sudo cp kestrel-saltminer-api.service.txt /etc/systemd/system/kestrel-saltminer-api.service
    sudo systemctl enable kestrel-saltminer-api
    sudo systemctl start kestrel-saltminer-api
    sudo cp kestrel-saltminer-ui-api.service.txt /etc/systemd/system/kestrel-saltminer-ui-api.service
    sudo systemctl enable kestrel-saltminer-ui-api
    sudo systemctl start kestrel-saltminer-ui-api
    sudo cp saltminer-service-manager.service.txt /etc/systemd/system/saltminer-service-manager.service
    sudo systemctl enable saltminer-service-manager
    sudo systemctl start saltminer-service-manager
    sudo cp saltminer-job-manager.service.txt /etc/systemd/system/saltminer-job-manager.service
    sudo systemctl enable saltminer-job-manager
    sudo systemctl start saltminer-job-manager
        
    if [ "$os" == "U" ]; then
      echo "Running pip install for SM25 dependencies"
      cmd="pip install -r $smapp2/requirements.txt"
      sudo su svc-saltminer -c "$cmd"
    else
      echo "Attempting pip install for SM25 dependencies, only supports python 3.9 right now on this OS because of the versioned calls (pip3.9, python3.9)"
      cmd="pip3.9 install -r $smapp2/requirements.txt"
      sudo su svc-saltminer -c "$cmd"
    fi  
  fi
fi

rm "$plog"

echo ""
echo "Installation complete.  Good show!"
echo ""
echo "Suggested next steps:"
echo "1. Edit your local hosts file and set name 'saltminer' to point to the IP address of the host"
echo "   c:\windows\system32\drivers\etc\hosts or /private/etc/hosts"
echo "2. Navigate to http://server/smapi/swagger and verify API is up and running"
echo "3. Run 'pip install -r requirements.txt' from the SM25 directory (if not run by this script)"
echo "4. Run 'python3 RunTestConnections.py' from the SM25 directory (RHEL may be python3.9 or another alias)"
echo "   Example invocation:"
echo "   sudo su svc-saltminer"
echo "   cd /usr/share/saltworks/saltminer-2.5.0"
echo "   env \"SALTMINER_2_CONFIG_PATH=/etc/saltworks/saltminer-2.5.0\" python3 /usr/share/saltworks/saltminer-2.5.0/RunTestConnections.py"
echo ""
echo "Useful monitoring commands:"
echo "   sudo systemctl status kestrel-saltminer-api -l"
echo "   sudo systemctl status kestrel-saltminer-ui-api -l"
echo "   sudo systemctl status saltminer-service-manager -l"
echo "   sudo systemctl status saltminer-job-manager -l"
