using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using DatabaseAPI;
using Messages.Client.Admin;
using Messages.Server.AdminTools;
using Mirror;
using UnityEngine;

namespace AdminTools
{
	public class AdminToAdminChat : MonoBehaviour
	{
		[SerializeField] private ChatScroll chatScroll = null;
		private const string NotificationKey = "adminchat";

		/// <summary>
		/// All messages sent and recieved between admins
		/// </summary>
		private List<AdminChatMessage> serverAdminChatLogs
			= new List<AdminChatMessage>();

		/// <summary>
		/// The admins client local cache for admin to admin chat
		/// </summary>
		private List<AdminChatMessage> clientAdminChatLogs
			= new List<AdminChatMessage>();

		public void ClearLogs()
		{
			serverAdminChatLogs.Clear();
			clientAdminChatLogs.Clear();
		}

		private void OnEnable()
		{
			chatScroll.OnInputFieldSubmit += OnInputSend;
			UIManager.Instance.adminChatButtons.adminNotification.ClearAll();
			chatScroll.LoadChatEntries(clientAdminChatLogs.Cast<ChatEntryData>().ToList());
			ClientGetUnreadAdminPlayerMessages(ServerData.UserID);
		}

		private void OnDisable()
		{
			chatScroll.OnInputFieldSubmit -= OnInputSend;
		}

		public void ServerAddChatRecord(string message, string userId)
		{
			var entry = new AdminChatMessage
			{
				fromUserid = userId,
				Message = message
			};

			serverAdminChatLogs.Add(entry);
			AdminChatUpdateMessage.SendSingleEntryToAdmins(entry);
			AdminChatNotifications.SendToAll(NotificationKey, AdminChatWindow.AdminToAdminChat, 1);
		}

		public void ServerGetUnreadMessages(string adminId, int currentCount, NetworkConnection requestee)
		{
			if (!PlayerList.Instance.IsAdmin(adminId)) return;

			if (currentCount >= serverAdminChatLogs.Count)
			{
				return;
			}

			AdminChatUpdate update = new AdminChatUpdate();

			update.messages = serverAdminChatLogs;

			AdminChatUpdateMessage.SendLogUpdateToAdmin(requestee, update);
		}

		void ClientGetUnreadAdminPlayerMessages(string playerId)
		{
			AdminCheckAdminMessages.Send(playerId, clientAdminChatLogs.Count);
		}

		public void ClientUpdateChatLog(string unreadMessagesJson)
		{
			if (string.IsNullOrEmpty(unreadMessagesJson)) return;

			var update = JsonUtility.FromJson<AdminChatUpdate>(unreadMessagesJson);
			clientAdminChatLogs.AddRange(update.messages);

			chatScroll.AppendChatEntries(update.messages.Cast<ChatEntryData>().ToList());
		}

		public void OnInputSend(string message)
		{
			var adminMsg = new AdminChatMessage
			{
				fromUserid = ServerData.UserID,
				Message = message,
				wasFromAdmin = true
			};

			var msg = $"{ServerData.Auth.CurrentUser.DisplayName}: {message}";
			RequestAdminChatMessage.Send(ServerData.UserID, PlayerList.Instance.AdminToken, msg);
		}
	}
}
