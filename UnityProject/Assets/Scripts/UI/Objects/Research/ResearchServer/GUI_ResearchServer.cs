using UnityEngine;
using UI.Core.NetUI;
using System.Collections;
using Systems.Research.Objects;
using Systems.Research;
using Systems.Research.Data;

namespace UI.Objects.Research
{
	public class GUI_ResearchServer : NetTab
	{
		[SerializeField] private GUI_TechwebPage techWebPage;
		[SerializeField] private GUI_FocusPage focusPage;
		[SerializeField] private NetPageSwitcher pageSwitcher;

		public NetPage CurrentPage => pageSwitcher.CurrentPage;

		public Techweb TechWeb => Server.Techweb;
		public ResearchServer Server { get; private set; }

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

			Server = Provider.GetComponent<ResearchServer>();
			TechWeb.UIupdate += UpdateGUI;

			if (CustomNetworkManager.Instance._isServer == false) yield break;

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
