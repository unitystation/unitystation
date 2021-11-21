#!/bin/bash
if [ $(whoami) == "root" ]
then
        echo "THIS SCRIPT MAY NOT BE RUN AS ROOT!!!"
        echo "PLEASE DEESCALATE TO NON-ADMINISTRATOR BEFORE CONTINUING!!!"
        echo "IF THIS SERVER NORMALLY RUNS AS ROOT, YOUR SETUP IS UNSUPPORTED AND CANNOT BE UPDATED USING THIS TOOL!"
        exit 1;
fi
 
SDIR='/home/unitystation/us13' # Change this if your server directory differs!!!!
SVER=$(wget -qO- https://api.unitystation.org/latestbuild)
SAST='Unitystation_Data/StreamingAssets'
BINFO="$SDIR/$SAST/buildinfo.json"
LVER=$(grep -Po "(?<=BuildNumber....)\d+" $BINFO)
echo ''
echo '====================================='
echo '            Server Updater           '
echo '====================================='
echo ''
echo "Local version: $LVER - Server version: $SVER"
if [[ "$SVER" == "$LVER" ]]
then
        echo "Server installation is already up to date."
else
        echo "New version available. Performing server update.."

        wget -O us13server.zip "https://unitystationfile.b-cdn.net/us13server$SVER.zip"
        ./manageuss stop
        mkdir update
        unzip us13server.zip -d update
        rm -rf "./update/$SAST/config"
        rm -rf "./update/$SAST/admin"
        rsync -a ./update/* "$SDIR/"
        sed -i "s/${LVER}.zip/${SVER}.zip/g" ${SDIR}/${SAST}/config/config.json
        rm -r ./update
        rm -f us13server.zip
        chmod +x "$SDIR/Unitystation"
        echo '====================================='
        ./manageuss start
        echo 'Update complete. Server has been restarted.'
fi 
echo ''
