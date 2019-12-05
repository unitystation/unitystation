
using System;
using UnityEngine;
using Random = UnityEngine.Random;

/// <summary>
/// Deconstruct the tile and spawn its deconstruction prefab (if defined) when an item with a particular
/// trait is used on the tile.
/// </summary>
[CreateAssetMenu(fileName = "DeconstructWhenItemUsed", menuName = "Interaction/TileInteraction/DeconstructWhenItemUsed")]
public class DeconstructWhenItemUsed : TileInteraction
{
	[Tooltip("Trait required on the used item in order to deconstruct the tile.")]
	[SerializeField]
	private ItemTrait requiredTrait;

	public override bool WillInteract(TileApply interaction, NetworkSide side)
	{
		if (!DefaultWillInteract.Default(interaction, side)) return false;
		return Validations.HasItemTrait(interaction.HandObject, requiredTrait);
	}

	public override void ServerPerformInteraction(TileApply interaction)
	{
		interaction.TileChangeManager.RemoveTile(interaction.TargetCellPos, interaction.BasicTile.LayerType);
		ToolUtils.ServerUseTool(interaction);
		interaction.TileChangeManager.SubsystemManager.UpdateAt(interaction.TargetCellPos);

		if (interaction.BasicTile.SpawnOnDeconstruct != null && interaction.BasicTile.SpawnAmountOnDeconstruct > 0)
		{
			Spawn.ServerPrefab(interaction.BasicTile.SpawnOnDeconstruct, interaction.WorldPositionTarget, count: interaction.BasicTile.SpawnAmountOnDeconstruct);
		}

	}
}
