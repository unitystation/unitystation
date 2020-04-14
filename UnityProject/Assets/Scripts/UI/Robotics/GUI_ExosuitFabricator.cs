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
	private GUI_ExoFabQueueDisplay queueDisplay = null;

	[SerializeField]
	private NetPageSwitcher nestedSwitcher = null;

	private ExosuitFabricator exosuitFabricator;

	public Dictionary<string, GameObject[]> categoryNameToProductEntries = new Dictionary<string, GameObject[]>();

	private ExoFabProductAddClickEvent onProductAddClicked;
	public ExoFabProductAddClickEvent OnProductAddClicked { get => onProductAddClicked; }
	private ExoFabCategoryClickEvent onCategoryClicked;
	public ExoFabCategoryClickEvent OnCategoryClicked { get => onCategoryClicked; }
	private ExoFabRemoveProductClickedEvent onRemoveProductClicked;
	public ExoFabRemoveProductClickedEvent OnRemoveProductClicked { get => onRemoveProductClicked; }

	private ExoFabUpQueueClickedEvent onUpQueueClicked;
	public ExoFabUpQueueClickedEvent OnUpQueueClicked { get => onUpQueueClicked; }
	private ExoFabDownQueueClickedEvent onDownQueueClicked;
	public ExoFabDownQueueClickedEvent OnDownQueueClicked { get => onDownQueueClicked; }
	private ExoFabDispenseSheetClickEvent onDispenseSheetClicked;
	public ExoFabDispenseSheetClickEvent OnDispenseSheetClicked { get => onDispenseSheetClicked; }

	private ExoFabClearQueueClickEvent onClearQueueClicked;
	public ExoFabClearQueueClickEvent OnClearQueueClicked { get => onClearQueueClicked; }

	private bool inited = false;

	protected override void InitServer()
	{
		StartCoroutine(WaitForProvider());
	}

	private IEnumerator WaitForProvider()
	{
		while (Provider == null)
		{
			yield return WaitFor.EndOfFrame;
		}
		inited = true;
		onProductAddClicked = new ExoFabProductAddClickEvent();

		OnProductAddClicked.AddListener(AddProductToQueue);

		onCategoryClicked = new ExoFabCategoryClickEvent();

		OnCategoryClicked.AddListener(OpenCategory);

		onRemoveProductClicked = new ExoFabRemoveProductClickedEvent();

		OnRemoveProductClicked.AddListener(RemoveFromQueue);

		onUpQueueClicked = new ExoFabUpQueueClickedEvent();

		OnUpQueueClicked.AddListener(UpQueue);

		onDownQueueClicked = new ExoFabDownQueueClickedEvent();

		OnDownQueueClicked.AddListener(DownQueue);

		onDispenseSheetClicked = new ExoFabDispenseSheetClickEvent();

		OnDispenseSheetClicked.AddListener(DispenseSheet);

		onClearQueueClicked = new ExoFabClearQueueClickEvent();

		OnClearQueueClicked.AddListener(ClearQueue);

		//Makes sure it connects with the ExosuitFabricator
		exosuitFabricator = Provider.GetComponentInChildren<ExosuitFabricator>();
		//Subscribes to the MaterialsManipulated event
		ExosuitFabricator.MaterialsManipulated += UpdateServerMaterials;

		materialsAndCategoryDisplay.InitMaterialList(exosuitFabricator.materialStorage);
		materialsAndCategoryDisplay.InitCategories(exosuitFabricator.exoFabProducts);
		OnTabOpened.AddListener(UpdateGUIForPeepers);
	}

	//Updates the GUI and adds any visible NetUIElements to the server.
	public void UpdateServerMaterials()
	{
		materialsAndCategoryDisplay.UpdateMaterialList(exosuitFabricator.materialStorage);
	}

	//Everytime someone new looks at the tab, update the tab for the client
	public void UpdateGUIForPeepers(ConnectedPlayer notUsed)
	{
		StartCoroutine(WaitForClient());
	}

	private IEnumerator WaitForClient()
	{
		yield return new WaitForSeconds(0.2f);
		materialsAndCategoryDisplay.UpdateMaterialList(exosuitFabricator.materialStorage);
		queueDisplay.UpdateQueue();
	}

	//Used by buttons, which contains the amount and type to dispense
	public void DispenseSheet(int amount, ItemTrait materialType)
	{
		exosuitFabricator.DispenseMaterialSheet(amount, materialType);
	}

	public void AddProductToQueue(MachineProduct product)
	{
		Logger.Log("Adding Product");
		queueDisplay.CurrentProducts.Add(product);
		queueDisplay.UpdateQueue();
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

	public void RemoveFromQueue(int productNumber)
	{
		queueDisplay.RemoveProduct(productNumber);
	}

	public void UpQueue(int productNumber)
	{
		queueDisplay.MoveProductUpInQueue(productNumber);
	}

	public void DownQueue(int productNumber)
	{
		queueDisplay.MoveProductDownInqueue(productNumber);
	}

	public void ClearQueue()
	{
		Logger.Log("Clearing Queue");
		queueDisplay.ClearQueue();
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
		ExosuitFabricator.MaterialsManipulated -= UpdateServerMaterials;
	}
}

[System.Serializable]
public class ExoFabProductAddClickEvent : UnityEvent<MachineProduct>
{
}

[System.Serializable]
public class ExoFabRemoveProductClickedEvent : UnityEvent<int>
{
}

[System.Serializable]
public class ExoFabUpQueueClickedEvent : UnityEvent<int>
{
}

[System.Serializable]
public class ExoFabDownQueueClickedEvent : UnityEvent<int>
{
}

[System.Serializable]
public class ExoFabCategoryClickEvent : UnityEvent<MachineProductList>
{
}

[System.Serializable]
public class ExoFabDispenseSheetClickEvent : UnityEvent<int, ItemTrait>
{
}

[System.Serializable]
public class ExoFabClearQueueClickEvent : UnityEvent
{
}