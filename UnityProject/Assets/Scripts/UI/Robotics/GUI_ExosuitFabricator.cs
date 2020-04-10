using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

public class GUI_ExosuitFabricator : NetTab
{
	[SerializeField]
	private GUI_ExoFabPageMaterialsAndCategory materialsAndCategoryDisplay = null;

	[SerializeField]
	private GUI_ExoFabPageProducts productDisplay = null;

	[SerializeField]
	private NetPageSwitcher nestedSwitcher = null;

	private ExosuitFabricator exosuitFabricator;

	public Dictionary<string, GameObject[]> categoryNameToProductEntries = new Dictionary<string, GameObject[]>();

	private ExoFabProductAddClickEvent onProductAddClicked;
	public ExoFabProductAddClickEvent OnProductAddClicked;
	private ExoFabCategoryClickEvent onCategoryClicked;
	public ExoFabCategoryClickEvent OnCategoryClicked { get => onCategoryClicked; }

	protected override void InitServer()
	{
	}

	private void Start()
	{
		if (OnProductAddClicked == null)
		{
			onProductAddClicked = new ExoFabProductAddClickEvent();

			OnProductAddClicked.AddListener(AddProductToQueue);
		}
		if (OnCategoryClicked == null)
		{
			onCategoryClicked = new ExoFabCategoryClickEvent();

			OnCategoryClicked.AddListener(OpenCategory);
		}
		//Makes sure it connects with the ExosuitFabricator
		exosuitFabricator = Provider.GetComponentInChildren<ExosuitFabricator>();
		//Subscribes to the MaterialsManipulated event
		ExosuitFabricator.MaterialsManipulated += UpdateAll;

		materialsAndCategoryDisplay.InitMaterialList(exosuitFabricator);
		materialsAndCategoryDisplay.InitCategories(exosuitFabricator.exoFabProducts);
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
	public void DispenseSheet(GUI_ExoFabRemoveMaterialButton button)
	{
		int sheetAmount = button.value;
		ItemTrait materialType = button.itemTrait;
		exosuitFabricator.DispenseMaterialSheet(sheetAmount, materialType);
	}

	public void AddProductToQueue(MachineProduct product)
	{
		Logger.Log("Adding product to queue");
	}

	public void AddAllProductsToQueue(NetButton button)
	{
		Logger.Log("Click");
	}

	public void ReturnFromProductPage(GUI_ExoFabProductButton button)
	{
		//foreach (GameObject productEntries in categoryNameToProductEntries[button.categoryName])
		//{
		//	productEntries.SetActive(false);
		//}
		Logger.Log("RETURNING TO MATERIAL CATEGORY PAGE");
		nestedSwitcher.SetActivePage(materialsAndCategoryDisplay);
	}

	public void OpenCategory(MachineProductList categoryProducts)
	{
		Logger.Log("OPENING CATEGORY");
		nestedSwitcher.SetActivePage(productDisplay);
		productDisplay.DisplayProducts(categoryProducts);
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

[System.Serializable]
public class ExoFabProductAddClickEvent : UnityEvent<MachineProduct>
{
}

public class ExoFabCategoryClickEvent : UnityEvent<MachineProductList>
{
}

[System.Serializable]
public class ExoFabRemoveProductClickedEvent : UnityEvent<DynamicEntry>
{
}