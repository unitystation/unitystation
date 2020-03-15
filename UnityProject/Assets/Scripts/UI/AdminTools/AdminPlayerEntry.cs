using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace AdminTools
{
	public class AdminPlayerEntry : MonoBehaviour
	{
		private Action<AdminPlayerEntry> OnClickEvent;
		[SerializeField] private Text displayName = null;
		[SerializeField] private Image bg = null;
		[SerializeField] private GameObject msgPendingNot = null;
		[SerializeField] private Text msgPendingCount = null;
		[SerializeField] private GameObject offlineNot = null;
		public Button button;

		public Color selectedColor;
		public Color defaultColor;
		public Color antagTextColor;

		public AdminPlayerEntryData PlayerData { get; set; }

		public void UpdateButton(AdminPlayerEntryData playerEntryData, Action<AdminPlayerEntry> onClickEvent)
		{
			OnClickEvent = onClickEvent;
			PlayerData = playerEntryData;
			displayName.text = $"{playerEntryData.name} - {playerEntryData.currentJob}. ACC: {playerEntryData.accountName} {playerEntryData.ipAddress}";

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

			if (PlayerData.isOnline)
			{
				offlineNot.SetActive(false);
			}
			else
			{
				offlineNot.SetActive(true);
			}
		}

		public void OnClick()
		{
			if (OnClickEvent != null)
			{
				OnClickEvent.Invoke(this);
			}
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
