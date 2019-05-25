using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class GUI_CargoTabCart : GUI_CargoTab
{
	[SerializeField]
	private GUI_CargoItem itemPrefab = null;
	[SerializeField]
	private Transform itemHolder = null;
	[SerializeField]
	private Text confirmButtonText = null;
	[SerializeField]
	private Text totalPriceText = null;

	[SerializeField]
	private GUI_CargoItemList orderList;

	private void Start()
	{
		if (!CustomNetworkManager.Instance._isServer)
			return;
		CargoManager.Instance.OnCartUpdate += DisplayCurrentCart;
		/*
		List<CargoOrder> initList = CargoManager.Instance.CurrentCart;
		for ( var i = 0; i < initList.Count; i++ )
		{
			EntryList.AddItem(itemPrefab.gameObject);
		}
		*/
	}

	public override void OnTabClosed()
	{

	}

	public override void OnTabOpened()
	{
		DisplayCurrentCart();
		//UpdateTab();
	}

	public override void UpdateTab()
	{
		DisplayCurrentCart();
		if (CanAffordCart())
		{
			confirmButtonText.text = "CONFIRM CART";
		}
		else
		{
			confirmButtonText.text = "NOT ENOUGH CREDITS";
		}
		totalPriceText.text = "TOTAL: " + CargoManager.Instance.TotalCartPrice().ToString() + " CREDITS";
		if (CargoManager.Instance.CurrentCart.Count == 0)
		{
			confirmButtonText.text = "CART IS EMPTY";
			totalPriceText.text = "";
		}
	}

	public void ConfirmCart()
	{
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
		ClearList();
		List<CargoOrder> currentCart = CargoManager.Instance.CurrentCart;

		if (currentCart.Count > orderList.Entries.Length)
		{
			Debug.Log(currentCart.Count + "/" + orderList.Entries.Length);
			PopulateList();
		}

		for (int i = 0; i < currentCart.Count; i++)
		{
			GUI_CargoItem item = orderList.Entries[i] as GUI_CargoItem;
			item.Order = currentCart[i];
			item.gameObject.SetActive(true);
			Debug.Log("Reinited " + currentCart[i].OrderName);
		}
	}

	public void AddItem(CargoOrder order)
	{
		Debug.Log("Adding " + order.OrderName);
		orderList.AddItem(order);
	}

	private void RemoveItem(CargoOrder order)
	{
		orderList.RemoveItem(order);
	}

	private void ClearList()
	{
		for (int i = 0; i < orderList.Entries.Length; i++)
		{
			Debug.Log("removed");
			orderList.Entries[i].gameObject.SetActive(false);
		}
	}

	private void PopulateList()
	{
		List<CargoOrder> currentCart = CargoManager.Instance.CurrentCart;

		for (int i = orderList.Entries.Length; i < currentCart.Count; i++)
		{
			AddItem(currentCart[i]);
		}
	}
}
