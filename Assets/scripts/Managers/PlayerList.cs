using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UI;

public class PlayerList : MonoBehaviour
{
	public static PlayerList playerList;
	private Dictionary<string, GameObject> connectedPlayers = new Dictionary<string, GameObject>();

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

	public void AddPlayer(GameObject playerObj)
	{
		connectedPlayers.Add(playerObj.name, playerObj);
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
