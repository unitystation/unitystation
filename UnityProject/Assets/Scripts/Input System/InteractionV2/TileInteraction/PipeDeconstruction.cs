using System.Collections;
using System.Collections.Generic;
using Pipes;
using UnityEngine;
/// <summary>
/// Used for handling data cleanup of pipe deconstruction and any consequences of de wrenching High pressure pipes
/// </summary>
[CreateAssetMenu(fileName = "PipeDeconstruction",
	menuName = "Interaction/TileInteraction/PipeDeconstruction")]
public class PipeDeconstruction : TileInteraction
{
	[Tooltip(
		"Trait required on the used item in order to deconstruct the tile. If welder, will check if welder is on.")]
	[SerializeField]
	private ItemTrait requiredTrait = null;

	[Tooltip("Action message to performer when they begin this interaction.")] [SerializeField]
	private string performerStartActionMessage = " you unwrench the pipe ";

	[Tooltip(
		"Use {performer} for performer name. Action message to others when the performer begins this interaction.")]
	[SerializeField]
	private string othersStartActionMessage = "{performer} unwrenches the pipe ";

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
		string othersMessage = Chat.ReplacePerformer(othersStartActionMessage, interaction.Performer);
		Chat.AddActionMsgToChat(interaction.Performer, performerStartActionMessage, othersMessage);
		if (interaction.BasicTile.LayerType != LayerType.Underfloor) return;

		var PipeTile = interaction.BasicTile as PipeTile;
		var matrix = interaction.TileChangeManager.MetaTileMap.Layers[LayerType.Underfloor].matrix;
		var metaDataNode = matrix.GetMetaDataNode(interaction.TargetCellPos);
		for (var i = 0; i < metaDataNode.PipeData.Count; i++)
		{
			if (metaDataNode.PipeData[i].RelatedTile != PipeTile) continue;
			var Transform =  matrix.UnderFloorLayer.GetMatrix4x4(metaDataNode.PipeData[i].NodeLocation, metaDataNode.PipeData[i].RelatedTile);
			var pipe = Spawn.ServerPrefab(PipeTile.SpawnOnDeconstruct, interaction.WorldPositionTarget, localRotation : QuaternionFromMatrix(Transform)).GameObject;
			var itempipe = pipe.GetComponent<PipeItem>();
			itempipe.Colour = matrix.UnderFloorLayer.GetColour(metaDataNode.PipeData[i].NodeLocation, metaDataNode.PipeData[i].RelatedTile);
			itempipe.Setsprite();
			matrix.RemoveUnderFloorTile( metaDataNode.PipeData[i].NodeLocation, metaDataNode.PipeData[i].RelatedTile);
			metaDataNode.PipeData[i].pipeData.OnDisable();
			metaDataNode.PipeData.RemoveAt(i);
			return;
		}


	}
	public static Quaternion QuaternionFromMatrix(Matrix4x4 m) { return Quaternion.LookRotation(m.GetColumn(2), m.GetColumn(1)); }
}
