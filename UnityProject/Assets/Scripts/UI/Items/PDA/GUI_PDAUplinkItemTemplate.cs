using UnityEngine;
using UI.Core.NetUI;

namespace UI.Items.PDA
{
	public class GUI_PDAUplinkItemTemplate : DynamicEntry
	{
		[SerializeField]
		private NetLabel itemName = null;
		[SerializeField]
		private NetLabel itemCost = null;

		private GUI_PDAUplinkItem itemPage = null;
		private UplinkItem item;

		public void SelectItem()
		{
			itemPage.SelectItem(item);
		}

		public void ReInit(UplinkItem assignedItem)
		{
			itemPage = MasterTab.GetComponent<GUI_PDA>().uplinkPage.itemPage;
			item = assignedItem;
			itemName.SetValueServer(item.Name);;
			itemCost.SetValueServer(item.Cost.ToString());
		}
	}
}
