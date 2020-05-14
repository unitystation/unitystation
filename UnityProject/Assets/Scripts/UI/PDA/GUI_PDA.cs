using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

namespace UI.PDA
{
	public class GUI_PDA : NetTab
	{
		[SerializeField] [Tooltip("Put the main netpage switcher here")]
		private NetPageSwitcher mainSwitcher; //The main switcher used to switch pages

		[SerializeField] [Tooltip("Main menu here")]
		private GUI_PDAMainMenu menuPage; //The menuPage for reference

		[SerializeField] [Tooltip("Setting Menu here")]
		private GUI_PDASettingMenu settingPage; //The settingPage for reference

		[SerializeField] [Tooltip("Uplink here")]
		private GUI_PDAUplinkMenu uplinkPage; //The uplinkPage for reference

		[NonSerialized]
		public Items.PDA.PDA Pda;

		private UplinkItemClickedEvent onItemClicked;
		public UplinkItemClickedEvent OnItemClickedEvent {get => onItemClicked;}

		private UplinkCategoryClickedEvent onCategoryClicked;
		public UplinkCategoryClickedEvent OnCategoryClickedEvent {get => onCategoryClicked;}


		// Grabs the PDA component and opens the mainmenu
		private void Start()
		{
			onItemClicked = new UplinkItemClickedEvent();
			OnItemClickedEvent.AddListener(SpawnUplinkItem);
			onCategoryClicked = new UplinkCategoryClickedEvent();
			OnCategoryClickedEvent.AddListener(OpenUplinkCategory);
			Pda = Provider.GetComponent<global::Items.PDA.PDA>();
			Pda.AntagCheck(Pda.TabOnGameObject.LastInteractedPlayer().GetComponent<PlayerScript>().mind.GetAntag());
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
			if (IsServer && Pda.ActivateUplink(notificationString) )
			{
				OpenUplink();
				return true;
			}

			return false;
		}

		public void OpenUplink()
		{
			uplinkPage.ShowCategories();
			mainSwitcher.SetActivePage(uplinkPage);
		}

		public void OpenUplinkCategory(List<UplinkItems> items)
		{
			uplinkPage.OpenSelectedCategory(items);
		}

		public void SpawnUplinkItem(UplinkItems itemRequested)
		{
			Pda.SpawnUplinkItem(itemRequested.Item, itemRequested.Cost);
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
	public class UplinkCategoryClickedEvent : UnityEvent<List<UplinkItems>>
	{
	}

	[Serializable]
	public class UplinkItemClickedEvent : UnityEvent<UplinkItems>
	{
	}
}