﻿using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using PlayGroup;
using UI;
using UnityEngine;
using UnityEngine.Networking;
using Util;

/// Comfy place to get players and their info (preferably via their connection)
/// Has limited scope for clients (ClientConnectedPlayers only), sweet things are mostly for server
public class PlayerList : NetworkBehaviour
{
	//ConnectedPlayer list, server only
	private static List<ConnectedPlayer> values = new List<ConnectedPlayer>();
	private static List<ConnectedPlayer> oldValues = new List<ConnectedPlayer>();

	//For client needs: updated via UpdateConnectedPlayersMessage, useless for server
	public List<ClientConnectedPlayer> ClientConnectedPlayers = new List<ClientConnectedPlayer>();

	public static PlayerList Instance;
	public int ConnectionCount => values.Count;
	public List<ConnectedPlayer> InGamePlayers => values.FindAll( player => player.GameObject != null );

	//For job formatting purposes
	private static readonly TextInfo textInfo = new CultureInfo("en-US", false).TextInfo;

    private static int LifeCount;

	private void Awake()
	{
		if ( Instance == null )
		{
			Instance = this;
            Instance.ResetSyncedState();
        }
		else
		{
			Destroy(gameObject);
		}
	}

	/// Allowing players to sync after round restart
	public void ResetSyncedState() {
		for ( var i = 0; i < values.Count; i++ ) {
			var player = values[i];
			player.Synced = false;
		}
	}

	public void RefreshPlayerListText()
	{
		UIManager.Instance.playerListUIControl.nameList.text = "";
		foreach (var player in ClientConnectedPlayers)
		{
			string curList = UIManager.Instance.playerListUIControl.nameList.text;
        }
	}

	/// Don't do this unless you realize the consequences
	[Server]
	public void Clear()
	{
		values.Clear();
	}

	[Server]
	public ConnectedPlayer UpdatePlayer(NetworkConnection conn, GameObject newGameObject)
	{
		ConnectedPlayer connectedPlayer = Get(conn);
		connectedPlayer.GameObject = newGameObject;
		CheckRcon();
		return connectedPlayer;
	}

	/// Add previous ConnectedPlayer state to the old values list
	/// So that you could find owner of the long dead body GameObject
	[Server]
	public void AddPrevious( ConnectedPlayer oldPlayer )
	{
		oldValues.Add( ConnectedPlayer.ArchivedPlayer( oldPlayer ) );
	}

	//filling a struct without connections and gameobjects for client's ClientConnectedPlayers list
	public List<ClientConnectedPlayer> ClientConnectedPlayerList =>
		values.Aggregate(new List<ClientConnectedPlayer>(), (list, player) =>
		{
			//not including headless server player
			if ( !GameData.IsHeadlessServer || player.Connection != ConnectedPlayer.Invalid.Connection )
			{
				list.Add(new ClientConnectedPlayer {Name = player.Name, Job = player.Job});
			}
			return list;
		});

	[Server]
	private void TryAdd(ConnectedPlayer player)
	{
		if ( player.Equals(ConnectedPlayer.Invalid) )
		{
			Logger.Log("Refused to add invalid connected player",Category.Connections);
			return;
		}
		if ( ContainsConnection(player.Connection) )
		{
//			Logger.Log($"Updating {Get(player.Connection)} with {player}");
			ConnectedPlayer existingPlayer = Get(player.Connection);
			existingPlayer.GameObject = player.GameObject;
			existingPlayer.Name = player.Name; //Note that name won't be changed to empties/nulls
			existingPlayer.Job = player.Job;
			existingPlayer.SteamId = player.SteamId;
		}
		else
		{
			values.Add(player);
			Logger.Log($"Added {player}. Total:{values.Count}; {string.Join("; ",values)}",Category.Connections);
			//Adding kick timer for new players only
			StartCoroutine(KickTimer(player));
		}
		CheckRcon();
	}

	private IEnumerator KickTimer(ConnectedPlayer player)
	{
		if ( IsConnWhitelisted( player ) || !BuildPreferences.isForRelease )
		{
//			Logger.Log( "Ignoring kick timer for invalid connection" );
			yield break;
		}
		int tries = 5;
        while (!player.IsAuthenticated)
        {
            if (tries-- < 0)
            {
                CustomNetworkManager.Kick(player, "Auth timed out");
                yield break;
            }
            yield return YieldHelper.Second;
        }
    }

	public static bool IsConnWhitelisted( ConnectedPlayer player )
	{
		return player.Connection == null || 
		       player.Connection == ConnectedPlayer.Invalid.Connection ||
		       !player.Connection.isConnected;
	}

	[Server]
	private void TryRemove(ConnectedPlayer player)
	{
		Logger.Log($"Removed {player}",Category.Connections);
		values.Remove(player);
		AddPrevious( player );
		NetworkServer.Destroy(player.GameObject);
		UpdateConnectedPlayersMessage.Send();
		CheckRcon();
	}

	[Server]
	public void Add(ConnectedPlayer player) => TryAdd(player);

	public bool ContainsConnection(NetworkConnection connection)
	{
		return !Get(connection).Equals(ConnectedPlayer.Invalid);
	}
	
	[Server]
	public bool ContainsName(string name)
	{
		return !Get(name).Equals(ConnectedPlayer.Invalid);
	}
	
	[Server]
	public bool ContainsGameObject(GameObject gameObject)
	{
		return !Get(gameObject).Equals(ConnectedPlayer.Invalid);
	}
	
	[Server]
	public ConnectedPlayer Get(NetworkConnection byConnection, bool lookupOld = false)
	{
		return getInternal(player => player.Connection == byConnection, lookupOld);
	}
	
	[Server]
	public ConnectedPlayer Get(string byName, bool lookupOld = false)
	{
		return getInternal(player => player.Name == byName, lookupOld);
	}
	
	[Server]
	public ConnectedPlayer Get(GameObject byGameObject, bool lookupOld = false)
	{
		return getInternal(player => player.GameObject == byGameObject, lookupOld);
	}	
	
	[Server]
	public ConnectedPlayer Get(ulong bySteamId, bool lookupOld = false)
	{
		return getInternal(player => player.SteamId == bySteamId, lookupOld);
	}

	private ConnectedPlayer getInternal(Func<ConnectedPlayer,bool> condition, bool lookupOld = false)
	{
		for ( var i = 0; i < values.Count; i++ )
		{
			if ( condition(values[i]) )
			{
				return values[i];
			}
		}
		if ( lookupOld )
		{
			for ( var i = 0; i < oldValues.Count; i++ )
			{
				if ( condition(oldValues[i]) )
				{
					Logger.Log( $"Returning old player {oldValues[i]}", Category.Connections);
					return oldValues[i];
				}
			}
		}

		return ConnectedPlayer.Invalid;
	}

	[Server]
	public void Remove(NetworkConnection connection)
	{
		ConnectedPlayer connectedPlayer = Get(connection);
		if ( connectedPlayer.Equals(ConnectedPlayer.Invalid) )
		{
			Logger.LogError($"Cannot remove by {connection}, not found", Category.Connections);
		}
		else
		{
			TryRemove(connectedPlayer);
		}
	}
    public void ReportScores()
    {
        foreach (ConnectedPlayer player in PlayerList.Instance.InGamePlayers)
        {
             if (!player.GameObject.GetComponent<HealthBehaviour>().IsDead)
             {
                LifeCount++;
                PostToChatMessage.Send(player.Name + " has survived the Syndicate terrorist attack on the NSS Cyberiad.", ChatChannel.OOC);
             }
        }

        if (LifeCount == 0)
        {
            PostToChatMessage.Send("No one has survived the Syndicate terrorist attack on the NSS Cyberiad.", ChatChannel.OOC);

            if (GetComponent<NukeInteract>().detonated)
            {
                PostToChatMessage.Send("The syndicate terrorists have detonated the on-board self-destruct.", ChatChannel.OOC);
            }
            else
            {
                PostToChatMessage.Send("The syndicate terrorists have instead of detonating the on board self destruct, chosen to slaughter everyone on board, this sends a terrifing chill down", ChatChannel.OOC);
            }
        }

        if (LifeCount == 1)
        {
            PostToChatMessage.Send(LifeCount + " person survived the Syndicate terrorist attack on the NSS Cyberiad.", ChatChannel.OOC);
        }

        if (LifeCount > 1)
        {
            PostToChatMessage.Send(LifeCount + " people survived the Syndicate terrorist attack on the NSS Cyberiad.", ChatChannel.OOC);
        }

        if (LifeCount >= 1)
        {
             PostToChatMessage.Send("The nuke ops have failed to detonated the bomb.", ChatChannel.OOC);
        }
        PostToChatMessage.Send("Restarting in 10 seconds.", ChatChannel.OOC);
    }
=======
	[Server]
	private void CheckRcon(){
		if(Rcon.RconManager.Instance != null){
			Rcon.RconManager.UpdatePlayerListRcon();
		}
	}

}

/// Minimalistic connected player information that all clients can posess
public struct ClientConnectedPlayer
{
	public string Name;
	public JobType Job;

	public override string ToString()
	{
		return $"[{nameof( Name )}='{Name}', {nameof( Job )}={Job}]";
	}
}