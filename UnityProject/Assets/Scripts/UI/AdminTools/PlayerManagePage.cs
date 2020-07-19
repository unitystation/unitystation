using System.Collections.Generic;
using DatabaseAPI;
using UnityEngine;
using UnityEngine.UI;

namespace AdminTools
{
	public class PlayerManagePage : AdminPage
	{
		[SerializeField] private Button kickBtn;
		[SerializeField] private Button banBtn;
		[SerializeField] private Button deputiseBtn = null;
		[SerializeField] private Button respawnBtn = null;
		[SerializeField] private Button respawnAsBtn = null;
		[SerializeField] private Dropdown adminJobsDropdown = null;
		[SerializeField] private Button teleportAdminToPlayer = null;
		[SerializeField] private Button teleportPlayerToAdmin = null;
		[SerializeField] private Button teleportAdminToPlayerAghost = null;
		[SerializeField] private Button teleportAllPlayersToPlayer = null;
		private AdminPlayerEntry playerEntry;

		public override void OnPageRefresh(AdminPageRefreshData adminPageData)
		{
			base.OnPageRefresh(adminPageData);

			var optionData = new List<Dropdown.OptionData>
			{
				new Dropdown.OptionData
				{
					text = "Select an admin job..."
				}
			};

			foreach (var job in SOAdminJobsList.Instance.AdminAvailableJobs)
			{
				optionData.Add(new Dropdown.OptionData
				{
					text = job.DisplayName
				});
			}

			adminJobsDropdown.value = 0;
			adminJobsDropdown.options = optionData;
		}

		public void SetData(AdminPlayerEntry entry)
		{
			playerEntry = entry;
			deputiseBtn.interactable = !entry.PlayerData.isAdmin;
			respawnBtn.interactable = !playerEntry.PlayerData.isAlive;
			respawnAsBtn.interactable = !playerEntry.PlayerData.isAlive &&
			                            adminJobsDropdown.value != 0;
			adminJobsDropdown.interactable = !playerEntry.PlayerData.isAlive;
		}

		public void OnKickBtn()
		{
			adminTools.kickBanEntryPage.SetPage(false, playerEntry.PlayerData, false);
		}

		public void OnBanBtn()
		{
			adminTools.kickBanEntryPage.SetPage(true, playerEntry.PlayerData, false);
		}

		public void OnJobBanBtn()
		{
			adminTools.kickBanEntryPage.SetPage(false, playerEntry.PlayerData, true);
		}

		public void OnDeputiseBtn()
		{
			adminTools.areYouSurePage.SetAreYouSurePage(
				$"Are you sure you want to make {playerEntry.PlayerData.name} an admin?",
				SendMakePlayerAdminRequest);
		}

		public void OnRespawnButton()
		{
			adminTools.areYouSurePage.SetAreYouSurePage(
				$"Respawn the player: {playerEntry.PlayerData.name}?",
				SendPlayerRespawnRequest);
		}

		public void OnRespawnAsButton()
		{
			adminTools.areYouSurePage.SetAreYouSurePage(
				$"Respawn the player: {playerEntry.PlayerData.name}" +
				$" as {SOAdminJobsList.Instance.AdminAvailableJobs[adminJobsDropdown.value - 1].name}?",
				SendPlayerRespawnAsRequest);
		}

		public void OnDropdownValueChanged()
		{
			respawnAsBtn.interactable = !playerEntry.PlayerData.isAlive &&
			                            adminJobsDropdown.value != 0;
		}

		void SendMakePlayerAdminRequest()
		{
			RequestAdminPromotion.Send(
				ServerData.UserID,
				PlayerList.Instance.AdminToken,
				playerEntry.PlayerData.uid);
			RefreshPage();
		}

		void SendPlayerRespawnRequest()
		{
			RequestRespawnPlayer.Send(
				ServerData.UserID,
				PlayerList.Instance.AdminToken,
				playerEntry.PlayerData.uid);
			RefreshPage();
		}

		void SendPlayerRespawnAsRequest()
		{
			var value = adminJobsDropdown.value;
			var occupation = value != 0
				? SOAdminJobsList.Instance.AdminAvailableJobs[value - 1]
				//Just a safe value in case for whatever reason user didn't select a job and can click the button
				: SOAdminJobsList.Instance.AdminAvailableJobs.PickRandom();

			RequestRespawnPlayer.SendAdminJob(
				ServerData.UserID,
				PlayerList.Instance.AdminToken,
				playerEntry.PlayerData.uid,
				occupation);

			RefreshPage();
		}

		public void OnTeleportAdminToPlayer()
		{
			adminTools.areYouSurePage.SetAreYouSurePage(
				$"Teleport yourself to {playerEntry.PlayerData.name}?",
				SendTeleportAdminToPlayerRequest);
		}

		private void SendTeleportAdminToPlayerRequest()
		{

			RequestAdminTeleport.Send(
				ServerData.UserID,
				PlayerList.Instance.AdminToken,
				null,
				playerEntry.PlayerData.uid,
				RequestAdminTeleport.OpperationList.AdminToPlayer,
				false,
				new Vector3(0, 0, 0)
				);
		}

		public void OnTeleportPlayerToAdmin()
		{
			adminTools.areYouSurePage.SetAreYouSurePage(
				$"Teleport {playerEntry.PlayerData.name} to you?",
				SendTeleportPlayerToAdmin);
		}

		private void SendTeleportPlayerToAdmin()
		{
			RequestAdminTeleport.Send(
				ServerData.UserID,
				PlayerList.Instance.AdminToken,
				playerEntry.PlayerData.uid,
				null,
				RequestAdminTeleport.OpperationList.PlayerToAdmin,
				false,
				PlayerManager.LocalPlayerScript.PlayerSync.ClientPosition
				);
		}

		public void OnTeleportAdminToPlayerAghost()
		{
			adminTools.areYouSurePage.SetAreYouSurePage(
				$"Teleport yourself to {playerEntry.PlayerData.name} as a ghost?",
				SendTeleportAdminToPlayerAghost);
		}

		private void SendTeleportAdminToPlayerAghost()
		{
			if (!PlayerManager.LocalPlayerScript.IsGhost)
			{
				PlayerManager.LocalPlayerScript.playerNetworkActions.CmdAGhost(ServerData.UserID, PlayerList.Instance.AdminToken);
			}

			RequestAdminTeleport.Send(
				ServerData.UserID,
				PlayerList.Instance.AdminToken,
				null,
				playerEntry.PlayerData.uid,
				RequestAdminTeleport.OpperationList.AdminToPlayer,
				true,
				new Vector3 (0,0,0)
				);
		}

		public void OnTeleportAllPlayersToPlayer()
		{
			adminTools.areYouSurePage.SetAreYouSurePage(
				$"Teleport EVERYONE to {playerEntry.PlayerData.name}?",
				SendTeleportAllPlayersToPlayer);
		}

		private void SendTeleportAllPlayersToPlayer()
		{
			Vector3 coord;

			bool isAghost;

			if (PlayerManager.LocalPlayerScript.IsGhost && playerEntry.PlayerData.uid == ServerData.UserID)
			{
				coord = PlayerManager.LocalPlayerScript.PlayerSync.ClientPosition;
				isAghost = true;
			}
			else
			{
				coord = new Vector3(0, 0, 0);
				isAghost = false;
			}

			RequestAdminTeleport.Send(
				ServerData.UserID,
				PlayerList.Instance.AdminToken,
				null,
				playerEntry.PlayerData.uid,
				RequestAdminTeleport.OpperationList.AllPlayersToPlayer,
				isAghost,
				coord
				);
		}
	}
}