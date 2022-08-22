using System.Collections.Generic;
using UnityEngine;
using UI.Core.NetUI;
using Systems.Cargo;

namespace UI.Objects.Cargo
{
	public class GUI_CargoItem : DynamicEntry
	{
		private CargoOrderSO Order;

		[SerializeField] private NetText_label supplyNameLabel;
		[SerializeField] private NetText_label priceLabel;

		public void AddToCart()
		{
			CargoManager.Instance.AddToCart(Order);
		}

		public void SetValues(CargoOrderSO newOrder)
		{
			Order = newOrder;
			supplyNameLabel.SetValueServer(Order.OrderName);
			priceLabel.SetValueServer($"{Order.CreditCost} credits");
		}
	}
}
