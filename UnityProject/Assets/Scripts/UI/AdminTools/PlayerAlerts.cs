using System;
using System.Collections.Generic;
using UnityEngine;

namespace AdminTools
{
	public class PlayerAlerts : MonoBehaviour
	{
		[SerializeField] private GameObject playerAlertsWindow;
		[SerializeField] private PlayerAlertsScroll playerAlertsScroll;
		[SerializeField] private GUI_Notification notifications;

		public void LoadAllEntries(List<PlayerAlertData> alertEntries)
		{
			playerAlertsScroll.LoadAlertEntries(alertEntries);
		}

		public void AppendEntries(List<PlayerAlertData> alertEntries)
		{
			playerAlertsScroll.AppendAlertEntries(alertEntries);
		}

		void OnEnable()
		{
			playerAlertsWindow.SetActive(false);
			ServerRequestAllEntries();
		}

		public void ServerRequestAllEntries()
		{

		}



		public void ToggleWindow()
		{
			playerAlertsWindow.SetActive(!playerAlertsWindow.activeInHierarchy);
		}
	}

	public enum PlayerAlertTypes
	{
		RDM,
		PlasmaOpen
	}

	[Serializable]
	public class PlayerAlertData : ChatEntryData
	{
		public PlayerAlertTypes playerAlertType;
		public uint playerNetId;
		public string roundTime;
		public bool takenCareOf;
		public bool gibbed;
	}
}
