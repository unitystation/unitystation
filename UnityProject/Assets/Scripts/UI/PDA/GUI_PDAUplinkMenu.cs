using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

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

	private void Start()
	{
		UpdateTc();
	}

	/// <summary>
	/// Shows the categories and wipes any entries from itempage
	/// </summary>
	public void ShowCategories()
	{
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
		itemPage.ClearItems();
		categoryPage.ClearCategory();
		itemPage.GenerateEntries(items);
		subSwitcher.SetActivePage(itemPage);
		UpdateTc();
	}

	/// <summary>
	/// Updates the TCcounter
	/// </summary>
	public void UpdateTc()
	{
		tcCounter.Value = $"TC:{mainController.Pda.teleCrystals}";
	}
}