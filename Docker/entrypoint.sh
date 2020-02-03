cd /server/Unitystation-Server_Data/StreamingAssets/config
jq --arg v "$SERVER_NAME"   '.ServerName = $v' config.json | sponge config.json
jq --arg v "7778"           '.RconPort = $v'   config.json | sponge config.json 
jq --arg v "$RCON_PASSWORD" '.RconPass = $v'   config.json | sponge config.json
jq --arg v "$HUB_USER"      '.HubUser = $v'    config.json | sponge config.json
jq --arg v "$HUB_PASSWORD"  '.HubPass = $v'    config.json | sponge config.json

/server/Unitystation-Server -batchmode -nographics -logfile /dev/stdout