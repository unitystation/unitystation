using System.Collections.Generic;
using System.Linq;
using PlayGroup;
using UI;
using UnityEngine;
using UnityEngine.Networking;

public class PlayerList : NetworkBehaviour
{
	public static PlayerList Instance;

	public Dictionary<string, GameObject> connectedPlayers = new Dictionary<string, GameObject>();

	//For TDM demo
	public Dictionary<Department, int> departmentScores = new Dictionary<Department, int>();

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

		while (connectedPlayers.ContainsKey(checkName))
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
		Department perpetratorDept = SpawnPoint.GetJobDepartment(perpetratorJob);

		if (!departmentScores.ContainsKey(perpetratorDept))
		{
			departmentScores.Add(perpetratorDept, 0);
		}

		if (victim == null)
		{
			return;
		}

		JobType victimJob = victim.GetComponent<PlayerScript>().JobType;
		Department victimDept = SpawnPoint.GetJobDepartment(victimJob);

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

		foreach (KeyValuePair<Department, int> ds in scoreSort)
		{
			PostToChatMessage.Send("<b>" + ds.Key + "</b>  total kills:  <b>" + ds.Value + "</b>", ChatChannel.System);
		}

		PostToChatMessage.Send("Game Restarting in 10 seconds...", ChatChannel.System);
	}

	public void RemovePlayer(string playerName)
	{
		if (connectedPlayers.ContainsKey(playerName))
		{
			connectedPlayers.Remove(playerName);
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