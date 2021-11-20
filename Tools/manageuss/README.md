# ManageUSS
This folder contains the source for ManageUSS, a (admittedly hacky) management tool for controlling the unitystation systemd unit.
For it to work, Your unit should be `/etc/systemd/system/unitystation.service`
## /!\ **DO NOT OVERLOOK** /!\
Ensure the `manageuss` binary is `chmod 4711 root:root` or you may introduce serious privilage escalation vulnerabilities on your system!
