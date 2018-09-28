using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Networking;

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

	public bool reportDone = false;

	//For TDM demo
	//public Dictionary<JobDepartment, int> departmentScores = new Dictionary<JobDepartment, int>();

	//For combat demo
	public Dictionary<string, int> playerScores = new Dictionary<string, int>();

	//Nuke Ops (TODO: throughoutly remove all unnecessary TDM variables)
	public bool nukeSetOff = false;
	//Kill counts for crew members and syndicate for display at end of round, similar to past TDM department scores
	public int crewKills;
	public int syndicateKills;


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

	//Called on the server when a kill is confirmed
	[Server]
	public void UpdateKillScore(GameObject perpetrator, GameObject victim)
	{
		if ( perpetrator == null )
		{
			return;
		}
		var perPlayer = perpetrator.Player();
		var victimPlayer = victim.Player();
		if ( perPlayer == null || victimPlayer == null ) {
			return;
		}

		var playerName = perPlayer.Name;
		if ( playerScores.ContainsKey(playerName) )
		{
			playerScores[playerName]++;
		}

		//If killer is syndicate, add kills to syndicate score, if not - to crew.
		if(perPlayer.Job == JobType.SYNDICATE)
		{
			syndicateKills++;
		}
		else
		{
			crewKills++;
		}
	}

	[Server]
	public void TryAddScores(string uniqueName)
	{
		if ( !playerScores.ContainsKey(uniqueName) )
		{
			playerScores.Add(uniqueName, 0);
		}
	}

	[Server]
	public void ReportScores()
	{
		if (!reportDone)
		{
			//if (!nukeSetOff && syndicateKills == 0 && crewKills == 0)
			//{
			//	PostToChatMessage.Send("Nobody killed anybody. Fucking hippies.", ChatChannel.System);
			//}

			if (nukeSetOff)
			{
				PostToChatMessage.Send("Nuke has been detonated, <b>Syndicate wins.</b>", ChatChannel.System);
				ReportKills();
			}
			else if (GetCrewAliveCount() > 0 && EscapeShuttle.Instance.GetCrewCountOnboard() == 0)
			{
				PostToChatMessage.Send("Station crew failed to escape, <b>Syndicate wins.</b>", ChatChannel.System);
				ReportKills();
			}
			else if (GetCrewAliveCount() == 0)
			{
				PostToChatMessage.Send("All crew members are dead, <b>Syndicate wins.</b>", ChatChannel.System);
				ReportKills();
			}
			else if (GetCrewAliveCount() > 0 && EscapeShuttle.Instance.GetCrewCountOnboard() > 0)
			{
				PostToChatMessage.Send(EscapeShuttle.Instance.GetCrewCountOnboard() + " Crew member(s) have managed to escape the station. <b>Syndicate lost.</b>", ChatChannel.System);
				ReportKills();
			}
			
			PostToChatMessage.Send("Game Restarting in 30 seconds...", ChatChannel.System);
			reportDone = true;
		}
	}

	[Server]
	public void ReportKills()
	{
		if (syndicateKills != 0)
		{
			PostToChatMessage.Send("Syndicate managed to kill " + syndicateKills + " crew members.", ChatChannel.System);
		}

		if (crewKills != 0)
		{
			PostToChatMessage.Send("Crew managed to kill " + crewKills + " Syndicate operators.", ChatChannel.System);
		}
	}

	public int GetCrewAliveCount()
	{
		int alive = 0;

		List<ConnectedPlayer> alivePlayers = InGamePlayers;
		foreach (ConnectedPlayer ps in alivePlayers)
		{
			if(ps.Job == JobType.SYNDICATE || ps.GameObject.GetComponent<PlayerMove>().allowInput == false)
			{
				//Do nothing
			}
			else
			{
				alive++; //If player is alive and is not syndicate, add to alive count
			}
		}

		return alive;
	}

	public void RefreshPlayerListText()
	{
		UIManager.Instance.playerListUIControl.nameList.text = "";
		foreach (var player in ClientConnectedPlayers)
		{
			string curList = UIManager.Instance.playerListUIControl.nameList.text;
			UIManager.Instance.playerListUIControl.nameList.text = $"{curList}{player.Name} ({player.Job.JobString()})\r\n";
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

	[Server]
	private void CheckRcon(){
		if(RconManager.Instance != null){
			RconManager.UpdatePlayerListRcon();
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