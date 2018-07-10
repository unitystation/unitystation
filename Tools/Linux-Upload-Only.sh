#!/bin/bash
set -e
script_dir=`pwd`
echo "Starting Upload from: "
echo $script_dir

echo "Please enter your steam developer-upload credentials"
read -p 'Username: ' uservar
read -sp 'Password: ' passvar

bash $script_dir/ContentBuilder/builder_linux/steamcmd.sh +login $uservar $passvar <<EOF
run_app_build $script_dir/ContentBuilder/scripts/app_build_801140.vdf
quit
EOF