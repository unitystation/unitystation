using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using PlayGroup;
using UI;
using UnityEngine;
using UnityEngine.Networking;

/// Comfy place to get players and their info (preferably via their connection)
/// Has limited scope for clients (ClientConnectedPlayers only), sweet things are mostly for server
public class PlayerList : NetworkBehaviour
{
	//ConnectedPlayer list, server only
	private static List<ConnectedPlayer> values = new List<ConnectedPlayer>();
	
	//For client needs: updated via UpdateConnectedPlayersMessage, useless for server
	public List<ClientConnectedPlayer> ClientConnectedPlayers = new List<ClientConnectedPlayer>();

	public static PlayerList Instance;
	public int ConnectionCount => values.Count;
	public int PlayerCount => values.FindAll(player => player.GameObject != null).Count;

	//For TDM demo
	public Dictionary<JobDepartment, int> departmentScores = new Dictionary<JobDepartment, int>();

	//For combat demo
	public Dictionary<string, int> playerScores = new Dictionary<string, int>();

	//For job formatting purposes

	private static readonly TextInfo textInfo = new CultureInfo("en-US", false).TextInfo;

	public static readonly ConnectedPlayer InvalidConnectedPlayer = new ConnectedPlayer
	{
		Connection = new NetworkConnection(),
		GameObject = null,
		Name = "kek",
		Job = JobType.NULL
	};

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

		var playerName = Get(perpetrator).Name;
		if ( playerScores.ContainsKey(playerName) )
		{
			playerScores[playerName]++;
		}

		JobType perpetratorJob = perpetrator.GetComponent<PlayerScript>().JobType;
		JobDepartment perpetratorDept = SpawnPoint.GetJobDepartment(perpetratorJob);

		if ( !departmentScores.ContainsKey(perpetratorDept) )
		{
			departmentScores.Add(perpetratorDept, 0);
		}

		if ( victim == null )
		{
			return;
		}

		JobType victimJob = victim.GetComponent<PlayerScript>().JobType;
		JobDepartment victimDept = SpawnPoint.GetJobDepartment(victimJob);

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
		return job.ToString().Equals("NULL") ? "Just joined" : textInfo.ToTitleCase(job.ToString().ToLower());
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

	//filling a struct without connections and gameobjects for client's ClientConnectedPlayers list
	public List<ClientConnectedPlayer> ClientConnectedPlayerList =>
		values.Aggregate(new List<ClientConnectedPlayer>(), (list, player) =>
		{
			//not including headless server player
			if ( !GameData.IsHeadlessServer || player.Connection != InvalidConnectedPlayer.Connection )
			{
				list.Add(new ClientConnectedPlayer {Name = player.Name, Job = player.Job});
			}
			return list;
		});

	[Server]
	private void TryAdd(ConnectedPlayer player)
	{
		if ( player.Equals(InvalidConnectedPlayer) )
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
		}
		else
		{
			values.Add(player);

			Debug.Log($"Added {player}. Total:{values.Count}; {string.Join("; ",values)}");
		}
		
	}

	[Server]
	private void TryRemove(ConnectedPlayer player)
	{
		Debug.Log($"Removed {player}");
		values.Remove(player);
		NetworkServer.Destroy(player.GameObject);
		UpdateConnectedPlayersMessage.Send();
	}

	[Server]
	public void Add(ConnectedPlayer player) => TryAdd(player);

	public bool ContainsConnection(NetworkConnection connection)
	{
		return !Get(connection).Equals(InvalidConnectedPlayer);
	}
	
	[Server]
	public bool ContainsName(string name)
	{
		return !Get(name).Equals(InvalidConnectedPlayer);
	}
	
	[Server]
	public bool ContainsGameObject(GameObject gameObject)
	{
		return !Get(gameObject).Equals(InvalidConnectedPlayer);
	}
	
	[Server]
	public ConnectedPlayer Get(NetworkConnection byConnection)
	{
		return getInternal(player => player.Connection == byConnection);
	}
	
	[Server]
	public ConnectedPlayer Get(string byName)
	{
		return getInternal(player => player.Name == byName);
	}
	
	[Server]
	public ConnectedPlayer Get(GameObject byGameObject)
	{
		return getInternal(player => player.GameObject == byGameObject);
	}

	private ConnectedPlayer getInternal(Func<ConnectedPlayer,bool> condition)
	{
		for ( var i = 0; i < values.Count; i++ )
		{
			if ( condition(values[i]) )
			{
				return values[i];
			}
		}

		return InvalidConnectedPlayer;
	}

	[Server]
	public void Remove(NetworkConnection connection)
	{
		ConnectedPlayer connectedPlayer = Get(connection);
		if ( connectedPlayer.Equals(InvalidConnectedPlayer) )
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

/// Server-only full player information class
public class ConnectedPlayer
{
	private string name;
	private JobType job;

	public NetworkConnection Connection { get; set; }

	public GameObject GameObject { get; set; }

	public string Name
	{
		get { return name; }
		set
		{
			TryChangeName(value);
			TrySendUpdate();
		}
	}

	public JobType Job
	{
		get { return job; }
		set
		{
			job = value;
			TrySendUpdate();
		}
	}

	public bool HasNoName()
	{
		return name == null || name.Trim().Equals("");
	}

	private void TryChangeName(string playerName)
	{
		if ( playerName == null || playerName.Trim().Equals("") || name == playerName )
		{
			return;
		}
		var playerList = PlayerList.Instance;
		if ( playerList == null )
		{
//			Debug.Log("PlayerList not instantiated, setting name blindly");
			name = playerName;
			return;
		}

		string uniqueName = GetUniqueName(playerName);
		name = uniqueName;
		
		if ( !playerList.playerScores.ContainsKey(uniqueName) )
		{
			playerList.playerScores.Add(uniqueName, 0);
		}
	}

	/// Generating a unique name (Player -> Player2 -> Player3 ...)
	private static string GetUniqueName(string name, int sameNames = 0)
	{
		string proposedName = name;
		if ( sameNames != 0 )
		{
			proposedName = $"{name}{sameNames + 1}";
			Debug.Log($"TRYING: {proposedName}");
		}
		if ( PlayerList.Instance.ContainsName(proposedName) )
		{
			Debug.Log($"NAME ALREADY EXISTS: {proposedName}");
			sameNames++;
			return GetUniqueName(name, sameNames);
		}

		return proposedName;
	}

	private static void TrySendUpdate()
	{
		if ( CustomNetworkManager.Instance != null && CustomNetworkManager.Instance._isServer && PlayerList.Instance != null )
		{
			UpdateConnectedPlayersMessage.Send();
		}
	}

	public override string ToString()
	{
		return $"[conn={Connection.connectionId}|go={GameObject.name}|name='{Name}'|job={Job}]";
	}
}