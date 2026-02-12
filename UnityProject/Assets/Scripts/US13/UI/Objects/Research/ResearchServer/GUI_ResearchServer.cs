using System.Collections;
using UnityEngine;
using US13.Managers;
using US13.Managers.NetworkManagement;
using US13.Systems.Research;
using US13.Systems.Research.Data;
using US13.UI.Core.Net;
using US13.UI.Core.Net.Page;

namespace US13.UI.Objects.Research.ResearchServer
{
	public class GUI_ResearchServer : NetTab
	{
		[SerializeField] private GUI_TechwebPage techWebPage;
		[SerializeField] private GUI_FocusPage focusPage;
		[SerializeField] private NetPageSwitcher pageSwitcher;

		public NetPage CurrentPage => pageSwitcher.CurrentPage;

		public Techweb TechWeb => Server.Techweb;
		public US13.Systems.Research.Objects.ResearchServer Server { get; private set; }

		private bool isUpdating = false;

		private const float CLIENT_UPDATE_DELAY = 0.2f;

		public void Awake()
		{
			StartCoroutine(WaitForProvider());
		}

		public void OnDestroy()
		{
			TechWeb.UIupdate -= UpdateGUI;
		}

		private IEnumerator WaitForProvider()
		{
			while (Provider == null)
			{
				yield return WaitFor.EndOfFrame;
			}

			Server = Provider.GetComponent<US13.Systems.Research.Objects.ResearchServer>();
			TechWeb.UIupdate += UpdateGUI;

			if (CustomNetworkManager.IsServer == false) yield break;

			UpdateGUI();

			OnTabOpened.AddListener(UpdateGUIForPeepers);

		}

		public void UpdateGUIForPeepers(PlayerInfo notUsed)
		{
			if (isUpdating == false)
			{
				isUpdating = true;
				StartCoroutine(WaitForClient());
			}
		}

		private IEnumerator WaitForClient()
		{
			yield return new WaitForSeconds(CLIENT_UPDATE_DELAY);

			UpdateGUI();

			isUpdating = false;
		}

		private void UpdateGUI()
		{
			if (CurrentPage == techWebPage) techWebPage.UpdateGUI();
			if (CurrentPage == focusPage) focusPage.UpdateGUI();
		}

		public void OpenFocusPage()
		{
			if (TechWeb.ResearchFocus != TechType.None) return;

			pageSwitcher.SetActivePage(focusPage);
			UpdateGUI();
		}

		public void OpenTechWebPage()
		{
			pageSwitcher.SetActivePage(techWebPage);
			UpdateGUI();
		}

	}
}
