using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

public class GUI_PDA : NetTab
{
	[SerializeField] [Tooltip("Put the main netpage switcher here")]
	private NetPageSwitcher mainSwitcher; //The main switcher used to switch pages

	[SerializeField] [Tooltip("Main menu here")]
	private GUI_PDAMainMenu menuPage; //The menuPage for reference

	[NonSerialized] public PDA Pda;

	[SerializeField] [Tooltip("Setting Menu here")]
	private GUI_PDASettingMenu settingPage; //The settingPage for reference

	[SerializeField] [Tooltip("Uplink switcher here")]
	private NetPageSwitcher uplinkSwitcher; //The uplinkPage for reference

	[SerializeField] [Tooltip("Uplink here")]
	private NetPage uplinkPage; //The uplinkPage for reference

	[SerializeField] [Tooltip("Uplink here")]
	private GUI_PDAUplinkCategory uplinkCategoryPage; //The uplinkPage for reference



	private UplinkCategoryClickedEvent onCategoryClicked;
	public UplinkCategoryClickedEvent OnCategoryClickedEvent {get => onCategoryClicked;}


	// Grabs the PDA component and opens the mainmenu
	private void Start()
	{
		onCategoryClicked = new UplinkCategoryClickedEvent();
		OnCategoryClickedEvent.AddListener(OpenUplinkCategory);
		Pda = Provider.GetComponent<PDA>();
		OpenMainMenu();
	}

	/// <summary>
	/// It opens the updates the ID strings then sets the menu page
	/// </summary>
	public void OpenMainMenu()
	{
		menuPage.UpdateId();
		mainSwitcher.SetActivePage(menuPage);
	}

	/// <summary>
	/// It opens settings, what did you expect?
	/// </summary>
	public void OpenSettings()
	{
		mainSwitcher.SetActivePage(settingPage);
	}

	/// <summary>
	/// Asks the PDA to test the notification string against its Uplinkstring server side
	/// </summary>
	public bool TestForUplink(string notificationString)
	{
		if (Pda.ActivateUplink(notificationString) != true || IsServer != true)
		{
			return false;
		}
		if (IsServer && Pda.ActivateUplink(notificationString))
		{
			uplinkSwitcher.SetActivePage(uplinkCategoryPage);
			uplinkCategoryPage.UpdateCategory();
			mainSwitcher.SetActivePage(uplinkPage);
			return true;
		}

		return false;
	}
	/// <summary>
	/// Tells the item page to make entries depending on the category given
	/// </summary>
	public void OpenUplinkCategory(UplinkCatagories category)
	{
		List<UplinkItems> items = category.ItemList;
		//uplinkItemPage.ClearEntries();
		//uplinkItemPage.GenerateEntries(item);
		//uplinkSwitcher.SetActivePage(uplinkItemPage);
	}
	/// <summary>
	/// Opens the messenger that does not exist yet
	/// </summary>
	public void OpenMessenger()
	{
		Debug.LogError("Not implimented");
	}
	/// <summary>
	/// Closes the PDA
	/// </summary>
	public void CloseTab()
	{
		ControlTabs.CloseTab(Type, Provider);
	}
	/// <summary>
	/// Tells the PDA to remove the ID card and updates the menu page
	/// </summary>
	public void RemoveId()
	{
		if (Pda.IdCard) Pda.RemoveDevice(true);
		menuPage.UpdateId();
	}
	/// <summary>
	/// Resets the PDA's name and ID, opens the menu
	/// </summary>
	public void ResetPda()
	{
		Pda.PdaReset();
		OpenMainMenu();
	}
}

[Serializable]
public class UplinkCategoryClickedEvent : UnityEvent<UplinkCatagories>
{
}