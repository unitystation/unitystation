using System.Collections.Generic;
using UnityEngine;
using UI.Core.NetUI;
using Systems.Cargo;

namespace UI.Objects.Cargo
{
	public class GUI_CargoCartItem : DynamicEntry
	{
		private CargoOrderSO Order;

		[SerializeField]
		private NetText_label cartNameLabel;

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
