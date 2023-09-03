using System;
using System.Collections.Generic;
using System.Linq;
using Antagonists;
using Logs;
using SecureStuff;
using Messages.Server;
using UnityEngine;
using Mirror;
using Systems.Character;
using UI.CharacterCreator;

/// Comfy place to get players and their info (preferably via their connection)
/// Has limited scope for clients (ClientConnectedPlayers only), sweet things are mostly for server
public partial class PlayerList : NetworkBehaviour
{
	//ConnectedPlayer list, server only
	private List<PlayerInfo> loggedIn = new List<PlayerInfo>();
	public List<PlayerInfo> loggedOff = new List<PlayerInfo>();

	/// <summary>
	/// The ConnectedPlayers who have been in this current round, clears at round end
	/// </summary>
	private HashSet<PlayerInfo> roundPlayers = new HashSet<PlayerInfo>();

	//For client needs: updated via UpdateConnectedPlayersMessage, useless for server
	public List<ClientConnectedPlayer> ClientConnectedPlayers = new List<ClientConnectedPlayer>();

	public static PlayerList Instance;
	public int ConnectionCount => loggedIn.Count;
	public int OfflineConnCount => loggedOff.Count;
	public int OnlineAndOfflineConnCount => loggedIn.Count + loggedOff.Count;

	/// <summary>
	/// All players inside this list are online players.
	/// </summary>
	public List<PlayerInfo> InGamePlayers => loggedIn.FindAll(player => player.Script != null);

	public List<PlayerInfo> NonAntagPlayers =>
		loggedIn.FindAll(player => player?.Mind != null && !player.Mind.IsAntag);

	public List<PlayerInfo> AntagPlayers =>
		loggedIn.FindAll(player => player?.Mind != null && player.Mind.IsAntag);

	public List<PlayerInfo> AllPlayers =>
		loggedIn.FindAll(player => (player?.Mind  != null || player?.ViewerScript != null));

	/// <summary>
	/// Players in the pre-round lobby who have clicked the ready button and have up to date CharacterSettings
	/// </summary>
	public List<PlayerInfo> ReadyPlayers { get; } = new List<PlayerInfo>();

	/// <summary>
	/// Used to track who killed who. Could be used to check that a player actually killed someone themselves.
	/// </summary>
	public Dictionary<PlayerScript, List<PlayerScript>>
		KillTracker = new Dictionary<PlayerScript, List<PlayerScript>>();

	/// <summary>
	/// Records the last round player count
	/// </summary>
	public static int LastRoundPlayerCount = 0;

	private void Awake()
	{
		if (Instance == null)
		{
			Instance = this;
		}
		else
		{
			Destroy(gameObject);
		}
	}

	void OnEnable()
	{
		EventManager.AddHandler(Event.RoundStarted, OnRoundStart);
		EventManager.AddHandler(Event.RoundEnded, OnEndOfRound);
	}

	void OnDisable()
	{
		EventManager.RemoveHandler(Event.RoundStarted, OnRoundStart);
		EventManager.RemoveHandler(Event.RoundEnded, OnEndOfRound);
	}

	private void OnRoundStart()
	{
		PopulateRoundPlayers();
	}

	private void OnEndOfRound()
	{
		SetEndOfRoundPlayerCount();
		ClearRoundPlayers();
	}

	private void SetEndOfRoundPlayerCount()
	{
		LastRoundPlayerCount = Instance.ConnectionCount;
	}

	public override void OnStartServer()
	{
		base.OnStartServer();
		InitAdminController();
	}

	/// <summary>
	/// Called on the server when a kill is confirmed to track which players killed eachother.
	/// </summary>
	[Server]
	public void TrackKill(GameObject perpetrator, GameObject victim)
	{
		var perPlayer = perpetrator.OrNull()?.Player()?.Script;
		var victimPlayer = victim.OrNull()?.Player()?.Script;

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
	public List<PlayerInfo> GetPlayersOnMatrix(MatrixInfo matrix)
	{
		return InGamePlayers.FindAll(p => (p.Script != null) && p.Script.RegisterPlayer.Matrix.Id == matrix?.Id);
	}

	public PlayerInfo GetPlayerByID(string id)
	{
		foreach (var player in AllPlayers)
		{
			if (player.UserId == id) return player;
		}

		return null;
	}

	public List<PlayerInfo> GetAlivePlayers(List<PlayerInfo> players = null)
	{
		if (players == null)
		{
			players = InGamePlayers;
		}

		return players.FindAll(p => !p.Script.IsGhost && p.Script.playerMove.AllowInput);
	}

	/// Don't do this unless you realize the consequences
	[Server]
	public void Clear()
	{
		loggedIn.Clear();
	}

	/// <summary>
	/// Set this user's controlled game object to newGameObject (which may be a ghost or a body)
	/// </summary>
	/// <param name="conn">connection whose object should be updated</param>
	/// <param name="newGameObject">new game object they are controlling (should be a ghost or a body)</param>
	[Server]
	public void UpdatePlayer(NetworkConnection conn, GameObject newGameObject)
	{
		PlayerInfo connectedPlayer = GetOnline(conn);
		connectedPlayer.GameObject = newGameObject;
		CheckRcon();
	}

	[Server]
	public void UpdatePlayer(PlayerInfo connectedPlayer, GameObject newGameObject)
	{
		connectedPlayer.GameObject = newGameObject;
		CheckRcon();
	}

	/// <summary>
	/// Adds this connected player to the list, or updates an existing entry if there's already one for
	/// this player's networkconnection. Returns the ConnectedPlayer that was added or updated.
	/// </summary>
	/// <param name="player"></param>
	[Server]
	public PlayerInfo AddOrUpdate(PlayerInfo player)
	{
		if (player.Equals(PlayerInfo.Invalid))
		{
			Loggy.Log("Refused to add invalid connected player to this server's player list", Category.Connections);
			return player;
		}

		if (loggedOff.Contains(player))
		{
			loggedOff.Remove(player);
		}

		if (loggedIn.Contains(player))
		{
			return player;
		}

		Loggy.LogTrace($"Player {player.Username}'s client ID is: {player.ClientId} User ID: {player.UserId}.", Category.Connections);

		loggedIn.Add(player);
		Loggy.Log($"Player with account {player.UserId} has joined the game. Player count: {loggedIn.Count}.", Category.Connections);
		CheckRcon();
		return player;
	}

	[Server]
	private void TryMoveClientToOfflineList(PlayerInfo player)
	{
		if (!loggedIn.Contains(player))
		{
			Loggy.Log($"Player with name {player.Name} was not found in online player list. " +
			           "Verifying player lists for integrity...", Category.Connections);
			ValidatePlayerListRecords();
			return;
		}

		Loggy.Log($"Added {player.Name} to offline player list.", Category.Connections);
		loggedOff.Add(player);
		loggedIn.Remove(player);
		UpdateConnectedPlayersMessage.Send();
		CheckRcon();
	}

	[Server]
	public NetworkConnection GetRelatedNetworkConnection(GameObject _object)
	{
		try
		{
			if (_object == null) return null;
			foreach (var info in loggedIn)
			{
				if (info.ViewerScript.OrNull()?.gameObject == _object)
				{
					return info.Connection;
				}

				if (info.Mind != null && info.Mind.IsRelatedToObject(_object))
				{
					return info.Connection;
				}
			}

		}
		catch (Exception e)
		{
			Loggy.LogError(e.ToString());
			throw;
		}

		return null;
	}

	[Server]
	public bool Has(NetworkConnection connection)
	{
		return !GetOnline(connection).Equals(PlayerInfo.Invalid);
	}

	[Server]
	public PlayerInfo GetLoggedOffClient(string clientID, string userId)
	{
		var index = loggedOff.FindIndex(x => x.ClientId == clientID || x.UserId == userId);
		if (index != -1)
		{
			return loggedOff[index];
		}

		return null;
	}


	[Server]
	public PlayerInfo GetLoggedOnClient(string clientID, string userId)
	{
		var index = loggedIn.FindIndex(x => x.ClientId == clientID || x.UserId == userId);
		if (index != -1)
		{
			return loggedIn[index];
		}

		return null;
	}

	[Server]
	public bool Has(string characterName, string userId)
	{
		var character = GetByCharacter(characterName);
		if (character.Equals(PlayerInfo.Invalid)) return false;

		return character.UserId != userId;
	}

	[Server]
	public bool HasOnline(string characterName, string userId)
	{
		var character = GetOnlineByCharacter(characterName);
		if (character.Equals(PlayerInfo.Invalid)) return false;

		return character.UserId != userId;
	}

	[Server]
	public bool Has(GameObject gameObject)
	{
		return !Get(gameObject).Equals(PlayerInfo.Invalid);
	}

	[Server]
	public bool HasOnline(GameObject gameObject)
	{
		return !GetOnline(gameObject).Equals(PlayerInfo.Invalid);
	}

	[Server]
	public PlayerInfo Get(NetworkConnection byConnection)
	{
		return GetInternalAll(player => player.Connection == byConnection);
	}

	[Server]
	public PlayerInfo GetOnline(NetworkConnection byConnection)
	{
		return GetInternalLoggedOn(player => player.Connection == byConnection);
	}

	[Server]
	public PlayerInfo Get(GameObject byGameObject)
	{
		return GetInternalAll(player =>
		{
			if (player.GameObject == byGameObject) return true;
			if (player.Mind != null)
			{
				return player.Mind.IsRelatedToObject(byGameObject);
			}
			else
			{
				if (player.GameObject != null)
				{
					return player.GameObject == byGameObject;
				}
				else if (player.ViewerScript != null)
				{
					return player.ViewerScript.gameObject == byGameObject;
				}
			}

			return false;

		});
	}

	[Server]
	public PlayerInfo GetOnline(GameObject byGameObject)
	{
		return GetInternalLoggedOn(player => player.GameObject == byGameObject);
	}

	[Server]
	public bool TryGetByUserID(string userID, out PlayerInfo player)
	{
		player = GetInternalAll(player => player.UserId == userID);
		return player != null && player.Equals(PlayerInfo.Invalid) == false;
	}

	[Server]
	public bool TryGetOnlineByUserID(string userID, out PlayerInfo player)
	{
		player = GetInternalLoggedOn(player => player.UserId == userID);
		return player != null && player.Equals(PlayerInfo.Invalid) == false;
	}

	[Server]
	public PlayerInfo GetByCharacter(string characterName)
	{
		return GetInternalAll(player => player.Name == characterName);
	}

	[Server]
	public PlayerInfo GetOnlineByCharacter(string characterName)
	{
		return GetInternalLoggedOn(player => player.Name == characterName);
	}

	/// <summary>
	/// Get all players with specific state, logged in and logged off
	/// </summary>
	[Server]
	public List<PlayerInfo> GetAllByPlayersOfState(PlayerTypes type)
	{
		return GetAllPlayers().Where(player => player.Script.PlayerType == type).ToList();
	}

	/// <summary>
	/// Get all in game players, logged in and logged off
	/// </summary>
	[Server]
	public List<PlayerInfo> GetAllPlayers()
	{
		var players = InGamePlayers;
		players.AddRange(loggedOff.FindAll(player => player.Script != null));

		return players.ToList();
	}

	/// <summary>
	/// Check logged in and logged off players
	/// </summary>
	/// <param name="condition"></param>
	/// <returns></returns>
	private PlayerInfo GetInternalAll(Func<PlayerInfo, bool> condition)
	{
		var connectedPlayer = GetInternalLoggedOn(condition);

		if(connectedPlayer.Equals(PlayerInfo.Invalid))
		{
			connectedPlayer = GetInternalLoggedOff(condition);
		}

		return connectedPlayer;
	}

	/// <summary>
	/// Check logged in players
	/// </summary>
	/// <param name="condition"></param>
	/// <returns></returns>
	private PlayerInfo GetInternalLoggedOn(Func<PlayerInfo, bool> condition)
	{
		for (var i = 0; i < loggedIn.Count; i++)
		{
			if (condition(loggedIn[i]))
			{
				return loggedIn[i];
			}
		}

		return PlayerInfo.Invalid;
	}

	/// <summary>
	/// Check logged off players
	/// </summary>
	/// <param name="condition"></param>
	/// <returns></returns>
	private PlayerInfo GetInternalLoggedOff(Func<PlayerInfo, bool> condition)
	{
		for (var i = 0; i < loggedOff.Count; i++)
		{
			if (condition(loggedOff[i]))
			{
				return loggedOff[i];
			}
		}

		return PlayerInfo.Invalid;
	}

	[Server]
	public void Remove(PlayerInfo connectedPlayer)
	{

		if (loggedOff.Contains(connectedPlayer))
		{
			loggedOff.Remove(connectedPlayer);
		}

		if (loggedIn.Contains(connectedPlayer))
		{
			loggedIn.Remove(connectedPlayer);
		}

		Loggy.LogError($"Disconnecting player {connectedPlayer.Name} via Remove From playlist");
		connectedPlayer.Connection.Disconnect();


	}

	[Server]
	public void RemoveByConnection(NetworkConnection connection)
	{
		if (connection?.identity?.connectionToClient?.address == null || connection.identity == null)
		{
			Loggy.Log($"Unknown player disconnected: verifying playerlists for integrity - connection, its address and identity was null.", Category.Connections);
			ValidatePlayerListRecords();
			return;
		}

		var player = GetOnline(connection);
		if (player.Equals(PlayerInfo.Invalid))
		{
			Loggy.Log($"Unknown player disconnected: verifying playerlists for integrity - connected player was invalid. " +
			           $"IP: {connection?.identity?.connectionToClient?.address}. Name: {connection.identity.name}.", Category.Connections);
			ValidatePlayerListRecords();
			return;
		}

		SetPlayerReady(player, false);
		CheckForLoggedOffAdmin(player.UserId, player.Username);
		CheckForLoggedOffMentor(player.UserId, player.Username);
		TryMoveClientToOfflineList(player);
	}

	/// <summary>
	/// Verify the data of the player lists
	/// This is good to do if something unexpected has happened
	/// </summary>
	void ValidatePlayerListRecords()
	{
		//verify loggedIn clients:
		for (int i = loggedIn.Count - 1; i >= 0; i--)
		{
			if (loggedIn[i].Connection == null || loggedIn[i].Equals(PlayerInfo.Invalid))
			{
				TryMoveClientToOfflineList(loggedIn[i]);
			}
		}

		//verify loggedOff clients:
		for (int i = loggedOff.Count - 1; i >= 0; i--)
		{
			if (loggedOff[i].Equals(PlayerInfo.Invalid))
			{
				loggedOff.RemoveAt(i);
				continue;
			}

			if (loggedOff[i].GameObject == null)
			{
				loggedOff.RemoveAt(i);
				continue;
			}
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
	public GameObject TakeLoggedOffPlayerbyUserId(string userId)
	{
		Loggy.LogTraceFormat("Searching for logged off players with userId {0}", Category.Connections, userId);
		foreach (var player in loggedOff)
		{
			Loggy.LogTraceFormat("Found logged off player with userId {0}", Category.Connections, player.UserId);
			if (player.UserId == userId)
			{
				loggedOff.Remove(player);
				return player.GameObject;
			}
		}

		return null;
	}

	[Server]
	public PlayerInfo RemovePlayerbyUserId(string userId, PlayerInfo newPlayer)
	{
		Loggy.LogTraceFormat("Searching for players with userId: {0}", Category.Connections, userId);
		foreach (var player in loggedOff)
		{
			if ((player.UserId == userId))
			{
				Loggy.LogTraceFormat("Found player with userId {0} clientId: {1}", Category.Connections, player.UserId, player.ClientId);
				loggedOff.Remove(player);
				return player;
			}
		}
		foreach (var player in loggedIn)
		{
			if (PlayerManager.LocalViewerScript && PlayerManager.LocalViewerScript.gameObject == player.GameObject ||
			    PlayerManager.LocalPlayerObject == player.GameObject)
			{
				continue; //server player
			}

			if (GameData.Instance.OfflineMode)
			{
				if (serverAdmins.Contains(player.UserId)) continue; //Allow admins to multikey (local devs connecting multiple clients)
			}


			if (player.UserId == userId && newPlayer != player)
			{
				Loggy.LogError($"Disconnecting {player.Name} by RemovePlayerbyUserId ", Category.Connections);
				player.Connection.Disconnect(); //new client while online or dc timer not triggering yet
				loggedIn.Remove(player);
				return player;
			}
		}

		return null;
	}


	[Server]
	public PlayerInfo RemovePlayerbyClientId(string clientId, string userId, PlayerInfo newPlayer)
	{
		Loggy.LogTraceFormat("Searching for players with userId: {0} clientId: {1}", Category.Connections, userId, clientId);
		foreach (var player in loggedOff)
		{
			if ((player.ClientId == clientId || player.UserId == userId) && newPlayer != player)
			{
				Loggy.LogTraceFormat("Found player with userId {0} clientId: {1}", Category.Connections, player.UserId, player.ClientId);
				loggedOff.Remove(player);
				return player;
			}
		}
		foreach (var player in loggedIn)
		{
			if (PlayerManager.LocalViewerScript && PlayerManager.LocalViewerScript.gameObject == player.GameObject ||
			    PlayerManager.LocalPlayerObject == player.GameObject)
			{
				continue; //server player
			}

			if (serverAdmins.Contains(player.UserId)) continue; // Allow admins to multikey (local devs connecting multiple clients)

			if ((player.ClientId == clientId || player.UserId == userId) && newPlayer != player)
			{
				Loggy.LogError($"Disconnecting {player.Name} by RemovePlayerbyClientId ", Category.Connections);
				player.Connection.Disconnect(); //new client while online or dc timer not triggering yet
				loggedIn.Remove(player);
				return player;
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
		AccessFile.UnRegister(LoadCurrentAdmins);
		AccessFile.UnRegister(LoadCurrentMentors);
		AccessFile.UnRegister(LoadWhiteList);
	}

	/// <summary>
	/// Makes a player ready/unready for job allocations
	/// </summary>
	public void SetPlayerReady(PlayerInfo player, bool isReady, CharacterSheet charSettings = null)
	{
		if (isReady)
		{
			// Update connection with locked in job prefs
			if (charSettings != null)
			{
				charSettings.ValidateSpeciesCanBePlayerChosen(); //Probably a better way to do this but IDK
				player.RequestedCharacterSettings = charSettings;
			}
			else
			{
				Loggy.LogError($"{player.Username} was set to ready with NULL character settings:\n{player}", Category.Round);
			}

			ReadyPlayers.Add(player);
			Loggy.Log($"Set {player.Username} to ready with these character settings:\n{charSettings}", Category.Round);
		}
		else
		{
			ReadyPlayers.Remove(player);
			Loggy.Log($"Set {player.Username} to NOT ready!", Category.Round);
		}
	}

	/// <summary>
	/// Clears the list of ready players
	/// </summary>
	[Server]
	public void ClearReadyPlayers()
	{
		ReadyPlayers.Clear();
	}

	/// <summary>
	/// Clears the list of round players
	/// </summary>
	[Server]
	public void ClearRoundPlayers()
	{
		roundPlayers.Clear();
	}

	[Server]
	public void AddToRoundPlayers(PlayerInfo newPlayer)
	{
		if(roundPlayers.Contains(newPlayer)) return;

		roundPlayers.Add(newPlayer);
	}

	[Server]
	public void PopulateRoundPlayers()
	{
		foreach (var player in loggedIn)
		{
			AddToRoundPlayers(player);
		}
	}

	[Server]
	public bool IsAntag(GameObject playerObj)
	{
		var conn = Get(playerObj);
		if (conn == null || conn.Script == null || conn.Script.Mind == null) return false;
		return conn.Script.Mind.IsAntag;
	}

	public static bool HasAntagEnabled(AntagPrefsDict antagPrefs, Antagonist antag)
	{
		return !antag.ShowInPreferences ||
		       (antagPrefs.ContainsKey(antag.AntagName) && antagPrefs[antag.AntagName]);
	}

	public static bool HasAntagEnabled(PlayerInfo connectedPlayer, Antagonist antag)
	{
		if (connectedPlayer.RequestedCharacterSettings == null)
		{
			if (connectedPlayer.Script.characterSettings == null) return false;

			connectedPlayer.RequestedCharacterSettings = connectedPlayer.Script.characterSettings;
		}

		return !antag.ShowInPreferences ||
		       (connectedPlayer.RequestedCharacterSettings.AntagPreferences.ContainsKey(antag.AntagName)
		        && connectedPlayer.RequestedCharacterSettings.AntagPreferences[antag.AntagName]);
	}
}

[Serializable]/// Minimalistic connected player information that all clients can posess
public struct ClientConnectedPlayer
{
	public string UserName;
	public string Tag;
	public int PingToServer;

	//Used to make this ClientConnectedPlayer unique even if UserName and Tags are the same
	public int Index;
}
