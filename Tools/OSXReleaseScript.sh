#!/bin/bash
set -e
script_dir=$(dirname $0)
echo "Starting Unitystation buildscript from:"
echo $script_dir
cd $script_dir
cd ../Unityproject
project_dir=$(pwd)
echo "Starting to build from Unityproject directory:"
echo $project_di

echo "Attempting build of UnityStation for Windows"
/Applications/Unity/Unity.app/Contents/MacOS/Unity \
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


echo "Attempting build of UnityStation for Linux"
/Applications/Unity/Unity.app/Contents/MacOS/Unity \
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

echo "Attempting build of UnityStation Server"
/Applications/Unity/Unity.app/Contents/MacOS/Unity \
	-batchmode \
	-nographics \
	-silent-crashes \
	-logFile $script_dir/Logs/ServerBuild.log \
	-projectPath $project_dir \
	-executeMethod BuildScript.PerformServerBuild \
	-quit
rc3=$?
echo "Build logs (Server)"
cat $script_dir/Logs/ServerBuild.log
