using System.Collections.Generic;
using System.Linq;
using Mirror;
using Newtonsoft.Json;
using UnityEngine;
using US13.Core.Chat.ChatScrollRect;
using US13.Managers;
using US13.Messages.Client.Admin;
using US13.Messages.Server.AdminTools;
using US13.Player;
using Util;

namespace US13.UI.Systems.AdminTools
{
	public class AdminLogWindow : MonoBehaviour
	{
		[SerializeField] private ChatScroll chatScroll = null;
		private const string NotificationKey = "adminlog";

		/// <summary>
		/// All messages sent and recieved between admins
		/// </summary>
		private readonly List<AdminChatMessage> serverAdminLogs = new();

		/// <summary>
		/// The admins client local cache for admin to admin chat
		/// </summary>
		private readonly List<AdminChatMessage> clientAdminLogs = new();

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
			if (PlayerList.HasTAGServer(TAG.ADMIN_LOGS,adminId) == false) return;

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
			RequestAdminChatMessage.Send($"{PlayerManager.Account.Username}: {message}");
		}
	}
}
