using System;
using System.Collections.Generic;
using UnityEngine;
using UI.Core.NetUI;
using Objects.Machines;

namespace UI.Objects.Cargo
{
	public class GUI_MaterialsList : NetPage
	{
		[SerializeField] private EmptyItemList materialList = null;
		public MaterialStorageLink materialStorageLink;

		public void UpdateMaterialList()
		{
			var materialRecords = materialStorageLink.usedStorage.MaterialList;
			materialList.Clear();
			materialList.AddItems(materialRecords.Count);
			var i = 0;
			foreach (var material in materialRecords.Keys)
			{
				var item = materialList.Entries[i] as GUI_MaterialEntry;
				item.SetValues(material, materialRecords[material], this);
				i++;
			}
		}
	}
}
