using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class GUI_CargoTabCart : GUI_CargoTab
{
	[SerializeField]
	private NetLabel confirmButtonText = null;
	[SerializeField]
	private NetLabel totalPriceText = null;

	[SerializeField]
	private GUI_CargoItemList orderList = null;

	public override void Init()
	{
		if (!CustomNetworkManager.Instance._isServer)
			return;

		Debug.Log(CargoManager.Instance);
		Debug.Log(CargoManager.Instance.OnCartUpdate);
		CargoManager.Instance.OnCartUpdate.AddListener(UpdateTab);
	}

	public override void OpenTab()
	{
		UpdateTab();
	}

	private void UpdateTab()
	{
		if (!CustomNetworkManager.Instance._isServer)
			return;

		DisplayCurrentCart();
		if (CanAffordCart())
		{
			confirmButtonText.SetValue = "CONFIRM CART";
		}
		else
		{
			confirmButtonText.SetValue = "NOT ENOUGH CREDITS";
		}
		totalPriceText.SetValue = "TOTAL: " + CargoManager.Instance.TotalCartPrice().ToString() + " CREDITS";
		if (CargoManager.Instance.CurrentCart.Count == 0)
		{
			confirmButtonText.SetValue = "CART IS EMPTY";
			totalPriceText.SetValue = "";
		}
		
	}

	public void ConfirmCart()
	{
		if (!CustomNetworkManager.Instance._isServer)
			return;

		if (!CanAffordCart())
		{
			return;
		}
		CargoManager.Instance.ConfirmCart();
	}

	private bool CanAffordCart()
	{
		return (CargoManager.Instance.TotalCartPrice() <= CargoManager.Instance.Credits);
	}

	private void DisplayCurrentCart()
	{
		List<CargoOrder> currentCart = CargoManager.Instance.CurrentCart;

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
