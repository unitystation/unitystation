﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Used for deconstructing cables in a Thread Friendly way
/// </summary>
[CreateAssetMenu(fileName = "ElectricalCableDeconstruction",
	menuName = "Interaction/TileInteraction/ElectricalCableDeconstruction")]
public class ElectricalCableDeconstruction : TileInteraction
{
	[Tooltip(
		"Trait required on the used item in order to deconstruct the tile. If welder, will check if welder is on.")]
	[SerializeField]
	private ItemTrait requiredTrait = null;

	[Tooltip("Action message to performer when they begin this interaction.")] [SerializeField]
	private string performerStartActionMessage = null;

	[Tooltip(
		"Use {performer} for performer name. Action message to others when the performer begins this interaction.")]
	[SerializeField]
	private string othersStartActionMessage = null;

	// Start is called before the first frame update
	public override bool WillInteract(TileApply interaction, NetworkSide side)
	{
		if (!DefaultWillInteract.Default(interaction, side)) return false;
		if (requiredTrait == CommonTraits.Instance.Welder)
		{
			return Validations.HasUsedActiveWelder(interaction);
		}

		return Validations.HasItemTrait(interaction.HandObject, requiredTrait);
	}

	public override void ServerPerformInteraction(TileApply interaction)
	{
		Chat.ReplacePerformer(othersStartActionMessage, interaction.Performer);
		if (interaction.BasicTile.LayerType == LayerType.Underfloor)
		{
			var ElectricalCable = interaction.BasicTile as ElectricalCableTile;
			if (ElectricalCable != null)
			{
				var matrix = interaction.TileChangeManager.MetaTileMap.Layers[LayerType.Underfloor].matrix;
				var metaDataNode = matrix.GetMetaDataNode(interaction.TargetCellPos);

				foreach (var ElectricalData in metaDataNode.ElectricalData)
				{
					if (ElectricalData.RelatedTile == ElectricalCable)
					{
						ElectricalData.InData.DestroyThisPlease();
						Spawn.ServerPrefab(ElectricalCable.SpawnOnDeconstruct, interaction.WorldPositionTarget,
							count: ElectricalCable.SpawnAmountOnDeconstruct);
						return;
					}
				}
			}
		}
	}
}