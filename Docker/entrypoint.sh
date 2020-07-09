cd /server/Unitystation_Data/StreamingAssets/config
if test "$RCON_PASSWORD"; then jq --arg v "$RCON_PASSWORD" '.RconPass = $v'   config.json | sponge config.json; fi
if test "$HUB_USERNAME" ; then jq --arg v "$HUB_USERNAME"  '.HubUser = $v'    config.json | sponge config.json; fi
if test "$HUB_PASSWORD" ; then jq --arg v "$HUB_PASSWORD"  '.HubPass = $v'    config.json | sponge config.json; fi
if test "$SERVER_NAME"  ; then jq --arg v "$SERVER_NAME"   '.ServerName = $v' config.json | sponge config.json; fi
if test "$ERROR_WEBHOOK"  ; then jq --arg v "$ERROR_WEBHOOK"   '.DiscordWebhookErrorLogURL = $v' config.json | sponge config.json; fi

/server/Unitystation -batchmode -nographics -logfile /dev/stdout