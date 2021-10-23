using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Messages.Client.Admin;


namespace AdminTools
{
	public class PlayerChatPage : AdminPage
	{
		[SerializeField] private InputField inputField = null;
		[SerializeField] private Transform chatContent = null;
		[SerializeField] private GameObject adminChatEntryPrefab = null;
		private readonly List<AdminChatEntry> loadedChatEntries = new List<AdminChatEntry>();
		private AdminPlayerEntry selectedPlayer;

		public override void OnPageRefresh(AdminPageRefreshData adminPageData)
		{
			base.OnPageRefresh(adminPageData);
		}

		private readonly Dictionary<string, List<string>> chatLogs = new Dictionary<string, List<string>>();

		private bool refreshClock;
		private float waitTime;

		public void SetData(AdminPlayerEntry entry)
		{
			if (entry != null)
			{
				selectedPlayer = entry;
			}

			if (selectedPlayer == null) return;

			UIManager.IsInputFocus = true;
			UIManager.PreventChatInput = true;
			RefreshChatLog(selectedPlayer.PlayerData.uid);
			refreshClock = true;
			inputField.ActivateInputField();
		}

		private void Update()
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
				AddMessageToLogs(userID, msg.Message);
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

		private void RefreshChatLog(string userID)
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

			AddMessageToLogs(selectedPlayer.PlayerData.uid, $"You: {inputField.text}");
			RefreshChatLog(selectedPlayer.PlayerData.uid);
			var message = $"{PlayerManager.CurrentCharacterSettings.Username}: {inputField.text}";
			RequestAdminBwoink.Send(selectedPlayer.PlayerData.uid, message);
			inputField.text = "";
			inputField.ActivateInputField();
			StartCoroutine(AfterSubmit());
		}

		private IEnumerator AfterSubmit()
		{
			yield return WaitFor.EndOfFrame;
			UIManager.IsInputFocus = true;
		}

		public void GoBack()
		{
			refreshClock = false;
			UIManager.IsInputFocus = false;
			UIManager.PreventChatInput = false;
			adminTools.ShowMainPage();
		}

		private void OnDisable()
		{
			UIManager.IsInputFocus = false;
			UIManager.PreventChatInput = false;
		}
	}
}
