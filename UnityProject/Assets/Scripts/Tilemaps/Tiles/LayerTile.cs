using System;
using UnityEngine;

namespace Tiles
{
	[Serializable]
	public class LayerTile : GenericTile
	{
		[SerializeField]
		[Tooltip("Name to dispay to the player for this tile.")]
		private string displayName = null;

		[SerializeField]
		[Tooltip("Text seen by the player when examining the tile.")]
		private string description = default;

		/// <summary>
		/// Name to display to the player for this tile. Defaults to the tile type.
		/// </summary>
		public string DisplayName => string.IsNullOrWhiteSpace(displayName) ? TileType.ToString().ToLower() : displayName;

		/// <summary>
		/// Text seen by the player when examining the tile.
		/// </summary>
		public string Description => description;

		private static LayerTile emptyTile;

		public static LayerTile EmptyTile => emptyTile ? emptyTile : (emptyTile = ScriptableObject.CreateInstance<LayerTile>());

		public LayerType LayerType;
		public TileType TileType;

		public LayerTile[] RequiredTiles = { };

		public virtual Matrix4x4 Rotate(Matrix4x4 transformMatrix, bool anticlockwise = true, int count = 1)
		{
			return transformMatrix;
		}
	}
}
