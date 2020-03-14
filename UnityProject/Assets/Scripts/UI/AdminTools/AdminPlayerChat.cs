using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using Mirror;
using UnityEngine;

namespace AdminTools
{
	public class AdminPlayerChat : MonoBehaviour
	{
		[SerializeField] private ChatScroll chatScroll = null;
		private AdminPlayerEntryData selectedPlayer;

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

			ServerMessageRecordingAndTrim(playerId, entry);
		}

		void ServerMessageRecordingAndTrim(string playerId, AdminChatMessage entry)
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
				File.Create(filePath);
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

			File.AppendAllText(filePath, $"[{DateTime.Now.ToString("O")}] {entryName}: {entry.Message}");

			if (serverAdminPlayerChatLogs[playerId].Count == 70)
			{
				var firstEntry = serverAdminPlayerChatLogs[playerId][0];
				serverAdminPlayerChatLogs[playerId].Remove(firstEntry);
			}
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
			update.messages = serverAdminPlayerChatLogs[playerId].GetRange(currentCount - 1,
				serverAdminPlayerChatLogs[playerId].Count - currentCount);

			//FIXME: MESSAGE BACK!!
			//TargetUpdateChatLog(requestee, JsonUtility.ToJson(update), playerId);
		}

		void ClientGetUnreadAdminPlayerMessages(string playerId)
		{
			if (!clientAdminPlayerChatLogs.ContainsKey(playerId))
			{
				clientAdminPlayerChatLogs.Add(playerId, new List<AdminChatMessage>());
			}

			AdminCheckMessages.Send(playerId, clientAdminPlayerChatLogs[playerId].Count);
		}

		//FIXME THE MESSAGE BACK TO CLIENT
		public void TargetUpdateChatLog(string unreadMessagesJson, string playerId)
		{
			if (string.IsNullOrEmpty(unreadMessagesJson)) return;

			if (!clientAdminPlayerChatLogs.ContainsKey(playerId))
			{
				clientAdminPlayerChatLogs.Add(playerId, new List<AdminChatMessage>());
			}

			clientAdminPlayerChatLogs[playerId].AddRange(JsonUtility.FromJson<AdminChatUpdate>(unreadMessagesJson).messages);
		}

		public void OnPlayerSelect(AdminPlayerEntryData playerData)
		{
			selectedPlayer = playerData;
			ClientGetUnreadAdminPlayerMessages(playerData.uid);
			if (!clientAdminPlayerChatLogs.ContainsKey(playerData.uid))
			{
				clientAdminPlayerChatLogs.Add(playerData.uid, new List<AdminChatMessage>());
			}


		}
	}
}
