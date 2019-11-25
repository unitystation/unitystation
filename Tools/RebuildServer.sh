#!/bin/bash
set -e
script_dir=`pwd`
echo "Starting Unitystation buildscript from:"
echo $script_dir
cd ..

echo "Shutting down active servers"
if pgrep -x screen >/dev/null 2>&1
  then
     killall screen
fi

cd UnityProject
project_dir=$(pwd)
echo "Starting to build from Unityproject directory:"
echo $project_dir

cp -r $script_dir/config $project_dir/Assets/StreamingAssets/config

echo "Attempting build of UnityStation Server"
/opt/2019.2.11f1/Editor/Unity \
	-batchmode \
	-nographics \
	-silent-crashes \
	-logFile $script_dir/Logs/ServerBuild.log \
	-projectPath $project_dir \
	-executeMethod BuildScript.PerformServerBuild \
	-quit
rc3=$?

rm -r $project_dir/Assets/StreamingAssets/config

echo "Build logs (Server)"
cat $script_dir/Logs/ServerBuild.log
echo "Building finished successfully"
