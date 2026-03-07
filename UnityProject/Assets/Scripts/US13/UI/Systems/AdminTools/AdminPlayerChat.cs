using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using Logs;
using Mirror;
using Newtonsoft.Json;
using SecureStuff;
using TMPro;
using UnityEngine;
using US13.Core.Chat;
using US13.Core.Chat.ChatScrollRect;
using US13.Core.Database;
using US13.Managers;
using US13.Messages.Client.Admin;
using US13.Messages.Server.AdminTools;

namespace US13.UI.Systems.AdminTools
{
	public class AdminPlayerChat : MonoBehaviour
	{
		[SerializeField] protected ChatScroll chatScroll = null;
		protected AdminPlayerEntryData selectedPlayer;
		public AdminPlayerEntryData SelectedPlayer => selectedPlayer;


		public TMP_Dropdown RoundIDsDropDown;


		public static string ChatLogsFolder => "Chatlogs";


		/// <summary>
		/// The admins client local cache for admin to player chat
		/// </summary>
		private readonly Dictionary<string, Dictionary<int,  List<AdminChatMessage>>> clientAdminPlayerChatLogs = new();

		public Dictionary<string, Dictionary<int, List<AdminChatMessage>>> ClientAdminPlayerChatLogs =>
			clientAdminPlayerChatLogs;

		public void Awake()
		{
			RoundIDsDropDown.options.Clear();
		}

		public void ClearLogs()
		{
			clientAdminPlayerChatLogs.Clear();
		}

		public virtual void ServerAddChatRecord(string message, PlayerInfo player, PlayerInfo admin = default)
		{
			message = admin == null
				? $"{player.Username}: {message}"
				: $"{admin.Username}: {message}";


			var entry = new AdminChatMessage
			{
				fromUserid = player.AccountId,
				Message = GameManager.Instance.RoundTime.ToString(@"hh\:mm\:ss") + " - " +  message
			};

			if (admin != null)
			{
				entry.fromUserid = admin.AccountId;
				entry.wasFromAdmin = true;
			}

			AdminPlayerChatUpdateMessage.SendSingleEntryToAdmins(entry, player.AccountId, GameManager.RoundID, true);
			if (admin != null)
			{
				AdminChatNotifications.SendToAll(player.AccountId, AdminChatWindow.AdminPlayerChat, 0, true);
			}
			else
			{
				AdminChatNotifications.SendToAll(player.AccountId, AdminChatWindow.AdminPlayerChat, 1);
			}

			ServerMessageRecording(player.AccountId, entry);
		}

		public void ServerMessageRecording(string playerId, AdminChatMessage entry)
		{
			var filePath = GetFilePath(GameManager.RoundID, playerId);

			if (AccessFile.Exists(filePath, true, FolderType.Logs) == false)
			{
				if (PlayerList.Instance.TryGetByUserID(playerId, out var player) == false)
				{
					Loggy.Error($"Could not find player with ID '{playerId}'. Unable to record admin dialogue.");
					return;
				}

				AdminChatMessage header = new AdminChatMessage()
				{
					fromUserid = "SYSTEM",
					wasFromAdmin = true,
					Message = $"Username: {player.Username} Character Name: {player.Name} " +
					          $"IsAntag: {PlayerList.Instance.AntagPlayers.Contains(player)}  role: {player.Job} " +
					          $"-----Chat Log----- " +
					          $""
				};
				AccessFile.AppendAllText(filePath,  JsonConvert.SerializeObject(header) +  "\n" , FolderType.Logs);
			}

			string entryName = entry.fromUserid;
			if (entry.wasFromAdmin && PlayerList.Instance.TryGetByUserID(entry.fromUserid, out var adminPlayer))
			{
				entryName = "[A] " + adminPlayer.Name;
			}

			string mentionID = null;
			var discordMessage = entry.Message;

			bool isPlayerMessage = entry.wasFromAdmin == false;
			bool noAdminsOnline = PlayerList.Instance.AnyWithTAG(TAG.ADMIN_CHAT) == false;

			if (isPlayerMessage && noAdminsOnline)
			{
				mentionID = ServerData.ServerConfig.DiscordWebhookOOCMentionsID;
				discordMessage = $"@ServerAdmin someone needs help in game and there are no admins online!\n{entry.Message}";
			}

			DiscordWebhookMessage.Instance.AddWebHookMessageToQueue(DiscordWebhookURLs.DiscordWebhookAdminURL, discordMessage, entryName, mentionID);

			AccessFile.AppendAllText(filePath, JsonConvert.SerializeObject(entry) + "\n", FolderType.Logs);
		}

		public string GetFilePath(int roundID, string playerId)
		{
			return Path.Combine(ChatLogsFolder, playerId, $"{roundID}.txt");
		}

		private static string ParseRoundId(string fileName)
		{
			return fileName.Replace(".txt", "");
		}

		public string LoadData(string filePath)
		{
			try
			{
				return AccessFile.Load(filePath, FolderType.Logs, false);
			}
			catch (Exception e)
			{
				Loggy.Error($"Exception during file read: {e}");
				return string.Empty;
			}
		}

		public void ServerGetMessageRound(string playerId, NetworkConnection requestee)
		{
			var playerLogPath = Path.Combine(ChatLogsFolder, playerId);
			if (AccessFile.Exists(playerLogPath, false, FolderType.Logs))
			{
				var rounds = AccessFile.DirectoriesOrFilesIn(playerLogPath, FolderType.Logs);
				AdminPlayerChatRoundsMessage.SendAvailableRoundsToAdmin(requestee, playerId, rounds);
			}
		}

		public void ServerGetUnreadMessages(string playerId, int currentCount, int roundID, NetworkConnection requestee)
		{
			bool forceShow = false;
			if (roundID == -1)
			{
				forceShow = true;
				if (AccessFile.Exists(Path.Combine(ChatLogsFolder, playerId), false, FolderType.Logs) == false) return;

				var rounds = AccessFile.DirectoriesOrFilesIn(Path.Combine(ChatLogsFolder, playerId), FolderType.Logs).OrderByDescending(x => x);
				var data = ParseRoundId(rounds.First());
				roundID = int.Parse(data, NumberStyles.Integer);
			}

			var filePath = GetFilePath(roundID, playerId);
			if (AccessFile.Exists(filePath, true, FolderType.Logs, false) == false)
			{
				return;
			}
			string[] logLines = LoadData(filePath).Split(new[] {'\n'}, StringSplitOptions.RemoveEmptyEntries);
			if (currentCount >= logLines.Length)
			{
				return;
			}

			AdminChatUpdate update = new AdminChatUpdate()
			{
				messages = logLines.ToList().GetRange(currentCount,
					logLines.Length - currentCount).Select(JsonConvert.DeserializeObject<AdminChatMessage>).ToList()
			};

			AdminPlayerChatUpdateMessage.SendLogUpdateToAdmin(requestee, update, playerId, roundID, forceShow);
		}

		protected void ClientGetUnreadAdminPlayerMessages(string playerId, int currentCount, int roundID)
		{
			clientAdminPlayerChatLogs.TryAdd(playerId, new Dictionary<int, List<AdminChatMessage>>());

			AdminChatRequestRounds.Send(playerId);
			AdminCheckMessages.Send(playerId, currentCount, roundID);
		}

		public void getMessagesForRound()
		{
			clientAdminPlayerChatLogs.TryAdd(selectedPlayer.uid, new Dictionary<int, List<AdminChatMessage>>());

			int roundID = int.Parse(ParseRoundId(RoundIDsDropDown.options[RoundIDsDropDown.value].text));
			clientAdminPlayerChatLogs[selectedPlayer.uid].TryAdd(roundID, new List<AdminChatMessage>());


			AdminCheckMessages.Send(selectedPlayer.uid,
				clientAdminPlayerChatLogs[selectedPlayer.uid][roundID].Count,
				roundID);

			chatScroll.LoadChatEntries(clientAdminPlayerChatLogs[selectedPlayer.uid][roundID].Cast<ChatEntryData>().ToList());
		}

		public void ClientUpdateAvailableRounds(string playerId, string[] roundIDs)
		{
			if (selectedPlayer.uid == playerId)
			{
				roundIDs = roundIDs.OrderByDescending(x => x).ToArray();
				List<TMP_Dropdown.OptionData> optionDatas = new List<TMP_Dropdown.OptionData>();
				foreach (var roundID in roundIDs)
				{
					optionDatas.Add(new TMP_Dropdown.OptionData
					{
						text = roundID
					});


				}
				RoundIDsDropDown.options = optionDatas;
				RoundIDsDropDown.SetValueWithoutNotify( 0);

			}
		}


		public void ClientUpdateChatLog(string unreadMessagesJson, string playerId, int roundID, bool forceShow)
		{
			if (string.IsNullOrEmpty(unreadMessagesJson)) return;

			clientAdminPlayerChatLogs.TryAdd(playerId, new Dictionary<int, List<AdminChatMessage>>());
			clientAdminPlayerChatLogs[playerId].TryAdd(roundID, new List<AdminChatMessage>());

			var update = JsonConvert.DeserializeObject<AdminChatUpdate>(unreadMessagesJson);
			clientAdminPlayerChatLogs[playerId][roundID].AddRange(update.messages);

			if ((selectedPlayer != null
			     && selectedPlayer.uid == playerId
			     && (RoundIDsDropDown.options.Count == 0 || (int.Parse(ParseRoundId(RoundIDsDropDown.options[RoundIDsDropDown.value].text)) == roundID))
			     )
				|| forceShow)
			{
				if (RoundIDsDropDown.options.Count > 0 && forceShow && int.Parse(ParseRoundId(RoundIDsDropDown.options[RoundIDsDropDown.value].text)) !=
				    roundID)
				{
					var match = RoundIDsDropDown.options
						.Select((option, index) => (option, index))
						.FirstOrDefault(x => ParseRoundId(x.option.text) == roundID.ToString());

					if (match.option != null)
					{
						RoundIDsDropDown.value = match.index;
					}
					else
					{
						AdminChatRequestRounds.Send(playerId);
						chatScroll.LoadChatEntries(update.messages.Cast<ChatEntryData>().ToList());
					}
				}
				else
				{
					chatScroll.AppendChatEntries(update.messages.Cast<ChatEntryData>().ToList());
				}
			}
		}

		public void OnPlayerSelect(AdminPlayerEntryData playerData)
		{
			selectedPlayer = playerData;

			clientAdminPlayerChatLogs.TryAdd(playerData.uid, new Dictionary<int, List<AdminChatMessage>>());

			var firstOrDefault = clientAdminPlayerChatLogs[playerData.uid].OrderByDescending(x => x.Key).FirstOrDefault();

			if (firstOrDefault.Value == null)
			{
				ClientGetUnreadAdminPlayerMessages(playerData.uid, 0, -1);
				chatScroll.LoadChatEntries(new List<ChatEntryData>());
				return;
			}
			else
			{
				ClientGetUnreadAdminPlayerMessages(playerData.uid, firstOrDefault.Value.Count, firstOrDefault.Key);
			}

			chatScroll.LoadChatEntries(firstOrDefault.Value.Cast<ChatEntryData>().ToList());
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
