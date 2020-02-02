#!/bin/bash  
mkdir -p UnityStation_Data/StreamingAssets/config
template='{"certKey":"123456","RconPort":7778,"RconPass":"%s","HubUser":"%s","HubPass":"%s","ServerName":"",
    "WinDownload": "https://unitystation.org/clients/win3966.zip",
    "OSXDownload": "https://unitystation.org/clients/osx3966.zip",
    "LinuxDownload": "https://unitystation.org/clients/lin3966.zip"}'
json=$(printf "$template" "$RCON_PASSWORD" "$HUB_USER" "$HUB_PASSWORD")
echo $json > UnityStation_Data/StreamingAssets/config/config.json
./UnityStation -batchmode -nographics -logfile /dev/stdout