using System;
using System.Collections.Generic;
using DatabaseAPI;
using Mirror;
using UnityEngine;

namespace AdminTools
{
	public class PlayerAlerts : MonoBehaviour
	{
		[SerializeField] private GameObject playerAlertsWindow;
		[SerializeField] private PlayerAlertsScroll playerAlertsScroll;
		[SerializeField] private GUI_Notification notifications;
		private const string NotificationKey = "playeralert";


		private List<PlayerAlertData> serverPlayerAlerts
			= new List<PlayerAlertData>();

		private List<PlayerAlertData> clientPlayerAlerts
			= new List<PlayerAlertData>();


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
		}

		public void ClearLogs()
		{
			serverPlayerAlerts.Clear();
			clientPlayerAlerts.Clear();
			notifications.ClearAll();
		}

		public void ClientUpdateAlertLog(string data)
		{
			if (string.IsNullOrEmpty(data)) return;

			var update = JsonUtility.FromJson<PlayerAlertsUpdate>(data);
			clientPlayerAlerts.AddRange(update.playerAlerts);
			LoadAllEntries(clientPlayerAlerts);
		}

		public void ServerRequestEntries(string userId, int count, NetworkConnection requestee)
		{
			if (!PlayerList.Instance.IsAdmin(userId)) return;

			if (count >=  serverPlayerAlerts.Count)
			{
				return;
			}

			PlayerAlertsUpdate update = new PlayerAlertsUpdate();

			update.playerAlerts = serverPlayerAlerts;

			PlayerAlertsUpdateMessage.SendLogUpdateToAdmin(requestee, update);
		}

		public void ServerSendEntryToAllAdmins(PlayerAlertData entry)
		{

		}

		public void ToggleWindow()
		{
			if (!playerAlertsWindow.activeInHierarchy)
			{
				playerAlertsWindow.SetActive(true);
				notifications.ClearAll();
				AdminCheckPlayerAlerts.Send(ServerData.UserID, clientPlayerAlerts.Count);
			}
			else
			{
				playerAlertsWindow.SetActive(false);
			}
		}
	}

	public enum PlayerAlertTypes
	{
		RDM,
		PlasmaOpen
	}

	[Serializable]
	public class PlayerAlertsUpdate
	{
		public List<PlayerAlertData> playerAlerts = new List<PlayerAlertData>();
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
