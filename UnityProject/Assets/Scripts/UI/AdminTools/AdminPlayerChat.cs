using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using DatabaseAPI;
using Mirror;
using UnityEngine;
using DiscordWebhook;

namespace AdminTools
{
	public class AdminPlayerChat : MonoBehaviour
	{
		[SerializeField] private ChatScroll chatScroll = null;
		private AdminPlayerEntryData selectedPlayer;
		public AdminPlayerEntryData SelectedPlayer
		{
			get { return selectedPlayer; }
		}
		/// <summary>
		/// All messages sent and recieved from players to admins
		/// </summary>
		private Dictionary<string, List<AdminChatMessage>> serverAdminPlayerChatLogs
			= new Dictionary<string, List<AdminChatMessage>>();

		/// <summary>
		/// The admins client local cache for admin to player chat
		/// </summary>
		private Dictionary<string, List<AdminChatMessage>> clientAdminPlayerChatLogs
			= new Dictionary<string, List<AdminChatMessage>>();

		public void ClearLogs()
		{
			serverAdminPlayerChatLogs.Clear();
			clientAdminPlayerChatLogs.Clear();
		}

		public void ServerAddChatRecord(string message, string playerId, string adminId = "")
		{
			if (!serverAdminPlayerChatLogs.ContainsKey(playerId))
			{
				serverAdminPlayerChatLogs.Add(playerId, new List<AdminChatMessage>());
			}

			var entry = new AdminChatMessage
			{
				fromUserid = playerId,
				Message = message
			};

			if (!string.IsNullOrEmpty(adminId))
			{
				entry.fromUserid = adminId;
				entry.wasFromAdmin = true;
			}
			serverAdminPlayerChatLogs[playerId].Add(entry);
			AdminPlayerChatUpdateMessage.SendSingleEntryToAdmins(entry, playerId);
			if (!string.IsNullOrEmpty(adminId))
			{
				AdminChatNotifications.SendToAll(playerId, AdminChatWindow.AdminPlayerChat, 0, true);
			}
			else
			{
				AdminChatNotifications.SendToAll(playerId, AdminChatWindow.AdminPlayerChat, 1);
			}

			ServerMessageRecording(playerId, entry);
		}

		void ServerMessageRecording(string playerId, AdminChatMessage entry)
		{
			var chatlogDir = Path.Combine(Application.streamingAssetsPath, "chatlogs");
			if (!Directory.Exists(chatlogDir))
			{
				Directory.CreateDirectory(chatlogDir);
			}

			var filePath = Path.Combine(chatlogDir, $"{playerId}.txt");

			var connectedPlayer = PlayerList.Instance.GetByUserID(playerId);

			if (!File.Exists(filePath))
			{
				var stream = File.Create(filePath);
				stream.Close();
				string header = $"Username: {connectedPlayer.Username} Player Name: {connectedPlayer.Name} \r\n" +
				                $"IsAntag: {PlayerList.Instance.AntagPlayers.Contains(connectedPlayer)}  role: {connectedPlayer.Job} \r\n" +
				                $"-----Chat Log----- \r\n" +
				                $" \r\n";
				File.AppendAllText(filePath, header);
			}

			string entryName = connectedPlayer.Name;
			if (entry.wasFromAdmin)
			{
				var adminPlayer = PlayerList.Instance.GetByUserID(entry.fromUserid);
				entryName = "[A] " + adminPlayer.Name;
			}

			DiscordWebhookMessage.Instance.AddWebHookMessageToQueue(DiscordWebhookURLs.DiscordWebhookAdminURL, entry.Message, entryName);

			File.AppendAllText(filePath, $"[{DateTime.Now.ToString("O")}] {entryName}: {entry.Message}");
		}

		public void ServerGetUnreadMessages(string playerId, int currentCount, NetworkConnection requestee)
		{
			if (!serverAdminPlayerChatLogs.ContainsKey(playerId))
			{
				serverAdminPlayerChatLogs.Add(playerId, new List<AdminChatMessage>());
			}

			if (currentCount >= serverAdminPlayerChatLogs[playerId].Count)
			{
				return;
			}

			AdminChatUpdate update = new AdminChatUpdate();

			update.messages = serverAdminPlayerChatLogs[playerId].GetRange(currentCount,
				serverAdminPlayerChatLogs[playerId].Count - currentCount);

			AdminPlayerChatUpdateMessage.SendLogUpdateToAdmin(requestee, update, playerId);
		}

		void ClientGetUnreadAdminPlayerMessages(string playerId)
		{
			if (!clientAdminPlayerChatLogs.ContainsKey(playerId))
			{
				clientAdminPlayerChatLogs.Add(playerId, new List<AdminChatMessage>());
			}

			AdminCheckMessages.Send(playerId, clientAdminPlayerChatLogs[playerId].Count);
		}

		public void ClientUpdateChatLog(string unreadMessagesJson, string playerId)
		{
			if (string.IsNullOrEmpty(unreadMessagesJson)) return;

			if (!clientAdminPlayerChatLogs.ContainsKey(playerId))
			{
				clientAdminPlayerChatLogs.Add(playerId, new List<AdminChatMessage>());
			}

			var update = JsonUtility.FromJson<AdminChatUpdate>(unreadMessagesJson);
			clientAdminPlayerChatLogs[playerId].AddRange(update.messages);

			if (selectedPlayer != null && selectedPlayer.uid == playerId)
			{
				chatScroll.AppendChatEntries(update.messages.Cast<ChatEntryData>().ToList());
			}
		}

		public void OnPlayerSelect(AdminPlayerEntryData playerData)
		{
			selectedPlayer = playerData;
			ClientGetUnreadAdminPlayerMessages(playerData.uid);
			if (!clientAdminPlayerChatLogs.ContainsKey(playerData.uid))
			{
				clientAdminPlayerChatLogs.Add(playerData.uid, new List<AdminChatMessage>());
			}

			chatScroll.LoadChatEntries(clientAdminPlayerChatLogs[playerData.uid].Cast<ChatEntryData>().ToList());
		}

		private void OnEnable()
		{
			chatScroll.OnInputFieldSubmit += OnInputSend;
			if (selectedPlayer != null)
			{
				OnPlayerSelect(selectedPlayer);
			}
		}

		private void OnDisable()
		{
			chatScroll.OnInputFieldSubmit -= OnInputSend;
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
			RequestAdminBwoink.Send(ServerData.UserID, PlayerList.Instance.AdminToken, selectedPlayer.uid,
			msg);
		}
	}
}
