#!/bin/bash
script_dir=$(dirname $0)
echo "Starting to build Unitystation from the following directory:"
echo $script_dir
cd $script_dir
echo "Attempting build of UnityStation for Windows"
/Applications/Unity/Unity.app/Contents/MacOS/Unity \
	-batchmode \
	-nographics \
	-silent-crashes \
	-logFile Builds/Logs/Windows.log \
	-projectPath $script_dir \
	-buildWindows64Player "Builds/Clients/Windows/Unitystation.exe" \
	-quit
echo "Build logs (Windows)"
cat Builds/Logs/Windows.log

echo "Attempting build of UnityStation for OSX"
/Applications/Unity/Unity.app/Contents/MacOS/Unity \
	-batchmode \
	-nographics \
	-silent-crashes \
	-logFile Builds/Logs/OSX.log \
	-projectPath $script_dir \
	-buildOSXUniversalPlayer "Builds/Clients/OSX/Unitystation.app" \
	-quit
echo "Build logs (OSX)"
cat Builds/Logs/OSX.log


echo "Attempting build of UnityStation for Linux"
/Applications/Unity/Unity.app/Contents/MacOS/Unity \
	-batchmode \
	-nographics \
	-silent-crashes \
	-logFile Builds/Logs/Linux.log \
	-projectPath $script_dir \
	-buildLinuxUniversalPlayer "Builds/Clients/Linux/Unitystation" \
	-quit

echo "Build logs (Linux)"
cat Builds/Logs/Linux.log

echo "Attempting build of UnityStation Server"
/Applications/Unity/Unity.app/Contents/MacOS/Unity \
	-batchmode \
	-nographics \
	-silent-crashes \
	-logFile Builds/Logs/Server.log \
	-projectPath $script_dir \
	-executeMethod BuildServer.PerformBuild \
	-quit
echo "Build logs (Server)"
cat Builds/Logs/Server.log
