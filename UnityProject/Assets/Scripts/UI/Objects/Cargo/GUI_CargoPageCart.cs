using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.UI;

public class GUI_CargoPageCart : GUI_CargoPage
{
	[SerializeField]
	private NetLabel confirmButtonText = null;
	[SerializeField]
	private NetLabel totalPriceText = null;

	[SerializeField]
	private EmptyItemList orderList = null;
	private bool inited = false;

	[SerializeField]
	private GUI_Cargo cargoController = null;

	public override void Init()
	{
		if (inited || !gameObject.activeInHierarchy)
			return;
		if (!CustomNetworkManager.Instance._isServer)
			return;
		CargoManager.Instance.OnCartUpdate.AddListener(UpdateTab);
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

	private void Start()
	{
		CheckTotalPrice();
		DisplayCurrentCart();
	}

	public void UpdateTab()
	{
		if (!CustomNetworkManager.Instance._isServer)
			return;


		DisplayCurrentCart();
		if (cargoController.CurrentId())
		{
			if (CanAffordCart())
			{
				confirmButtonText.SetValueServer("CONFIRM CART");
			}
			else
			{
				confirmButtonText.SetValueServer("NOT ENOUGH CREDITS");
			}
			CheckTotalPrice();
			if (CargoManager.Instance.CurrentCart.Count == 0)
			{
				confirmButtonText.SetValueServer("CART IS EMPTY");
				totalPriceText.SetValueServer("");
			}
		}
		else
		{
			confirmButtonText.SetValueServer("InvalidID");
		}

	}

	private void CheckTotalPrice()
	{
		totalPriceText.SetValueServer("TOTAL: " + CargoManager.Instance.TotalCartPrice().ToString() + " CREDITS");
	}

	public void ConfirmCart()
	{
		if (!CustomNetworkManager.Instance._isServer)
			return;

		if (!CanAffordCart() || !cargoController.CurrentId())
		{
			return;
		}
		CargoManager.Instance.ConfirmCart();
		cargoController.ResetId();
	}

	private bool CanAffordCart()
	{
		return (CargoManager.Instance.TotalCartPrice() <= CargoManager.Instance.Credits);
	}

	private void DisplayCurrentCart()
	{
		List<CargoOrder> currentCart = CargoManager.Instance.CurrentCart;

		orderList.Clear();
		orderList.AddItems(currentCart.Count);

		for (int i = 0; i < currentCart.Count; i++)
		{
			GUI_CargoItem item = orderList.Entries[i] as GUI_CargoItem;
			item.Order = currentCart[i];
			item.gameObject.SetActive(true);
		}
		if (cargoController.CurrentId())
		{
			confirmButtonText.SetValueServer("InvalidID");
		}
		CheckTotalPrice();
	}

}
