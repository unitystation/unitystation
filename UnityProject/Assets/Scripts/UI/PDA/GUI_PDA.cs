using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using NUnit.Framework.Constraints;
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

		[SerializeField] [Tooltip("CrewManifest here")]
		private GUI_PDACrewManifest crewManifestPage; //The uplinkPage for reference

		[SerializeField] [Tooltip("Notes Page")]
		private GUI_PDANotes notesPage; //The uplinkPage for reference

		[NonSerialized]
		public Items.PDA.PDALogic Pda;

		private UplinkItemClickedEvent onItemClicked;
		public UplinkItemClickedEvent OnItemClickedEvent {get => onItemClicked;}

		private UplinkCategoryClickedEvent onCategoryClicked;
		public UplinkCategoryClickedEvent OnCategoryClickedEvent {get => onCategoryClicked;}



		// Grabs the PDA component and opens the mainmenu
		[SuppressMessage("ReSharper", "UEA0006")] // Supresses the couroutine allocation warning, because it's annoying
		private void Start()
		{
			onItemClicked = new UplinkItemClickedEvent();
			OnItemClickedEvent.AddListener(SpawnUplinkItem);
			onCategoryClicked = new UplinkCategoryClickedEvent();
			OnCategoryClickedEvent.AddListener(OpenUplinkCategory);
			StartCoroutine(WaitForProvider());
			Pda.AntagCheck(Pda.TabOnGameObject.LastInteractedPlayer());
			OpenMainMenu();
		}

		/// <summary>
		/// Refreshes the strings of tabs if they're active
		/// </summary>
		public override void RefreshTab()
		{
			menuPage.RefreshText();
			notesPage.RefreshText();
			base.RefreshTab();
		}

		IEnumerator WaitForProvider()
		{
			while (Provider == null)
			{
				yield return WaitFor.EndOfFrame;
			}
			Pda = Provider.GetComponent<Items.PDA.PDALogic>();
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

		public void OpenManifest()
		{
			crewManifestPage.GenerateEntries();
			mainSwitcher.SetActivePage(crewManifestPage);
		}

		public void OpenNotes()
		{
			mainSwitcher.SetActivePage(notesPage);
		}

		/// <summary>
		/// Asks the PDA to test the notification string against its Uplinkstring server side
		/// </summary>
		public bool TestForUplink(string notificationString)
		{

			if (!IsServer || !Pda.ActivateUplink(notificationString)) return false;
			OpenUplink();
			return true;
		}

		/// <summary>
		/// Opens the uplink
		/// </summary>
		private void OpenUplink()
		{
			uplinkPage.ShowCategories();
			mainSwitcher.SetActivePage(uplinkPage);
		}

		/// <summary>
		/// Generates a list of items to select from using a list containing said items
		/// </summary>
		private void OpenUplinkCategory(List<UplinkItems> items)
		{
			uplinkPage.OpenSelectedCategory(items);
		}

		/// <summary>
		/// //Tells the PDA script to spawn an item at the cost of TC
		/// </summary>
		private void SpawnUplinkItem(UplinkItems itemRequested)
		{
			Pda.SpawnUplinkItem(itemRequested.Item, itemRequested.Cost);
		}

		/// <summary>
		/// Opens the messenger that does not exist yet
		/// </summary>
		public void OpenMessenger()
		{
			//TODO Get someone else to do messenger
			throw new NotImplementedException();
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