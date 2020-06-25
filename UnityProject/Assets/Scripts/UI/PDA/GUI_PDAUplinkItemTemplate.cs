using UnityEngine;

namespace UI.PDA
{
	public class GUI_PDAUplinkItemTemplate : DynamicEntry
	{
		private GUI_PDA pdaMasterTab = null;

		[SerializeField]
		private NetLabel itemName;

		[SerializeField]
		private NetLabel itemCost;

		private UplinkItems item;

		private void Start()
		{
			pdaMasterTab = MasterTab.GetComponent<GUI_PDA>();
		}


		public void SelectItem()
		{
			if (pdaMasterTab == null) { MasterTab.GetComponent<GUI_PDA>().OnItemClickedEvent.Invoke(item); }
			else { pdaMasterTab.OnItemClickedEvent.Invoke(item); }
		}


		public void ReInit(UplinkItems assignedItem)
		{
			item = assignedItem;
			itemName.SetValueServer(item.Name);;
			itemCost.SetValueServer($"Cost {item.Cost} TC");
		}
	}
}
