using UnityEngine;
using UI.Core.NetUI;

namespace UI.Items.PDA
{
	public class GUI_PDAUplinkItemTemplate : DynamicEntry
	{
		[SerializeField]
		private NetText_label itemName = null;
		[SerializeField]
		private NetText_label itemCost = null;

		private GUI_PDAUplinkItem itemPage = null;
		private UplinkItem item;

		public void SelectItem()
		{
			itemPage.SelectItem(item);
		}

		public void ReInit(UplinkItem assignedItem)
		{
			itemPage = containedInTab.GetComponent<GUI_PDA>().uplinkPage.itemPage;
			item = assignedItem;
			itemName.MasterSetValue(item.Name);;
			itemCost.MasterSetValue(item.Cost.ToString());
		}
	}
}
