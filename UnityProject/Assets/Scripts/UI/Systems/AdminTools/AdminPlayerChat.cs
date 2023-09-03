using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using SecureStuff;
using Mirror;
using UnityEngine;
using DatabaseAPI;
using DiscordWebhook;
using Logs;
using Messages.Client.Admin;
using Messages.Server.AdminTools;
using Newtonsoft.Json;


namespace AdminTools
{
	public class AdminPlayerChat : MonoBehaviour
	{
		[SerializeField] protected ChatScroll chatScroll = null;
		protected AdminPlayerEntryData selectedPlayer;
		public AdminPlayerEntryData SelectedPlayer
		{
			get { return selectedPlayer; }
		}

		public static string ChatLogsFolder => "Chatlogs";

		/// <summary>
		/// All messages sent and recieved from players to admins
		/// </summary>
		protected readonly Dictionary<string, List<AdminChatMessage>> serverAdminPlayerChatLogs
				= new Dictionary<string, List<AdminChatMessage>>();

		/// <summary>
		/// The admins client local cache for admin to player chat
		/// </summary>
		protected readonly Dictionary<string, List<AdminChatMessage>> clientAdminPlayerChatLogs
				= new Dictionary<string, List<AdminChatMessage>>();

		public void ClearLogs()
		{
			serverAdminPlayerChatLogs.Clear();
			clientAdminPlayerChatLogs.Clear();
		}

		public virtual void ServerAddChatRecord(string message, PlayerInfo player, PlayerInfo admin = default)
		{
			message = admin == null
				? $"{player.Username}: {message}"
				: $"{admin.Username}: {message}";

			if (!serverAdminPlayerChatLogs.ContainsKey(player.UserId))
			{
				serverAdminPlayerChatLogs.Add(player.UserId, new List<AdminChatMessage>());
			}

			var entry = new AdminChatMessage
			{
				fromUserid = player.UserId,
				Message = GameManager.Instance.RoundTime.ToString(@"hh\:mm\:ss") + " - " +  message
			};

			if (admin != null)
			{
				entry.fromUserid = admin.UserId;
				entry.wasFromAdmin = true;
			}
			serverAdminPlayerChatLogs[player.UserId].Add(entry);
			AdminPlayerChatUpdateMessage.SendSingleEntryToAdmins(entry, player.UserId);
			if (admin != null)
			{
				AdminChatNotifications.SendToAll(player.UserId, AdminChatWindow.AdminPlayerChat, 0, true);
			}
			else
			{
				AdminChatNotifications.SendToAll(player.UserId, AdminChatWindow.AdminPlayerChat, 1);
			}

			ServerMessageRecording(player.UserId, entry);
		}

		public void ServerMessageRecording(string playerId, AdminChatMessage entry)
		{
			if (PlayerList.Instance.TryGetByUserID(playerId, out var player) == false)
			{
				Loggy.LogError($"Could not find player with ID '{playerId}'. Unable to record admin dialogue.");
				return;
			}

			var filePath = Path.Combine(ChatLogsFolder, $"{playerId}.txt");

			if (AccessFile.Exists(filePath, true, FolderType.Logs) == false)
			{
				string header = $"Username: {player.Username} Character Name: {player.Name} \r\n" +
				                $"IsAntag: {PlayerList.Instance.AntagPlayers.Contains(player)}  role: {player.Job} \r\n" +
				                $"-----Chat Log----- \r\n" +
				                $" \r\n";
				AccessFile.AppendAllText(filePath, header, FolderType.Logs);
			}

			string entryName = player.Name;
			if (entry.wasFromAdmin && PlayerList.Instance.TryGetByUserID(entry.fromUserid, out var adminPlayer))
			{
				entryName = "[A] " + adminPlayer.Name;
			}

			DiscordWebhookMessage.Instance.AddWebHookMessageToQueue(DiscordWebhookURLs.DiscordWebhookAdminURL, entry.Message, entryName);

			AccessFile.AppendAllText(filePath, $"[{DateTime.Now.ToString("O")}] {entryName}: {entry.Message}", FolderType.Logs);
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

			AdminChatUpdate update = new AdminChatUpdate()
			{
				messages = serverAdminPlayerChatLogs[playerId].GetRange(currentCount,
				serverAdminPlayerChatLogs[playerId].Count - currentCount)
			};

			AdminPlayerChatUpdateMessage.SendLogUpdateToAdmin(requestee, update, playerId);
		}

		protected void ClientGetUnreadAdminPlayerMessages(string playerId)
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

			var update = JsonConvert.DeserializeObject<AdminChatUpdate>(unreadMessagesJson);
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

		protected void OnEnable()
		{
			chatScroll.OnInputFieldSubmit += OnInputSend;
			if (selectedPlayer != null)
			{
				OnPlayerSelect(selectedPlayer);
			}
		}

		protected void OnDisable()
		{
			chatScroll.OnInputFieldSubmit -= OnInputSend;
		}

		public virtual void OnInputSend(string message)
		{
			RequestAdminBwoink.Send(selectedPlayer.uid, message);
		}
	}
}
