## Requirements:

* In release mode the GUI should first show a box for the player to log into their account. If they have chosen to save their credentials on a previous launch then skip this screen entirely and log the player in by verifying the credentials and account status via the api.
* If the player is logging in for the first time then show the character customization pop up screen (which will appear over the top of the server screen on a higher layer)
* Otherwise show the server list: A scroll view with active servers and stats, like pop count. This information will be delivered via the api in json format each time the client refreshes it
* Once a player clicks connect on a server, then command the NetworkManager to attempt a connection to that server using the ip and port data from the server entry
* If the server is in lobby mode then the player will load a new lobby scene (called ServerLobby.unity) where they will be presented with a full screen chat window and a list of connected players who are waiting for the server start on the right. A count down timer will also be visible up the top right. Once the timer has counted down the server will load the Outpost map scene and begin the round

