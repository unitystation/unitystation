using System.Collections.Generic;
using System.Linq;
using PlayGroup;
using UI;
using UnityEngine;
using UnityEngine.Networking;

public class PlayerList : NetworkBehaviour
{
	//ConnectedPlayer list
	private static List<ConnectedPlayer> values = new List<ConnectedPlayer>();
	public List<ConnectedPlayer> Values => values;

	public static PlayerList Instance;


	//For TDM demo
	public Dictionary<JobDepartment, int> departmentScores = new Dictionary<JobDepartment, int>();

	public SyncListString nameList = new SyncListString();

	//For combat demo
	public Dictionary<string, int> playerScores = new Dictionary<string, int>();


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

//	//Check name on server
//	public string CheckName(string name)
//	{
//		string checkName = name;
//
//		while ( ContainsName(checkName) )
//		{
//			Debug.Log("NAME ALREADY EXISTS: " + checkName);
//			numSameNames++;
//			checkName = name + numSameNames;
//			Debug.Log("TRYING: " + checkName);
//		}
//
//		nameList.Add(checkName);
//		if ( !playerScores.ContainsKey(checkName) )
//		{
//			playerScores.Add(checkName, 0);
//		}
//
//		return checkName;
//	}

	//Called on the server when a kill is confirmed

	[Server]
	public void UpdateKillScore(GameObject perpetrator, GameObject victim)
	{
		if ( perpetrator == null )
		{
			return;
		}

		if ( playerScores.ContainsKey(perpetrator.name) )
		{
			playerScores[perpetrator.name]++;
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
		UIManager.Instance.playerListUIControl.nameList.text = "";
		foreach ( string name in nameList )
		{
			string curList = UIManager.Instance.playerListUIControl.nameList.text;
			UIManager.Instance.playerListUIControl.nameList.text = curList + name + "\r\n";
		}
	}

	/// <summary>
	/// Don't do this on server unless you realize the consequences
	/// </summary>
	public void Clear()
	{
		values.Clear();
	}

	public void UpdatePlayer(NetworkConnection conn, GameObject newGameObject)
	{
		ConnectedPlayer connectedPlayer = Get(conn);
		connectedPlayer.GameObject = newGameObject;
	}

	private void TryAdd(ConnectedPlayer player)
	{
		if ( player.Equals(InvalidConnectedPlayer) )
		{
			Debug.Log("Refused to add invalid connected player");
			return;
		}

		Debug.Log($"Added {player}. \nTotal: {values.Aggregate("",(s, connectedPlayer) => s + connectedPlayer + "; ")}");

		if ( ContainsConnection(player.Connection) )
		{
			values.Remove(Get(player.Connection));
		}

		values.Add(player);

//		//workaround I don't like, but placing TryAddName to ConnectedPlayer setter gives NRE on build
//		if ( player.Name != null )
//		{
//			TryAddName(player.Connection, player.Name);
//		}
	}


	private void TryRemove(ConnectedPlayer player)
	{
		Debug.Log($"Removed {player}");
		UpdateConnectedPlayersMessage.Send();
		values.Remove(player);
		nameList.Remove(player.Name);
	}

	/// Server-only!
	public void Add(ConnectedPlayer player) => TryAdd(player);

	//todo: rewrite CCP storage as a separate thing?
	///Client-only!
	public void Add(ClientConnectedPlayer player)
	{
		TryAdd(new ConnectedPlayer
		{
			Connection = InvalidConnectedPlayer.Connection,
			GameObject = InvalidConnectedPlayer.GameObject,
			Job = player.Job,
			Name = player.Name
		});
	}

	public bool ContainsConnection(NetworkConnection connection)
	{
		if ( !Get(connection).Equals(InvalidConnectedPlayer) )
		{
			return true;
		}

		return false;
	}

	public bool ContainsName(string name)
	{
		if ( !Get(name).Equals(InvalidConnectedPlayer) )
		{
			return true;
		}

		return false;
	}

	public bool ContainsGameObject(GameObject gameObject)
	{
		if ( !Get(gameObject).Equals(InvalidConnectedPlayer) )
		{
			return true;
		}

		return false;
	}

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

	public int ConnectionCount => values.Count;
	public int PlayerCount => values.FindAll(player => player.GameObject != null).Count;

	//filling a struct without connections and gameobjects for clients
	public List<ClientConnectedPlayer> DiscreetPlayerList =>
		values.Aggregate(new List<ClientConnectedPlayer>(), (list, player) =>
		{
			list.Add(new ClientConnectedPlayer {Name = player.Name, Job = player.Job});
			return list;
		});

	public static readonly ConnectedPlayer InvalidConnectedPlayer = new ConnectedPlayer
	{
		Connection = new NetworkConnection(),
		GameObject = null,
		Name = "kek",
		Job = JobType.NULL
	};
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
		set { TryAddName(value); }
	}

	public JobType Job
	{
		get { return job; }
		set { job = value; }
	}

	private void TryAddName(string playerName)
	{
		int numSameNames = 0;

		var playerList = PlayerList.Instance;

		if ( playerList == null )
		{
			Debug.LogWarning("PlayerList not instantiated, setting name blindly");
			name = playerName;
			return;
		}
		
		while ( PlayerList.Instance.ContainsName(playerName) )
		{
			Debug.Log("NAME ALREADY EXISTS: " + playerName);
			numSameNames++;
			playerName = playerName + numSameNames;
			Debug.Log("TRYING: " + playerName);
		}

		PlayerList.Instance.nameList.Add(playerName);
		if ( !PlayerList.Instance.playerScores.ContainsKey(playerName) )
		{
			PlayerList.Instance.playerScores.Add(playerName, 0);
		}

		name = playerName;
	}

	public override string ToString()
	{
		return $"{nameof( Connection )}: {Connection}, {nameof( GameObject )}: {GameObject}, {nameof( Name )}: {Name}, {nameof( Job )}: {Job}";
	}
}

public struct ClientConnectedPlayer
{
	public string Name;
	public JobType Job;
}