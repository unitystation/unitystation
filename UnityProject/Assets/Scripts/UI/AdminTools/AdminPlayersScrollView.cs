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
		[SerializeField] private Transform playerListContent = null;
		[SerializeField] private GameObject playerEntryPrefab = null;
		[Tooltip("Used to send the master notification reference to the admin player buttons")]
		[SerializeField] private GUI_Notification masterNotification = null;

		[SerializeField] private bool showAdminsOnly = false;
		[SerializeField] private bool disableButtonInteract = false;
		private float refreshTime = 3f;
		private float currentCount = 0f;

		private List<GameObject> HiddenButtons = new List<GameObject>();
		[SerializeField] private AdminSearchBar searchBar = null;

		//Loaded playerEntries
		private List<AdminPlayerEntry> playerEntries = new List<AdminPlayerEntry>();

		public OnSelectPlayerEvent OnSelectPlayer;

		public AdminPlayerEntry SelectedPlayer { get; private set; }

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
				currentCount = 0;
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
					playerEntries[index].UpdateButton(p, SelectPlayerInList, masterNotification, disableButtonInteract);
				}
				else
				{
					if (showAdminsOnly)
					{
						if(!p.isAdmin) continue;
					}
					var e = Instantiate(playerEntryPrefab, playerListContent);
					var entry = e.GetComponent<AdminPlayerEntry>();
					entry.UpdateButton(p, SelectPlayerInList, masterNotification, disableButtonInteract);
					playerEntries.Add(entry);
					index = playerEntries.Count - 1;
				}

				if (!p.isOnline)
				{
					playerEntries[index].transform.SetAsLastSibling();
				}
			}

			Search();

			if (SelectedPlayer == null)
			{
				if (playerEntries.Count == 0) return;

				SelectPlayerInList(playerEntries[0]);
			}
			else
			{
				if (gameObject.activeInHierarchy)
				{
					SelectedPlayer.pendingMsgNotification.ClearAll();
					if (masterNotification != null)
					{
						masterNotification.RemoveNotification(SelectedPlayer.PlayerData.uid);
					}
				}
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
					SelectedPlayer = selectedEntry;
				}
			}

			SelectedPlayer = selectedEntry;
			if(OnSelectPlayer != null) OnSelectPlayer.Invoke(selectedEntry.PlayerData);
		}

		public void Search()
		{
			if (searchBar == null) return;

			foreach (GameObject x in HiddenButtons)//Hidden Buttons stores list of the hidden items which dont contain the search phrase
			{
				if (x != null)
				{
					x.SetActive(true);
				}
			}
			HiddenButtons.Clear();

			var buttons = playerEntries;//Grabs fresh list of all the possible buttons

			var Searchtext = searchBar.SearchText();

			for (int i = 0; i < buttons.Count; i++)
			{
				if (buttons[i] != null)
				{
					if (buttons[i].displayName.text.ToLower().Contains(Searchtext.text.ToLower()) || Searchtext.text.Length == 0)
					{
					}
					else
					{
						HiddenButtons.Add(buttons[i].gameObject);//non-results get hidden
						buttons[i].gameObject.SetActive(false);
					}
				}
			}
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