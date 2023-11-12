using System.Collections;
using System.Collections.Generic;
using TileManagement;
using UnityEngine;

public class SmokeNode : SpreadNode
{
	//Smoke FOV?

	public override bool CheckIsEdge()
	{
		bool hasEmptyNeighbour = false;
		var worldPos = OnMetaDataNode.WorldPosition.To3();

		foreach (var dir in dirs) //Might be lag
		{
			var newWorldPos = worldPos + dir;

			MetaDataNode node = GetNodeFromPosition(newWorldPos);

			if (node.SmokeNode.IsActive == false || node.SmokeNode.SourceReservoir != SourceReservoir)
			{
				hasEmptyNeighbour = true;
			}
		}

		return hasEmptyNeighbour;
	}

	public override void TrySpread()
	{
		var worldPos = OnMetaDataNode.WorldPosition;

		foreach (var dir in dirs) //Might be lag
		{
			var newWorldPos = worldPos + dir;

			MetaDataNode node = GetNodeFromPosition(newWorldPos);

			if (node.SmokeNode.IsActive == false && node.IsOccupied == false)
			{
				SourceReservoir.SpreadToNode(this ,node.SmokeNode);
			}
		}
	}

	private MetaDataNode GetNodeFromPosition(Vector3 pos)
	{
		var matrix = MatrixManager.AtPoint(pos, CustomNetworkManager.IsServer);

		MetaDataNode node = null;
		if (MatrixManager.Instance.spaceMatrix.MatrixInfo != matrix)
		{
			var newLocal = pos.ToLocal(matrix);
			node = matrix.MetaDataLayer.Get(newLocal.RoundToInt());
		}
		else
		{
			var newLocal = pos.ToLocal(OnMetaDataNode.PositionMatrix);
			node = OnMetaDataNode.PositionMatrix.MetaDataLayer.Get(newLocal.RoundToInt());
		}

		return node;
	}

	public override void DistributeToTile(SpreadNode SpreadingFrom,SourceReservoir sourceReservoir)
	{
		base.DistributeToTile(SpreadingFrom, sourceReservoir);
		var Colour = Present.MixColor;
		if (Present.MixColor == Color.clear)
		{
			Colour = Color.white;
		}

		OnMetaDataNode.PositionMatrix.MetaTileMap.AddOverlay(OnMetaDataNode.LocalPosition, SmokeAndFoamManager.Instance.OverlayTileSmoke, Matrix4x4.identity, Colour);

		if (SpreadingFrom != null) //So it doesn't try adding itself to Splat it came from bugging out the reaction
		{
			MatrixManager.ReagentReact(Present, OnMetaDataNode.WorldPosition);
		}
	}

	public override void Update()
	{
		//TODO apply reagents to people and stuff??
		//but lag?
		//but Applying to items
		//not yet

		PresentTimeCount += 1;
		if (PresentTimeCount > MaxTimePresent)
		{
			OnMetaDataNode.PositionMatrix.MetaTileMap.RemoveOverlaysOfType(OnMetaDataNode.LocalPosition, LayerType.Effects,OverlayType.Smoke);
			SourceReservoir.RemoveTile(this);
		}
	}

}
