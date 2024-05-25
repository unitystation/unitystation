using System.Collections;
using System.Collections.Generic;
using TileManagement;
using Tiles;
using UnityEngine;


[CreateAssetMenu(fileName = "Electrical_Cable_Tile", menuName = "Tiles/Electrical Cable Tile", order = 1)]
public class ElectricalCableTile : FuncPlaceRemoveTile
{
	public List<PowerTypeCategory> CanConnectTo = new List<PowerTypeCategory>();

	public PowerTypeCategory PowerType;
	public Connection WireEndB;
	public Connection WireEndA;
	public Sprite sprite;
	public override Sprite PreviewSprite => sprite;

	public override void OnPlaced(Vector3Int TileLocation, Matrix AssociatedMatrix, TileLocation tileLocation)
	{
		AssociatedMatrix.AddElectricalNode(TileLocation, this);
	}

	public override void OnRemoved(Vector3Int TileLocation, Matrix AssociatedMatrix, TileLocation tileLocation)
	{
		var Node = AssociatedMatrix.MetaDataLayer.Get(TileLocation, false);
		if (Node != null)
		{
			foreach (var ElectricalData in Node.ElectricalData)
			{
				if (ElectricalData.RelatedTile != this) continue;
				if (ElectricalData.InData.DestroyQueueing) return;
				ElectricalData.InData.DestroyThisPlease(true);
				Spawn.ServerPrefab(this.SpawnOnDeconstruct, TileLocation.ToWorld(AssociatedMatrix),
					count: this.SpawnAmountOnDeconstruct);
				return;
			}
		}
	}
}
