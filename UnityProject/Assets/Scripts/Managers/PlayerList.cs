using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Mirror;

/// Comfy place to get players and their info (preferably via their connection)
/// Has limited scope for clients (ClientConnectedPlayers only), sweet things are mostly for server
public partial class PlayerList : NetworkBehaviour
{
	//ConnectedPlayer list, server only
	private static List<ConnectedPlayer> values = new List<ConnectedPlayer>();
	private static List<ConnectedPlayer> oldValues = new List<ConnectedPlayer>();

	private static List<ConnectedPlayer> loggedOff = new List<ConnectedPlayer>();

	//For client needs: updated via UpdateConnectedPlayersMessage, useless for server
	public List<ClientConnectedPlayer> ClientConnectedPlayers = new List<ClientConnectedPlayer>();

	public static PlayerList Instance;
	public int ConnectionCount => values.Count;
	public List<ConnectedPlayer> InGamePlayers => values.FindAll(player => player.Script != null);

	public List<ConnectedPlayer> NonAntagPlayers =>
		values.FindAll(player => player.Script != null && !player.Script.mind.IsAntag);

	public List<ConnectedPlayer> AntagPlayers =>
		values.FindAll(player => player.Script != null && player.Script.mind.IsAntag);

	public List<ConnectedPlayer> AllPlayers =>
		values.FindAll(player => (player.Script != null || player.ViewerScript != null));

	/// <summary>
	/// Used to track who killed who. Could be used to check that a player actually killed someone themselves.
	/// </summary>
	public Dictionary<PlayerScript, List<PlayerScript>>
		KillTracker = new Dictionary<PlayerScript, List<PlayerScript>>();

	//Nuke Ops (TODO: throughoutly remove all unnecessary TDM variables)
	public bool nukeSetOff = false;

	private void Awake()
	{
		if (Instance == null)
		{
			Instance = this;
			Instance.ResetSyncedState();
		}
		else
		{
			Destroy(gameObject);
		}
	}

	public override void OnStartServer()
	{
		base.OnStartServer();
		InitAdminController();
	}


	/// <summary>
	/// Gets the connected player From providing the game object of the player.
	/// </summary>
	/// <returns>The connected player.</returns>
	/// <param name="Objectively">Game object of the player.</param>
	public static ConnectedPlayer GetConnectedPlayer(GameObject Objectively)
	{
		return (PlayerList.Instance.AllPlayers.First(x => x.GameObject == Objectively));
	}

	/// Allowing players to sync after round restart
	public void ResetSyncedState()
	{
		for (var i = 0; i < values.Count; i++)
		{
			var player = values[i];
			player.Synced = false;
		}
	}

	/// <summary>
	/// Called on the server when a kill is confirmed to track which players killed eachother.
	/// </summary>
	[Server]
	public void TrackKill(GameObject perpetrator, GameObject victim)
	{
		var perPlayer = perpetrator?.Player()?.Script;
		var victimPlayer = victim?.Player()?.Script;

		if (perPlayer == null || victimPlayer == null)
		{
			return;
		}

		// Check if the victim list needs to be created
		if (KillTracker.ContainsKey(perPlayer))
		{
			KillTracker[perPlayer].Add(victimPlayer);
		}
		else
		{
			KillTracker[perPlayer] = new List<PlayerScript>()
			{
				victimPlayer
			};
		}
	}

	/// <summary>
	/// Get all players currently located on provided matrix
	/// </summary>
	public List<ConnectedPlayer> GetPlayersOnMatrix(MatrixInfo matrix)
	{
		return InGamePlayers.FindAll(p => (p.Script != null) && p.Script.registerTile.Matrix.Id == matrix.Id);
	}

	public List<ConnectedPlayer> GetAlivePlayers(List<ConnectedPlayer> players = null)
	{
		if (players == null)
		{
			players = InGamePlayers;
		}

		return players.FindAll(p => !p.Script.IsGhost && p.Script.playerMove.allowInput);
	}

	public void RefreshPlayerListText()
	{
		UIManager.Instance.playerListUIControl.nameList.text = "";
		foreach (var player in ClientConnectedPlayers)
		{
			string curList = UIManager.Instance.playerListUIControl.nameList.text;
			UIManager.Instance.playerListUIControl.nameList.text =
				$"{curList}{player.Name} ({player.Job.JobString()})\r\n";
		}
	}

	/// Don't do this unless you realize the consequences
	[Server]
	public void Clear()
	{
		values.Clear();
	}

	/// <summary>
	/// Set this user's controlled game object to newGameObject (which may be a ghost or a body)
	/// </summary>
	/// <param name="conn">connection whose object should be updated</param>
	/// <param name="newGameObject">new game object they are controlling (should be a ghost or a body)</param>
	[Server]
	public void UpdatePlayer(NetworkConnection conn, GameObject newGameObject)
	{
		ConnectedPlayer connectedPlayer = Get(conn);
		connectedPlayer.GameObject = newGameObject;
		CheckRcon();
	}

	/// Add previous ConnectedPlayer state to the old values list
	/// So that you could find owner of the long dead body GameObject
	[Server]
	public void AddPrevious(ConnectedPlayer oldPlayer)
	{
		oldValues.Add(ConnectedPlayer.ArchivedPlayer(oldPlayer));
	}

	//filling a struct without connections and gameobjects for client's ClientConnectedPlayers list
	public List<ClientConnectedPlayer> ClientConnectedPlayerList =>
		values.Aggregate(new List<ClientConnectedPlayer>(), (list, player) =>
		{
			//not including headless server player
			if (!GameData.IsHeadlessServer || player.Connection != ConnectedPlayer.Invalid.Connection)
			{
				list.Add(new ClientConnectedPlayer {Name = player.Name, Job = player.Job});
			}

			return list;
		});

	/// <summary>
	/// Adds this connected player to the list, or updates an existing entry if there's already one for
	/// this player's networkconnection. Returns the ConnectedPlayer that was added or updated.
	/// </summary>
	/// <param name="player"></param>
	[Server]
	public ConnectedPlayer AddOrUpdate(ConnectedPlayer player)
	{
		if (player.Equals(ConnectedPlayer.Invalid))
		{
			Logger.Log("Refused to add invalid connected player to this server's player list", Category.Connections);
			return player;
		}

		if (ContainsConnection(player.Connection))
		{
//			Logger.Log($"Updating {Get(player.Connection)} with {player}");

			ConnectedPlayer existingPlayer = Get(player.Connection);
			Logger.LogFormat(
				"ConnectedPlayer {0} already exists in this server's PlayerList as {1}. Will update existing player instead of adding this new connected player.",
				Category.Connections, player, existingPlayer);
			//TODO: Are we sure these are the only things that need to be updated?
			existingPlayer.GameObject = player.GameObject;
			existingPlayer.Name = player.Name; //Note that name won't be changed to empties/nulls
			existingPlayer.Job = player.Job;
			existingPlayer.ClientId = player.ClientId;
			CheckRcon();
			return existingPlayer;
		}
		else
		{
			values.Add(player);
			Logger.LogFormat("Added to this server's PlayerList {0}. Total:{1}; {2}", Category.Connections, player,
				values.Count, string.Join(";", values));
			CheckRcon();
			return player;
		}
	}

	[Server]
	private void TryRemove(ConnectedPlayer player)
	{
		Logger.Log($"Removed {player}", Category.Connections);
		loggedOff.Add(player);
		values.Remove(player);
		AddPrevious(player);
		UpdateConnectedPlayersMessage.Send();
		CheckRcon();
	}

	[Server]
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

	private ConnectedPlayer getInternal(Func<ConnectedPlayer, bool> condition, bool lookupOld = false)
	{
		for (var i = 0; i < values.Count; i++)
		{
			if (condition(values[i]))
			{
				return values[i];
			}
		}

		if (lookupOld)
		{
			for (var i = 0; i < oldValues.Count; i++)
			{
				if (condition(oldValues[i]))
				{
					Logger.Log($"Returning old player {oldValues[i]}", Category.Connections);
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
		CheckForLoggedOffAdmin(connectedPlayer.UserId, connectedPlayer.Username);
		if (connectedPlayer.Equals(ConnectedPlayer.Invalid))
		{
			Logger.LogError($"Cannot remove by {connection}, not found", Category.Connections);
		}
		else
		{
			TryRemove(connectedPlayer);
		}
	}

	[Server]
	private void CheckRcon()
	{
		if (RconManager.Instance != null)
		{
			RconManager.UpdatePlayerListRcon();
		}
	}

	[Server]
	public GameObject TakeLoggedOffPlayer(string clientId)
	{
		Logger.LogTraceFormat("Searching for logged off players with id {0}", Category.Connections, clientId);
		foreach (var player in loggedOff)
		{
			Logger.LogTraceFormat("Found logged off player with id {0}", Category.Connections, player.ClientId);
			if (player.ClientId == clientId)
			{
				loggedOff.Remove(player);
				return player.GameObject;
			}
		}

		return null;
	}

	[Server]
	public void UpdateLoggedOffPlayer(GameObject newBody, GameObject oldBody)
	{
		for (int i = 0; i < loggedOff.Count; i++)
		{
			var player = loggedOff[i];
			if (player.GameObject == oldBody)
			{
				player.GameObject = newBody;
			}
		}
	}

	private void OnDestroy()
	{
		if (adminListWatcher != null)
		{
			adminListWatcher.Changed -= LoadCurrentAdmins;
			adminListWatcher.Dispose();
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
		return $"[{nameof(Name)}='{Name}', {nameof(Job)}={Job}]";
	}
}