using System.Collections.Generic;
using UnityEngine;

namespace UI.PDA
{
	public class GUI_PDAUplinkItem : NetPage
	{
		[SerializeField]
		private GUI_PDAUplinkMenu controller = null;

		[SerializeField]
		private EmptyItemList itemTemplate = null;

		public void GenerateEntries(List<UplinkItems> itementries)
		{
			itemTemplate.Clear();
			itemTemplate.AddItems(itementries.Count);
			for (int i = 0; i < itementries.Count; i++)
			{
				itemTemplate.Entries[i].GetComponent<GUI_PDAUplinkItemTemplate>().ReInit(itementries[i]);
			}
		}

		public void ClearItems()
		{
			itemTemplate.Clear();
		}

		public void Back()
		{
			ClearItems();
			controller.ShowCategories();
		}

	}
}
