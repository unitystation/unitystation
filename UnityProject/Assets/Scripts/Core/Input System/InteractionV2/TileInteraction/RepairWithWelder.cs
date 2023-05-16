using System.Collections;
using System.Collections.Generic;
using TileManagement;
using Tiles;
using UnityEngine;


[CreateAssetMenu(fileName = "RepairWithWelder", menuName = "Interaction/TileInteraction/RepairWithWelder")]
public class RepairWithWelder : TileInteraction
{



	public LayerTile ReplaceWith; //TODO Tiles should handle itself

	public override bool WillInteract(TileApply interaction, NetworkSide side)
	{
		if (!DefaultWillInteract.Default(interaction, side)) return false;
		if (interaction.Intent == Intent.Harm) return false;

		if (interaction.HandObject == null) return false;

		if (Validations.HasItemTrait(interaction.HandObject, CommonTraits.Instance.Welder) == false) return false;
		if (Validations.HasUsedActiveWelder(interaction) == false) return false;

		return true;
	}

	public override void ServerPerformInteraction(TileApply interaction)
	{

		 ToolUtils.ServerUseToolWithActionMessages(interaction, 4f,
				$"You begin repairing the {interaction.BasicTile.DisplayName}...",
				$"{interaction.Performer.ExpensiveName()} begins to repair the {interaction.BasicTile.DisplayName}...",
				$"You repair the {interaction.BasicTile.DisplayName}.",
				$"{interaction.Performer.ExpensiveName()} repairs the {interaction.BasicTile.DisplayName}.",
				() => RepairTile(interaction));

	}

	private void RepairTile(TileApply interaction)
	{
		var tileMapDamage = interaction.TargetInteractableTiles.GetComponentInChildren<MetaTileMap>()
			.Layers[interaction.BasicTile.LayerType].gameObject.GetComponent<TilemapDamage>();

		if (ReplaceWith != null)
		{
			interaction.TargetInteractableTiles.MetaTileMap.SetTile(interaction.TargetCellPos, ReplaceWith);
		}


		tileMapDamage.RemoveTileEffects(interaction.TargetCellPos);
	}
}