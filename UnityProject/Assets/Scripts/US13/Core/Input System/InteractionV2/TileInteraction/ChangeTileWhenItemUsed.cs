using UnityEngine;
using US13.Core.Lifecycle;
using US13.Core.Lifecycle.Spawnable;
using US13.Items.Tool;
using US13.Items.Traits;
using US13.Tilemaps.Behaviours.Layers;
using US13.Tilemaps.Tiles;
using US13.UI.Systems.MainHUD.UI_Bottom;
//using NUnit.Framework.Internal;

namespace US13.Core.Input_System.InteractionV2.TileInteraction
{
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

		[Tooltip("What intent is required to perform this interaction? None means any intent works.")]
		[SerializeField]
		private Intent requiredIntent = Intent.Disarm;

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

			if (requiredIntent != Intent.None && interaction.Intent != requiredIntent) return false;

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
				Chat.Chat.ReplacePerformer(othersStartActionMessage, interaction.Performer),
				performerFinishActionMessage,
				Chat.Chat.ReplacePerformer(othersFinishActionMessage, interaction.Performer),
				() =>
				{
					//change tile
					interaction.TileChangeManager.MetaTileMap.SetTile(interaction.TargetCellPos, toTile);
					//remove overlays
					interaction.TileChangeManager.MetaTileMap.RemoveFloorWallOverlaysOfType(interaction.TargetCellPos, OverlayType.Cleanable);
					if (objectsToSpawn != null)
					{
						objectsToSpawn.SpawnAt(SpawnDestination.At(interaction.WorldPositionTarget));
					}
					interaction.TileChangeManager.SubsystemManager.UpdateAt(interaction.TargetCellPos);
				});
		}
	}
}
