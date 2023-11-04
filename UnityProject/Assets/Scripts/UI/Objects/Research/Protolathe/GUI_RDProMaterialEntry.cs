using UI.Core.NetUI;

namespace UI.Objects
{
	public class GUI_RDProMaterialEntry : DynamicEntry
	{
		private GUI_RDProductionMachine RDProMasterTab => containedInTab as GUI_RDProductionMachine;

		private ItemTrait materialType;
		private int currentAmount;

		private NetText_label amountLabel;

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
