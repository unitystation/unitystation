using System.Collections.Generic;
using UnityEngine;
using UI.Core.NetUI;
using Systems.Cargo;

namespace UI.Objects.Cargo
{
	public class GUI_CargoPageCart : MonoBehaviour
	{
		[SerializeField]
		private NetText_label confirmButtonText;
		[SerializeField]
		private NetText_label totalPriceText;
		[SerializeField]
		private EmptyItemList orderList;

		[SerializeField]
		private GUI_Cargo cargoGUI;

		public void SetUpTab()
		{
			CargoManager.Instance.OnCartUpdate.AddListener(UpdateTab);
			UpdateTab();
		}

		public void UpdateTab()
		{
			DisplayCurrentCart();
			if (cargoGUI.cargoConsole.CorrectID || cargoGUI.IsAIInteracting())
			{
				confirmButtonText.SetValue(CanAffordCart() ? "Confirm cart" : "Not enough credits!");

				CheckTotalPrice();
				if (CargoManager.Instance.CurrentCart.Count == 0)
				{
					confirmButtonText.SetValue("Cart is empty!");
					totalPriceText.SetValue("");
				}
			}
			else
			{
				confirmButtonText.SetValue("InvalidID");
			}

		}

		private void CheckTotalPrice()
		{
			totalPriceText.SetValue($"Cost: {CargoManager.Instance.TotalCartPrice()} credits");
		}

		public void ConfirmCart()
		{
			if (CanAffordCart() == false || (cargoGUI.cargoConsole.CorrectID == false && cargoGUI.IsAIInteracting() == false)) return;

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

			if (cargoGUI.cargoConsole.CorrectID || cargoGUI.IsAIInteracting())
			{
				confirmButtonText.MasterSetValue("InvalidID");
			}

			CheckTotalPrice();
		}
	}
}
