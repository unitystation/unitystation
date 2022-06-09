using System.Collections;
using UnityEngine;
using UI.Core.NetUI;
using Systems.Research.Objects;
using Items.Science;

namespace UI.Objects.Research
{
	public class GUI_ArtifactConsole : NetTab
	{
		public NetPage[] Pages;
		public NetPageSwitcher mainSwitcher;

		public NetLabel pageLabel;

		[SerializeField]
		private InputFieldFocus radInput;

		private ArtifactData inputData;

		bool isUpdating;

		protected override void InitServer()
		{
			if (CustomNetworkManager.Instance._isServer)
			{
				StartCoroutine(WaitForProvider());
			}
		}

		private IEnumerator WaitForProvider()
		{

			while (Provider == null)
			{
				yield return WaitFor.EndOfFrame;
			}

			OnTabOpened.AddListener(UpdateGUIForPeepers);

			Logger.Log(nameof(WaitForProvider), Category.Research);
		}

		public void UpdateGUIForPeepers(PlayerInfo notUsed)
		{
			if (!isUpdating)
			{
				isUpdating = true;
				StartCoroutine(WaitForClient());
			}
		}

		private IEnumerator WaitForClient()
		{
			yield return new WaitForSeconds(0.2f);
			UpdateGUI();
			isUpdating = false;
		}

		public void UpdateGUI()
		{
			
		}

		public void Save()
		{

		}

		public void EjectDisk()
		{

		}

		public void NextPage()
		{
			mainSwitcher.NextPage(true); 

			int a = mainSwitcher.Pages.IndexOf(mainSwitcher.CurrentPage) + 1;
			pageLabel.SetValueServer("Page (" + a + "/3)");
		}

		public void PrevPage()
		{
			mainSwitcher.PreviousPage(true);

			int a = mainSwitcher.Pages.IndexOf(mainSwitcher.CurrentPage) + 1;
			pageLabel.SetValueServer("Page (" + a + "/3)");
		}
	}
}
