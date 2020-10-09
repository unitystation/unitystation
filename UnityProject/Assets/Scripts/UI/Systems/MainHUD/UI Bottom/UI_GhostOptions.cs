using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UI.Core.Windows;
using Systems.Teleport;

namespace UI.Systems.Ghost
{
	public class UI_GhostOptions : MonoBehaviour
	{
		[SerializeField] private Text ghostHearText = null;

		private TeleportWindow TeleportWindow => UIManager.TeleportWindow;

		private void OnEnable()
		{
			TeleportWindow.onTeleportRequested += TeleportUtils.TeleportLocalGhostTo;
			TeleportWindow.onTeleportToVector += TeleportUtils.TeleportLocalGhostTo;
			DetermineGhostHearText();
		}

		private void OnDisable()
		{
			TeleportWindow.onTeleportRequested -= TeleportUtils.TeleportLocalGhostTo;
			TeleportWindow.onTeleportToVector -= TeleportUtils.TeleportLocalGhostTo;
		}

		public void JumpToMob()
		{
			TeleportWindow.SetWindowTitle("Jump To Mob");
			TeleportWindow.gameObject.SetActive(true);
			TeleportWindow.GenerateButtons(TeleportUtils.GetMobDestinations());
		}

		public void Orbit()
		{
		}

		public void ReenterCorpse()
		{
			PlayerManager.LocalPlayerScript.playerNetworkActions.CmdGhostCheck();
		}

		public void Teleport()
		{
			TeleportWindow.SetWindowTitle("Jump to Place");
			TeleportWindow.gameObject.SetActive(true);
			TeleportWindow.GenerateButtons(TeleportUtils.GetSpawnDestinations());
		}

		public void pAIcandidate()
		{
		}

		public void Respawn()
		{
			PlayerManager.LocalPlayerScript.playerNetworkActions.CmdRespawnPlayer();
		}

		public void ToggleAllowCloning()
		{
			PlayerManager.LocalPlayerScript.playerNetworkActions.CmdToggleAllowCloning();
		}

		public void ToggleGhostHearRange()
		{
			Chat.Instance.GhostHearAll = !Chat.Instance.GhostHearAll;
			DetermineGhostHearText();
		}

		private void DetermineGhostHearText()
		{
			if (Chat.Instance.GhostHearAll)
			{
				ghostHearText.text = "HEAR\r\n \r\nLOCAL";
			}
			else
			{
				ghostHearText.text = "HEAR\r\n \r\nALL";
			}
		}
	}
}
