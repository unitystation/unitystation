
using UnityEngine;

/// <summary>
/// Interaction logic for table tiles. Simply places the item on the table.
/// </summary>
[CreateAssetMenu(fileName = "TableInteraction", menuName = "Interaction/TileInteraction/TableInteraction")]
public class TableInteraction : TileInteraction
{
	public override bool WillInteract(TileApply interaction, NetworkSide side)
	{
		if (!DefaultWillInteract.Default(interaction, side)) return false;
		if (interaction.TileApplyType != TileApply.ApplyType.HandApply) return false;
		if (interaction.Intent == Intent.Harm) return false;
		return interaction.HandObject != null;
	}

	public override void ServerPerformInteraction(TileApply interaction)
	{
		//place item
		Inventory.ServerDrop(interaction.HandSlot, interaction.TargetVector);
	}
}
