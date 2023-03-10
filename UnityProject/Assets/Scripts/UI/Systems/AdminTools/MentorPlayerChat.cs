using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;
using Mirror;
using DiscordWebhook;
using Messages.Client.Admin;
using Messages.Server.AdminTools;


namespace AdminTools
{
	public class MentorPlayerChat : MonoBehaviour
	{
		[SerializeField] private ChatScroll chatScroll = null;
		private AdminPlayerEntryData selectedPlayer;
		public AdminPlayerEntryData SelectedPlayer => selectedPlayer;

		/// <summary>
		/// All messages sent and recieved from players to mentors
		/// </summary>
		private readonly Dictionary<string, List<AdminChatMessage>> serverMentorPlayerChatLogs = new();

		/// <summary>
		/// The mentors client local cache for mentor to player chat
		/// </summary>
		private readonly Dictionary<string, List<AdminChatMessage>> clientMentorPlayerChatLogs = new();

		public void ClearLogs()
		{
			serverMentorPlayerChatLogs.Clear();
			clientMentorPlayerChatLogs.Clear();
		}

		public void ServerAddChatRecord(string message, PlayerInfo player, PlayerInfo mentor = default)
		{
			message = mentor == null
				? $"{player.Username}: {message}"
				: $"{mentor.Username}: {message}";

			if (!serverMentorPlayerChatLogs.ContainsKey(player.AccountId))
			{
				serverMentorPlayerChatLogs.Add(player.AccountId, new List<AdminChatMessage>());
			}

			var entry = new AdminChatMessage
			{
				fromUserid = player.AccountId,
				Message = message
			};

			if (mentor != null)
			{
				entry.fromUserid = mentor.AccountId;
				entry.wasFromAdmin = true;
			}
			serverMentorPlayerChatLogs[player.AccountId].Add(entry);
			MentorPlayerChatUpdateMessage.SendSingleEntryToMentors(entry, player.AccountId);
			if (mentor != null)
			{
				AdminChatNotifications.SendToAll(player.AccountId, AdminChatWindow.MentorPlayerChat, 0, true);
			}
			else
			{
				AdminChatNotifications.SendToAll(player.AccountId, AdminChatWindow.MentorPlayerChat, 1);
			}

			ServerMessageRecording(player.AccountId, entry);
		}

		private void ServerMessageRecording(string playerId, AdminChatMessage entry)
		{
			if (PlayerList.Instance.TryGetByUserID(playerId, out var player) == false)
			{
				Logger.LogError($"Could not find player with ID '{playerId}'. Unable to record mentor dialogue.");
				return;
			}

			var chatlogDir = Path.Combine(Application.streamingAssetsPath, "chatlogs");
			if (!Directory.Exists(chatlogDir))
			{
				Directory.CreateDirectory(chatlogDir);
			}

			var filePath = Path.Combine(chatlogDir, $"{playerId}-mentor.txt");

			if (!File.Exists(filePath))
			{
				var stream = File.Create(filePath);
				stream.Close();
				string header = $"Username: {player.Username} Character Name: {player.Name} \r\n" +
				                $"IsAntag: {PlayerList.Instance.AntagPlayers.Contains(player)}  role: {player.Job} \r\n" +
				                $"-----Chat Log----- \r\n" +
				                $" \r\n";
				File.AppendAllText(filePath, header);
			}

			string entryName = player.Name;
			if (entry.wasFromAdmin && PlayerList.Instance.TryGetByUserID(entry.fromUserid, out var mentorPlayer))
			{
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
			RequestMentorBwoink.Send(selectedPlayer.uid, message);
		}
	}
}
