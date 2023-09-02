using System;
using System.Collections.Generic;
using Logs;
using Messages.Client.Admin;
using Messages.Server.AdminTools;
using Mirror;
using Newtonsoft.Json;
using Shared.Managers;
using UnityEngine;

namespace AdminTools
{
	public class PlayerAlerts : SingletonManager<PlayerAlerts>
	{
		[SerializeField] private GameObject playerAlertsWindow = null;
		[SerializeField] private PlayerAlertsScroll playerAlertsScroll = null;
		[SerializeField] private GUI_Notification notifications = null;
		private const string NotificationKey = "playeralert";

		private readonly List<PlayerAlertData> serverPlayerAlerts = new List<PlayerAlertData>();

		private readonly List<PlayerAlertData> clientPlayerAlerts = new List<PlayerAlertData>();


		public void LoadAllEntries(List<PlayerAlertData> alertEntries)
		{
			playerAlertsScroll.LoadAlertEntries(alertEntries);
		}

		public void AppendEntries(List<PlayerAlertData> alertEntries)
		{
			playerAlertsScroll.AppendAlertEntries(alertEntries);
		}

		private void OnEnable()
		{
			playerAlertsWindow.SetActive(false);
		}

		public void ClearAllNotifications()
		{
			notifications.ClearAll();
		}

		public void UpdateNotifications(int amt)
		{
			notifications.AddNotification(NotificationKey, amt);
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

			var update = JsonConvert.DeserializeObject<PlayerAlertsUpdate>(data);
			clientPlayerAlerts.AddRange(update.playerAlerts);
			LoadAllEntries(clientPlayerAlerts);
		}

		public void ClientUpdateSingleEntry(PlayerAlertData entry)
		{
			var index = clientPlayerAlerts.FindIndex(x =>
				x.playerNetId == entry.playerNetId && x.roundTime == entry.roundTime);
			if (index == -1)
			{
				playerAlertsScroll.AddNewPlayerAlert(entry);
			}
			else
			{
				playerAlertsScroll.UpdateExistingPlayerAlert(entry);
			}
		}

		public void ServerRequestEntries(string userId, int count, NetworkConnection requestee)
		{
			if (!PlayerList.Instance.IsAdmin(userId)) return;

			if (count >=  serverPlayerAlerts.Count)
			{
				return;
			}

			PlayerAlertsUpdate update = new PlayerAlertsUpdate
			{
				playerAlerts = serverPlayerAlerts
			};

			PlayerAlertsUpdateMessage.SendLogUpdateToAdmin(requestee, update);
			if (notifications.notifications.ContainsKey(NotificationKey))
			{
				PlayerAlertNotifications.Send(requestee, notifications.notifications[NotificationKey]);
			}
		}

		public void ServerAddNewEntry(string incidentTime, PlayerAlertTypes alertType, PlayerInfo perp, string message)
		{
			var netId = NetId.Invalid;

			if (perp?.Connection == null)
			{
				return;
			}

			if (perp.Connection.identity != null)
			{
				netId = perp.Connection.identity.netId;
			}

			var entry = new PlayerAlertData
			{
				roundTime = incidentTime,
				playerNetId = netId,
				playerAlertType = alertType,
				Message = message
			};
			serverPlayerAlerts.Add(entry);
			PlayerAlertNotifications.SendToAll(1);
			ServerSendEntryToAllAdmins(entry);
		}

		public void ServerSendEntryToAllAdmins(PlayerAlertData entry)
		{
			PlayerAlertsUpdateMessage.SendSingleEntryToAdmins(entry);
		}

		public void ServerProcessActionRequest(
				string adminId, PlayerAlertActions actionRequest,
				string roundTimeOfIncident, uint perpId, string adminToken)
		{
			if (!PlayerList.Instance.IsAdmin(adminId)) return;

			var admin = PlayerList.Instance.GetAdmin(adminId, adminToken);
			if (admin == null) return;

			var index = serverPlayerAlerts.FindIndex(x =>
				x.playerNetId == perpId && x.roundTime == roundTimeOfIncident);
			if (index == -1)
			{
				Loggy.Log($"Could not find perp id {perpId} with roundTime incident: {roundTimeOfIncident}", Category.Admin);
				return;
			}

			if (NetworkServer.spawned.ContainsKey(perpId) == false)
			{
				Loggy.Log($"Perp id {perpId} not found in Spawnlist", Category.Admin);
				return;
			}

			var perp = NetworkServer.spawned[perpId];

			switch (actionRequest)
			{
				case PlayerAlertActions.Gibbed:
					ProcessGibRequest(perp.gameObject, admin, serverPlayerAlerts[index], adminId);
					break;
				case PlayerAlertActions.TakenCareOf:
					ProcessTakenCareOfRequest(perp.gameObject, admin, serverPlayerAlerts[index], adminId);
					break;
			}
		}

		private void ProcessGibRequest(GameObject perp, GameObject admin, PlayerAlertData alertEntry, string adminId)
		{
			if (alertEntry.gibbed) return;

			var playerScript = perp.GetComponent<PlayerScript>();
			if (playerScript == null || playerScript.IsGhost || playerScript.playerHealth == null) return;
			PlayerInfo perpPlayer = perp.Player();
			UIManager.Instance.adminChatWindows.adminLogWindow.ServerAddChatRecord(
					$"{admin.Player().Username} BRUTALLY GIBBED player {perpPlayer.Name} ({perpPlayer.Username}) for a " +
			        $"{alertEntry.playerAlertType.ToString()} incident that happened at roundtime: {alertEntry.roundTime}", adminId);

			playerScript.playerHealth.OnGib();

			alertEntry.gibbed = true;
			ServerSendEntryToAllAdmins(alertEntry);
			notifications.AddNotification(NotificationKey, -1);
			PlayerAlertNotifications.SendToAll(-1);
		}

		private void ProcessTakenCareOfRequest(GameObject perp, GameObject admin, PlayerAlertData alertEntry, string adminId)
		{
			if (alertEntry.takenCareOf) return;

			var playerScript = perp.GetComponent<PlayerScript>();
			if (playerScript == null || playerScript.IsGhost || playerScript.playerHealth == null) return;
			PlayerInfo perpPlayer = perp.Player();
			UIManager.Instance.adminChatWindows.adminLogWindow.ServerAddChatRecord(
					$"{admin.Player().Username} is talking to or monitoring player {perpPlayer.Name} ({perpPlayer.Username}) for a " +
			        $"{alertEntry.playerAlertType} incident that happened at roundtime: {alertEntry.roundTime}", adminId);

			alertEntry.takenCareOf = true;
			ServerSendEntryToAllAdmins(alertEntry);
			notifications.AddNotification(NotificationKey, -1);
			PlayerAlertNotifications.SendToAll(-1);
		}

		public void ToggleWindow()
		{
			if (!playerAlertsWindow.activeInHierarchy)
			{
				playerAlertsWindow.SetActive(true);
				notifications.ClearAll();
				AdminCheckPlayerAlerts.Send(clientPlayerAlerts.Count);
			}
			else
			{
				playerAlertsWindow.SetActive(false);
			}
		}

		public static void LogPlayerAction(string incidentTime, PlayerAlertTypes alertType, PlayerInfo perp, string message)
		{
			if (Instance == null)
			{
				Loggy.LogError("[PlayerAlerts] - Instance is null!");
				return;
			}
			if (perp == null)
			{
				Loggy.LogError("[PlayerAlerts/LogPlayerAction] - PlayerInfo cannot be null!");
				return;
			}

			Instance.ServerAddNewEntry(incidentTime, alertType, perp, message);
		}
	}

	public enum PlayerAlertTypes
	{
		RDM,
		PlasmaOpen,
		Emag
	}

	public enum PlayerAlertActions
	{
		Gibbed,
		TakenCareOf,
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
