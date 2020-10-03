using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GUI_ExoFabQueueDisplay : NetUIStringElement
{
	private List<MachineProduct> currentProducts = new List<MachineProduct>();
	public List<MachineProduct> CurrentProducts { get => currentProducts; }

	//Temporary until the queue has been optimized.
	public int maxProductsInQueue = 20;

	public int MaxProductsInQueue { get => maxProductsInQueue; }

	[SerializeField]
	private GUI_ExoFabButton processQueueButton = null;

	[SerializeField]
	private GUI_ExoFabButton clearQueueButton = null;

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
		else Logger.Log("Queue is full!!!");
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
		SetProcessAndClearButtonInteractable();
	}

	private void SetProcessAndClearButtonInteractable()
	{
		if (currentProducts.Count == 0)
		{
			processQueueButton.SetValueServer("false");
			clearQueueButton.SetValueServer("false");
		}
		else
		{
			processQueueButton.SetValueServer("true");
			clearQueueButton.SetValueServer("true");
		}
	}

	//The first entry on list must have its up button disabled, the last down button disabled.
	private void DisableUpDownButtons(GUI_ExoFabQueueItem item)
	{
		//Only one item
		if (currentProducts.Count == 1)
		{
			item.DownButton.SetValueServer("false");
			item.UpButton.SetValueServer("false");
		}
		else
		{
			if (item.NumberInQueue == 0) item.UpButton.SetValueServer("false");

			if (item.NumberInQueue == currentProducts.Count - 1) item.DownButton.SetValueServer("false");
		}
	}

	public override void ExecuteServer(ConnectedPlayer subject)
	{
	}
}