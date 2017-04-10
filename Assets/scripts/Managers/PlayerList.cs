using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UI;
using UI;
using PlayGroup;

public class PlayerList : NetworkBehaviour
{
	public static PlayerList playerList;
	public SyncListString nameList = new SyncListString();
	public Dictionary<string, GameObject> connectedPlayers = new Dictionary<string, GameObject>();
    int numSameNames = 0;

	public static PlayerList Instance {
		get {
			if (!playerList) {
				playerList = FindObjectOfType<PlayerList>();
			}
			return playerList;
		}
	}

	public override void OnStartClient(){
		nameList.Callback = UpdateFromServer;
		RefreshPlayerListText();
		base.OnStartClient();
	}
	void UpdateFromServer(SyncListString.Operation op, int index){
		RefreshPlayerListText();
	}
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
		return checkName;
	}
		
	public void RemovePlayer(string playerName)
	{
		if (connectedPlayers.ContainsKey(playerName)) {
			connectedPlayers.Remove(playerName);
			nameList.Remove(playerName);
		}
	}

	public void RefreshPlayerListText()
	{
		UIManager.Instance.playerListUIControl.nameList.text = "";
		foreach (string name in nameList) {
			string curList = UIManager.Instance.playerListUIControl.nameList.text;
			UIManager.Instance.playerListUIControl.nameList.text = curList + name + "\r\n"; 
		}
	}
}
