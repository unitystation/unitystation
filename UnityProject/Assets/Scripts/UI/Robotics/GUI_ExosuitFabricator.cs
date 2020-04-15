using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

//Current features not implemented yet:
//Add all products. There are not enough items yet for this feature to make sense.
//The queue system has to be optimized, the list is currently recreated fully and then sent to the peepers.
//
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

	private ExoFabProductAddClickedEvent onProductAddClicked;
	public ExoFabProductAddClickedEvent OnProductAddClicked { get => onProductAddClicked; }
	private ExoFabCategoryClickedEvent onCategoryClicked;
	public ExoFabCategoryClickedEvent OnCategoryClicked { get => onCategoryClicked; }
	private ExoFabRemoveProductClickedEvent onRemoveProductClicked;
	public ExoFabRemoveProductClickedEvent OnRemoveProductClicked { get => onRemoveProductClicked; }

	private ExoFabUpQueueClickedEvent onUpQueueClicked;
	public ExoFabUpQueueClickedEvent OnUpQueueClicked { get => onUpQueueClicked; }
	private ExoFabDownQueueClickedEvent onDownQueueClicked;
	public ExoFabDownQueueClickedEvent OnDownQueueClicked { get => onDownQueueClicked; }
	private ExoFabDispenseSheetClickedEvent onDispenseSheetClicked;
	public ExoFabDispenseSheetClickedEvent OnDispenseSheetClicked { get => onDispenseSheetClicked; }

	private ExoFabClearQueueClickedEvent onClearQueueClicked;
	public ExoFabClearQueueClickedEvent OnClearQueueClicked { get => onClearQueueClicked; }

	private ExoFabProcessQueueClickedEvent onProcessQueueClicked;

	public ExoFabProcessQueueClickedEvent OnProcessQueueClicked { get => onProcessQueueClicked; }

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
		onProductAddClicked = new ExoFabProductAddClickedEvent();

		OnProductAddClicked.AddListener(AddProductToQueue);

		onCategoryClicked = new ExoFabCategoryClickedEvent();

		OnCategoryClicked.AddListener(OpenCategory);

		onRemoveProductClicked = new ExoFabRemoveProductClickedEvent();

		OnRemoveProductClicked.AddListener(RemoveFromQueue);

		onUpQueueClicked = new ExoFabUpQueueClickedEvent();

		OnUpQueueClicked.AddListener(UpQueue);

		onDownQueueClicked = new ExoFabDownQueueClickedEvent();

		OnDownQueueClicked.AddListener(DownQueue);

		onDispenseSheetClicked = new ExoFabDispenseSheetClickedEvent();

		OnDispenseSheetClicked.AddListener(DispenseSheet);

		onClearQueueClicked = new ExoFabClearQueueClickedEvent();

		OnClearQueueClicked.AddListener(ClearQueue);

		onProcessQueueClicked = new ExoFabProcessQueueClickedEvent();

		onProcessQueueClicked.AddListener(ProcessQueue);

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
		queueDisplay.CurrentProducts.Add(product);
		queueDisplay.UpdateQueue();
	}

	public void ProcessQueue()
	{
		MachineProduct ProcessedProduct = queueDisplay.CurrentProducts[0];
		if (exosuitFabricator.CanProcessProduct(ProcessedProduct))
		{
			queueDisplay.RemoveProduct(0);
			//Need more logic here, continue until it cannot process more products
		}
	}

	public void AddAllProductsToQueue(NetButton button)
	{
	}

	public void ReturnFromProductPage(GUI_ExoFabProductButton button)
	{
		//foreach (GameObject productEntries in categoryNameToProductEntries[button.categoryName])
		//{
		//	productEntries.SetActive(false);
		//}
		nestedSwitcher.SetActivePage(materialsAndCategoryDisplay);
		UpdateServerMaterials();
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
		queueDisplay.ClearQueue();
	}

	public void OpenCategory(MachineProductList categoryProducts)
	{
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
public class ExoFabProductAddClickedEvent : UnityEvent<MachineProduct>
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
public class ExoFabCategoryClickedEvent : UnityEvent<MachineProductList>
{
}

[System.Serializable]
public class ExoFabDispenseSheetClickedEvent : UnityEvent<int, ItemTrait>
{
}

[System.Serializable]
public class ExoFabClearQueueClickedEvent : UnityEvent
{
}

[System.Serializable]
public class ExoFabProcessQueueClickedEvent : UnityEvent
{
}