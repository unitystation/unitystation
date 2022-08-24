using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

namespace Lobby
{
	public class HistoryLogEntry : MonoBehaviour
	{
		[SerializeField] private TMP_Text serverIPtext;
		private int indexInHistory = 0;

		public void SetData(string ip, int index)
		{

			serverIPtext.text = ip;
			indexInHistory = index;
			if (Application.isEditor)
			{
				serverIPtext.text = "localhost";
			}
		}

		public void OnJoinButton()
		{
			LobbyManager.Instance.lobbyDialogue.ConnectToServerFromHistory(indexInHistory);
		}
	}
}

