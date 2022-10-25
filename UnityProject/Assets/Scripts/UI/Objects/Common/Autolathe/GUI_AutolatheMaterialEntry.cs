using System.Collections.Generic;
using UnityEngine;
using UI.Core.NetUI;
using UI.Objects.Robotics;

namespace UI.Objects
{
	public class GUI_AutolatheMaterialEntry : DynamicEntry
	{
		private GUI_Autolathe ExoFabMasterTab => containedInTab as GUI_Autolathe;

		private ItemTrait materialType;
		private int currentAmount;

		private NetText_label amountLabel;

		private NetInteractiveButton buttonOne;
		private NetInteractiveButton buttonTen;
		private NetInteractiveButton buttonFifty;

		public void DispenseMaterial(int amount)
		{
			if (ExoFabMasterTab == null)
			{
				ExoFabMasterTab.GetComponent<GUI_Autolathe>().OnDispenseSheetClicked.Invoke(amount, materialType);
			}
			else
			{
				ExoFabMasterTab?.OnDispenseSheetClicked.Invoke(amount, materialType);
			}
		}

		public void ReInit(ItemTrait material, int amount)
		{
			currentAmount = amount;
			materialType = material;

			foreach (var element in Elements)
			{
				string nameBeforeIndex = element.name.Split('~')[0];
				switch (nameBeforeIndex)
				{
					case "MaterialName":
						((NetUIElement<string>)element).MasterSetValue(CraftingManager.MaterialSheetData[material].displayName + ":");
						break;

					case "MaterialAmount":
						((NetUIElement<string>)element).MasterSetValue(currentAmount + " cm3");
						amountLabel = element as NetText_label;
						break;

					case "OneSheetButton":
						buttonOne = element as NetInteractiveButton;
						break;

					case "TenSheetButton":
						buttonTen = element as NetInteractiveButton;
						break;

					case "FiftySheetButton":
						buttonFifty = element as NetInteractiveButton;
						break;
				}
			}
		}
	}
}
