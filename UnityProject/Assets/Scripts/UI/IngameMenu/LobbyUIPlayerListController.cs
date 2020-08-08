using System.Collections;
using System.Collections.Generic;
using System;
using UnityEngine;
using TMPro;

public class LobbyUIPlayerListController : MonoBehaviour
{
	public TMP_Text title = null;
	public TMP_Text playerCount = null;

	public GameObject playerTemplate = null;

	private IDictionary<ClientConnectedPlayer, LobbyUIListTemplate> playerEntryList = new Dictionary<ClientConnectedPlayer, LobbyUIListTemplate>();

	public void GenerateList()
	{
		playerEntryList.Clear();

		var list = PlayerList.Instance.ClientConnectedPlayers;

		var count = list.Count;

		for (var i = 0; i < count; i++)
		{
			GameObject playerEntry = Instantiate(playerTemplate);//creates new button
			playerEntry.SetActive(true);
			var c = playerEntry.GetComponent<LobbyUIListTemplate>();
			c.playerName.text = list[i].UserName;
			c.playerNumber.text = i.ToString();
			playerEntryList.Add(list[i], c);

			playerEntry.transform.SetParent(playerTemplate.transform.parent, false);
		}
	}
}
