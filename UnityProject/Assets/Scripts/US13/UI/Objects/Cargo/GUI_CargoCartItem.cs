using UnityEngine;
using US13.Managers;
using US13.Systems.Cargo;
using US13.UI.Core.Net.Elements;
using US13.UI.Core.Net.Elements.Dynamic;

namespace US13.UI.Objects.Cargo
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
			cartNameLabel.MasterSetValue($"{Order.OrderName}\n{Order.CreditCost} credits");
		}
	}
}
