cd /server/Unitystation_Data/StreamingAssets/Config
if test "$SERVER_NAME"  ; then jq --arg v "$SERVER_NAME"   '.ServerName = $v' config.json | sponge config.json; fi
if test "$DISCORDLINKID"  ; then jq --arg v "$DISCORDLINKID"   '.DiscordLinkID = $v' config.json | sponge config.json; fi
/server/Unitystation -batchmode -trusted -nographics -logfile /dev/stdout 
