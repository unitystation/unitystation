#!/bin/bash
DIR="$( cd "$( dirname "${BASH_SOURCE[0]}" )" && pwd )"
export LD_LIBRARY_PATH="$DIR/Unitystation-Server_Data/Plugins/x86_64"
$DIR/Unitystation-Server -batchmode -nographics -logfile log1.txt