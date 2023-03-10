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

			if (!serverAdminPlayerChatLogs.ContainsKey(player.AccountId))
			{
				serverAdminPlayerChatLogs.Add(player.AccountId, new List<AdminChatMessage>());
			}

			var entry = new AdminChatMessage
			{
				fromUserid = player.AccountId,
				Message = message
			};

			if (admin != null)
			{
				entry.fromUserid = admin.AccountId;
				entry.wasFromAdmin = true;
			}
			serverAdminPlayerChatLogs[player.AccountId].Add(entry);
			PrayerChatUpdateMessage.SendSinglePrayerEntryToAdmins(entry, player.AccountId);

			if (admin != null)
			{
				AdminChatNotifications.SendToAll(player.AccountId, AdminChatWindow.PrayerWindow, 0, true);
			}
			else
			{
				AdminChatNotifications.SendToAll(player.AccountId, AdminChatWindow.PrayerWindow, 1);
			}

			ServerMessageRecording(player.AccountId, entry);

		}

		public void OnHealUpButton()
		{
			AdminCommandsManager.Instance.CmdHealUpPlayer(SelectedPlayer.uid);
		}

		public void GiveItemToPlayerButton()
		{
			adminTools.gameObject.SetActive(true);

			adminTools.giveItemPage.selectedPlayerId = SelectedPlayer.uid;

			adminTools.ShowGiveItemPagePage();
		}

		public void SendTeleportAdminToPlayerAghost()
		{
			PlayerManager.LocalMindScript.CmdAGhost();
			RequestAdminTeleport.Send(
				null,
				SelectedPlayer.uid,
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

			RequestPrayerBwoink.Send(SelectedPlayer.uid, message);
		}
	}
}