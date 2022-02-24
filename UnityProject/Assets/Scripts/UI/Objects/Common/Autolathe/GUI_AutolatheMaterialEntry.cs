using System.Collections.Generic;
using UnityEngine;
using UI.Core.NetUI;
using UI.Objects.Robotics;

namespace UI.Objects
{
	public class GUI_AutolatheMaterialEntry : DynamicEntry
	{
		private GUI_Autolathe ExoFabMasterTab => MasterTab as GUI_Autolathe;

		private ItemTrait materialType;
		private int currentAmount;

		private NetLabel amountLabel;

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
			UpdateButtonVisibility();
		}

		public void UpdateButtonVisibility()
		{
			int sheetsDispensable = currentAmount / 2000;
			if (sheetsDispensable < 1)
			{
				buttonOne.SetValueServer("false");
				buttonTen.SetValueServer("false");
				buttonFifty.SetValueServer("false");
			}
			else if (sheetsDispensable >= 1 && sheetsDispensable < 10)
			{
				buttonOne.SetValueServer("true");
				buttonTen.SetValueServer("false");
				buttonFifty.SetValueServer("false");
			}
			else if (sheetsDispensable >= 10 && sheetsDispensable < 50)
			{
				buttonOne.SetValueServer("true");
				buttonTen.SetValueServer("true");
				buttonFifty.SetValueServer("false");
			}
			else
			{
				buttonOne.SetValueServer("true");
				buttonTen.SetValueServer("true");
				buttonFifty.SetValueServer("true");
			}
		}
	}
}
