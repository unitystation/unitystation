using System.Collections.Generic;
using UnityEngine;
using US13.Core.Lifecycle;
using US13.Tilemaps.Behaviours.Layers;
using US13.Tilemaps.Tiles;
using Util;

namespace US13.Systems.Electricity.FunctionsAndClasses
{
	[CreateAssetMenu(fileName = "Electrical_Cable_Tile", menuName = "Tiles/Electrical Cable Tile", order = 1)]
	public class ElectricalCableTile : FuncPlaceRemoveTile
	{
		public List<PowerTypeCategory> CanConnectTo = new List<PowerTypeCategory>();

		public PowerTypeCategory PowerType;
		public Connection WireEndB;
		public Connection WireEndA;


		public override void OnPlaced(Vector3Int TileLocation, Matrix AssociatedMatrix, TileLocation tileLocation)
		{
			AssociatedMatrix.AddElectricalNode(TileLocation, this, false, true);
		}

		public override void OnRemoved(Vector3Int TileLocation, Matrix AssociatedMatrix, TileLocation tileLocation, bool SpawnItems)
		{
			var Node = AssociatedMatrix.MetaDataLayer.Get(TileLocation, false);
			if (Node != null)
			{
				foreach (var ElectricalData in Node.ElectricalData)
				{
					if (ElectricalData.RelatedTile != this) continue;
					if (ElectricalData.InData.DropIngredients == false) return;
					if (ElectricalData.InData.DestroyQueueing) return;
					ElectricalData.InData.DestroyThisPlease(true);
					if (SpawnItems)
					{
						Spawn.ServerPrefab(this.SpawnOnDeconstruct, TileLocation.ToWorld(AssociatedMatrix),
							count: this.SpawnAmountOnDeconstruct);
					}
					return;
				}
			}
		}
	}
}
