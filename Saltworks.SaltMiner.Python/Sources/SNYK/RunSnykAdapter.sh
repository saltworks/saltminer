#!/bin/bash
cd /usr/share/saltworks/saltminer-2.5.0
export SALTMINER_2_CONFIG_PATH=/etc/saltworks/saltminer-2.5.0

python3 -m Sources.SNYK.RunSnykAdapter

