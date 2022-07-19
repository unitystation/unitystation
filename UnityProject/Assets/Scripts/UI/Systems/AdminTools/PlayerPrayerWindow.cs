using System.Collections.Generic;
using Messages.Client.Admin;
using Messages.Server.AdminTools;
using UnityEngine;
using AdminCommands;

namespace AdminTools
{
	public class PlayerPrayerWindow : AdminPlayerChat
	{
		public string MessagePrefix = "You hear a voice in your head...";
		[SerializeField]
		InputFieldFocus prefixInputField = null;

		[SerializeField]
		GUI_AdminTools adminTools;

		public override void ServerAddChatRecord(string message, PlayerInfo player, PlayerInfo admin = default) 
		{
			message = admin != null ? $"<i><color=yellow>{admin.Username}: {message}</color></i>" : $"{player.Username} ({player.Name}) prays to the gods: {message}";

			if (!serverAdminPlayerChatLogs.ContainsKey(player.UserId))
			{
				serverAdminPlayerChatLogs.Add(player.UserId, new List<AdminChatMessage>());
			}

			var entry = new AdminChatMessage
			{
				fromUserid = player.UserId,
				Message = message
			};

			if (admin != null)
			{
				entry.fromUserid = admin.UserId;
				entry.wasFromAdmin = true;
			}
			serverAdminPlayerChatLogs[player.UserId].Add(entry);
			PrayerChatUpdateMessage.SendSinglePrayerEntryToAdmins(entry, player.UserId);

			if (admin != null)
			{
				AdminChatNotifications.SendToAll(player.UserId, AdminChatWindow.PrayerWindow, 0, true);
			}
			else
			{
				AdminChatNotifications.SendToAll(player.UserId, AdminChatWindow.PrayerWindow, 1);
			}

			ServerMessageRecording(player.UserId, entry);
			
		}

		public void OnHealUpButton()
		{
			AdminCommandsManager.Instance.CmdHealUpPlayer(selectedPlayer.uid);
		}

		public void GiveItemToPlayerButton()
		{
			adminTools.gameObject.SetActive(true);

			adminTools.giveItemPage.selectedPlayer = null;

			adminTools.giveItemPage.selectedPlayer = PlayerList.Instance.GetPlayerByID(selectedPlayer.uid).GameObject;

			if (adminTools.giveItemPage.selectedPlayer == null)
			{
				Logger.LogWarning("Unable to find player to give item to! Are you sure that they joined the game?");
				return;
			}
			adminTools.ShowGiveItemPagePage();
		}

		public void SendTeleportAdminToPlayerAghost()
		{
			if (PlayerManager.LocalPlayerScript.IsGhost == false)
			{
				PlayerManager.LocalPlayerScript.playerNetworkActions.CmdAGhost();
			}

			RequestAdminTeleport.Send(
				null,
				selectedPlayer.uid,
				RequestAdminTeleport.OpperationList.AdminToPlayer,
				true,
				new Vector3(0, 0, 0)
				);
		}

		public void ChangePrefix()
		{
			MessagePrefix = prefixInputField.text;
		}

		public override void OnInputSend(string message)
		{
			message = $"{MessagePrefix} {message}";

			RequestPrayerBwoink.Send(selectedPlayer.uid, message);
		}
	}
}