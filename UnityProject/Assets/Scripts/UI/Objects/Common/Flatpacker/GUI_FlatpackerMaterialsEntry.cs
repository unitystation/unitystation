using UI.Core.NetUI;
using UI.Objects;
using UnityEngine;

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
