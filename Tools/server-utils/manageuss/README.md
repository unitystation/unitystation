# ManageUSS
This folder contains the source for ManageUSS, a (admittedly hacky) management tool for controlling the unitystation systemd unit.
For it to work, Your unit should be `/etc/systemd/system/unitystation.service`

If your setup was not automatically configured to use ManageUSS & systemd by default, It is recommended to save your server configurations and do a complete re-installation.
The installation script can be found in Tools/server-utils/installserver.sh

## /!\ **DO NOT OVERLOOK** /!\
Ensure the `manageuss` binary is `chmod 4711 root:root` or you may introduce serious security vulnerabilities on your system!
