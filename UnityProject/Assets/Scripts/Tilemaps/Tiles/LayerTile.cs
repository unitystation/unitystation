using UnityEngine;

namespace Tilemaps.Scripts.Tiles
{
	public enum LayerType
	{
		Walls,
		Windows,
		Objects,
		Floors,
		Base,
		None
	}

	public enum TileType
	{
		None,
		Wall,
		Window,
		Floor,
		Table
	}

	public class LayerTile : GenericTile
	{
		private static LayerTile _emptyTile;

		public LayerType LayerType;

		public LayerTile[] RequiredTiles;
		public TileType TileType;
		public static LayerTile EmptyTile => _emptyTile ?? (_emptyTile = CreateInstance<LayerTile>());

		public virtual Matrix4x4 Rotate(Matrix4x4 transformMatrix, bool anticlockwise = true, int count = 1)
		{
			return transformMatrix;
		}
	}
}