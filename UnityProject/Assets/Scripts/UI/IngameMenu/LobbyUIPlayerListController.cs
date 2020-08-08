using System.Collections;
using System.Collections.Generic;
using System;
using UnityEngine;
using TMPro;
using ServerInfo;

public class LobbyUIPlayerListController : MonoBehaviour
{
	public TMP_Text title = null;
	public TMP_Text playerCount = null;

	public GameObject playerTemplate = null;

	private ServerInfoUI serverInfoUi;

	private IDictionary<ClientConnectedPlayer, LobbyUIListTemplate> playerEntryList = new Dictionary<ClientConnectedPlayer, LobbyUIListTemplate>();

	public void GenerateList()
	{
		foreach (var entry in playerEntryList)
		{
			Destroy(entry.Value.gameObject);
		}

		playerEntryList.Clear();

		var list = PlayerList.Instance.ClientConnectedPlayers;

		var count = list.Count;

		playerCount.text = $"Player Count: {count}";

		title.text = "Player List";

		for (var i = 0; i < count; i++)
		{
			var player = list[i];
			GameObject playerEntry = Instantiate(playerTemplate);//creates new button
			playerEntry.SetActive(true);
			var c = playerEntry.GetComponent<LobbyUIListTemplate>();
			c.playerName.text = $"{player.Tag} {player.UserName}";
			c.playerNumber.text = i.ToString();
			playerEntryList.Add(player, c);

			playerEntry.transform.SetParent(playerTemplate.transform.parent, false);
		}
	}
}
