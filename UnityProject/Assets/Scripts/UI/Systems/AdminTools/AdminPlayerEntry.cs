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
		public Text displayName = null;
		[SerializeField] private Image bg = null;
		//The notification counter on the button
		public GUI_Notification pendingMsgNotification = null;
		/// The reference to the notification counter on the admin chat button (the master one)
		private GUI_Notification parentNotification = null;
		[SerializeField] private GameObject offlineNot = null;
		public Button button;

		public Color selectedColor;
		public Color defaultColor;
		public Color antagTextColor;

		public AdminPlayerEntryData PlayerData { get; set; }

		public void UpdateButton(AdminPlayerEntryData playerEntryData, Action<AdminPlayerEntry> onClickEvent, GUI_Notification masterNotification = null,
			bool disableInteract = false, bool hideSensitiveFields = false)
		{
			parentNotification = masterNotification;
			OnClickEvent = onClickEvent;
			PlayerData = playerEntryData;
			if(!hideSensitiveFields)
				displayName.text = $"{playerEntryData.name} - {playerEntryData.currentJob}. ACC: {playerEntryData.accountName} {playerEntryData.ipAddress} UUID { playerEntryData.uid}";
			else 
				displayName.text = $"{playerEntryData.accountName}";
			if (PlayerData.isAntag && !hideSensitiveFields)
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

			if (PlayerData.ipAddress == "")
			{
				offlineNot.SetActive(true);
			}
			else
			{
				offlineNot.SetActive(false);
			}

			if (disableInteract)
			{
				button.interactable = false;
				bg.color = selectedColor;
			}
			else
			{
				button.interactable = true;
			}

			RefreshNotification();
		}

		public void RefreshNotification()
		{
			if (parentNotification == null) return;

			if (parentNotification.notifications.ContainsKey(PlayerData.uid))
			{
				pendingMsgNotification.ClearAll();
				pendingMsgNotification.AddNotification(PlayerData.uid,
					parentNotification.notifications[PlayerData.uid]);
			}
			else
			{
				pendingMsgNotification.ClearAll();
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
			if(parentNotification != null) parentNotification.RemoveNotification(PlayerData.uid);
			pendingMsgNotification.ClearAll();
		}

		public void SelectPlayer()
		{
			bg.color = selectedColor;
			ClearMessageNot();
		}

		public void DeselectPlayer()
		{
			bg.color = defaultColor;
		}
	}
}
