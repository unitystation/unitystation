using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace AdminTools
{
	public class PlayerManagePage : AdminPage
	{
		[SerializeField] private Button kickBtn;
		[SerializeField] private Button banBtn;
		[SerializeField] private Button deputiseBtn;
		[SerializeField] private Button respawnBtn;

		private AdminPlayerEntry playerEntry;

		public override void OnPageRefresh(AdminPageRefreshData adminPageData)
		{
			base.OnPageRefresh(adminPageData);
		}

		public void SetData(AdminPlayerEntry entry)
		{
			playerEntry = entry;
			Debug.Log("//TODO: DISABLE BUTTONS WHEN THEY ARE NOT NEEDED!! ");

			if (playerEntry.PlayerData.isAlive)
			{
				respawnBtn.interactable = false;
			}
			else
			{
				respawnBtn.interactable = true;
			}
		}

		public void OnKickBtn()
		{
			adminTools.kickBanEntryPage.SetPage(false, playerEntry.PlayerData);
		}

		public void OnBanBtn()
		{
			adminTools.kickBanEntryPage.SetPage(true, playerEntry.PlayerData);
		}

		public void OnDeputiseBtn()
		{
			adminTools.areYouSurePage.SetAreYouSurePage($"Are you sure you want to make {playerEntry.PlayerData.name} an admin?", SendMakePlayerAdminRequest);
		}

		public void OnRespawnButton()
		{
			adminTools.areYouSurePage.SetAreYouSurePage($"Respawn the player: {playerEntry.PlayerData.name}?", SendPlayerRespawnRequest);
		}

		void SendMakePlayerAdminRequest()
		{
			Debug.Log("//TODO: SEND THE REQUEST TO SERVER");
		}

		void SendPlayerRespawnRequest()
		{
			Debug.Log("//TODO: SEND THE REQUEST TO SERVER");
		}
	}
}