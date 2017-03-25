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
	private Dictionary<string, GameObject> connectedPlayers = new Dictionary<string, GameObject>();
    int numSameNames = 0;

	public static PlayerList Instance {
		get {
			if (!playerList) {
				playerList = FindObjectOfType<PlayerList>();
			}
			return playerList;
		}
	}

	void Start(){
		RefreshPlayerListText();
	}
        
    public void AddPlayer(GameObject playerObj, string name)
	{
        string checkName = name;
     
            while (connectedPlayers.ContainsKey(checkName))
            {
            numSameNames++;
            checkName = name + numSameNames.ToString();
            }
    
        CmdUpdateHeirarchy(playerObj, checkName);

	}
    [Command]
    void CmdUpdateHeirarchy(GameObject playerObj, string name){
        playerObj.GetComponent<PlayerScript>().playerName = name;
        playerObj.name = name;
        connectedPlayers.Add(name, playerObj);
        playerObj.transform.parent = this.gameObject.transform;
        RefreshPlayerListText();
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
