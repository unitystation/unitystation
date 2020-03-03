#!/bin/bash

echo "Stopping lag simulation..."

sudo dnctl -q flush
sudo pfctl -f /etc/pf.conf

echo "Done. Call lag.sh to re-enable lag simulation."