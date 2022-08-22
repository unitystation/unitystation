#!/bin/bash
set -e
script_dir=`pwd`
echo "Starting Unitystation buildscript from:"
echo $script_dir
cd ..
cd UnityProject
project_dir=$(pwd)
echo "Starting to build from Unityproject directory:"
echo $project_dir

echo "Attempting build of UnityStation for Windows"
/opt/2019.2.11f1/Editor/Unity \
	-batchmode \
	-nographics \
	-silent-crashes \
	-logFile $script_dir/Logs/WindowsBuild.log \
	-projectPath $project_dir \
	-executeMethod BuildScript.PerformWindowsBuild \
	-quit
rc0=$?
echo "Build logs (Windows)"
cat $script_dir/Logs/WindowsBuild.log



echo "Attempting build of UnityStation for OSX"
/opt/2019.2.11f1/Editor/Unity \
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


echo "Attempting build of UnityStation for Linux"
/opt/2019.2.11f1/Editor/Unity \
	-batchmode \
	-nographics \
	-silent-crashes \
	-logFile $script_dir/Logs/LinuxBuild.log \
	-projectPath $project_dir \
	-executeMethod BuildScript.PerformLinuxBuild \
	-quit
rc2=$?
echo "Build logs (Linux)"
cat $script_dir/Logs/LinuxBuild.log

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