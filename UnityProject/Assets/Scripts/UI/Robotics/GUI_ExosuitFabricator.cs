using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GUI_ExosuitFabricator : NetTab
{
	[SerializeField] private GUI_ExoFabPageMaterialsAndCategory materialsAndCategoryDisplay;
	[SerializeField] private GUI_ExoFabPageProducts productDisplay;

	[SerializeField]
	private NetPageSwitcher nestedSwitcher = null;

	private ExosuitFabricator exosuitFabricator;

	public Dictionary<string, GameObject[]> categoryNameToProductEntries = new Dictionary<string, GameObject[]>();

	protected override void InitServer()
	{
	}

	private void Start()
	{
		//Makes sure it connects with the ExosuitFabricator
		exosuitFabricator = Provider.GetComponentInChildren<ExosuitFabricator>();
		//Subscribes to the MaterialsManipulated event
		ExosuitFabricator.MaterialsManipulated += UpdateAll;

		materialsAndCategoryDisplay.InitMaterialList(exosuitFabricator);
		categoryNameToProductEntries = materialsAndCategoryDisplay.InitCategoryList(exosuitFabricator.productsCollection, exosuitFabricator.materialStorage.MaterialToNameRecord);
		UpdateAll();
	}

	//Updates the GUI and adds any visible NetUIElements to the server.
	public void UpdateAll()
	{
		materialsAndCategoryDisplay.UpdateMaterialCount(exosuitFabricator);
		foreach (ItemTrait materialType in exosuitFabricator.materialStorage.ItemTraitToMaterialRecord.Keys)
		{
			int materialAmount = exosuitFabricator.materialStorage.ItemTraitToMaterialRecord[materialType].currentAmount;
			int cm3PerSheet = exosuitFabricator.materialStorage.cm3PerSheet;

			materialsAndCategoryDisplay.UpdateButtonVisibility(materialAmount, cm3PerSheet, materialType);
			RescanElements();
		}
	}

	//Used by buttons, which contains the amount and type to dispense
	public void DispenseSheet(ExoFabRemoveMaterialButton button)
	{
		int sheetAmount = button.value;
		ItemTrait materialType = button.itemTrait;
		exosuitFabricator.DispenseMaterialSheet(sheetAmount, materialType);
	}

	public void AddProductToQueue()
	{
		Logger.Log("Click");
	}

	public void AddAllProductsToQueue(ExoFabCategoryButton button)
	{
	}

	//Called after a category button is pressed. The button contains data for the products on the page.
	public void SetupAndOpenProductsPage(ExoFabCategoryButton button)
	{
		productDisplay.SetupPage(button, categoryNameToProductEntries);
		nestedSwitcher.SetActivePage(productDisplay);
		RescanElements();
	}

	public void ReturnFromProductPage(ExoFabProductButton button)
	{
		foreach (GameObject productEntries in categoryNameToProductEntries[button.categoryName])
		{
			productEntries.SetActive(false);
		}
		nestedSwitcher.SetActivePage(materialsAndCategoryDisplay);
	}

	public void OpenTab(NetPage pageToOpen)
	{
		nestedSwitcher.SetActivePage(pageToOpen);
	}

	public void CloseTab()
	{
		ControlTabs.CloseTab(Type, Provider);
	}

	private void OnDestroy()
	{
		ExosuitFabricator.MaterialsManipulated -= UpdateAll;
	}
}