#!/bin/bash
cd /opt/saltworks/saltminer/app/python
src="$1"
act="$2"
num="$3"

if [ $# -ne 3 ]; then
  echo ""
  echo "Usage:"
  echo "fortify-sync.sh src act num"
  echo ""
  echo ":src: source, SSC1 or FOD1 (or your instance)"
  echo ":act: action, sync/loadqueue/all (usually loadqueue)"
  echo ":num: log appendix number (usually 0 unless using sync and multiple jobs)
  exit 1
fi

lck="/tmp/fortify-sync-$num.lock"

echo "Attempting to start Fortify sync instance $num, $src - action: $act"
echo "If it does not start, then this sync instance may already be running."
touch -a "$lck"
flock -n "$lck" python3 RunSync.py "$src" "$act" "$num"