using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;
using UI;
using System.Linq;

public class PlayerList : NetworkBehaviour
{
    public static PlayerList playerList;
    public SyncListString nameList = new SyncListString();
    public Dictionary<string, GameObject> connectedPlayers = new Dictionary<string, GameObject>();
    //For combat demo
    public Dictionary<string, int> playerScores = new Dictionary<string, int>();
    int numSameNames = 0;

    public static PlayerList Instance
    {
        get
        {
            if (!playerList)
            {
                playerList = FindObjectOfType<PlayerList>();
            }
            return playerList;
        }
    }

    public override void OnStartClient()
    {
        nameList.Callback = UpdateFromServer;
        RefreshPlayerListText();
        base.OnStartClient();
    }

    void UpdateFromServer(SyncListString.Operation op, int index)
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
            checkName = name + numSameNames.ToString();
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
    public void UpdateKillScore(string playerName)
    {
        if (playerName != null && playerScores.ContainsKey(playerName))
        {
            playerScores[playerName]++;
        }
    }

    [Server]
    public void ReportScores()
    {

        var scoreSort = playerScores.OrderByDescending(pair => pair.Value)
            .ToDictionary(pair => pair.Key, pair => pair.Value);

        foreach (KeyValuePair<string, int> ps in scoreSort)
        {
            UIManager.Chat.ReportToChannel("<b>" + ps.Key + "</b>  total kills:  <b>" + ps.Value + "</b>");
        }

        UIManager.Chat.ReportToChannel("Game Restarting in 10 seconds...");
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
