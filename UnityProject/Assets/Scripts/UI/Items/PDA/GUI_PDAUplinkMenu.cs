using System;
using System.Collections.Generic;
using UnityEngine;
using UI.Core.NetUI;

namespace UI.Items.PDA
{
	public class GUI_PDAUplinkMenu : NetPage, IPageReadyable
	{
		private const string ROOT_DIRECTORY = "???";
		public readonly string UPLINK_DIRECTORY = "tc-red34.syn";

		public GUI_PDA mainController;

		[SerializeField]
		private NetPageSwitcher subSwitcher = null;

		[SerializeField]
		public GUI_PDAUplinkItem itemPage = null;

		[SerializeField]
		public GUI_PDAUplinkCategory categoryPage = null;

		[SerializeField]
		private NetText_label tcCounter = null;

		public void OnPageActivated()
		{
			mainController.SetBreadcrumb(ROOT_DIRECTORY);
			UpdateTCCounter();
			OpenSubPage(categoryPage);
		}

		public void OpenSubPage(NetPage page)
		{
			mainController.OpenPageOnSwitcher(subSwitcher, page);
		}

		public void UpdateTCCounter()
		{
			tcCounter.MasterSetValue($"TC:{mainController.PDA.UplinkTC}");
		}

		public void LockUplink()
		{
			mainController.PDA.LockUplink();
			mainController.OpenPage(mainController.MainPage);
		}

		public void SetBreadcrumb(string directory)
		{
			mainController.SetBreadcrumb($"{ROOT_DIRECTORY}/{directory}");
		}
	}
}
