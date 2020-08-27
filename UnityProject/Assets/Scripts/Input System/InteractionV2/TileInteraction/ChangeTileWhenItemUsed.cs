
using System;
//using NUnit.Framework.Internal;
using UnityEngine;
using Random = UnityEngine.Random;

/// <summary>
/// Changes the tile to a different tile when an item with a particular
/// trait is used on the tile. Uses progress actions, so also supports specifying a time
/// and messages.
/// </summary>
[CreateAssetMenu(fileName = "ChangeTileWhenItemUsed", menuName = "Interaction/TileInteraction/ChangeTileWhenItemUsed")]
public class ChangeTileWhenItemUsed : TileInteraction
{
	[Tooltip("Trait required on the used item in order to deconstruct the tile. If welder, checks if it's on.")]
	[SerializeField]
	private ItemTrait requiredTrait = null;
	
	//First implemented for allowing players to both repair and unsecure reinforced windows using welders depending on intent.
	[Tooltip("Do you need to be on harm intent to perform this interaction?")]
	[SerializeField]
	private bool harmIntentRequired = false;

	[Tooltip("Action message to performer when they begin this interaction.")]
	[SerializeField]
	private string performerStartActionMessage = null;

	[Tooltip("Use {performer} for performer name. Action message to others when the performer begins this interaction.")]
	[SerializeField]
	private string othersStartActionMessage = null;

	[Tooltip("Seconds taken to perform this action. Leave at 0 for instant.")]
	[SerializeField]
	private float seconds = 0;

	[Tooltip("Additional objects to spawn at the changed tile.")]
	[SerializeField]
	private SpawnableList objectsToSpawn = null;

	[Tooltip("Tile to change to on completion.")]
	[SerializeField]
	private LayerTile toTile = null;

	[Tooltip("Action message to performer when they finish this interaction.")]
	[SerializeField]
	private string performerFinishActionMessage = null;

	[Tooltip("Use {performer} for performer name. Action message to others when performer finishes this interaction.")]
	[SerializeField]
	private string othersFinishActionMessage = null;

	public override bool WillInteract(TileApply interaction, NetworkSide side)
	{
		if (!DefaultWillInteract.Default(interaction, side)) return false;
		
		if (harmIntentRequired == true)
		{
			if (interaction.Intent != Intent.Harm) return false;
		}

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
				interaction.TileChangeManager.UpdateTile(interaction.TargetCellPos, toTile);
				if (objectsToSpawn != null)
				{
					objectsToSpawn.SpawnAt(SpawnDestination.At(interaction.WorldPositionTarget));
				}
				interaction.TileChangeManager.SubsystemManager.UpdateAt(interaction.TargetCellPos);
			});
	}
}
