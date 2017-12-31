#!/bin/bash
script_dir=$(dirname $0)
echo "Starting to build Unitystation from the following directory:"
echo $script_dir
cd $script_dir

echo "Attempting build of UnityStation for OSX"
/Applications/Unity/Unity.app/Contents/MacOS/Unity \
  -batchmode \
  -nographics \
  -silent-crashes \
  -logFile Builds/Logs/OSX.log \
  -projectPath $script_dir \
  -executeMethod BuildServer.PerformOSXBuild \
  -quit
echo "Build logs (OSX)"
cat Builds/Logs/OSX.log

open -n Builds/Clients/OSX/Unitystation.app

echo "Attemping build of UnityStation Server for OSX"
/Applications/Unity/Unity.app/Contents/MacOS/Unity \
  -batchmode \
  -nographics \
  -silent-crashes \
  -logFile Builds/Logs/OSXServer.log \
  -projectPath $script_dir \
  -executeMethod BuildServer.PerformOSXServerBuild \
  -quit
echo "Build logs (Server)"
cat Builds/Logs/OSXServer.log

open -n Builds/Server/OSX/Unitystation-Server.app
