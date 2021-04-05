using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Systems.Cargo;

namespace UI.Objects.Cargo
{
	public class GUI_CargoCartItem : DynamicEntry
	{
		private CargoOrderSO Order;

		public NetLabel cartNameLabel;

		public void RemoveFromCart()
		{
			CargoManager.Instance.RemoveFromCart(Order);
		}

		public void SetValues(CargoOrderSO newOrder)
		{
			Order = newOrder;
			cartNameLabel.SetValueServer($"{Order.OrderName}\n{Order.CreditCost} credits");
		}

	}
}
