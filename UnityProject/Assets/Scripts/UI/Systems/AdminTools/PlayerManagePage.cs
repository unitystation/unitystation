using AdminCommands;
using DatabaseAPI;
using UI.AdminTools;
using UnityEngine;
using UnityEngine.UI;

namespace AdminTools
{
	public class PlayerManagePage : AdminPage
	{
		[SerializeField] private Button deputiseBtn = null;
		[SerializeField] private AdminRespawnPage adminRespawnPage = default;

		public AdminPlayerEntry PlayerEntry { get; private set; }

		public void SetData(AdminPlayerEntry entry)
		{
			PlayerEntry = entry;
			deputiseBtn.interactable = !entry.PlayerData.isAdmin;
			// respawnBtn.interactable = !playerEntry.PlayerData.isAlive;
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
			adminTools.areYouSurePage.SetAreYouSurePage(
				$"Are you sure you want to make {PlayerEntry.PlayerData.name} an admin?",
				SendMakePlayerAdminRequest);
		}

		public void OnRespawnButton()
		{
			adminRespawnPage.SetTabsWithPlayerEntry(PlayerEntry);
			adminTools.ShowRespawnPage();
		}

		/// <summary>
		/// Sends the command to smite a player
		/// </summary>
		void SendSmitePlayerRequest()
		{
			ServerCommandVersionTwoMessageClient.Send(ServerData.UserID, PlayerList.Instance.AdminToken, PlayerEntry.PlayerData.uid, "CmdSmitePlayer");
			RefreshPage();
		}

		void SendMakePlayerAdminRequest()
		{
			RequestAdminPromotion.Send(
				ServerData.UserID,
				PlayerList.Instance.AdminToken,
				PlayerEntry.PlayerData.uid);
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
				ServerData.UserID,
				PlayerList.Instance.AdminToken,
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
				ServerData.UserID,
				PlayerList.Instance.AdminToken,
				PlayerEntry.PlayerData.uid,
				null,
				RequestAdminTeleport.OpperationList.PlayerToAdmin,
				false,
				PlayerManager.LocalPlayerScript.PlayerSync.ClientPosition
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
			if (!PlayerManager.LocalPlayerScript.IsGhost)
			{
				PlayerManager.LocalPlayerScript.playerNetworkActions.CmdAGhost(ServerData.UserID, PlayerList.Instance.AdminToken);
			}

			RequestAdminTeleport.Send(
				ServerData.UserID,
				PlayerList.Instance.AdminToken,
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
				PlayerEntry.PlayerData.uid,
				RequestAdminTeleport.OpperationList.AllPlayersToPlayer,
				isAghost,
				coord
				);
		}
	}
}
