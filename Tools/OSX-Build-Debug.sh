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

echo "Attempting build of UnityStation for OSX"
/Applications/Unity/Unity.app/Contents/MacOS/Unity \
	-batchmode \
	-nographics \
	-silent-crashes \
	-logFile $script_dir/Logs/OSXBuild.log \
	-projectPath $project_dir \
	-executeMethod BuildScript.PerformOSXBuild \
	-quit
rc1=$?
echo "Build logs (OSX)"
cat $script_dir/Logs/OSXBuild.log

cp ../Builds/OSX/Unitystation.app ../Builds/OSX/Unitystation-Server.app
echo "Building finished successfully"
