using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Mirror;
using Core.Accounts;
using Messages.Client.Admin;
using Messages.Server.AdminTools;


namespace AdminTools
{
	public class AdminToAdminChat : MonoBehaviour
	{
		[SerializeField] private ChatScroll chatScroll = null;
		private const string NotificationKey = "adminchat";

		/// <summary>
		/// All messages sent and recieved between admins
		/// </summary>
		private readonly List<AdminChatMessage> serverAdminChatLogs = new();

		/// <summary>
		/// The admins client local cache for admin to admin chat
		/// </summary>
		private readonly List<AdminChatMessage> clientAdminChatLogs = new();

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
			ClientGetUnreadAdminPlayerMessages();
		}

		private void OnDisable()
		{
			chatScroll.OnInputFieldSubmit -= OnInputSend;
		}

		public void ServerAddChatRecord(string message, PlayerInfo fromPlayer)
		{
			var entry = new AdminChatMessage
			{
				fromUserid = fromPlayer.AccountId,
				Message = $"{fromPlayer.Username}: {message}",
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

			foreach (var adminChatChunk in serverAdminChatLogs.ToList().Chunk(100))
			{
				AdminChatUpdate update = new()
				{
					messages = adminChatChunk.ToList()
				};
				AdminChatUpdateMessage.SendLogUpdateToAdmin(requestee, update);
			}

		}

		private void ClientGetUnreadAdminPlayerMessages()
		{
			AdminCheckAdminMessages.Send(clientAdminChatLogs.Count);
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
			RequestAdminChatMessage.Send(message);
		}
	}
}
