
using System;
using UnityEngine;
using Random = UnityEngine.Random;

/// <summary>
/// Deconstruct the tile and spawn its deconstruction object (if defined) and (optionally) additional objects when an item with a particular
/// trait is used on the tile.
/// </summary>
[CreateAssetMenu(fileName = "DeconstructWhenItemUsed", menuName = "Interaction/TileInteraction/DeconstructWhenItemUsed")]
public class DeconstructWhenItemUsed : TileInteraction
{
	[Tooltip("Trait required on the used item in order to deconstruct the tile. If welder, will check if welder is on.")]
	[SerializeField]
	private ItemTrait requiredTrait;

	[Tooltip("Action message to performer when they begin this interaction.")]
	[SerializeField]
	private string performerStartActionMessage;

	[Tooltip("Use {performer} for performer name. Action message to others when the performer begins this interaction.")]
	[SerializeField]
	private string othersStartActionMessage;

	[Tooltip("Seconds taken to perform this action. Leave at 0 for instant.")]
	[SerializeField]
	private float seconds;

	[Tooltip("Additional objects to spawn in addition to the tile's deconstruction object.")]
	[SerializeField]
	private SpawnableList objectsToSpawn;

	[Tooltip("Action message to performer when they finish this interaction.")]
	[SerializeField]
	private string performerFinishActionMessage;

	[Tooltip("Use {performer} for performer name. Action message to others when performer finishes this interaction.")]
	[SerializeField]
	private string othersFinishActionMessage;

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
		ToolUtils.ServerUseToolWithActionMessages(interaction, seconds,
			performerStartActionMessage,
			Chat.ReplacePerformer(othersStartActionMessage, interaction.Performer),
			performerFinishActionMessage,
			Chat.ReplacePerformer(othersFinishActionMessage, interaction.Performer),
			() =>
			{
				interaction.TileChangeManager.RemoveTile(interaction.TargetCellPos, interaction.BasicTile.LayerType);
				//spawn things that need to be spawned
				if (interaction.BasicTile.SpawnOnDeconstruct != null && interaction.BasicTile.SpawnAmountOnDeconstruct > 0)
				{
					Spawn.ServerPrefab(interaction.BasicTile.SpawnOnDeconstruct, interaction.WorldPositionTarget, count: interaction.BasicTile.SpawnAmountOnDeconstruct);
				}
				if (objectsToSpawn != null)
				{
					objectsToSpawn.SpawnAt(SpawnDestination.At(interaction.WorldPositionTarget));
				}
				interaction.TileChangeManager.SubsystemManager.UpdateAt(interaction.TargetCellPos);
			});
	}
}
