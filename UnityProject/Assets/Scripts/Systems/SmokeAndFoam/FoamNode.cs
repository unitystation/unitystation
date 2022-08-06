using System.Collections;
using System.Collections.Generic;
using TileManagement;
using Tiles;
using UnityEngine;

public class FoamNode : SpreadNode
{

	public const float MaxTimePresentFoam = 10f;
	public FoamSourceReservoir FoamSourceReservoir => SourceReservoir as FoamSourceReservoir;

	public override void Update()
	{
		//TODO apply reagents to people and stuff??
		//but lag?
		//but Applying to items
		//not yet

		PresentTimeCount += 1;
		if (PresentTimeCount > MaxTimePresentFoam)
		{
			if (FoamSourceReservoir.WallFoam)
			{
				if (FoamSourceReservoir.SmartFoam)
				{
					if (SourceReservoir.EdgeTiles.Contains(this))
					{
						OnMetaDataNode.PositionMatrix.MetaTileMap.SetTile(OnMetaDataNode.Position,
							SmokeAndFoamManager.Instance.WallFoam);
					}
				}
				else
				{
					OnMetaDataNode.PositionMatrix.MetaTileMap.SetTile(OnMetaDataNode.Position,
						SmokeAndFoamManager.Instance.WallFoam);
				}

				var Tile = OnMetaDataNode.PositionMatrix.MetaTileMap.GetTile(OnMetaDataNode.Position, LayerType.Base);
				if (Tile == null || ((BasicTile) Tile).IsSpace())
				{
					OnMetaDataNode.PositionMatrix.MetaTileMap.SetTile(OnMetaDataNode.Position,
						SmokeAndFoamManager.Instance.BaseFoam);
				}
			}
			OnMetaDataNode.PositionMatrix.MetaTileMap.RemoveOverlaysOfType(OnMetaDataNode.Position, LayerType.Floors,OverlayType.Foam);
			SourceReservoir.RemoveTile(this);
		}
	}


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

			if (node.FoamNode.IsActive == false || node.FoamNode.SourceReservoir != SourceReservoir)
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
			if (SourceReservoir.StacksLeft == 0) break;
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

			if (node.FoamNode.IsActive == false && node.IsOccupied == false)
			{
				SourceReservoir.SpreadToNode(this ,node.FoamNode);
			}
		}
	}

	public override void Clean()
	{
		base.Clean();
		OnMetaDataNode.IsSlippery = false;
	}


	public override void DistributeToTile(SourceReservoir sourceReservoir)
	{
		base.DistributeToTile(sourceReservoir);
		OnMetaDataNode.IsSlippery = true;

		var Colour = Present.MixColor;
		if (Present.MixColor == Color.clear)
		{
			Colour = Color.white;
		}

		OnMetaDataNode.PositionMatrix.MetaTileMap.AddOverlay(OnMetaDataNode.Position, SmokeAndFoamManager.Instance.OverlayTileFoam, Matrix4x4.identity, Colour);

	}
}
public class FoamSourceReservoir : SourceReservoir
{

	public bool SmartFoam = false;
	public bool WallFoam = false;

	public override void RemoveTileInherit()
	{

	}

}
