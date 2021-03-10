using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Systems.Cargo;

namespace UI.Objects.Cargo
{
	public class GUI_CargoItem : DynamicEntry
	{
		private CargoOrder order = null;
		public CargoOrder Order {
			get {
				return order;
			}
			set {
				order = value;
				ReInit();
			}
		}
		private CargoOrderCategory category;

		public void AddToCart()
		{
			if (category != null)
			{
				OpenCategory();
				return;
			}
			CargoManager.Instance.AddToCart(Order);
		}

		public void RemoveFromCart()
		{
			CargoManager.Instance.RemoveFromCart(Order);
		}

		public void OpenCategory()
		{
			CargoManager.Instance.OpenCategory(category);
		}

		public void ReInit()
		{
			category = null;
			if (order == null)
			{
				Logger.Log("CargoItem: no order found, not doing init", Category.Cargo);
				return;
			}
			foreach (var element in Elements)
			{
				string nameBeforeIndex = element.name.Split('~')[0];
				switch (nameBeforeIndex)
				{
					case "SupplyName":
						((NetUIElement<string>)element).SetValueServer(order.OrderName);
						break;
					case "Price":
						((NetUIElement<string>)element).SetValueServer(order.CreditsCost + " credits");
						break;
					case "CartName":
						((NetUIElement<string>)element).SetValueServer(order.OrderName + "\n" + order.CreditsCost + " credits");
						break;
					case "Cancel":
						((NetUIElement<string>)element).SetValueServer("CANCEL");
						break;
				}
			}
		}


		public void ReInit(CargoOrderCategory _category)
		{
			category = _category;
			foreach (var element in Elements)
			{
				string nameBeforeIndex = element.name.Split('~')[0];
				switch (nameBeforeIndex)
				{
					case "SupplyName":
						((NetUIElement<string>)element).SetValueServer(category.CategoryName);
						break;
					case "Price":
						((NetUIElement<string>)element).SetValueServer("ENTER");
						break;
				}
			}
		}
	}
}
