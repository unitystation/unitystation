using UnityEngine;
using Objects.Atmospherics;


namespace Tiles.Pipes
{
	/// <summary>
	/// Used for handling data cleanup of pipe deconstruction and any consequences of de-wrenching high-pressure pipes
	/// </summary>
	[CreateAssetMenu(fileName = "PipeDeconstruction",
		menuName = "Interaction/TileInteraction/PipeDeconstruction")]
	public class PipeDeconstruction : TileInteraction
	{
		[Tooltip("Trait required on the used item in order to deconstruct the tile. If welder, will check if welder is on.")]
		[SerializeField]
		private ItemTrait requiredTrait = null;

		[Tooltip("Action message to performer when they begin this interaction.")]
		[SerializeField]
		private string performerStartActionMessage = null;

		[Tooltip("Use {performer} for performer name. Action message to others when the performer begins this interaction.")]
		[SerializeField]
		private string othersStartActionMessage = null;

		[Tooltip("Seconds taken to perform this action. Leave at 0 for instant.")]
		[SerializeField]
		private float seconds = 0;

		[Tooltip("Action message to performer when they finish this interaction.")]
		[SerializeField]
		private string performerFinishActionMessage = null;

		[Tooltip("Use {performer} for performer name. Action message to others when performer finishes this interaction.")]
		[SerializeField]
		private string othersFinishActionMessage = null;


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
			if (interaction.BasicTile.LayerType != LayerType.Pipe) return;

			var pipeTile = interaction.BasicTile as PipeTile;
			var matrix = interaction.TileChangeManager.MetaTileMap.Layers[LayerType.Pipe].Matrix;
			var metaDataNode = matrix.GetMetaDataNode(interaction.TargetCellPos);

			for (var i = 0; i < metaDataNode.PipeData.Count; i++)
			{
				if (metaDataNode.PipeData[i].RelatedTile != pipeTile) continue; //TODO Stuff like layers and stuff can be included

				ToolUtils.ServerUseToolWithActionMessages(interaction, seconds,
					performerStartActionMessage,
					Chat.ReplacePerformer(othersStartActionMessage, interaction.Performer),
					performerFinishActionMessage,
					Chat.ReplacePerformer(othersFinishActionMessage, interaction.Performer),
					() => { metaDataNode.PipeData[i].pipeData.Remove(); });
				return;
				// var Transform =  matrix.UnderFloorLayer.GetMatrix4x4(metaDataNode.PipeData[i].NodeLocation, metaDataNode.PipeData[i].RelatedTile);
				// var pipe = Spawn.ServerPrefab(PipeTile.SpawnOnDeconstruct, interaction.WorldPositionTarget, localRotation : QuaternionFromMatrix(Transform)).GameObject;
				// var itempipe = pipe.GetComponent<PipeItemTile>();
				// itempipe.Colour = matrix.UnderFloorLayer.GetColour(metaDataNode.PipeData[i].NodeLocation, metaDataNode.PipeData[i].RelatedTile);
				// itempipe.Setsprite();
				// matrix.RemoveUnderFloorTile( metaDataNode.PipeData[i].NodeLocation, metaDataNode.PipeData[i].RelatedTile);
				// var savedpipe = metaDataNode.PipeData[i].pipeData;
				// metaDataNode.PipeData.RemoveAt(i);
			}
		}

		public static Quaternion QuaternionFromMatrix(Matrix4x4 m)
		{
			return Quaternion.LookRotation(m.GetColumn(2), m.GetColumn(1));
		}
	}
}
