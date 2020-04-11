using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GUI_QueueDisplay : MonoBehaviour
{
	private List<MachineProduct> currentProducts = new List<MachineProduct>();

	[SerializeField]
	private EmptyItemList itemsInQueue;

	public void MoveProductUpInQueue()
	{
	}

	public void MoveProductDownInqueue()
	{
	}

	public void AddToQueue(MachineProduct product)
	{
	}

	public void UpdateQueue()
	{
		itemsInQueue.Clear();
		itemsInQueue.AddItems(currentProducts.Count);
		for (int i = 0; i < currentProducts.Count; i++)
		{
			GUI_ExoFabItem item = itemsInQueue.Entries[i] as GUI_ExoFabItem;
			item.Product = currentProducts[i];
		}
	}
}