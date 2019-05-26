using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class GUI_CargoTabSupplies : GUI_CargoTab
{
	[SerializeField]
	private GUI_CargoItemList orderList = null;
	private bool inited = false;

	public override void Init()
	{
		if (inited || !gameObject.activeInHierarchy)
			return;
		if (!CustomNetworkManager.Instance._isServer)
			return;
	}

	public override void OpenTab()
	{
		if (!inited)
		{
			Init();
			inited = true;
		}
		UpdateTab();
	}

	private void UpdateTab()
	{
		DisplayCurrentSupplies();
		Debug.Log("Update");
	}

	private void DisplayCurrentSupplies()
	{
		List<CargoOrder> currentCart = CargoManager.Instance.Supplies;

		orderList.Clear();
		orderList.AddItems(currentCart);
		for (int i = 0; i < currentCart.Count; i++)
		{
			GUI_CargoItem item = orderList.Entries[i] as GUI_CargoItem;
			item.Order = currentCart[i];
			item.gameObject.SetActive(true);
		}
	}
}
