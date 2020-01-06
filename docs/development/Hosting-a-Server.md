# Hosting a Server

NOTE: This is a WIP and should be updated as needed.

This documents how to run a server, either by hosting + playing or by running headlessly (no GUI).

You can host a game by checking the box when starting a game, which allows you to host + play. However, the actual server for the game is "headless", meaning it runs without a UI. If you are having difficulty reproducing issues by hosting as a player, you can try running a headless server.

For a headless server, there are a few options:
1. Check the "Test Server" box in Managers > GameData in the lobby scene before running (make sure to leave this unchecked in the client build though). You should be able to join using a client once it's started, you won't get any GUI. This is supposed to simulate running a headless server but we aren't 100% confident on it yet.
2. Run a build from the command line:
   ```
   Unitystation-Server -batchmode -nographics -logfile log1.txt
   ```

TODO: Anything else needed for running a proper headless server? Some way to register with a central server list?

