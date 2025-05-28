#!/bin/ash
handle_error() {
  echo "Error on line $1"
  echo "This does not affect SaltMiner container start."
  # Errors shouldn't prevent other containers from starting.
  exit 0
}

trap 'handle_error $LINENO' ERR
