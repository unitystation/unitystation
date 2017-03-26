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
		RefreshPlayerListText();
		base.OnStartClient();
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
		return checkName;
	}
		
	public void RemovePlayer(string playerName)
	{
		if (connectedPlayers.ContainsKey(playerName)) {
			connectedPlayers.Remove(playerName);
			RefreshPlayerListText();
		}
	}

	public void RefreshPlayerListText()
	{
		UIManager.Instance.playerListUIControl.nameList.text = "";
		foreach (KeyValuePair<string,GameObject> player in connectedPlayers) {
			string curList = UIManager.Instance.playerListUIControl.nameList.text;
			UIManager.Instance.playerListUIControl.nameList.text = curList + player.Value.name + "\r\n"; 
		}
	}
}
