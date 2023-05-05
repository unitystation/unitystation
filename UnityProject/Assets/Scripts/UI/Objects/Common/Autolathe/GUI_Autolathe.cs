using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UI.Core.NetUI;
using Systems.Electricity;
using Objects.Machines;

//Reused code from GUI_ExosuitFabricator

namespace UI.Objects
{
	public class GUI_Autolathe : NetTab
	{
		[SerializeField]
		private GUI_AutolathePageMaterialsAndCategory materialsAndCategoryDisplay = null;

		[SerializeField]
		private GUI_AutolathePageProducts productDisplay = null;

		[SerializeField]
		private GUI_AutolatheQueueDisplay queueDisplay = null;

		[SerializeField]
		private GUI_RDProPageBuildingProcess buildingPage = null;

		[SerializeField]
		private NetPageSwitcher nestedSwitcher = null;

		private Autolathe autolathe;
		public Autolathe Autolathe => autolathe;

		public Dictionary<string, GameObject[]> categoryNameToProductEntries = new Dictionary<string, GameObject[]>();

		private AutolatheProductAddClickedEvent onProductAddClicked;
		public AutolatheProductAddClickedEvent OnProductAddClicked { get => onProductAddClicked; }
		private AutolatheCategoryClickedEvent onCategoryClicked;
		public AutolatheCategoryClickedEvent OnCategoryClicked { get => onCategoryClicked; }
		private RDProRemoveProductClickedEvent onRemoveProductClicked;
		public RDProRemoveProductClickedEvent OnRemoveProductClicked { get => onRemoveProductClicked; }

		private RDProUpQueueClickedEvent onUpQueueClicked;
		public RDProUpQueueClickedEvent OnUpQueueClicked { get => onUpQueueClicked; }
		private RDProDownQueueClickedEvent onDownQueueClicked;
		public RDProDownQueueClickedEvent OnDownQueueClicked { get => onDownQueueClicked; }
		private RDProDispenseSheetClickedEvent onDispenseSheetClicked;
		public RDProDispenseSheetClickedEvent OnDispenseSheetClicked { get => onDispenseSheetClicked; }

		private RDProClearQueueClickedEvent onClearQueueClicked;
		public RDProClearQueueClickedEvent OnClearQueueClicked { get => onClearQueueClicked; }

		private RDProProcessQueueClickedEvent onProcessQueueClicked;

		public RDProProcessQueueClickedEvent OnProcessQueueClicked { get => onProcessQueueClicked; }

		private RDProProductFinishedEvent onProductFinishedEvent;

		public RDProProductFinishedEvent OnProductFinishedEvent { get => onProductFinishedEvent; }

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

			onProductAddClicked = new AutolatheProductAddClickedEvent();

			OnProductAddClicked.AddListener(AddProductToQueue);

			onCategoryClicked = new AutolatheCategoryClickedEvent();

			OnCategoryClicked.AddListener(OpenCategory);

			onRemoveProductClicked = new RDProRemoveProductClickedEvent();

			OnRemoveProductClicked.AddListener(RemoveFromQueue);

			onUpQueueClicked = new RDProUpQueueClickedEvent();

			OnUpQueueClicked.AddListener(UpQueue);

			onDownQueueClicked = new RDProDownQueueClickedEvent();

			OnDownQueueClicked.AddListener(DownQueue);

			onDispenseSheetClicked = new RDProDispenseSheetClickedEvent();

			OnDispenseSheetClicked.AddListener(DispenseSheet);

			onClearQueueClicked = new RDProClearQueueClickedEvent();

			OnClearQueueClicked.AddListener(ClearQueue);

			onProcessQueueClicked = new RDProProcessQueueClickedEvent();

			onProcessQueueClicked.AddListener(ProcessQueue);

			onProductFinishedEvent = new RDProProductFinishedEvent();

			onProductFinishedEvent.AddListener(ProcessQueue);

			//Makes sure it connects with the ExosuitFabricator
			autolathe = Provider.GetComponentInChildren<Autolathe>();
			//Subscribes to the MaterialsManipulated event
			Autolathe.MaterialsManipulated += UpdateMaterialsDisplay;

			materialsAndCategoryDisplay.InitMaterialList(autolathe.materialStorageLink.usedStorage);
			materialsAndCategoryDisplay.InitCategories(autolathe.AutolatheProducts);
			OnTabOpened.AddListener(UpdateGUIForPeepers);
		}

		public void UpdateMaterialsDisplay()
		{
			materialsAndCategoryDisplay.UpdateMaterialList(autolathe.materialStorageLink.usedStorage);
		}

		//Everytime someone new looks at the tab, update the tab for the client
		public void UpdateGUIForPeepers(PlayerInfo notUsed)
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
			materialsAndCategoryDisplay.UpdateMaterialList(autolathe.materialStorageLink.usedStorage);
			queueDisplay.UpdateQueue();
			isUpdating = false;
		}

		//Used by buttons, which contains the amount and type to dispense
		public void DispenseSheet(int amount, ItemTrait materialType)
		{
			autolathe.DispenseMaterialSheet(amount, materialType);
		}

		public void AddProductToQueue(MachineProduct product)
		{
			if (APCPoweredDevice.IsOn(autolathe.PoweredState))
			{
				queueDisplay.AddToQueue(product);
			}
		}

		public void ProcessQueue()
		{
			if (APCPoweredDevice.IsOn(autolathe.PoweredState))
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
					if (buildingPage.IsAnimating)
					{
						buildingPage.StopAnimatingLabel();
					}

					if (!nestedSwitcher.CurrentPage.Equals(materialsAndCategoryDisplay))
					{
						nestedSwitcher.SetActivePage(materialsAndCategoryDisplay);
					}

					UpdateMaterialsDisplay();
				}
			}
		}

		private IEnumerator ProcessQueueUntilUnable(MachineProduct processedProduct)
		{
			if (autolathe.CanProcessProduct(processedProduct))
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

		public void ReturnFromProductPage(GUI_RDProProductButton button)
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
			Autolathe.MaterialsManipulated -= UpdateMaterialsDisplay;
		}
	}
}
