using System.Collections;
using System.Collections.Generic;
using System.Linq;
using DatabaseAPI;
using UnityEngine;
using UnityEngine.UI;

namespace AdminTools
{
	public class PlayerChatPage : AdminPage
	{
		[SerializeField] private InputField inputField;
		[SerializeField] private Transform chatContent;
		[SerializeField] private GameObject adminChatEntryPrefab;
		private List<AdminChatEntry> loadedChatEntries = new List<AdminChatEntry>();
		private AdminPlayerEntry selectedPlayer;
		public override void OnPageRefresh(AdminPageRefreshData adminPageData)
		{
			base.OnPageRefresh(adminPageData);
		}

		Dictionary<string, List<string>> chatLogs = new Dictionary<string, List<string>>();

		private bool refreshClock;
		private float waitTime;

		public void SetData(AdminPlayerEntry entry)
		{
			selectedPlayer = entry;
			UIManager.IsInputFocus = true;
			UIManager.PreventChatInput = true;
			RefreshChatLog(entry.PlayerData.uid);
			refreshClock = true;
			inputField.ActivateInputField();
		}

		void Update()
		{
			if (refreshClock)
			{
				waitTime += Time.deltaTime;
				if (waitTime > 4f)
				{
					waitTime = 0f;
					RefreshPage();
				}
			}
		}

		public void AddPendingMessagesToLogs(string userID, List<AdminChatMessage> pendingMessages)
		{
			foreach (var msg in pendingMessages)
			{
				AddMessageToLogs(userID, msg.message);
			}
		}

		public void AddMessageToLogs(string userID, string message)
		{
			if (!chatLogs.ContainsKey(userID))
			{
				chatLogs.Add(userID, new List<string>());
			}

			chatLogs[userID].Add(message);
		}

		void RefreshChatLog(string userID)
		{
			foreach (var e in loadedChatEntries)
			{
				Destroy(e.gameObject);
			}

			loadedChatEntries.Clear();

			if (!chatLogs.ContainsKey(userID)) return;

			foreach (var s in chatLogs[userID])
			{
				var entry = Instantiate(adminChatEntryPrefab, chatContent);
				var chatEntry = entry.GetComponent<AdminChatEntry>();
				chatEntry.SetText(s);
				loadedChatEntries.Add(chatEntry);
			}
		}

		public void OnInputSubmit()
		{
			if (string.IsNullOrEmpty(inputField.text)) return;

			AddMessageToLogs(selectedPlayer.PlayerData.uid, $"You wrote: {inputField.text}");
			RefreshChatLog(selectedPlayer.PlayerData.uid);
			var message = $"Admin PM from {PlayerManager.CurrentCharacterSettings.username}: {inputField.text}";
			RequestAdminBwoink.Send(ServerData.UserID, PlayerList.Instance.AdminToken, selectedPlayer.PlayerData.uid,
				message);
			inputField.text = "";
			UIManager.IsInputFocus = true;
			inputField.ActivateInputField();
		}

		public void GoBack()
		{
			refreshClock = false;
			UIManager.IsInputFocus = false;
			UIManager.PreventChatInput = false;
			adminTools.ShowMainPage();
		}
	}
}