﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Systems.Cargo;

namespace UI.Objects.Cargo
{
	public class GUI_CargoPageCart : GUI_CargoPage
	{
		public NetLabel confirmButtonText;
		public NetLabel totalPriceText;
		public EmptyItemList orderList;

		public override void OpenTab()
		{
			CargoManager.Instance.OnCartUpdate.AddListener(UpdateTab);
		}

		public override void UpdateTab()
		{
			DisplayCurrentCart();
			if (cargoGUI.cargoConsole.CorrectID)
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
			totalPriceText.SetValueServer($"TOTAL: {CargoManager.Instance.TotalCartPrice()} CREDITS");
		}

		public void ConfirmCart()
		{
			if (!CanAffordCart() || !cargoGUI.cargoConsole.CorrectID)
			{
				return;
			}
			CargoManager.Instance.ConfirmCart();
			cargoGUI.ResetId();
		}

		private bool CanAffordCart()
		{
			return CargoManager.Instance.TotalCartPrice() <= CargoManager.Instance.Credits;
		}

		private void DisplayCurrentCart()
		{
			var currentCart = CargoManager.Instance.CurrentCart;

			orderList.Clear();
			orderList.AddItems(currentCart.Count);

			for (var i = 0; i < currentCart.Count; i++)
			{
				var item = (GUI_CargoCartItem)orderList.Entries[i];
				item.SetValues(currentCart[i]);
				item.gameObject.SetActive(true);
			}
			if (cargoGUI.cargoConsole.CorrectID)
			{
				confirmButtonText.SetValueServer("InvalidID");
			}
			CheckTotalPrice();
		}
	}
}
