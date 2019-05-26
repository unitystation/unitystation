using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class GUI_CargoItem : DynamicEntry
{
	private CargoOrder order = null;
	public CargoOrder Order
	{
		get
		{
			return order;
		}
		set
		{
			order = value;
			ReInit();
		}
	}

	public void AddToCart()
	{
		CargoManager.Instance.AddToCart(Order);
	}

	public void RemoveFromCart()
	{
		CargoManager.Instance.RemoveFromCart(Order);
		Debug.Log("Remove");
	}

	public void ReInit()
	{
		if (order == null)
		{
			Logger.Log("CargoItem: no order found, not doing init", Category.NetUI);
			return;
		}
		foreach ( var element in Elements )
		{
			string nameBeforeIndex = element.name.Split('~')[0];
			switch (nameBeforeIndex)
			{
				case "SupplyName":
					element.SetValue = order.OrderName;
					break;
				case "Price":
					element.SetValue = order.CreditsCost.ToString() + " credits";
					break;
				case "CartName":
					element.SetValue = order.OrderName + "\n" + order.CreditsCost.ToString() + " credits";
					break;
				case "Cancel":
					element.SetValue = "CANCEL";
					break;
			}
		}
	}
}
