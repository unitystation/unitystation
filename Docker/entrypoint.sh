cd /server/Unitystation_Data/StreamingAssets/Config
if test "$RCON_PASSWORD"; then jq --arg v "$RCON_PASSWORD" '.RconPass = $v'   config.json | sponge config.json; fi
if test "$HUB_USERNAME" ; then jq --arg v "$HUB_USERNAME"  '.HubUser = $v'    config.json | sponge config.json; fi
if test "$HUB_PASSWORD" ; then jq --arg v "$HUB_PASSWORD"  '.HubPass = $v'    config.json | sponge config.json; fi
if test "$SERVER_NAME"  ; then jq --arg v "$SERVER_NAME"   '.ServerName = $v' config.json | sponge config.json; fi
if test "$ERROR_WEBHOOK"  ; then jq --arg v "$ERROR_WEBHOOK"   '.DiscordWebhookErrorLogURL = $v' config.json | sponge config.json; fi
if test "$OOC_WEBHOOK"  ; then jq --arg v "$OOC_WEBHOOK"   '.DiscordWebhookOOCURL = $v' config.json | sponge config.json; fi
if test "$ANNOUN_WEBHOOK"  ; then jq --arg v "$ANNOUN_WEBHOOK"   '.DiscordWebhookAnnouncementURL = $v' config.json | sponge config.json; fi
if test "$ADMIN_WEBHOOK"  ; then jq --arg v "$ADMIN_WEBHOOK"   '.DiscordWebhookAdminURL = $v' config.json | sponge config.json; fi
if test "$ADMINLOG_WEBHOOK"  ; then jq --arg v "$ADMINLOG_WEBHOOK"   '.DiscordWebhookAdminLogURL = $v' config.json | sponge config.json; fi
if test "$ALLCHAT_WEBHOOK"  ; then jq --arg v "$ALLCHAT_WEBHOOK"   '.DiscordWebhookAllChatURL = $v' config.json | sponge config.json; fi
if test "$DISCORDLINKID"  ; then jq --arg v "$DISCORDLINKID"   '.DiscordLinkID = $v' config.json | sponge config.json; fi
/server/Unitystation -batchmode -nographics -logfile /dev/stdout
