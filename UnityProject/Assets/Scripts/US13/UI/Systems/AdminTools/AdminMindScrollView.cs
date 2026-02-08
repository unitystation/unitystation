using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using US13.Core.Input_System;
using US13.Core.Physics;
using US13.Managers.UpdateManager;
using US13.Messages.Client.Admin;
using US13.Systems.Lobby;
using US13.UI.Systems.AdminTools.DevTools;
using US13.UI.Systems.MainHUD.UI_Bottom;
using Util;

namespace US13.UI.Systems.AdminTools
{
	public class AdminMindScrollView : MonoBehaviour
	{
		[SerializeField] private Transform MindListContent = null;
		[SerializeField] private GameObject MindEntryPrefab = null;


		[SerializeField] private bool disableButtonInteract = false;
		[SerializeField] private bool hideSensitiveFields = false;
		private readonly float refreshTime = 3f;

		private readonly List<GameObject> HiddenButtons = new List<GameObject>();
		[SerializeField] private AdminSearchBar searchBar = null;

		//Loaded MindEntries
		private readonly List<AdminMindEntry> MindEntries = new List<AdminMindEntry>();

		public OnSelectMindEvent OnSelectMind;

		public AdminMindEntry SelectedMind { get; private set; }

		public bool WaitingForPlayer = false;

		public GUI_AdminTools AdminPlayersScrollView;

		public AdminPlayersScrollView AdminPlayersScrollViewObject;

		public bool Updating = false;

		public void Awake()
		{
			AdminPlayersScrollView.OnSelectPlayer.AddListener(OnPlayerSelected);
		}

		private void OnEnable()
		{
			RefreshPlayerList();
			UpdateManager.Add(PeriodicUpdate, refreshTime);

		}

		private void OnDisable()
		{
			UpdateManager.Remove(CallbackType.PERIODIC_UPDATE, PeriodicUpdate);
		}

		private void PeriodicUpdate()
		{
			RefreshPlayerList();
		}

		private void RefreshPlayerList()
		{
			RequestMindPlayerList.Send();
		}

		public void ReceiveUpdatedPlayerList(AdminMindList playerList)
		{
			RefreshOnlinePlayerList(playerList);
		}

		private void RefreshOnlinePlayerList(AdminMindList playerList)
		{
			foreach (var p in playerList.players)
			{
				if (p == null) continue;

				var index = MindEntries.FindIndex(x => x.MindData?.MindUID == p?.MindUID);
				if (index != -1)
				{
					MindEntries[index].UpdateButton(p, SelectPlayerInList, disableButtonInteract, hideSensitiveFields);
				}
				else
				{
					var e = Instantiate(MindEntryPrefab, MindListContent);
					var entry = e.GetComponent<AdminMindEntry>();
					if (entry == null) continue;
					entry.UpdateButton(p, SelectPlayerInList, disableButtonInteract, hideSensitiveFields);
					MindEntries.Add(entry);
					index = MindEntries.Count - 1;
				}
			}

			Search();

			if (SelectedMind == null)
			{
				if (MindEntries.Count == 0) return;

				SelectPlayerInList(MindEntries[0]);
			}
			else
			{
			}
		}

		private void SelectPlayerInList(AdminMindEntry selectedEntry)
		{
			foreach (var p in MindEntries)
			{
				if (p != selectedEntry)
				{
					p.DeselectPlayer();
				}
				else
				{
					p.SelectPlayer();
					//SelectedPlayer = selectedEntry;
				}
			}

			SelectedMind = selectedEntry;
			if (SelectedMind != null) OnSelectMind.Invoke(selectedEntry.MindData);
		}

		public void Search()
		{
			if (searchBar == null) return;

			foreach (GameObject x in HiddenButtons) // Hidden Buttons stores list of the hidden items which dont contain the search phrase
			{
				if (x != null)
				{
					x.SetActive(true);
				}
			}

			HiddenButtons.Clear();

			//Grabs fresh list of all the possible buttons
			// TODO: encapsulate this adminPlayerList search more generic so that it can better be used across different admin systems
			var buttons = MindEntries;

			var Searchtext = searchBar.SearchText();

			for (int i = 0; i < buttons.Count; i++)
			{
				if (buttons[i] != null)
				{
					if (buttons[i].displayName.text.ToLower().Contains(Searchtext.text.ToLower()) ||
					    Searchtext.text.Length == 0) continue;

					HiddenButtons.Add(buttons[i].gameObject); // non-results get hidden
					buttons[i].gameObject.SetActive(false);
				}
			}
		}


		public void KickPlayerFromMind()
		{
			AdminRequestKickPlayerFromMind.Send(SelectedMind.MindData.MindUID);
		}


		public void SetPlayerMindControlling()
		{
			WaitingForPlayer = true;
			this.gameObject.SetActive(false);
			AdminPlayersScrollViewObject.SetActive(true);
		}


		public void SetMindPossessing()
		{

			if (Updating)
			{
				Updating = false;
				UpdateManager.Remove(CallbackType.UPDATE, UpdateMe);
			}
			else
			{
				Updating = true;
				UpdateManager.Add(CallbackType.UPDATE, UpdateMe);
			}

		}

		public void OnPlayerSelected(AdminPlayerEntryData AdminPlayerEntry)
		{
			if (WaitingForPlayer == false) return;
			AdminPlayersScrollViewObject.SetActive(false);
			this.gameObject.SetActive(true);
			AdminRequestSetPlayerMindControlling.Send(SelectedMind.MindData.MindUID , AdminPlayerEntry.uid);
		}

		private void UpdateMe()
		{
			if (CommonInput.GetMouseButtonDown(0))
			{
				OnMouseDown();
			}
		}

		public void OnMouseDown()
		{
			//Ignore spawn if pointer is hovering over GUI
			if (EventSystem.current.IsPointerOverGameObject()) return;


			var Object = MouseUtils
				.GetOrderedObjectsUnderMouse(useMappedItems: DevCameraControls.Instance.MappingItemState).FirstOrDefault();
			if (Object.TryGetComponent<UniversalObjectPhysics>(out var Physics) == false)
			{
				return;
			}

			Updating = false;
			UpdateManager.Remove(CallbackType.UPDATE, UpdateMe);

			AdminRequestSetMindPossessing.Send(SelectedMind.MindData.MindUID,Physics.netId);
		}


		[Serializable]
		public class OnSelectMindEvent : UnityEvent<AdminMindEntryData>
		{
		}
	}

	[Serializable]
	public class AdminMindList
	{
		//Player Management:
		public List<AdminMindEntryData> players = new List<AdminMindEntryData>();
	}

	[Serializable]
	public class AdminMindEntryData
	{
		public uint MindUID;
		public bool IsAntag;
		public string ControlledByID;
		public bool nonImportantMind;
		public uint PossessingObject;
		public string occupationName;
		public CharacterSheet CurrentCharacterSettings;
	}
}