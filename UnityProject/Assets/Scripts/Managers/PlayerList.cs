using System.Collections.Generic;
using PlayGroup;
using UI;
using UnityEngine;
using UnityEngine.Networking;

public class PlayerList : NetworkBehaviour
{
	public static PlayerList Instance;

	public Dictionary<string, GameObject> connectedPlayers = new Dictionary<string, GameObject>();
	public SyncListString nameList = new SyncListString();

	private int numSameNames;

    //For combat demo
    public Dictionary<string, int> playerScores = new Dictionary<string, int>();
    
    //For TDM demo
    public Dictionary<Department, int> departmentScores = new Dictionary<Department, int>();

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
    public void UpdateKillScore(GameObject player)
    {
        if (player != null && playerScores.ContainsKey(player.name))
        {
            playerScores[player.name]++;
        }
        if (player == null)
        {
            return;
        }
        
        Department dept = SpawnPoint.GetJobDepartment(player.GetComponent<PlayerScript>().JobType);

        if (!departmentScores.ContainsKey(dept))
        {
            departmentScores = new Dictionary<Department, int>();
            departmentScores[dept] = 0;
        }

        departmentScores[dept]++;

        foreach (KeyValuePair<Department, int> score in departmentScores)
        {
            Debug.Log(score.Key + " score is " + score.Value);
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