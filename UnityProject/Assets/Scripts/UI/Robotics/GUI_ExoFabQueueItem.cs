using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GUI_ExoFabQueueItem : DynamicEntry
{
	private GUI_ExosuitFabricator exoFabMasterTab;
	private MachineProduct product;

	private MachineProduct Product
	{
		get => product;
		set
		{
			product = value;
			ReInit();
		}
	}

	private int numberInQueue;

	public int NumberInQueue
	{
		get => numberInQueue;
		set
		{
			numberInQueue = value;
			ReInit();
		}
	}

	public void ForwardInQueue()
	{
		if (exoFabMasterTab == null) { MasterTab.GetComponent<GUI_ExosuitFabricator>().OnProductAddClicked.Invoke(Product); }
		else { exoFabMasterTab?.OnRemoveProductClicked.Invoke(NumberInQueue); }
	}

	public void BackwardsInQueue()
	{
		if (exoFabMasterTab == null) { MasterTab.GetComponent<GUI_ExosuitFabricator>().OnProductAddClicked.Invoke(Product); }
		else { exoFabMasterTab?.OnUpQueueClicked.Invoke(NumberInQueue); }
	}

	public void RemoveFromQueue()
	{
		if (exoFabMasterTab == null) { MasterTab.GetComponent<GUI_ExosuitFabricator>().OnProductAddClicked.Invoke(Product); }
		else { exoFabMasterTab?.OnDownQueueClicked.Invoke(NumberInQueue); }
	}

	public void ReInit()
	{
		if (product == null)
		{
			Logger.Log("ExoFab Product not found");
			return;
		}
		foreach (var element in Elements)
		{
			string nameBeforeIndex = element.name.Split('~')[0];
			switch (nameBeforeIndex)
			{
				case "QueueNumber":
					element.SetValue = NumberInQueue.ToString();
					break;

				case "ProductName":
					element.SetValue = Product.Name;
					break;
			}
		}
	}
}