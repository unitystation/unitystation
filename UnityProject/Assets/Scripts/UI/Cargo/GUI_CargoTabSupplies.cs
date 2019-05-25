using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class GUI_CargoTabSupplies : GUI_CargoTab
{
	[SerializeField]
	private GUI_CargoItem itemPrefab = null;
	[SerializeField]
	private Transform itemHolder = null;

	//Item Lists
	private List<GUI_CargoItem> itemsList = new List<GUI_CargoItem>();

	public override void OnTabClosed()
	{
		ClearList();
	}

	public override void OnTabOpened()
	{
		PopulateList();
		DisplayCurrentSupplies();
	}

	public override void UpdateTab()
	{
		ClearList();
		PopulateList();
		DisplayCurrentSupplies();
	}

	private void DisplayCurrentSupplies()
	{
		List<CargoOrder> supplies = CargoManager.Instance.Supplies;

		for (int i = 0; i < supplies.Count; i++)
		{
			//itemsList[i].TitleText.text = supplies[i].OrderName + " - " + supplies[i].CreditsCost.ToString() + " points";
			//itemsList[i].ButtonText.text = "BUY";
			//itemsList[i].TitleText.text = supplies[i].OrderName;
			//itemsList[i].ButtonText.text = supplies[i].CreditsCost.ToString() + " credits";
			itemsList[i].Order = supplies[i];
			itemsList[i].gameObject.SetActive(true);
		}
	}

	private void PopulateList()
	{
		while (itemsList.Count < CargoManager.Instance.Supplies.Count)
		{
			AddItemToList();
		}
	}

	private void AddItemToList()
	{
		GUI_CargoItem item = Instantiate(itemPrefab, itemHolder);
		itemsList.Add(item);
		item.gameObject.SetActive(false);
	}

	private void ClearList()
	{
		for (int i = 0; i < itemsList.Count; i++)
		{
			itemsList[i].gameObject.SetActive(false);
		}
	}
}
