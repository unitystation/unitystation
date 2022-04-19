using System.Collections.Generic;
using UnityEngine;
using UI.Core.NetUI;
using UI.Objects.Robotics;

namespace UI.Objects
{
	public class GUI_RDProMaterialEntry : DynamicEntry
	{
		private GUI_RDProductionMachine RDProMasterTab => MasterTab as GUI_RDProductionMachine;

		private ItemTrait materialType;
		private int currentAmount;

		private NetLabel amountLabel;

		private NetInteractiveButton buttonOne;
		private NetInteractiveButton buttonTen;
		private NetInteractiveButton buttonFifty;

		public void DispenseMaterial(int amount)
		{
			if (RDProMasterTab == null)
			{
				RDProMasterTab.GetComponent<GUI_RDProductionMachine>().OnDispenseSheetClicked.Invoke(amount, materialType);
			}
			else
			{
				RDProMasterTab?.OnDispenseSheetClicked.Invoke(amount, materialType);
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
						((NetUIElement<string>)element).SetValueServer(CraftingManager.MaterialSheetData[material].displayName + ":");
						break;

					case "MaterialAmount":
						((NetUIElement<string>)element).SetValueServer(currentAmount + " cm3");
						amountLabel = element as NetLabel;
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
