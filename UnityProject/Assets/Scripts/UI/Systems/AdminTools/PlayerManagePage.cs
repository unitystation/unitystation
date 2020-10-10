using AdminCommands;
using DatabaseAPI;
using UI.AdminTools;
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
		[SerializeField] private Button teleportAdminToPlayer = null; // TODO: this is unused and is creating a compiler warning.
		[SerializeField] private Button teleportPlayerToAdmin = null; // Same issue with this.
		[SerializeField] private Button teleportAdminToPlayerAghost = null; // This too.
		[SerializeField] private Button teleportAllPlayersToPlayer = null; // And this.
		[SerializeField] private AdminRespawnPage adminRespawnPage;
		private AdminPlayerEntry playerEntry;
		public AdminPlayerEntry PlayerEntry => playerEntry;

		public void SetData(AdminPlayerEntry entry)
		{
			playerEntry = entry;
			deputiseBtn.interactable = !entry.PlayerData.isAdmin;
			// respawnBtn.interactable = !playerEntry.PlayerData.isAlive;
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

		public void OnSmiteBtn()
		{
			adminTools.areYouSurePage.SetAreYouSurePage(
				$"Are you sure you want to smite {playerEntry.PlayerData.name}?",
				SendSmitePlayerRequest);
		}

		public void OnDeputiseBtn()
		{
			adminTools.areYouSurePage.SetAreYouSurePage(
				$"Are you sure you want to make {playerEntry.PlayerData.name} an admin?",
				SendMakePlayerAdminRequest);
		}

		public void OnRespawnButton()
		{
			adminRespawnPage.SetTabsWithPlayerEntry(playerEntry);
			adminTools.ShowRespawnPage();
		}

		/// <summary>
		/// Sends the command to smite a player
		/// </summary>
		void SendSmitePlayerRequest()
		{
			ServerCommandVersionTwoMessageClient.Send(ServerData.UserID, PlayerList.Instance.AdminToken, playerEntry.PlayerData.uid, "CmdSmitePlayer");
			RefreshPage();
		}

		void SendMakePlayerAdminRequest()
		{
			RequestAdminPromotion.Send(
				ServerData.UserID,
				PlayerList.Instance.AdminToken,
				playerEntry.PlayerData.uid);
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