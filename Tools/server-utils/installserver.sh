#!/bin/bash

echo ''
echo ''
echo ''
echo ''
echo '========================================' 
echo '====== UNITYSTATION SERVER SETUP ======='
echo '========================================' 
if [ "$(id -u)" != "0" ]; then
	echo ''
	echo 'You must be root to run this script.'
	echo 'Exiting...'
	echo ''
	exit 1
fi 
echo ''
echo 'A new user, unitystation, will be created'
echo 'If it does not already exist.'
echo 'This script will install the latest version'
echo 'of the Unitystation Server into its home.'
echo ''
echo 'Do you wish to continue? (y/n)'
read -n 1 -r
#if they reply anything other than yes, exit the script
if [[ ! $REPLY =~ ^[Yy]$ ]]
then
	echo ''
	echo 'Exiting...'
	echo ''
	exit 1
fi
#create the user if not exists
if [ -z "$(getent passwd unitystation)" ]; then
	useradd -m -d /home/unitystation -s /bin/false unitystation
fi
#check if the us13 folder exists, if so error out
if [ -d "/home/unitystation/us13" ]; then
	echo ''
	echo 'The server folder already exists.'
	echo 'Please remove it and try again.'
	echo 'Exiting...'
	echo ''
	exit 1
fi
apt install unzip
mkdir /home/unitystation/us13
wget -O /home/unitystation/update.sh https://unitystationfile.b-cdn.net/update.sh
wget -O /home/unitystation/editconfig.sh https://unitystationfile.b-cdn.net/editconfig.sh

chmod +x /home/unitystation/update.sh
chmod +x /home/unitystation/editconfig.sh

SVER=$(wget -qO- https://api.unitystation.org/latestbuild)
wget -O /home/unitystation/us13server.zip "https://unitystationfile.b-cdn.net/us13server$SVER.zip"
unzip /home/unitystation/us13server.zip -d /home/unitystation/us13
chmod +x /home/unitystation/us13/Unitystation

wget -O /home/unitystation/manageuss https://unitystationfile.b-cdn.net/manageuss
chown -R unitystation:unitystation /home/unitystation/
chmod 4711 /home/unitystation/manageuss
chown root:root /home/unitystation/manageuss

echo 'The following questions will help you'
echo 'set up a working headless unitystation'
echo 'server. To list on Station Hub you will'
echo 'need to speak to the lead dev (captain)'
echo 'on the unitystation discord so that you'
echo 'can be vetted and given a unique'
echo 'server hub account. If you dont want to be'
echo 'listed on the hub then just leave the values'
echo 'empty.'
echo ''
echo 'RCON stands for "remote console". Once you have'
echo 'set a RCON password and RCON port you will be'
echo 'able to access the admin console for your server'
echo 'at http://console.unitystation.org (NOT https,'
echo 'this is because of the way WebSockets work.)'
echo ''
echo 'Important note:'
echo 'For all command line inputs below, please use'
echo 'letters and numbers only as the config values'
echo 'are not escaped when being loaded into the' 
echo 'game server itself.'
echo ''
echo '========================================'
echo '========================================' 
echo ''
echo ''
echo 'Server Name:'
read SVRNAME
echo ''
echo 'RCON Password ( length > 16 ):'
read RCONPASS
#while rcon password is less than 16 characters
while [ ${#RCONPASS} -lt 16 ]; do
	echo 'Sorry, RCON Password must be 16 characters or longer.'
	echo 'RCON Password:'
	read RCONPASS
done
echo ''
echo 'RCON Port ( # > 1024, =/= 7777 ):'
read RCONPORT
while [ "${RCONPORT}" -le 1024 ] || [ "${RCONPORT}" -eq 7777 ]
do
	echo 'Sorry, RCON Port must be greater than 1024 and not equal to 7777'
	echo 'RCON Port ( > 1024, =/= 7777):'
	read RCONPORT
done
echo ''
echo 'Hub Username (blank if none):'
read HUBUSER
echo ''
echo 'Hub Password (blank if none):'
read HUBPASS
echo 'Discord Invite (blank if none):'
read DISCORDLINK

rm /home/unitystation/us13/Unitystation_Data/StreamingAssets/config/config.json
wget -O /home/unitystation/us13/Unitystation_Data/StreamingAssets/config/config.json https://unitystationfile.b-cdn.net/config.example.json
sed -i "s/SERVERNAME/$SVRNAME/g"      /home/unitystation/us13/Unitystation_Data/StreamingAssets/config/config.json
sed -i "s/RCONPASS/$RCONPASS/g"       /home/unitystation/us13/Unitystation_Data/StreamingAssets/config/config.json
sed -i "s/3010/$RCONPORT/g"           /home/unitystation/us13/Unitystation_Data/StreamingAssets/config/config.json
sed -i "s/HUBUSER/$HUBUSER/g"         /home/unitystation/us13/Unitystation_Data/StreamingAssets/config/config.json
sed -i "s/HUBPASS/$HUBPASS/g"         /home/unitystation/us13/Unitystation_Data/StreamingAssets/config/config.json
sed -i "s/DISCORDLINK/$DISCORDLINK/g" /home/unitystation/us13/Unitystation_Data/StreamingAssets/config/config.json
sed -i "s/SVER/$SVER/g"               /home/unitystation/us13/Unitystation_Data/StreamingAssets/config/config.json

wget -O /etc/systemd/system/unitystation.service https://unitystationfile.b-cdn.net/unitystation.service
systemctl daemon-reload
systemctl enable unitystation
systemctl start unitystation

echo ''
echo 'Set up complete! The server has now started on port 7777.'
echo 'Please ensure you have opened both the game port and (required) the RCON port (optional)'
echo 'in your firewall rules before attempting to join.'
echo 'You can manage the server state using the systemd unit unitystation.service (root)'
echo 'Or, through the management interface /home/unitystation/manageuss (non-root)'
echo 'You can also use the editconfig.sh script to edit the config file further.'
echo 'Including to add yourself as an admin!'
echo 'Enjoy!'

rm -f "$RDIR/us13server.zip"
rm -f "$RDIR/installserver.sh"
