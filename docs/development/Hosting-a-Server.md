# Hosting a Server

This documents how to run a server, either by hosting + playing or by running headlessly (no GUI).

### Listen server (play and host)
You can host a game (so-called listen server) by checking the box when starting a game, which allows you to host + play. 

If you're using Unity Hub, go to Installations and launch game executable from there. 

### Headless server (host on a dedicated machine)
If you want to dedicate a machine for server hosting you can make game run in "headless" mode, meaning it runs without a UI. 
This mode is sometimes required for reproducing complex issues.
For a headless server, there are a few options:
1. Run game executable from the command line:
   ```
   Unitystation-Server -batchmode -nographics -logfile log1.txt
   ```
2. If you're testing from Unity Editor: Check the "Test Server" box in Managers > GameData in the lobby scene before running (make sure to leave this unchecked in the client build though). You should be able to join using a client once it's started, you won't get any GUI. This is supposed to simulate running a headless server but we aren't 100% confident on it yet. We refer to it as "fake headless"
