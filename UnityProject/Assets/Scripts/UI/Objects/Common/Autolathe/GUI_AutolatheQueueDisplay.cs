using System.Collections.Generic;
using Logs;
using UnityEngine;
using UI.Core.NetUI;
using Objects.Machines;

namespace UI.Objects
{
	public class GUI_AutolatheQueueDisplay : NetUIStringElement
	{
		private List<MachineProduct> currentProducts = new List<MachineProduct>();
		public List<MachineProduct> CurrentProducts => currentProducts;

		//Temporary until the queue has been optimized.
		public int maxProductsInQueue = 20;

		public int MaxProductsInQueue => maxProductsInQueue;

		[SerializeField]
		private NetInteractiveButton processQueueButton = null;

		[SerializeField]
		private NetInteractiveButton clearQueueButton = null;

		[SerializeField]
		private EmptyItemList itemsInQueue = null;

		public void MoveProductUpInQueue(int productNumber)
		{
			if (productNumber != 0)
			{
				MachineProduct temp1 = currentProducts[productNumber];
				MachineProduct temp2 = currentProducts[productNumber - 1];
				currentProducts[productNumber] = temp2;
				currentProducts[productNumber - 1] = temp1;
				UpdateQueue();
			}
		}

		public void MoveProductDownInqueue(int productNumber)
		{
			if (currentProducts.Count != productNumber - 1)
			{
				MachineProduct temp1 = currentProducts[productNumber];
				MachineProduct temp2 = currentProducts[productNumber + 1];
				currentProducts[productNumber + 1] = temp1;
				currentProducts[productNumber] = temp2;
				UpdateQueue();
			}
		}

		public void RemoveProduct(int numberInQueue)
		{
			currentProducts.RemoveAt(numberInQueue);
			UpdateQueue();
		}

		public void ClearQueue()
		{
			currentProducts.Clear();
			itemsInQueue.Clear();
		}

		public void AddToQueue(MachineProduct product)
		{
			if (currentProducts.Count <= maxProductsInQueue)
			{
				currentProducts.Add(product);
				UpdateQueue();
			}
			else Loggy.Log("Tried to add to Autolathe queue, but queue was full", Category.Machines);
		}

		public void UpdateQueue()
		{
			itemsInQueue.Clear();
			itemsInQueue.AddItems(currentProducts.Count);
			for (int i = 0; i < currentProducts.Count; i++)
			{
				GUI_AutolatheQueueItem item = itemsInQueue.Entries[i] as GUI_AutolatheQueueItem;
				item.Product = currentProducts[i];
				item.NumberInQueue = i;
				item.ReInit();
				DisableUpDownButtons(item);
			}
			SetProcessAndClearButtonInteractable();
		}

		private void SetProcessAndClearButtonInteractable()
		{
			if (currentProducts.Count == 0)
			{
				processQueueButton.MasterSetValue("false");
				clearQueueButton.MasterSetValue("false");
			}
			else
			{
				processQueueButton.MasterSetValue("true");
				clearQueueButton.MasterSetValue("true");
			}
		}

		//The first entry on list must have its up button disabled, the last down button disabled.
		private void DisableUpDownButtons(GUI_AutolatheQueueItem item)
		{
			//Only one item
			if (currentProducts.Count == 1)
			{
				item.DownButton.MasterSetValue("false");
				item.UpButton.MasterSetValue("false");
			}
			else
			{
				if (item.NumberInQueue == 0) item.UpButton.MasterSetValue("false");

				if (item.NumberInQueue == currentProducts.Count - 1) item.DownButton.MasterSetValue("false");
			}
		}

		public override void ExecuteServer(PlayerInfo subject)
		{
		}
	}
}
