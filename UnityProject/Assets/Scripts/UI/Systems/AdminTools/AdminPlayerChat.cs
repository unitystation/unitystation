using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using SecureStuff;
using Mirror;
using UnityEngine;
using DiscordWebhook;
using Logs;
using Messages.Client.Admin;
using Messages.Server.AdminTools;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using TMPro;
using UnityEngine.UI;


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


		public TMP_Dropdown RoundIDsDropDown;


		public static string ChatLogsFolder => "Chatlogs";


		/// <summary>
		/// The admins client local cache for admin to player chat
		/// </summary>
		private readonly Dictionary<string, Dictionary<int,  List<AdminChatMessage>>> clientAdminPlayerChatLogs = new();

		public void Awake()
		{
			RoundIDsDropDown.options.Clear();
		}

		public void ClearLogs()
		{

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

			DiscordWebhookMessage.Instance.AddWebHookMessageToQueue(DiscordWebhookURLs.DiscordWebhookAdminURL, entry.Message, entryName);

			AccessFile.AppendAllText(filePath, JsonConvert.SerializeObject(entry) + "\n", FolderType.Logs);
		}

		public string GetFilePath(int RoundID, string playerId)
		{
			return Path.Combine(ChatLogsFolder, playerId, $"{RoundID}.txt");
		}

		public string LoadData(string filePath)
		{
			var data = "";
			try
			{
				data = AccessFile.Load(filePath, FolderType.Logs, false);
			}
			catch (Exception e)
			{
				Loggy.Error($"Exception during file read: {e}");
			}

			return data;
		}

		public void ServerGetMessageRound(string playerId, NetworkConnection requestee)
		{
			var Rounds = AccessFile.DirectoriesOrFilesIn(Path.Combine(ChatLogsFolder, playerId), FolderType.Logs);

			AdminPlayerChatRoundsMessage.SendAvailableRoundsToAdmin(requestee, playerId, Rounds);
		}

		public void ServerGetUnreadMessages(string playerId, int currentCount, int RoundID, NetworkConnection requestee)
		{
			bool ForceShow = false;
			if (RoundID == -1)
			{
				ForceShow = true;
				var Rounds = AccessFile.DirectoriesOrFilesIn(Path.Combine(ChatLogsFolder, playerId), FolderType.Logs).OrderByDescending(x => x);
				var data = Rounds.First().Replace(".txt", "");
				RoundID = int.Parse( data, NumberStyles.Integer);
			}

			var filePath = GetFilePath(RoundID, playerId);
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

			AdminPlayerChatUpdateMessage.SendLogUpdateToAdmin(requestee, update, playerId, RoundID, ForceShow);
		}

		protected void ClientGetUnreadAdminPlayerMessages(string playerId, int CurrentCount, int RoundID)
		{
			if (clientAdminPlayerChatLogs.ContainsKey(playerId) == false)
			{
				clientAdminPlayerChatLogs.Add(playerId, new Dictionary<int, List<AdminChatMessage>>( ));
			}

			AdminChatRequestRounds.Send(playerId);
			AdminCheckMessages.Send(playerId,CurrentCount, RoundID);
		}

		public void getMessagesForRound()
		{
			if (clientAdminPlayerChatLogs.ContainsKey(selectedPlayer.uid) == false)
			{
				clientAdminPlayerChatLogs[selectedPlayer.uid] = new Dictionary<int, List<AdminChatMessage>>();
			}

			int roundID = int.Parse(RoundIDsDropDown.options[RoundIDsDropDown.value].text.Replace(".txt", ""));
			if (clientAdminPlayerChatLogs[selectedPlayer.uid].ContainsKey(roundID) == false)
			{
				clientAdminPlayerChatLogs[selectedPlayer.uid][roundID] = new List<AdminChatMessage>();
			}


			AdminCheckMessages.Send(selectedPlayer.uid,
				clientAdminPlayerChatLogs[selectedPlayer.uid][roundID].Count,
				roundID);

			chatScroll.LoadChatEntries(clientAdminPlayerChatLogs[selectedPlayer.uid][roundID].Cast<ChatEntryData>().ToList());
		}

		public void ClientUpdateAvailableRounds( string playerId, string[] RoundIDs)
		{
			if (selectedPlayer.uid == playerId)
			{
				RoundIDs = RoundIDs.OrderByDescending(x => x).ToArray();
				List<TMP_Dropdown.OptionData> optionDatas = new List<TMP_Dropdown.OptionData>();
				foreach (var RoundID in RoundIDs)
				{
					optionDatas.Add(new TMP_Dropdown.OptionData
					{
						text = RoundID
					});


				}
				RoundIDsDropDown.options = optionDatas;
				RoundIDsDropDown.SetValueWithoutNotify( 0);

			}
		}


		public void ClientUpdateChatLog(string unreadMessagesJson, string playerId, int RoundID, bool ForceShow)
		{

			if (string.IsNullOrEmpty(unreadMessagesJson)) return;

			if (clientAdminPlayerChatLogs.ContainsKey(playerId) == false)
			{
				clientAdminPlayerChatLogs.Add(playerId, new Dictionary<int, List<AdminChatMessage>>());
			}

			if (clientAdminPlayerChatLogs[playerId].ContainsKey(RoundID) == false)
			{
				clientAdminPlayerChatLogs[playerId].Add(RoundID, new List<AdminChatMessage>());
			}

			var update = JsonConvert.DeserializeObject<AdminChatUpdate>(unreadMessagesJson);
			clientAdminPlayerChatLogs[playerId][RoundID].AddRange(update.messages);

			if ((selectedPlayer != null
			     && selectedPlayer.uid == playerId
			     &&  (int.Parse(RoundIDsDropDown.options[RoundIDsDropDown.value].text.Replace(".txt", "")) == RoundID)
			     )
				|| ForceShow)
			{
				if (RoundIDsDropDown.options[RoundIDsDropDown.value].text == "Option A")
				{
					return;
				}
				if (ForceShow && int.Parse(RoundIDsDropDown.options[RoundIDsDropDown.value].text.Replace(".txt", "")) !=
				    RoundID)
				{
					var match = RoundIDsDropDown.options
						.Select((Option, index) => (Option, index))
						.FirstOrDefault(x => x.Option.text.Replace(".txt", "") == RoundID.ToString());

					if (match.Option != null) // Check if a match was found
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

			if (clientAdminPlayerChatLogs.ContainsKey(playerData.uid) == false)
			{
				clientAdminPlayerChatLogs.Add(playerData.uid, new Dictionary<int, List<AdminChatMessage>>( ));
			}

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
