# Helpful Functions

This is a complicated codebase and it can be difficult for developers to how to check something or get a reference to what they need. This is a cheat sheet of common functions to help you quickly find what you need so you don't waste a lot of time looking or reimplementing something that already exists. This is not intended to be a complete list of functions, but just the most commonly used ones. Please expand it or modify it as you see fit.

## Logging

To write to the debug log:
```cs
Logger.Log(string msg, Enum Category)
```


To set the log level for different categories, go to unitystation/UnityProject/Assets/Scripts/Debug/Logger.cs, and modify 

```cs
private static readonly Dictionary<Category, Level> LogOverrides = new Dictionary<Category, Level>{
		[Category.Unknown]  = Level.Info,
		[Category.Movement] = Level.Error,
		[Category.Health] = Level.Trace, 
```
to your liking.

## Player
Get player info by gameObject (serverside only)
```cs
ConnectedPlayer player = PlayerList.Instance.Get(gameObject);
if (player != ConnectedPlayer.Invalid) {
    //Do your thing. If gameObject is not found in PlayerList, ConnectedPlayer.Invalid is returned
}
```


Check if player has spawned
```cs
bool playerSpawned = (PlayerManager.LocalPlayer != null);
```

Check if local player is dead
```cs
PlayerManager.LocalPlayerScript.playerHealth.IsDead
```

Check if local player is in critical health
```cs
PlayerManager.LocalPlayerScript.playerHealth.IsCrit
```

functions related to items the player has equipped
```cs
PlayerManager.Equipment
```

Functions related to player taking actions with the server
```cs
PlayerManager.LocalPlayerScript.NetworkActions
```

## Networking
Check if this is the server, but only inside a NetworkBehaviour
```cs
if(isServer)
```

Check if this is the server

```cs
CustomNetworkManager.Instance._isServer == false
```

## Matrix
Get the room number of a tile (only enclosed spaces will be defined as a room. RoomNumber will = -1 if it is not a room)
```cs
MatrixManager.GetMetaDataAt(Vector3Int.RoundToInt(world_position)).RoomNumber
```
## Other Useful Scripts
The `Assets\Scripts\Util` folder also contains a bunch of useful scripts to help deal with common problems. Have a look through there and you might find something useful!

