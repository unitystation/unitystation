using System.Collections.Generic;
using UnityEngine;
using UI.Core.NetUI;

namespace UI.Objects.Robotics
{
	public class GUI_ExoFabMaterialEntry : DynamicEntry
	{
		private GUI_ExosuitFabricator ExoFabMasterTab => containedInTab as GUI_ExosuitFabricator;

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
				ExoFabMasterTab.GetComponent<GUI_ExosuitFabricator>().OnDispenseSheetClicked.Invoke(amount, materialType);
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
			UpdateButtonVisibility();
		}

		public void UpdateButtonVisibility()
		{
			int sheetsDispensable = currentAmount / 2000;
			if (sheetsDispensable < 1)
			{
				buttonOne.MasterSetValue("false");
				buttonTen.MasterSetValue("false");
				buttonFifty.MasterSetValue("false");
			}
			else if (sheetsDispensable >= 1 && sheetsDispensable < 10)
			{
				buttonOne.MasterSetValue("true");
				buttonTen.MasterSetValue("false");
				buttonFifty.MasterSetValue("false");
			}
			else if (sheetsDispensable >= 10 && sheetsDispensable < 50)
			{
				buttonOne.MasterSetValue("true");
				buttonTen.MasterSetValue("true");
				buttonFifty.MasterSetValue("false");
			}
			else
			{
				buttonOne.MasterSetValue("true");
				buttonTen.MasterSetValue("true");
				buttonFifty.MasterSetValue("true");
			}
		}
	}
}
