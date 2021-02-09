using System;
using UnityEngine;

[Serializable]
public class LayerTile : GenericTile
{
	[Tooltip("Name to dispay to the player for this tile.")]
	[SerializeField]
	private string displayName = null;

	/// <summary>
	/// Name to display to the player for this tile. Defaults to the tile type.
	/// </summary>
	public string DisplayName => String.IsNullOrWhiteSpace(displayName) ? TileType.ToString().ToLower() : displayName;

	private static LayerTile emptyTile;

	public static LayerTile EmptyTile => emptyTile ? emptyTile : (emptyTile = CreateInstance<LayerTile>());

	public LayerType LayerType;
	public TileType TileType;

	public LayerTile[] RequiredTiles = {};

	public virtual Matrix4x4 Rotate(Matrix4x4 transformMatrix, bool anticlockwise = true, int count = 1)
	{

		return transformMatrix;
	}
}
