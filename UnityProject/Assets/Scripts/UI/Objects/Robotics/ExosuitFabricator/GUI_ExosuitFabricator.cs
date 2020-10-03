using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

//Current features not implemented yet:
//Add all products. There are not enough items yet for this feature to make sense.
//The queue system has to be optimized, the list is currently recreated fully and then sent to the peepers.

public class GUI_ExosuitFabricator : NetTab
{
	[SerializeField]
	private GUI_ExoFabPageMaterialsAndCategory materialsAndCategoryDisplay = null;

	[SerializeField]
	private GUI_ExoFabPageProducts productDisplay = null;

	[SerializeField]
	private GUI_ExoFabQueueDisplay queueDisplay = null;

	[SerializeField]
	private GUI_ExoFabPageBuildingProcess buildingPage = null;

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

	private ExoFabProductFinishedEvent onProductFinishedEvent;

	public ExoFabProductFinishedEvent OnProductFinishedEvent { get => onProductFinishedEvent; }

	private bool isUpdating = false;
	private bool isProcessing = false;

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

		onProductFinishedEvent = new ExoFabProductFinishedEvent();

		onProductFinishedEvent.AddListener(ProcessQueue);

		//Makes sure it connects with the ExosuitFabricator
		exosuitFabricator = Provider.GetComponentInChildren<ExosuitFabricator>();
		//Subscribes to the MaterialsManipulated event
		ExosuitFabricator.MaterialsManipulated += UpdateMaterialsDisplay;

		materialsAndCategoryDisplay.InitMaterialList(exosuitFabricator.materialStorage);
		materialsAndCategoryDisplay.InitCategories(exosuitFabricator.exoFabProducts);
		OnTabOpened.AddListener(UpdateGUIForPeepers);
	}

	public void UpdateMaterialsDisplay()
	{
		materialsAndCategoryDisplay.UpdateMaterialList(exosuitFabricator.materialStorage);
	}

	//Everytime someone new looks at the tab, update the tab for the client
	public void UpdateGUIForPeepers(ConnectedPlayer notUsed)
	{
		if (!isUpdating)
		{
			isUpdating = true;
			StartCoroutine(WaitForClient());
		}
	}

	private IEnumerator WaitForClient()
	{
		yield return new WaitForSeconds(0.2f);
		materialsAndCategoryDisplay.UpdateMaterialList(exosuitFabricator.materialStorage);
		queueDisplay.UpdateQueue();
		isUpdating = false;
	}

	//Used by buttons, which contains the amount and type to dispense
	public void DispenseSheet(int amount, ItemTrait materialType)
	{
		exosuitFabricator.DispenseMaterialSheet(amount, materialType);
	}

	public void AddProductToQueue(MachineProduct product)
	{
		queueDisplay.AddToQueue(product);
	}

	public void ProcessQueue()
	{
		//Checks if there's still products in the queue and if it's already processing
		if (queueDisplay.CurrentProducts.Count > 0 && !isProcessing)
		{
			MachineProduct processedProduct = queueDisplay.CurrentProducts[0];
			StartCoroutine(ProcessQueueUntilUnable(processedProduct));
		}
		else if (isProcessing)
		{
			//Do nothing
		}
		else
		{
			isProcessing = false;
			if (buildingPage.IsAnimating) { buildingPage.StopAnimatingLabel(); }
			if (!nestedSwitcher.CurrentPage.Equals(materialsAndCategoryDisplay)) { nestedSwitcher.SetActivePage(materialsAndCategoryDisplay); }
			UpdateMaterialsDisplay();
		}
	}

	private IEnumerator ProcessQueueUntilUnable(MachineProduct processedProduct)
	{
		if (exosuitFabricator.CanProcessProduct(processedProduct))
		{
			if (!nestedSwitcher.CurrentPage.Equals(buildingPage)) { nestedSwitcher.SetActivePage(buildingPage); }

			isProcessing = true;
			queueDisplay.RemoveProduct(0);
			queueDisplay.UpdateQueue();
			buildingPage.SetProductLabelProductName(processedProduct.Name);
			buildingPage.StartAnimateLabel();

			yield return new WaitForSeconds(processedProduct.ProductionTime + 0.2f);

			isProcessing = false;
			onProductFinishedEvent.Invoke();
		}
		else
		{
			isProcessing = false;
			buildingPage.StopAnimatingLabel();
			nestedSwitcher.SetActivePage(materialsAndCategoryDisplay);
		}
	}

	public void AddAllProductsToQueue(NetButton button)
	{
	}

	public void ReturnFromProductPage(GUI_ExoFabProductButton button)
	{
		nestedSwitcher.SetActivePage(materialsAndCategoryDisplay);
		UpdateMaterialsDisplay();
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

	private void OnDestroy()
	{
		ExosuitFabricator.MaterialsManipulated -= UpdateMaterialsDisplay;
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

[System.Serializable]
public class ExoFabProductFinishedEvent : UnityEvent
{
}
