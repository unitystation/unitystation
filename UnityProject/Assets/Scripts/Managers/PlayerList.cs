using System;
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

	//For TDM demo
	public Dictionary<JobDepartment, int> departmentScores = new Dictionary<JobDepartment, int>();

	//For combat demo
	public Dictionary<string, int> playerScores = new Dictionary<string, int>();

	//For job formatting purposes
	private static readonly TextInfo textInfo = new CultureInfo("en-US", false).TextInfo;

	private void Awake()
	{
		if ( Instance == null )
		{
			Instance = this;
		}
		else
		{
			Destroy(gameObject);
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

		JobDepartment perpetratorDept = SpawnPoint.GetJobDepartment(perPlayer.Job);

		if ( !departmentScores.ContainsKey(perpetratorDept) )
		{
			departmentScores.Add(perpetratorDept, 0);
		}

		JobDepartment victimDept = SpawnPoint.GetJobDepartment(victimPlayer.Job);

		if ( perpetratorDept == victimDept )
		{
			departmentScores[perpetratorDept]--;
		}
		else
		{
			departmentScores[perpetratorDept]++;
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
		/*
		var scoreSort = playerScores.OrderByDescending(pair => pair.Value)
		    .ToDictionary(pair => pair.Key, pair => pair.Value);

		foreach (KeyValuePair<string, int> ps in scoreSort)
		{
		    UIManager.Chat.ReportToChannel("<b>" + ps.Key + "</b>  total kills:  <b>" + ps.Value + "</b>");
		}
		*/

		if ( departmentScores.Count == 0 )
		{
			PostToChatMessage.Send("Nobody killed anybody. Fucking hippies.", ChatChannel.System);
		}

		var scoreSort = departmentScores.OrderByDescending(pair => pair.Value)
			.ToDictionary(pair => pair.Key, pair => pair.Value);

		foreach ( KeyValuePair<JobDepartment, int> ds in scoreSort )
		{
			PostToChatMessage.Send("<b>" + ds.Key + "</b>  total kills:  <b>" + ds.Value + "</b>", ChatChannel.System);
		}

		PostToChatMessage.Send("Game Restarting in 10 seconds...", ChatChannel.System);
	}

	public void RefreshPlayerListText()
	{
		UIManager.Instance.playerListUIControl.nameList.text = "";
		foreach (var player in ClientConnectedPlayers)
		{
			string curList = UIManager.Instance.playerListUIControl.nameList.text;
			UIManager.Instance.playerListUIControl.nameList.text = $"{curList}{player.Name} ({PrepareJobString(player.Job)})\r\n";
		}
	}

	private static string PrepareJobString(JobType job)
	{
		return job.ToString().Equals("NULL") ? "*just joined" : textInfo.ToTitleCase(job.ToString().ToLower());
	}

	/// Don't do this unless you realize the consequences
	[Server]
	public void Clear()
	{
		values.Clear();
	}

	[Server]
	public void UpdatePlayer(NetworkConnection conn, GameObject newGameObject)
	{
		ConnectedPlayer connectedPlayer = Get(conn);
		connectedPlayer.GameObject = newGameObject;
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
			Debug.Log("Refused to add invalid connected player");
			return;
		}
		if ( ContainsConnection(player.Connection) )
		{
//			Debug.Log($"Updating {Get(player.Connection)} with {player}");
			ConnectedPlayer existingPlayer = Get(player.Connection);
			existingPlayer.GameObject = player.GameObject;
			existingPlayer.Name = player.Name; //Note that name won't be changed to empties/nulls
			existingPlayer.Job = player.Job;
			existingPlayer.SteamId = player.SteamId;
		}
		else
		{
			values.Add(player);
			Debug.Log($"Added {player}. Total:{values.Count}; {string.Join("; ",values)}");
			//Adding kick timer for new players only
			StartCoroutine(KickTimer(player));
		}
	}

	private IEnumerator KickTimer(ConnectedPlayer player)
	{
		if ( IsConnWhitelisted( player ) || !Managers.instance.isForRelease )
		{
//			Debug.Log( "Ignoring kick timer for invalid connection" );
			yield break;
		}
		int tries = 5;
		while ( !player.IsAuthenticated )
		{			
			if ( tries-- < 0 )
			{
				CustomNetworkManager.Kick( player, "Auth timed out" );
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
		Debug.Log($"Removed {player}");
		values.Remove(player);
		AddPrevious( player );
		NetworkServer.Destroy(player.GameObject);
		UpdateConnectedPlayersMessage.Send();
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
					Debug.Log( $"Returning old player {oldValues[i]}" );
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
			Debug.LogError($"Cannot remove by {connection}, not found");
		}
		else
		{
			TryRemove(connectedPlayer);
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