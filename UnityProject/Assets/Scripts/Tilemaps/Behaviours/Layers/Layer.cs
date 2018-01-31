using Tilemaps.Tiles;
using Tilemaps.Utils;
using UnityEngine;
using UnityEngine.Tilemaps;

namespace Tilemaps.Behaviours.Layers
{
	[ExecuteInEditMode]
	public class Layer : MonoBehaviour
	{
		public LayerType LayerType;
		protected Tilemap tilemap;

		public BoundsInt Bounds => tilemap.cellBounds;

		public void Awake()
		{
			tilemap = GetComponent<Tilemap>();
		}

		public virtual bool IsPassableAt(Vector3Int from, Vector3Int to)
		{
			if (from == to)
			{
				return true;
			}
			
			BasicTile tileTo = tilemap.GetTile<BasicTile>(to);
			
			return TileUtils.IsPassable(tileTo);
		}

		public virtual bool IsPassableAt(Vector3Int position)
		{
			BasicTile tile = tilemap.GetTile<BasicTile>(position);
			return TileUtils.IsPassable(tile);
		}

		public virtual bool IsAtmosPassableAt(Vector3Int position)
		{
			BasicTile tile = tilemap.GetTile<BasicTile>(position);
			return  TileUtils.IsAtmosPassable(tile);
		}

		public virtual bool IsSpaceAt(Vector3Int position)
		{
			BasicTile tile = tilemap.GetTile<BasicTile>(position);
			return TileUtils.IsSpace(tile);
		}

		public virtual void SetTile(Vector3Int position, GenericTile tile, Matrix4x4 transformMatrix)
		{
			tilemap.SetTile(position, tile);
			tilemap.SetTransformMatrix(position, transformMatrix);
		}

		public virtual LayerTile GetTile(Vector3Int position)
		{
			return tilemap.GetTile<LayerTile>(position);
		}

		public virtual bool HasTile(Vector3Int position)
		{
			return GetTile(position);
		}

		public virtual void RemoveTile(Vector3Int position)
		{
			tilemap.SetTile(position, null);
		}

		public virtual void ClearAllTiles()
		{
			tilemap.ClearAllTiles();
		}

#if UNITY_EDITOR
		public void SetPreviewTile(Vector3Int position, LayerTile tile, Matrix4x4 transformMatrix)
		{
			tilemap.SetEditorPreviewTile(position, tile);
			tilemap.SetEditorPreviewTransformMatrix(position, transformMatrix);
		}

		public void ClearPreview()
		{
			tilemap.ClearAllEditorPreviewTiles();
		}
#endif
	}
}