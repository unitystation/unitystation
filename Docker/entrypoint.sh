cd /server/UnityStation_Data/StreamingAssets/config
if test $RCON_PASSWORD; then jq --arg v "$RCON_PASSWORD" '.RconPass = $v' config.json | sponge config.json; fi
if test $HUB_USER     ; then jq --arg v "$HUB_USER"      '.HubUser = $v'  config.json | sponge config.json; fi
if test $HUB_PASSWORD ; then jq --arg v "$HUB_PASSWORD"  '.HubPass = $v'  config.json | sponge config.json; fi

/server/UnityStation -batchmode -nographics -logfile /dev/stdout