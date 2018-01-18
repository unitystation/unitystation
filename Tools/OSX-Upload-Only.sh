#!/bin/bash
set -e
script_dir=`dirname -- "$0"`
echo "Starting Unitystation buildscript from:"
echo $script_dir
cd $script_dir
cd ../Unityproject
project_dir=$(pwd)
echo "Starting to build from Unityproject directory:"
echo $project_dir


echo "Please enter your steam developer-upload credentials"
read -p 'Username: ' uservar
read -sp 'Password: ' passvar

$script_dir/ContentBuilder/builder_osx/steamcmd +login $uservar $passvar <<EOF
run_app_build $script_dir/ContentBuilder/scripts/app_build_787180.vdf
quit
EOF