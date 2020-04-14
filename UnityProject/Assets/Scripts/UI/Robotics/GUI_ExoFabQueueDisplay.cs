using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GUI_ExoFabQueueDisplay : NetUIElement
{
	private List<MachineProduct> currentProducts = new List<MachineProduct>();
	public List<MachineProduct> CurrentProducts { get => currentProducts; }

	[SerializeField]
	private EmptyItemList itemsInQueue;

	public void MoveProductUpInQueue(int productNumber)
	{
		MachineProduct temp1 = currentProducts[productNumber];
		MachineProduct temp2 = currentProducts[productNumber - 1];
		currentProducts[productNumber] = temp2;
		currentProducts[productNumber - 1] = temp1;
		UpdateQueue();
	}

	public void MoveProductDownInqueue(int productNumber)
	{
		MachineProduct temp1 = currentProducts[productNumber];
		MachineProduct temp2 = currentProducts[productNumber + 1];
		currentProducts[productNumber + 1] = temp1;
		currentProducts[productNumber] = temp2;
		UpdateQueue();
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
		currentProducts.Add(product);
		UpdateQueue();
	}

	public void UpdateQueue()
	{
		itemsInQueue.Clear();
		itemsInQueue.AddItems(currentProducts.Count);
		for (int i = 0; i < currentProducts.Count; i++)
		{
			GUI_ExoFabQueueItem item = itemsInQueue.Entries[i] as GUI_ExoFabQueueItem;
			item.Product = currentProducts[i];
			item.NumberInQueue = i;
			item.ReInit();
			DisableUpDownButtons(item);
		}
	}

	//The first entry on list must have its up button disabled, the last down button disabled.
	private void DisableUpDownButtons(GUI_ExoFabQueueItem item)
	{
		//Only one item
		if (currentProducts.Count == 1)
		{
			item.DownButton.SetValue = "false";
		}
		else
		{
			if (item.NumberInQueue == 0) item.UpButton.SetValue = "false";

			if (item.NumberInQueue == currentProducts.Count - 1) item.DownButton.SetValue = "false";
		}
	}

	public override void ExecuteServer()
	{
	}
}