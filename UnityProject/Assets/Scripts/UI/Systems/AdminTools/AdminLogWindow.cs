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
	public class AdminLogWindow : MonoBehaviour
	{
		[SerializeField] private ChatScroll chatScroll = null;
		private const string NotificationKey = "adminlog";

		/// <summary>
		/// All messages sent and recieved between admins
		/// </summary>
		private readonly List<AdminChatMessage> serverAdminLogs = new List<AdminChatMessage>();

		/// <summary>
		/// The admins client local cache for admin to admin chat
		/// </summary>
		private readonly List<AdminChatMessage> clientAdminLogs = new List<AdminChatMessage>();

		public void ClearLogs()
		{
			serverAdminLogs.Clear();
			clientAdminLogs.Clear();
		}

		private void OnEnable()
		{
			chatScroll.OnInputFieldSubmit += OnInputSend;
			UIManager.Instance.adminChatButtons.adminLogNotification.ClearAll();
			chatScroll.LoadChatEntries(clientAdminLogs.Cast<ChatEntryData>().ToList());
			ClientGetUnreadAdminMessages();
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

			serverAdminLogs.Add(entry);
			AdminLogUpdateMessage.SendSingleEntryToAdmins(entry);
			AdminChatNotifications.SendToAll(NotificationKey, AdminChatWindow.AdminLogWindow, 1);
		}

		public void ServerGetUnreadMessages(string adminId, int currentCount, NetworkConnection requestee)
		{
			if (!PlayerList.Instance.IsAdmin(adminId)) return;

			if (currentCount >= serverAdminLogs.Count)
			{
				return;
			}

			foreach (var adminChatChunk in serverAdminLogs.ToList().Chunk(100))
			{
				AdminChatUpdate update = new AdminChatUpdate
				{
					messages = adminChatChunk.ToList()
				};
				AdminLogUpdateMessage.SendLogUpdateToAdmin(requestee, update);
			}

		}

		private void ClientGetUnreadAdminMessages()
		{
			AdminCheckAdminMessages.Send(clientAdminLogs.Count);
		}

		public void ClientUpdateChatLog(string unreadMessagesJson)
		{
			if (string.IsNullOrEmpty(unreadMessagesJson)) return;

			var update = JsonConvert.DeserializeObject<AdminChatUpdate>(unreadMessagesJson);
			clientAdminLogs.AddRange(update.messages);

			chatScroll.AppendChatEntries(update.messages.Cast<ChatEntryData>().ToList());
		}

		public void OnInputSend(string message)
		{
			RequestAdminChatMessage.Send($"{ServerData.Auth.CurrentUser.DisplayName}: {message}");
		}
	}
}
