using UnityEngine;


public class LayerTile : GenericTile
{
	private static LayerTile emptyTile;

	public static LayerTile EmptyTile => emptyTile ? emptyTile : (emptyTile = CreateInstance<LayerTile>());

	public LayerType LayerType;
	public TileType TileType;

	public LayerTile[] RequiredTiles;

	public virtual Matrix4x4 Rotate(Matrix4x4 transformMatrix, bool anticlockwise = true, int count = 1)
	{
		return transformMatrix;
	}
}