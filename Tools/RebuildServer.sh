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

echo "Getting latest changes from develop"
git checkout develop
git pull

cd UnityProject
project_dir=$(pwd)
echo "Starting to build from Unityproject directory:"
echo $project_dir

echo "Attempting build of UnityStation Server"
/opt/Unity-2017.4.0f1/Editor/Unity \
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
echo "Building finished successfully"

echo "Post processing builds"
cp $script_dir/ContentBuilder/content/Server/Unitystation-Server_Data/Plugins/libsteam_api64.so $script_dir/ContentBuilder/content/Server/Unitystation-Server_Data/Plugins/x86_64/libsteam_api64.so
cp $script_dir/ContentBuilder/content/Server/Unitystation-Server_Data/Plugins/libsteam_api.so $script_dir/ContentBuilder/content/Server/Unitystation-Server_Data/Plugins/x86_64/libsteam_api.so
cp $script_dir/steam1007/linux64/steamclient.so $script_dir/ContentBuilder/content/Server/Unitystation-Server_Data/Plugins/x86_64/steamclient.so
cp -Rf $script_dir/ContentBuilder/content/Server/Unitystation-Server_Data/Plugins $script_dir/ContentBuilder/content/Server/Unitystation-Server_Data/Mono

echo "Post-Processing done"
echo "Start server"
screen -d -m $script_dir/ContentBuilder/content/Server/Unitystation-Server -batchmode -nographics -logfile log2.txt
echo "Rebuild success. Server has restarted"
