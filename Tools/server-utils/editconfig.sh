#!/bin/bash
clear
while true; do
    echo ''
    echo '====================================='
    echo '            Config Editor            '
    echo '====================================='
    echo ''
    if [ "$INVALID" == "1" ]; then
        echo 'Invalid selection.'
    fi
    INVALID="0"
    echo 'Please select which config you wish to edit:'
    echo '(c) Server Settings'
    echo '(d) Server Description'
    echo '(g) Game Settings'
    echo '(m) Map Config'
    echo '(a) Admin List'
    echo '(m) Mentor List'
    echo '(w) Whitelist'
    echo '(q) Quit'
    read -p "Select:" -n 1
    if [ $REPLY == 'c' ]
    then
        nano /home/unitystation/us13/Unitystation_Data/StreamingAssets/config/config.json
    elif [ $REPLY == 'd' ]
    then
        nano /home/unitystation/us13/Unitystation_Data/StreamingAssets/config/serverDesc.txt
    elif [ $REPLY == 'g' ]
    then
        nano /home/unitystation/us13/Unitystation_Data/StreamingAssets/config/gameConfig.json
    elif [ $REPLY == 'm' ]
    then
        nano /home/unitystation/us13/Unitystation_Data/StreamingAssets/maps.json
    elif [ $REPLY == 'a' ]
    then
        nano /home/unitystation/us13/Unitystation_Data/StreamingAssets/admin/admins.txt
    elif [ $REPLY == 'm' ]
    then
        nano /home/unitystation/us13/Unitystation_Data/StreamingAssets/admin/mentors.txt
    elif [ $REPLY == 'w' ]
    then 
        nano /home/unitystation/us13/Unitystation_Data/StreamingAssets/admin/whitelist.txt
    elif [ $REPLY == 'q' ]
    then
        echo ''
        exit
    else
        INVALID="1"
    fi
    clear
done
