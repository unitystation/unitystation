using AdminCommands;
using DatabaseAPI;
using Logs;
using Messages.Client.Admin;
using UI.AdminTools;
using UI.Systems.AdminTools;
using UnityEngine;
using UnityEngine.UI;


namespace AdminTools
{
	public class PlayerManagePage : AdminPage
	{
		[SerializeField] private Toggle mentorToggle = null;
		[SerializeField] private Toggle quickRespawnToggle = default;
		[SerializeField] private Text mentorButtonText = null;
		[SerializeField] private AdminRespawnPage adminRespawnPage = default;

		[SerializeField] private Text oocMuteButtonText = null;


		public AdminPlayerEntry PlayerEntry { get; private set; }

		public void SetData(AdminPlayerEntry entry)
		{
			PlayerEntry = entry;

			mentorButtonText.text = entry.PlayerData.isMentor ? "REMOVE MENTOR" : "MAKE MENTOR";
			mentorToggle.gameObject.SetActive(entry.PlayerData.isMentor == false);

			oocMuteButtonText.text = entry.PlayerData.isOOCMuted ? "Unmute OOC" : "Mute OOC";
		}

		public void OnKickBtn()
		{
			adminTools.kickBanEntryPage.SetPage(false, PlayerEntry.PlayerData, false);
		}

		public void OnBanBtn()
		{
			adminTools.kickBanEntryPage.SetPage(true, PlayerEntry.PlayerData, false);
		}

		public void OnJobBanBtn()
		{
			adminTools.kickBanEntryPage.SetPage(false, PlayerEntry.PlayerData, true);
		}

		public void OnSmiteBtn()
		{
			adminTools.areYouSurePage.SetAreYouSurePage(
				$"Are you sure you want to smite {PlayerEntry.PlayerData.name}?",
				SendSmitePlayerRequest);
		}

		public void OnDeputiseBtn()
		{
			if (PlayerEntry.PlayerData.isMentor == false)
			{
				adminTools.areYouSurePage.SetAreYouSurePage(
					$"Are you sure you want to make {PlayerEntry.PlayerData.accountName} a {(mentorToggle.isOn ? "temporary" : "permanent")} mentor?",
					SendMakePlayerMentorRequest);

				return;
			}

			adminTools.areYouSurePage.SetAreYouSurePage(
				$"Are you sure you want to remove {PlayerEntry.PlayerData.accountName} mentor?",
				SendRemovePlayerMentorRequest);
		}

		public void OnRespawnButton()
		{
			if (quickRespawnToggle.isOn)
			{
				Occupation spawnOcc = new Occupation();
				foreach (var connectedPlayer in PlayerList.Instance.AllPlayers)
				{
					if(connectedPlayer.UserId != PlayerEntry.PlayerData.uid) continue;
					spawnOcc = connectedPlayer.Script.Mind.occupation;
				}
				if (spawnOcc == null)
				{
					Loggy.LogError("Cannot find Occupation for selected player, they most likely haven't joined yet.");
					return;
				}
				RequestRespawnPlayer.SendNormalRespawn(PlayerEntry.PlayerData.uid, spawnOcc);
				return;
			}
			adminRespawnPage.SetTabsWithPlayerEntry(PlayerEntry);
			adminTools.ShowRespawnPage();
		}

		public void OnHealUpButton()
		{
			AdminCommandsManager.Instance.CmdHealUpPlayer(PlayerEntry.PlayerData.uid);
			RefreshPage();
		}

		/// <summary>
		/// Sends the command to smite a player
		/// </summary>
		private void SendSmitePlayerRequest()
		{
			AdminCommandsManager.Instance.CmdSmitePlayer(PlayerEntry.PlayerData.uid);
			RefreshPage();
		}

		private void SendMakePlayerMentorRequest()
		{
			AdminCommandsManager.Instance.CmdAddMentor(PlayerEntry.PlayerData.uid, mentorToggle.isOn == false);
			RefreshPage();
		}

		private void SendRemovePlayerMentorRequest()
		{
			AdminCommandsManager.Instance.CmdRemoveMentor(PlayerEntry.PlayerData.uid);
			RefreshPage();
		}

		public void OnTeleportAdminToPlayer()
		{
			adminTools.areYouSurePage.SetAreYouSurePage(
				$"Teleport yourself to {PlayerEntry.PlayerData.name}?",
				SendTeleportAdminToPlayerRequest);
		}

		private void SendTeleportAdminToPlayerRequest()
		{
			RequestAdminTeleport.Send(
				null,
				PlayerEntry.PlayerData.uid,
				RequestAdminTeleport.OpperationList.AdminToPlayer,
				false,
				new Vector3(0, 0, 0)
			);
		}

		public void OnTeleportPlayerToAdmin()
		{
			adminTools.areYouSurePage.SetAreYouSurePage(
				$"Teleport {PlayerEntry.PlayerData.name} to you?",
				SendTeleportPlayerToAdmin);
		}

		private void SendTeleportPlayerToAdmin()
		{
			RequestAdminTeleport.Send(
				PlayerEntry.PlayerData.uid,
				null,
				RequestAdminTeleport.OpperationList.PlayerToAdmin,
				false,
				PlayerManager.LocalPlayerScript.PlayerSync.OrNull()?.registerTile.OrNull()?.WorldPosition != null ? PlayerManager.LocalPlayerScript.PlayerSync.registerTile.WorldPosition : PlayerManager.LocalPlayerScript.transform.position
				);
		}

		public void OnTeleportAdminToPlayerAghost()
		{
			adminTools.areYouSurePage.SetAreYouSurePage(
				$"Teleport yourself to {PlayerEntry.PlayerData.name} as a ghost?",
				SendTeleportAdminToPlayerAghost);
		}

		private void SendTeleportAdminToPlayerAghost()
		{
			PlayerManager.LocalMindScript.CmdAGhost();

			RequestAdminTeleport.Send(
				null,
				PlayerEntry.PlayerData.uid,
				RequestAdminTeleport.OpperationList.AdminToPlayer,
				true,
				new Vector3 (0,0,0)
				);
		}

		public void OnTeleportAllPlayersToPlayer()
		{
			adminTools.areYouSurePage.SetAreYouSurePage(
				$"Teleport EVERYONE to {PlayerEntry.PlayerData.name}?",
				SendTeleportAllPlayersToPlayer);
		}

		private void SendTeleportAllPlayersToPlayer()
		{
			Vector3 coord;

			bool isAghost;

			if (PlayerManager.LocalPlayerScript.IsGhost && PlayerEntry.PlayerData.uid == ServerData.UserID)
			{
				coord = PlayerManager.LocalPlayerScript.PlayerSync.registerTile.WorldPosition;
				isAghost = true;
			}
			else
			{
				coord = new Vector3(0, 0, 0);
				isAghost = false;
			}

			RequestAdminTeleport.Send(
				null,
				PlayerEntry.PlayerData.uid,
				RequestAdminTeleport.OpperationList.AllPlayersToPlayer,
				isAghost,
				coord
				);
		}

		public void GiveItemToPlayerButton()
		{
			adminTools.giveItemPage.selectedPlayerId = PlayerEntry.PlayerData.uid;

			adminTools.ShowGiveItemPagePage();
		}

		public void OnOOCMuteBtn()
		{
			AdminCommandsManager.Instance.CmdOOCMutePlayer(PlayerEntry.PlayerData.uid);
			RefreshPage();
		}
	}
}
