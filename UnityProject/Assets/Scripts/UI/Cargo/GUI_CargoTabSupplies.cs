using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class GUI_CargoTabSupplies : GUI_CargoTab
{
	[SerializeField]
	private GUI_CargoItemList orderList = null;
	private bool inited = false;
	[SerializeField]
	private NetLabel categoryText = null;

	public override void Init()
	{
		if (inited || !gameObject.activeInHierarchy)
		{
			return;
		}
		if (!CustomNetworkManager.Instance._isServer)
		{
			return;
		}
		CargoManager.Instance.OnCategoryUpdate.AddListener(UpdateTab);
		CargoManager.Instance.CurrentCategory = null;
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
		if (CargoManager.Instance.CurrentCategory == null)
		{
			DisplayCategoriesCatalog();
		}
		else
		{
			DisplayCurrentSupplies();
		}
		Debug.Log("Update");
	}

	private void DisplayCategoriesCatalog()
	{
		Debug.Log("Displaying catalogue");
		List<CargoOrderCategory> categories = CargoManager.Instance.Supplies;

		orderList.Clear();
		orderList.AddItems(categories.Count);
		for (int i = 0; i < categories.Count; i++)
		{
			GUI_CargoItem item = orderList.Entries[i] as GUI_CargoItem;
			item.ReInit(categories[i]);
		}
		categoryText.SetValue = "Categories";
	}

	private void DisplayCurrentSupplies()
	{
		Debug.Log("Displaying supplies");
		List<CargoOrder> supplies = CargoManager.Instance.CurrentCategory.Supplies;

		orderList.Clear();
		orderList.AddItems(supplies.Count);
		for (int i = 0; i < supplies.Count; i++)
		{
			GUI_CargoItem item = orderList.Entries[i] as GUI_CargoItem;
			item.Order = supplies[i];
		}
		categoryText.SetValue = CargoManager.Instance.CurrentCategory.CategoryName;
	}

	public void CloseCategory()
	{
		CargoManager.Instance.OpenCategory(null);
	}
}
