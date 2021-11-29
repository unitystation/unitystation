# Server Utilities.
This directory contains the installation helper scripts and template files for setting up a Unitystation server.  
File names should be self explanitory, however:  
`installserver.sh` > Prepare a system and install the Unitystation server and service.  
`update.sh` > Update an existing installation  
`editconfig.sh` > Edit configuration paramters  
`config.example.json` > Example & template configuration file (Used by installserver.sh, please don't change the template values!)  
`unitystation.service` > Systemd unit file for the server.  
`manageuss/` > ManageUSS server management program for starting/stopping the server
 All these files can be found hosted on the cdn, https://unitystationfile.b-cdn.net/ but are not updated automatically.  
  
## Server Setup Instructions.  
To setup the unitystation server, You will first need a a Linux Server with atleast 2 CPU Cores and 4GB of RAM (More is better!), Preferably running Debian or Ubuntu.   
If you do not have one, You can rent a VPS (Virtual Private Server) for between 5$ and 30$ monthly from one of the following providers:  
 - [DigitalOcean](https://docs.digitalocean.com/products/droplets/how-to/create/)
 - [Scaleway](https://www.scaleway.com/en/docs/compute/instances/how-to/create-an-instance/)
 - [Hetzner](https://hetzner.com)  
If you need help setting up your server, Check out https://www.digitalocean.com/community/tutorials/initial-server-setup-with-ubuntu-20-04 or, ask us in the [Unitystation Discord!](https://discord.gg/tFcTpBp)  
  
This guide assumes you *already* have a terminal, and are greeted by something similar to this:   
![image](https://user-images.githubusercontent.com/7443752/142751301-78efdffa-bc7b-4290-a893-ca384238bb22.png)  
Once you are here, You can use `wget` to download the installation helper:  
```
wget -O installserver.sh https://unitystationfile.b-cdn.net/installserver.sh
```
Mark the installation helper as an executable with:
```
chmod +x installserver.sh
```
And finally, run the installation helper with:
```
sudo ./installserver.sh
```
The installation helper will guide you through the process of installing the server.
### **/!\\ Be sure to read what it tells you! [Ask us for help if you are confused, or have problems!](https://discord.gg/tFcTpBp) /!\\**  
  
After installation, You will need to open the game's port **7777** and your selected RCON port to the internet in your [Firewall (if using a VPS)](https://www.digitalocean.com/community/tutorials/how-to-set-up-a-firewall-with-ufw-on-ubuntu-20-04) or [Router (if hosting at home!)](https://www.noip.com/support/knowledgebase/general-port-forwarding-guide/)  
Once finished, Your server is ready for action! Launch the latest Unitystation version through the **Installations** tab in Station Hub, Uncheck 'Host Server' and Connect with your Server's IP address (and port 7777) and Enjoy!  
You can start, stop, and restart your server with:
```
~/manageuss
```
