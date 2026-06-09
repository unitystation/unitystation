using UnityEngine;
using US13.Managers;
using US13.Systems.Cargo;
using US13.UI.Core.Net.Elements;
using US13.UI.Core.Net.Elements.Dynamic;

namespace US13.UI.Objects.Cargo
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
			supplyNameLabel.MasterSetValue(Order.OrderName);
			priceLabel.MasterSetValue($"{Order.CreditCost} credits");
		}
	}
}
