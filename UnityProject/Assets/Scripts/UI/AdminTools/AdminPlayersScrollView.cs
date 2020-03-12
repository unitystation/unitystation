using System;
using System.Collections;
using System.Collections.Generic;
using DatabaseAPI;
using UnityEngine;

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
			currentCount += Time.deltaTime
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

		}
	}

	[Serializable]
	public class AdminPlayersList
	{
		//Player Management:
		public List<AdminPlayerEntryData> players = new List<AdminPlayerEntryData>();
	}
}
