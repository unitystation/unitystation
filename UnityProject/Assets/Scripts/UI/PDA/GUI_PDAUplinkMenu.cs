using System;
using System.Collections.Generic;
using UnityEngine;

namespace UI.PDA
{
	public class GUI_PDAUplinkMenu : NetPage
	{
		public GUI_PDA mainController;

		[SerializeField]
		private NetPageSwitcher subSwitcher;

		[SerializeField]
		private GUI_PDAUplinkItem itemPage;

		[SerializeField]
		private GUI_PDAUplinkCategory categoryPage;

		[SerializeField]
		private NetLabel tcCounter;


		/// <summary>
		/// Shows the categories and wipes any entries from itempage
		/// </summary>
		public void ShowCategories()
		{
			UpdateCounter();
			itemPage.ClearItems();
			categoryPage.ClearCategory();
			categoryPage.UpdateCategory();
			subSwitcher.SetActivePage(categoryPage);
		}

		/// <summary>
		/// This generates the list of items in the selected category
		/// </summary>
		public void OpenSelectedCategory(List<UplinkItems> items)
		{
			UpdateCounter();
			itemPage.ClearItems();
			categoryPage.ClearCategory();
			itemPage.GenerateEntries(items);
			subSwitcher.SetActivePage(itemPage);
		}

		/// <summary>
		/// Updates the TCcounter
		/// </summary>
		public void UpdateCounter()
		{
			tcCounter.SetValueServer($"TC: {mainController.Pda.TeleCrystals}");
		}

		public void LockUplink()
		{
			mainController.OpenMainMenu();
			mainController.Pda.LockUplink();
		}
	}
}