using Tilemaps.Scripts.Tiles;
using UnityEngine;
using UnityEngine.Tilemaps;

namespace Tilemaps.Editor.Brushes.Utils
{
	public class PreviewTile : LayerTile
	{
		public GenericTile ReferenceTile;

		public override void GetTileData(Vector3Int position, ITilemap tilemap, ref TileData tileData)
		{
			tileData.sprite = ReferenceTile.PreviewSprite;
			tileData.colliderType = Tile.ColliderType.None;
			tileData.flags = TileFlags.None;
		}
	}
}