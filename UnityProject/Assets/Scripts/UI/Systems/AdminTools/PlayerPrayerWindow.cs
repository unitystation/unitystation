using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Mirror;
using UnityEngine;
using DatabaseAPI;
using DiscordWebhook;
using Messages.Client.Admin;
using Messages.Server.AdminTools;

namespace AdminTools
{
	public class PlayerPrayerWindow : AdminPlayerChat
	{
		public override void ServerAddChatRecord(string message, PlayerInfo player, PlayerInfo admin = default) 
		{
			if (admin != null)
			{
				Chat.AddExamineMsgFromServer(admin.GameObject, $"You whisper into {player.Name}'s head.");
				Chat.AddExamineMsgFromServer(player.GameObject, $"You hear a voice in your head... {message}");
				return;
			}

			message = $"{player.Username} prays to the gods: {message}";

			if (!serverAdminPlayerChatLogs.ContainsKey(player.UserId))
			{
				serverAdminPlayerChatLogs.Add(player.UserId, new List<AdminChatMessage>());
			}

			var entry = new AdminChatMessage
			{
				fromUserid = player.UserId,
				Message = message
			};

			serverAdminPlayerChatLogs[player.UserId].Add(entry);
			PrayerChatUpdateMessage.SendSingleEntryToAdmins(entry, player.UserId);

			AdminChatNotifications.SendToAll(player.UserId, AdminChatWindow.PrayerWindow, 1);


			ServerMessageRecording(player.UserId, entry);
		}
	}
}