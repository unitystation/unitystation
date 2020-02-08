cd /server/UnityStation_Data/StreamingAssets/config
if test $RCON_PASSWORD; then jq --arg v "$RCON_PASSWORD" '.RconPass = $v' config.json | sponge config.json; fi
if test $HUB_USER     ; then jq --arg v "$HUB_USER"      '.HubUser = $v'  config.json | sponge config.json; fi
if test $HUB_PASSWORD ; then jq --arg v "$HUB_PASSWORD"  '.HubPass = $v'  config.json | sponge config.json; fi

cd /server/UnityStation_Data/StreamingAssets
if test $BUILD_NUMBER; then jq --arg v "$BUILD_NUMBER" '.BuildNumber = $v' buildinfo.json | sponge buildinfo.json; fi
if test $BUILD_FORK  ; then jq --arg v "$BUILD_FORK"   '.ForkName = $v'    buildinfo.json | sponge buildinfo.json; fi

/server/UnityStation -batchmode -nographics -logfile /dev/stdout