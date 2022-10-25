using System.Collections.Generic;
using UnityEngine;
using UI.Core.NetUI;

namespace UI.Objects.Cargo
{
	public class GUI_MaterialEntry : DynamicEntry
	{
		private GUI_MaterialsList materialList;

		private ItemTrait materialType;

		public NetText_label labelName;
		public NetText_label labelAmount;

		public void DispenseMaterial(int amount)
		{
			materialList.materialStorageLink.usedStorage.DispenseSheet(amount, materialType, materialList.materialStorageLink.gameObject.AssumedWorldPosServer());
		}

		public void SetValues(ItemTrait material, int amount, GUI_MaterialsList matListDisplay)
		{
			materialList = matListDisplay;
			materialType = material;
			labelAmount.MasterSetValue($"{amount} cm3");
			labelName.MasterSetValue(material.name);
		}
	}
}
