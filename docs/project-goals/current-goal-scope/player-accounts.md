## Purpose:

 The player accounts system will be used to store character customization settings, player stats and admin data for each player who plays unitystation. It will be an easy process to sign up and will be required to join the closed preview demo of the game. A simple form page will be set up on unitystation.org to sign up each player. For the closed preview release a steam key will be emailed to each new player when they sign up. 

Changes to the Player Login GUI will need to be made to accept a password with an option to remember settings. Once the player logs in they will then move to a server list page to select the server they want to join (will just be the one server for a long time). Server count and stats will be retrieved via our api.

Selecting a server while it is waiting to start will take the player to a Server Lobby screen with the ability to chat to people while they wait for the server to start. Servers automatically start when there is more then 1 player in the lobby and a 2 minute timer has finished counting down. If the lobby player count goes back to 0 before the timer has finished counting down then the timer is reset back to the full 2 minute wait time. More on the new lobby screen later.

## Backend API:
While in development mode the server urls will point to our dev.unitystation.org api which will be separated from the release api. The url and pass key fields will be prepopulated with this data in our source code. At release time a new url and pass keys will be loaded via a config file on the build server to make sure db access is secure. All api requests should be hashed to prevent from sniffing. The development database will also have its own tables and db user credentials to ensure that the two services are sufficiently separated.

## Benefits for players:

The player account system will allow players to take their characters to different machines. It also lays the ground work for player stats in the future which we will cover in another scope.

## Benefits for admins:

We can now store IP addresses and Steam Ids to each account and admin notes. This will also be a good place to store account status (Active, Banned, Kicked etc). 

