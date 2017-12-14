﻿using System.Collections.Generic;
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
        var checkName = name;

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
        foreach (var name in nameList)
        {
            var curList = UIManager.Instance.playerListUIControl.nameList.text;
            UIManager.Instance.playerListUIControl.nameList.text = curList + name + "\r\n";
        }
    }
}