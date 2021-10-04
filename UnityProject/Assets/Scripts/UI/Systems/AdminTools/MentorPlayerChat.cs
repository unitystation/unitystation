using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;
using Mirror;
using DatabaseAPI;
using DiscordWebhook;
using Messages.Client.Admin;
using Messages.Server.AdminTools;


namespace AdminTools
{
	public class MentorPlayerChat : MonoBehaviour
	{
		[SerializeField] private ChatScroll chatScroll = null;
		private AdminPlayerEntryData selectedPlayer;
		public AdminPlayerEntryData SelectedPlayer
		{
			get { return selectedPlayer; }
		}
		/// <summary>
		/// All messages sent and recieved from players to mentors
		/// </summary>
		private Dictionary<string, List<AdminChatMessage>> serverMentorPlayerChatLogs
			= new Dictionary<string, List<AdminChatMessage>>();

		/// <summary>
		/// The mentors client local cache for mentor to player chat
		/// </summary>
		private Dictionary<string, List<AdminChatMessage>> clientMentorPlayerChatLogs
			= new Dictionary<string, List<AdminChatMessage>>();

		public void ClearLogs()
		{
			serverMentorPlayerChatLogs.Clear();
			clientMentorPlayerChatLogs.Clear();
		}

		public void ServerAddChatRecord(string message, string playerId, string mentorId = "")
		{
			if (!serverMentorPlayerChatLogs.ContainsKey(playerId))
			{
				serverMentorPlayerChatLogs.Add(playerId, new List<AdminChatMessage>());
			}

			var entry = new AdminChatMessage
			{
				fromUserid = playerId,
				Message = message
			};

			if (!string.IsNullOrEmpty(mentorId))
			{
				entry.fromUserid = mentorId;
				entry.wasFromAdmin = true;
			}
			serverMentorPlayerChatLogs[playerId].Add(entry);
			MentorPlayerChatUpdateMessage.SendSingleEntryToMentors(entry, playerId);
			if (!string.IsNullOrEmpty(mentorId))
			{
				AdminChatNotifications.SendToAll(playerId, AdminChatWindow.MentorPlayerChat, 0, true);
			}
			else
			{
				AdminChatNotifications.SendToAll(playerId, AdminChatWindow.MentorPlayerChat, 1);
			}

			ServerMessageRecording(playerId, entry);
		}

		private void ServerMessageRecording(string playerId, AdminChatMessage entry)
		{
			var chatlogDir = Path.Combine(Application.streamingAssetsPath, "chatlogs");
			if (!Directory.Exists(chatlogDir))
			{
				Directory.CreateDirectory(chatlogDir);
			}

			var filePath = Path.Combine(chatlogDir, $"{playerId}-mentor.txt");

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
				var mentorPlayer = PlayerList.Instance.GetByUserID(entry.fromUserid);
				entryName = "[Mentor] " + mentorPlayer.Name;
			}

			DiscordWebhookMessage.Instance.AddWebHookMessageToQueue(DiscordWebhookURLs.DiscordWebhookAdminURL, entry.Message, entryName);

			File.AppendAllText(filePath, $"[{DateTime.Now.ToString("O")}] {entryName}: {entry.Message}");
		}

		public void ServerGetUnreadMessages(string playerId, int currentCount, NetworkConnection requestee)
		{
			if (!serverMentorPlayerChatLogs.ContainsKey(playerId))
			{
				serverMentorPlayerChatLogs.Add(playerId, new List<AdminChatMessage>());
			}

			if (currentCount >= serverMentorPlayerChatLogs[playerId].Count)
			{
				return;
			}

			AdminChatUpdate update = new AdminChatUpdate();

			update.messages = serverMentorPlayerChatLogs[playerId].GetRange(currentCount,
				serverMentorPlayerChatLogs[playerId].Count - currentCount);

			AdminPlayerChatUpdateMessage.SendLogUpdateToAdmin(requestee, update, playerId);
		}

		private void ClientGetUnreadAdminPlayerMessages(string playerId)
		{
			if (!clientMentorPlayerChatLogs.ContainsKey(playerId))
			{
				clientMentorPlayerChatLogs.Add(playerId, new List<AdminChatMessage>());
			}

			AdminCheckMessages.Send(playerId, clientMentorPlayerChatLogs[playerId].Count);
		}

		public void ClientUpdateChatLog(string unreadMessagesJson, string playerId)
		{
			if (string.IsNullOrEmpty(unreadMessagesJson)) return;

			if (!clientMentorPlayerChatLogs.ContainsKey(playerId))
			{
				clientMentorPlayerChatLogs.Add(playerId, new List<AdminChatMessage>());
			}

			var update = JsonUtility.FromJson<AdminChatUpdate>(unreadMessagesJson);
			clientMentorPlayerChatLogs[playerId].AddRange(update.messages);

			if (selectedPlayer != null && selectedPlayer.uid == playerId)
			{
				chatScroll.AppendChatEntries(update.messages.Cast<ChatEntryData>().ToList());
			}
		}

		public void OnPlayerSelect(AdminPlayerEntryData playerData)
		{
			selectedPlayer = playerData;
			ClientGetUnreadAdminPlayerMessages(playerData.uid);
			if (!clientMentorPlayerChatLogs.ContainsKey(playerData.uid))
			{
				clientMentorPlayerChatLogs.Add(playerData.uid, new List<AdminChatMessage>());
			}

			chatScroll.LoadChatEntries(clientMentorPlayerChatLogs[playerData.uid].Cast<ChatEntryData>().ToList());
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
			RequestMentorBwoink.Send(selectedPlayer.uid, $"{ServerData.Auth.CurrentUser.DisplayName}: {message}");
		}
	}
}
