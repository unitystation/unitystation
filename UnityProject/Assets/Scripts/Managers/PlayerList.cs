using System.Collections.Generic;
using System.Linq;
using PlayGroup;
using UI;
using UnityEngine;
using UnityEngine.Networking;

/// Has limited scope for clients, sweet things are mostly for server
public class PlayerList : NetworkBehaviour
{
	//FIXME: nameList weirdness upon restart; recheck ClientConnectedPlayers contents and killing name integrity
	//ConnectedPlayer list, server only
	private static List<ConnectedPlayer> values = new List<ConnectedPlayer>();
	//For server needs, useless for client
	public List<ConnectedPlayer> Values => values;
	//For client needs: updated via UpdateConnectedPlayersMessage, useless for server
	public List<ClientConnectedPlayer> ClientConnectedPlayers = new List<ClientConnectedPlayer>();
	//For client needs: synced via uNet magic. Why need both? -idk
	public SyncListString nameList = new SyncListString();

	public static PlayerList Instance;
	public int ConnectionCount => values.Count;
	public int PlayerCount => values.FindAll(player => player.GameObject != null).Count;

	//For TDM demo
	public Dictionary<JobDepartment, int> departmentScores = new Dictionary<JobDepartment, int>();

	//For combat demo
	public Dictionary<NetworkConnection, int> playerScores = new Dictionary<NetworkConnection, int>();

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

	public override void OnStartClient()
	{
		nameList.Callback = UpdateFromServer;
		RefreshPlayerListText();
		base.OnStartClient();
	}

	private void UpdateFromServer(SyncList<string>.Operation op, int index)
	{
		RefreshPlayerListText();
	}


	//Called on the server when a kill is confirmed
	[Server]
	public void UpdateKillScore(GameObject perpetrator, GameObject victim)
	{
		if ( perpetrator == null )
		{
			return;
		}

		NetworkConnection playerConnection = Get(perpetrator).Connection;
		if ( playerScores.ContainsKey(playerConnection) )
		{
			playerScores[playerConnection]++;
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
		//TODO: Add server announcement messages
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

	[Server]
	public void RemovePlayer(NetworkConnection conn)
	{
		if ( ContainsConnection(conn) )
		{
			Remove(conn);
			nameList.Remove(conn.playerControllers[0].gameObject.name);
		}
	}

	public void RefreshPlayerListText()
	{
		UIManager.Instance.playerListUIControl.nameList.text = "nameList:\r\n";
		foreach ( string name in nameList )
		{
			UIManager.Instance.playerListUIControl.nameList.text += name + "\r\n";
		}
		UIManager.Instance.playerListUIControl.nameList.text += "ccpList:\r\n";
		foreach (var player in ClientConnectedPlayers)
		{
			string curList = UIManager.Instance.playerListUIControl.nameList.text;
			UIManager.Instance.playerListUIControl.nameList.text = $"{curList}{player.Name} ({player.Job})\r\n";
		}
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

	public static readonly ConnectedPlayer InvalidConnectedPlayer = new ConnectedPlayer
	{
		Connection = new NetworkConnection(),
		GameObject = null,
		Name = "kek",
		Job = JobType.NULL
	};
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
			Debug.Log($"Updating {Get(player.Connection)} with {player}");
			var existingPlayer = Get(player.Connection);
			existingPlayer.GameObject = player.GameObject;
			existingPlayer.Name = player.Name; //Note that name won't be changed to empties/nulls
			existingPlayer.Job = player.Job;
		}
		else
		{
			values.Add(player);
			if ( !playerScores.ContainsKey(player.Connection) )
			{
				playerScores.Add(player.Connection, 0);
			}
			Debug.Log($"Added {player}. Total:{values.Count}; {string.Join("; ",values)}");
		}
		
	}

	[Server]
	private void TryRemove(ConnectedPlayer player)
	{
		Debug.Log($"Removed {player}");
		UpdateConnectedPlayersMessage.Send();
		values.Remove(player);
		nameList.Remove(player.Name);
	}

	[Server]
	public void Add(ConnectedPlayer player) => TryAdd(player);

	public bool ContainsConnection(NetworkConnection connection)
	{
		if ( !Get(connection).Equals(InvalidConnectedPlayer) )
		{
			return true;
		}

		return false;
	}
	[Server]
	public bool ContainsName(string name)
	{
		if ( !Get(name).Equals(InvalidConnectedPlayer) )
		{
			return true;
		}

		return false;
	}
	[Server]
	public bool ContainsGameObject(GameObject gameObject)
	{
		if ( !Get(gameObject).Equals(InvalidConnectedPlayer) )
		{
			return true;
		}

		return false;
	}
	[Server]
	public ConnectedPlayer Get(NetworkConnection connection)
	{
		for ( var i = 0; i < values.Count; i++ )
		{
			if ( values[i].Connection == connection )
			{
				return values[i];
			}
		}

		return InvalidConnectedPlayer;
	}
	[Server]
	public ConnectedPlayer Get(string name)
	{
		for ( var i = 0; i < values.Count; i++ )
		{
			if ( values[i].Name == name )
			{
				return values[i];
			}
		}

		return InvalidConnectedPlayer;
	}
	[Server]
	public ConnectedPlayer Get(GameObject gameObject)
	{
		for ( var i = 0; i < values.Count; i++ )
		{
			if ( values[i].GameObject == gameObject )
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
	[Server]
	public void Remove(string name)
	{
		ConnectedPlayer connectedPlayer = Get(name);
		if ( connectedPlayer.Equals(InvalidConnectedPlayer) )
		{
			Debug.LogError($"Cannot remove by {name}, not found");
		}
		else
		{
			TryRemove(connectedPlayer);
		}
	}
	[Server]
	public void Remove(GameObject gameObject)
	{
		ConnectedPlayer connectedPlayer = Get(gameObject);
		if ( connectedPlayer.Equals(InvalidConnectedPlayer) )
		{
			Debug.LogError($"Cannot remove by {gameObject}, not found");
		}
		else
		{
			TryRemove(connectedPlayer);
		}
	}
}

public class ConnectedPlayer
{
	private NetworkConnection connection;
	private GameObject gameObject;
	private string name;
	private JobType job;

	public NetworkConnection Connection
	{
		get { return connection; }
		set { connection = value; }
	}

	public GameObject GameObject
	{
		get { return gameObject; }
		set { gameObject = value; }
	}

	public string Name
	{
		get { return name; }
		set { TryChangeName(value); }
	}

	public JobType Job
	{
		get { return job; }
		set { job = value; }
	}

	private void TryChangeName(string playerName)
	{
		if ( playerName == null || playerName.Equals("") || name == playerName )
		{
			return;
		}
		var playerList = PlayerList.Instance;
		if ( playerList == null )
		{
			Debug.LogWarning("PlayerList not instantiated, setting name blindly");
			name = playerName;
			return;
		}

		int numSameNames = 0;
		while ( PlayerList.Instance.ContainsName(playerName) )
		{
			Debug.Log($"NAME ALREADY EXISTS: {playerName}");
			numSameNames++;
			playerName = playerName + numSameNames;
			Debug.Log($"TRYING: {playerName}");
		}

		PlayerList.Instance.nameList.Remove(name);
		PlayerList.Instance.nameList.Add(playerName);


		name = playerName;
		if ( CustomNetworkManager.Instance != null && CustomNetworkManager.Instance._isServer )
		{
			UpdateConnectedPlayersMessage.Send();
		}
	}

	public override string ToString()
	{
		return $"[conn={Connection.connectionId}|go={GameObject}|name='{Name}'|job={Job}]";
	}
}

public struct ClientConnectedPlayer
{
	public string Name;
	public JobType Job;
}