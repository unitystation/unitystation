using UnityEngine;
using US13.Items.Traits;
using US13.UI.Core.Net.Elements;
using US13.UI.Core.Net.Elements.Dynamic;

namespace US13.UI.Objects.Common.Flatpacker
{
	public class GUI_FlatpackerMaterialsEntry : DynamicEntry
	{
		private GUI_Flatpacker FlatpackerTab => containedInTab as GUI_Flatpacker;

		private ItemTrait materialType;

		[SerializeField] private NetText_label materialLabel;

		public void DispenseMaterial(int amount)
		{
			if (FlatpackerTab == null) return;

			FlatpackerTab.OnDispenseItemButtonPressed(amount, materialType);
			FlatpackerTab.UpdateMaterialsPage();
		}

		public void ReInit(ItemTrait material, int amount)
		{
			materialType = material;
			materialLabel.MasterSetValue($"{material.Name}: {amount}");
		}
	}
}
