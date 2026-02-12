using UnityEngine;
using US13.Tilemaps.Behaviours.Layers;

namespace US13.Tilemaps.Tiles
{
	public class FuncPlaceRemoveTile : BasicTile
	{
		public virtual void OnPlaced(Vector3Int TileLocation, Matrix AssociatedMatrix, TileLocation tileLocation)
		{

		}

		public virtual void OnRemoved(Vector3Int TileLocation, Matrix AssociatedMatrix, TileLocation tileLocation, bool DropItems)
		{

		}
	}
}
