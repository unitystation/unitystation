using System.Collections;
using System.Collections.Generic;
using TileManagement;
using UnityEngine;

public class SmokeNode : SpreadNode
{
	//Smoke FOV?

	public override bool CheckIsEdge()
	{
		bool HasEmptyNeighbour = false;
		var Worldpos = OnMetaDataNode.Position.ToWorld(OnMetaDataNode.PositionMatrix);

		foreach (var dir in dirs) //Might be lag
		{
			var newWorldpos = Worldpos + dir;
			var Matrix = MatrixManager.AtPoint(Worldpos + dir, CustomNetworkManager.IsServer);
			MetaDataNode node = null;
			if (MatrixManager.Instance.spaceMatrix.MatrixInfo != Matrix)
			{
				var newLocal = newWorldpos.ToLocal(Matrix);
				node =  Matrix.MetaDataLayer.Get(newLocal.RoundToInt());
			}
			else
			{
				var newLocal = newWorldpos.ToLocal(OnMetaDataNode.PositionMatrix);
				node = OnMetaDataNode.PositionMatrix.MetaDataLayer.Get(newLocal.RoundToInt());
			}
			if (node.SmokeNode.IsActive == false || node.SmokeNode.SourceReservoir != SourceReservoir)
			{
				HasEmptyNeighbour = true;
			}
		}

		return HasEmptyNeighbour;
	}

	public override void TrySpread()
	{
		var Worldpos = OnMetaDataNode.Position.ToWorld(OnMetaDataNode.PositionMatrix);

		foreach (var dir in dirs) //Might be lag
		{
			var newWorldpos = Worldpos + dir;
			var Matrix = MatrixManager.AtPoint(Worldpos + dir, CustomNetworkManager.IsServer);
			MetaDataNode node = null;
			if (MatrixManager.Instance.spaceMatrix.MatrixInfo != Matrix)
			{
				var newLocal = newWorldpos.ToLocal(Matrix);
				node =  Matrix.MetaDataLayer.Get(newLocal.RoundToInt());
			}
			else
			{
				var newLocal = newWorldpos.ToLocal(OnMetaDataNode.PositionMatrix);
				node = OnMetaDataNode.PositionMatrix.MetaDataLayer.Get(newLocal.RoundToInt());
			}
			if (node.SmokeNode.IsActive == false && node.IsOccupied == false)
			{
				SourceReservoir.SpreadToNode(this ,node.SmokeNode);
			}
		}
	}

	public override void DistributeToTile(SourceReservoir sourceReservoir)
	{
		base.DistributeToTile(sourceReservoir);
		var Colour = Present.MixColor;
		if (Present.MixColor == Color.clear)
		{
			Colour = Color.white;
		}

		OnMetaDataNode.PositionMatrix.MetaTileMap.AddOverlay(OnMetaDataNode.Position, SmokeAndFoamManager.Instance.OverlayTileSmoke, Matrix4x4.identity, Colour);

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
			OnMetaDataNode.PositionMatrix.MetaTileMap.RemoveOverlaysOfType(OnMetaDataNode.Position, LayerType.Effects,OverlayType.Smoke);
			SourceReservoir.RemoveTile(this);
		}
	}

}

public class SmokeSourceReservoir : SourceReservoir
{


	public override void RemoveTileInherit()
	{
		SmokeAndFoamManager.Instance.ActiveNodes.Remove(this);
	}

}

