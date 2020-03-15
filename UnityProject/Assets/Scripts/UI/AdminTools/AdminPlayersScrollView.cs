using System;
using System.Collections;
using System.Collections.Generic;
using DatabaseAPI;
using UnityEngine;
using UnityEngine.Events;

namespace AdminTools
{
	public class AdminPlayersScrollView : MonoBehaviour
	{
		[SerializeField] private Transform playerListContent;
		[SerializeField] private GameObject playerEntryPrefab;
		private float refreshTime = 5f;
		private float currentCount = 0f;

		//Loaded playerEntries
		private List<AdminPlayerEntry> playerEntries = new List<AdminPlayerEntry>();

		public OnSelectPlayerEvent OnSelectPlayer;

		public string SelectedPlayer { get; private set; }

		private void OnEnable()
		{
			RefreshPlayerList();
			UpdateManager.Add(CallbackType.UPDATE, UpdateMe);
		}

		private void OnDisable()
		{
			UpdateManager.Remove(CallbackType.UPDATE, UpdateMe);
		}

		void UpdateMe()
		{
			currentCount += Time.deltaTime;
			if (currentCount > refreshTime)
			{
				RefreshPlayerList();
			}
		}

		void RefreshPlayerList()
		{
			RequestAdminPlayerList.Send(ServerData.UserID, PlayerList.Instance.AdminToken);
		}

		public void ReceiveUpdatedPlayerList(AdminPlayersList playerList)
		{
			RefreshOnlinePlayerList(playerList);
		}

		void RefreshOnlinePlayerList(AdminPlayersList playerList)
		{
			foreach (var p in playerList.players)
			{
				var index = playerEntries.FindIndex(x => x.PlayerData.uid == p.uid);
				if (index != -1)
				{
					playerEntries[index].UpdateButton(p, SelectPlayerInList);
				}
				else
				{
					var e = Instantiate(playerEntryPrefab, playerListContent);
					var entry = e.GetComponent<AdminPlayerEntry>();
					entry.UpdateButton(p, SelectPlayerInList);
					playerEntries.Add(entry);
					index = playerEntries.Count - 1;
				}

				if (!p.isOnline)
				{
					playerEntries[index].transform.SetAsLastSibling();
				}
			}

			if (string.IsNullOrEmpty(SelectedPlayer))
			{
				SelectPlayerInList(playerEntries[0]);
			}
		}

		void SelectPlayerInList(AdminPlayerEntry selectedEntry)
		{
			foreach (var p in playerEntries)
			{
				if (p != selectedEntry)
				{
					p.DeselectPlayer();
				}
				else
				{
					p.SelectPlayer();
					SelectedPlayer = selectedEntry.PlayerData.uid;
				}
			}

			SelectedPlayer = selectedEntry.PlayerData.uid;
			if(OnSelectPlayer != null) OnSelectPlayer.Invoke(selectedEntry.PlayerData);
		}
	}

	[Serializable]
	public class AdminPlayersList
	{
		//Player Management:
		public List<AdminPlayerEntryData> players = new List<AdminPlayerEntryData>();
	}

	[Serializable]
	public class OnSelectPlayerEvent : UnityEvent<AdminPlayerEntryData>{}
}