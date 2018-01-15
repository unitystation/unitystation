using System.Collections.Generic;
using System.Linq;
using PlayGroup;
using UI;
using UnityEngine;
using UnityEngine.Networking;

public struct ConnectedPlayers
{
	public List<ConnectedPlayer> Values;

	public bool ContainsConnection(NetworkConnection connection)
	{
		if ( !GetByConnection(connection).Equals(InvalidConnectedPlayer) )
		{
			return true;
		}
		return false;
	}
	public bool ContainsName(string name)
	{
		if ( !GetByName(name).Equals(InvalidConnectedPlayer) )
		{
			return true;
		}
		return false;
	}
	public bool ContainsGameObject(GameObject gameObject)
	{
		if ( !GetByGameObject(gameObject).Equals(InvalidConnectedPlayer) )
		{
			return true;
		}
		return false;
	}

	public ConnectedPlayer GetByConnection(NetworkConnection connection)
	{
		for ( var i = 0; i < Values.Count; i++ )
		{
			if ( Values[i].Connection == connection )
			{
				return Values[i];
			}
		}
		return InvalidConnectedPlayer;
	}
	public ConnectedPlayer GetByName(string name)
	{
		for ( var i = 0; i < Values.Count; i++ )
		{
			if ( Values[i].Name == name )
			{
				return Values[i];
			}
		}
		return InvalidConnectedPlayer;
	}
	public ConnectedPlayer GetByGameObject(GameObject gameObject)
	{
		for ( var i = 0; i < Values.Count; i++ )
		{
			if ( Values[i].GameObject == gameObject )
			{
				return Values[i];
			}
		}
		return InvalidConnectedPlayer;
	}

	public void RemoveByConnection(NetworkConnection connection)
	{
		ConnectedPlayer connectedPlayer = GetByConnection(connection);
		if ( connectedPlayer.Equals(InvalidConnectedPlayer) )
		{
			Debug.LogError($"Cannot remove by {connection}, not found");
		}
		else
		{
			Values.Remove(connectedPlayer);
		}
	}
	public void RemoveByName(string name)
	{
		ConnectedPlayer connectedPlayer = GetByName(name);
		if ( connectedPlayer.Equals(InvalidConnectedPlayer) )
		{
			Debug.LogError($"Cannot remove by {name}, not found");
		}
		else
		{
			Values.Remove(connectedPlayer);
		}
	}
	public void RemoveByGameObject(GameObject gameObject)
	{
		ConnectedPlayer connectedPlayer = GetByGameObject(gameObject);
		if ( connectedPlayer.Equals(InvalidConnectedPlayer) )
		{
			Debug.LogError($"Cannot remove by {gameObject}, not found");
		}
		else
		{
			Values.Remove(connectedPlayer);
		}
	}
	
	public static ConnectedPlayer InvalidConnectedPlayer = new ConnectedPlayer
	{
		Connection = new NetworkConnection(),
		GameObject = new GameObject(),
		Name = "kek"
	};
}

public struct ConnectedPlayer
{
	public NetworkConnection Connection;
	public GameObject GameObject;
	public string Name;
}

public class PlayerList : NetworkBehaviour
{
	public static PlayerList Instance;

	public ConnectedPlayers connectedPlayers;

	//For TDM demo
	public Dictionary<JobDepartment, int> departmentScores = new Dictionary<JobDepartment, int>();

	public SyncListString nameList = new SyncListString();

	private int numSameNames;

	//For combat demo
	public Dictionary<string, int> playerScores = new Dictionary<string, int>();

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

	//Check name on server
	public string CheckName(string name)
	{
		string checkName = name;

		while (connectedPlayers.ContainsName(checkName))
		{
			Debug.Log("NAME ALREADY EXISTS: " + checkName);
			numSameNames++;
			checkName = name + numSameNames;
			Debug.Log("TRYING: " + checkName);
		}
		nameList.Add(checkName);
		if (!playerScores.ContainsKey(checkName))
		{
			playerScores.Add(checkName, 0);
		}
		return checkName;
	}

	//Called on the server when a kill is confirmed
	[Server]
	public void UpdateKillScore(GameObject perpetrator, GameObject victim)
	{
		if (perpetrator == null)
		{
			return;
		}

		if (playerScores.ContainsKey(perpetrator.name))
		{
			playerScores[perpetrator.name]++;
		}

		JobType perpetratorJob = perpetrator.GetComponent<PlayerScript>().JobType;
		JobDepartment perpetratorDept = SpawnPoint.GetJobDepartment(perpetratorJob);

		if (!departmentScores.ContainsKey(perpetratorDept))
		{
			departmentScores.Add(perpetratorDept, 0);
		}

		if (victim == null)
		{
			return;
		}

		JobType victimJob = victim.GetComponent<PlayerScript>().JobType;
		JobDepartment victimDept = SpawnPoint.GetJobDepartment(victimJob);

		if (perpetratorDept == victimDept)
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

		if (departmentScores.Count == 0)
		{
			PostToChatMessage.Send("Nobody killed anybody. Fucking hippies.", ChatChannel.System);
		}

		var scoreSort = departmentScores.OrderByDescending(pair => pair.Value)
			.ToDictionary(pair => pair.Key, pair => pair.Value);

		foreach (KeyValuePair<JobDepartment, int> ds in scoreSort)
		{
			PostToChatMessage.Send("<b>" + ds.Key + "</b>  total kills:  <b>" + ds.Value + "</b>", ChatChannel.System);
		}

		PostToChatMessage.Send("Game Restarting in 10 seconds...", ChatChannel.System);
	}

	public void RemovePlayer(string playerName)
	{
		if (connectedPlayers.ContainsName(playerName))
		{
			connectedPlayers.RemoveByName(playerName);
			nameList.Remove(playerName);
		}
	}

	public void RefreshPlayerListText()
	{
		UIManager.Instance.playerListUIControl.nameList.text = "";
		foreach (string name in nameList)
		{
			string curList = UIManager.Instance.playerListUIControl.nameList.text;
			UIManager.Instance.playerListUIControl.nameList.text = curList + name + "\r\n";
		}
	}
}