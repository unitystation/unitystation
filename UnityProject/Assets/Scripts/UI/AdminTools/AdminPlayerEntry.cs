using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace AdminTools
{
	public class AdminPlayerEntry : MonoBehaviour
	{
		private GUI_AdminTools adminTools;

		[SerializeField] private Text displayName;
		[SerializeField] private Image bg;
		[SerializeField] private GameObject msgPendingNot;
		[SerializeField] private Text msgPendingCount;
		public Button button;

		public Color selectedColor;
		public Color defaultColor;
		public Color antagTextColor;

		public AdminPlayerEntryData PlayerData { get; set; }
		private List<AdminChatMessage> pendingMessages = new List<AdminChatMessage>();

		public void UpdateButton(AdminPlayerEntryData playerEntryData, GUI_AdminTools adminTools)
		{
			pendingMessages.AddRange(playerEntryData.newMessages);
			this.adminTools = adminTools;
			PlayerData = playerEntryData;
			displayName.text = $"{playerEntryData.name} - {playerEntryData.currentJob}. ACC: {playerEntryData.accountName} {playerEntryData.ipAddress}";

			if (PlayerData.newMessages.Count > 0)
			{
				msgPendingNot.SetActive(true);
				msgPendingCount.text = PlayerData.newMessages.Count.ToString();
			}
			else
			{
				msgPendingNot.SetActive(false);
			}

			if (PlayerData.isAntag)
			{
				displayName.color = antagTextColor;
			}
			else
			{
				displayName.color = Color.white;
			}

			if (PlayerData.isAdmin)
			{
				displayName.fontStyle = FontStyle.Bold;
			}
			else
			{
				displayName.fontStyle = FontStyle.Normal;
			}

			if (adminTools.SelectedPlayer == playerEntryData.uid)
			{
				adminTools.AddPendingMessagesToLogs(playerEntryData.uid, GetPendingMessage());
			}
		}

		public List<AdminChatMessage> GetPendingMessage()
		{
			var list = new List<AdminChatMessage>(pendingMessages);
			pendingMessages.Clear();
			ClearMessageNot();
			return list;
		}

		public void OnClick()
		{
			adminTools.SelectPlayerInList(this);
		}

		public void ClearMessageNot()
		{
			msgPendingCount.text = "0";
			msgPendingNot.SetActive(false);
		}

		public void SelectPlayer()
		{
			bg.color = selectedColor;
		}

		public void DeselectPlayer()
		{
			bg.color = defaultColor;
		}
	}
}
