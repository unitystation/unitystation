using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Systems.Cargo;

namespace UI.Objects.Cargo
{
	public class GUI_CargoPageSupplies : GUI_CargoPage
	{
		[SerializeField]
		private EmptyItemList orderList = null;
		private bool inited = false;
		[SerializeField]
		private NetLabel categoryText = null;

		public override void Init()
		{
			if (inited || !gameObject.activeInHierarchy)
			{
				return;
			}
			if (!CustomNetworkManager.Instance._isServer)
			{
				return;
			}
			CargoManager.Instance.OnCategoryUpdate.AddListener(UpdateTab);
			CargoManager.Instance.CurrentCategory = null;
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

		private void UpdateTab()
		{
			if (CargoManager.Instance.CurrentCategory == null)
			{
				DisplayCategoriesCatalog();
			}
			else
			{
				DisplayCurrentSupplies();
			}
		}

		private void DisplayCategoriesCatalog()
		{
			List<CargoCategory> categories = CargoManager.Instance.Supplies;

			orderList.Clear();
			orderList.AddItems(categories.Count);
			for (int i = 0; i < categories.Count; i++)
			{
				GUI_CargoItem item = orderList.Entries[i] as GUI_CargoItem;
				item.ReInit(categories[i]);
			}
			categoryText.SetValueServer("Categories");
		}

		private void DisplayCurrentSupplies()
		{
			List<CargoOrderSO> supplies = CargoManager.Instance.CurrentCategory.Orders;

			orderList.Clear();
			orderList.AddItems(supplies.Count);
			for (int i = 0; i < supplies.Count; i++)
			{
				GUI_CargoItem item = orderList.Entries[i] as GUI_CargoItem;
				item.Order = supplies[i];
			}
			categoryText.SetValueServer(CargoManager.Instance.CurrentCategory.CategoryName);
		}

		public void CloseCategory()
		{
			CargoManager.Instance.OpenCategory(null);
		}
	}
}
