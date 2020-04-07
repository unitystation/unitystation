using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GUI_ExosuitFabricator : NetTab
{
	[SerializeField] private GUI_ExoFabPageMaterialsAndCategory materialsDisplay;

	[SerializeField]
	private NetPageSwitcher nestedSwitcher = null;

	[SerializeField]
	private NetPage materialsAndCategoryPage = null;

	[SerializeField]
	private NetPage productsPage = null;

	private ExosuitFabricator exosuitFabricator;

	protected override void InitServer()
	{
	}

	private void Start()
	{
		//Makes sure it connects with the ExosuitFabricator
		exosuitFabricator = Provider.GetComponentInChildren<ExosuitFabricator>();
		//Subscribes to the MaterialsManipulated event
		ExosuitFabricator.MaterialsManipulated += UpdateAll;

		materialsDisplay.initMaterialList(exosuitFabricator);
		UpdateAll();
	}

	//Updates the GUI and adds any visible NetUIElements to the server.
	public void UpdateAll()
	{
		materialsDisplay.UpdateMaterialCount(exosuitFabricator);
		foreach (ItemTrait materialType in exosuitFabricator.materialStorage.ItemTraitToMaterialRecord.Keys)
		{
			int materialAmount = exosuitFabricator.materialStorage.ItemTraitToMaterialRecord[materialType].currentAmount;
			int cm3PerSheet = exosuitFabricator.materialStorage.cm3PerSheet;

			materialsDisplay.UpdateButtonVisibility(materialAmount, cm3PerSheet, materialType);
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