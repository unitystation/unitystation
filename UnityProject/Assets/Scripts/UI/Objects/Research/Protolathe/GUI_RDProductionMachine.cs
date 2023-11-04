using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UI.Core.NetUI;
using Systems.Electricity;
using Objects.Machines;
using Systems.Research.Objects;
using Systems.Research;
using UnityEngine.Events;

namespace UI.Objects
{
	public class GUI_RDProductionMachine : NetTab
	{
		[SerializeField]
		private NetSpriteImage serverLabel = null;

		[SerializeField]
		private GUI_RDProPageMaterialsAndCategory materialsAndCategoryDisplay = null;

		[SerializeField]
		private GUI_RDProPageProducts productDisplay = null;

		[SerializeField]
		private GUI_RDProQueueDisplay queueDisplay = null;

		[SerializeField]
		private GUI_RDProPageBuildingProcess buildingPage = null;

		[SerializeField]
		private NetPageSwitcher nestedSwitcher = null;

		[HideInInspector]
		public RDProductionMachine rdProductionMachine;

		public Dictionary<string, GameObject[]> categoryNameToProductEntries = new Dictionary<string, GameObject[]>();

		private RDProProductAddClickedEvent onProductAddClicked;
		public RDProProductAddClickedEvent OnProductAddClicked { get => onProductAddClicked; }
		private RDProCategoryClickedEvent onCategoryClicked;
		public RDProCategoryClickedEvent OnCategoryClicked { get => onCategoryClicked; }
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

			onProductAddClicked = new RDProProductAddClickedEvent();

			OnProductAddClicked.AddListener(AddProductToQueue);

			onCategoryClicked = new RDProCategoryClickedEvent();

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

			//Makes sure it connects with the RDProductionMachine
			rdProductionMachine = Provider.GetComponentInChildren<RDProductionMachine>();
			//Subscribes to the MaterialsManipulated event
			Autolathe.MaterialsManipulated += UpdateMaterialsDisplay;

			UpdateServerDisplay();

			materialsAndCategoryDisplay.InitMaterialList(rdProductionMachine.materialStorageLink.usedStorage);
			materialsAndCategoryDisplay.InitCategories(rdProductionMachine.Categories);
			OnTabOpened.AddListener(UpdateGUIForPeepers);
			if (rdProductionMachine != null) rdProductionMachine.MaterialsManipulated += UpdateMaterialsDisplay;
		}

		public void UpdateServerDisplay()
		{
			if (rdProductionMachine.researchServer == null)
			{
				serverLabel.SetSprite(0);
			}
			else
			{
				serverLabel.SetSprite(1);
			}
		}

		public void UpdateMaterialsDisplay()
		{
			materialsAndCategoryDisplay.UpdateMaterialList(rdProductionMachine.materialStorageLink.usedStorage);
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
			materialsAndCategoryDisplay.UpdateMaterialList(rdProductionMachine.materialStorageLink.usedStorage);
			UpdateServerDisplay();
			queueDisplay.UpdateQueue();
			isUpdating = false;
		}

		//Used by buttons, which contains the amount and type to dispense
		public void DispenseSheet(int amount, ItemTrait materialType)
		{
			rdProductionMachine.DispenseMaterialSheet(amount, materialType);
		}

		public void AddProductToQueue(string product)
		{
			if (APCPoweredDevice.IsOn(rdProductionMachine.PoweredState))
			{
				queueDisplay.AddToQueue(product);
			}
		}

		public void ProcessQueue()
		{
			if (APCPoweredDevice.IsOn(rdProductionMachine.PoweredState))
			{
				//Checks if there's still products in the queue and if it's already processing
				if (queueDisplay.CurrentProducts.Count > 0 && !isProcessing)
				{
					string processedProduct = queueDisplay.CurrentProducts[0];
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

		private IEnumerator ProcessQueueUntilUnable(string DesignID)
		{
			Design productDesign = Designs.Globals.InternalIDSearch[DesignID];

			if (rdProductionMachine.CanProcessProduct(DesignID))
			{
				if (!nestedSwitcher.CurrentPage.Equals(buildingPage)) { nestedSwitcher.SetActivePage(buildingPage); }

				isProcessing = true;
				queueDisplay.RemoveProduct(0);
				queueDisplay.UpdateQueue();
				buildingPage.SetProductLabelProductName(productDesign.Name);
				buildingPage.StartAnimateLabel();

				yield return new WaitForSeconds(0.3f + 0.2f);

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

		public void OpenCategory(List<string> categoryProducts, string categoryName)
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
			if (rdProductionMachine != null) rdProductionMachine.MaterialsManipulated -= UpdateMaterialsDisplay;
		}
	}

	#region Events

	[System.Serializable]
	public class RDProProductAddClickedEvent : UnityEvent<string>
	{
	}

	[System.Serializable]
	public class RDProCategoryClickedEvent : UnityEvent<List<string>, string>
	{
	}

	[System.Serializable]
	public class AutolatheProductAddClickedEvent : UnityEvent<MachineProduct>
	{
	}

	[System.Serializable]
	public class AutolatheCategoryClickedEvent : UnityEvent<MachineProductList>
	{
	}

	[System.Serializable]
	public class ExoFabProductAddClickedEvent : UnityEvent<MachineProduct>
	{
	}

	[System.Serializable]
	public class RDProRemoveProductClickedEvent : UnityEvent<int>
	{
	}

	[System.Serializable]
	public class RDProUpQueueClickedEvent : UnityEvent<int>
	{
	}

	[System.Serializable]
	public class RDProDownQueueClickedEvent : UnityEvent<int>
	{
	}

	[System.Serializable]
	public class RDProDispenseSheetClickedEvent : UnityEvent<int, ItemTrait>
	{
	}

	[System.Serializable]
	public class RDProClearQueueClickedEvent : UnityEvent
	{
	}

	[System.Serializable]
	public class RDProProcessQueueClickedEvent : UnityEvent
	{
	}

	[System.Serializable]
	public class RDProProductFinishedEvent : UnityEvent
	{
	}

	#endregion
}
