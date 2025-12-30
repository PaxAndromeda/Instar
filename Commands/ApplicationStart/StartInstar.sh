#!/usr/bin/env bash

SCRIPT_DIR=$( cd -- "$( dirname -- "${BASH_SOURCE[0]}" )" &> /dev/null && pwd )

startInstar() {
  local INSTAR_CONF_FILE;

  if ! INSTAR_CONF_FILE=$("$SCRIPT_DIR"/../Global/GetInstarConfiguration.sh); then
    echo "Could not get config file."
    exit 1
  fi 
  
  local INSTAR_CONF="/Instar/bin/Config/$INSTAR_CONF_FILE"

  /Instar/bin/InstarBot --config-path "$INSTAR_CONF" > /dev/null 2> /dev/null < /dev/null &
}

startInstar