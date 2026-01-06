#!/bin/bash
cd /usr/share/saltworks/saltminer-2.5.0
export SALTMINER_2_CONFIG_PATH=/etc/saltworks/saltminer-2.5.0
source .venv/bin/activate
python3 RunRiskRollup.py
deactivate