using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Mirror;
using DatabaseAPI;
using Messages.Client.Admin;
using Messages.Server.AdminTools;
using Newtonsoft.Json;


namespace AdminTools
{
	public class AdminToAdminChat : MonoBehaviour
	{
		[SerializeField] private ChatScroll chatScroll = null;
		private const string NotificationKey = "adminchat";

		/// <summary>
		/// All messages sent and recieved between admins
		/// </summary>
		private readonly List<AdminChatMessage> serverAdminChatLogs = new List<AdminChatMessage>();

		/// <summary>
		/// The admins client local cache for admin to admin chat
		/// </summary>
		private readonly List<AdminChatMessage> clientAdminChatLogs = new List<AdminChatMessage>();

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

		public void ServerAddChatRecord(string message, string userId)
		{
			var entry = new AdminChatMessage
			{
				fromUserid = userId,
				Message = GameManager.Instance.RoundTime.ToString(@"hh\:mm\:ss") + " - " + message
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
				AdminChatUpdate update = new AdminChatUpdate
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

			var update = JsonConvert.DeserializeObject<AdminChatUpdate>(unreadMessagesJson);
			clientAdminChatLogs.AddRange(update.messages);

			chatScroll.AppendChatEntries(update.messages.Cast<ChatEntryData>().ToList());
		}

		public void OnInputSend(string message)
		{
			RequestAdminChatMessage.Send($"{ServerData.Auth.CurrentUser.DisplayName}: {message}");
		}
	}
}
