using System.Linq;
using TileManagement;
using Tiles;
using UnityEditor;
using UnityEngine;
using UnityEngine.Tilemaps;


[CustomEditor(typeof(LevelBrush))]
	public class LevelBrushEditor : UnityEditor.Tilemaps.GridBrushEditor
	{
		private TileBase _currentPreviewTile;
		private MetaTileMap _currentPreviewTilemap;

		private PreviewTile previewTile; // Preview Wrapper for ObjectTiles

		private LayerTile[] previewTiles;

		public override GameObject[] validTargets
		{
			get
			{
				Grid[] grids = FindObjectsOfType<Grid>();

				return grids?.Select(x => x.gameObject).ToArray();
			}
		}

		public override void RegisterUndo(GameObject layer, GridBrushBase.Tool tool)
		{
			foreach (Tilemap tilemap in layer.GetComponentsInChildren<Tilemap>())
			{
				Undo.RegisterCompleteObjectUndo(tilemap, "Paint");
			}
		}

		public override void PaintPreview(GridLayout gridLayout, GameObject brushTarget, Vector3Int position)
		{
			if (brushTarget == null)
			{
				return;
			}

			MetaTileMap metaTilemap = brushTarget.GetComponent<MetaTileMap>();

			if (!metaTilemap)
			{
				return;
			}

			TileBase tile = brush.cells[0].tile;

			if (tile != _currentPreviewTile)
			{
				if (tile is LayerTile)
				{
					ObjectTile objectTile = tile as ObjectTile;
					if (objectTile && objectTile.Offset)
					{
						brush.cells[0].matrix = Matrix4x4.TRS(Vector3.up, Quaternion.identity, Vector3.one);
					}

					previewTiles = new[] {(LayerTile) tile};
				}
				else if (tile is MetaTile)
				{
					previewTiles = ((MetaTile) tile).GetTiles().ToArray();
				}

				_currentPreviewTile = tile;
			}

			if (previewTiles != null)
			{
				for (int i = 0; i < previewTiles.Length; i++)
				{
					SetPreviewTile(metaTilemap, position, previewTiles[i]);
				}
			}

			_currentPreviewTilemap = metaTilemap;
		}

		public override void ClearPreview()
		{
			if (_currentPreviewTilemap)
			{
				_currentPreviewTilemap.ClearPreview();
			}
		}

		private void SetPreviewTile(MetaTileMap metaTilemap, Vector3Int position, LayerTile tile)
		{
			if (tile is ObjectTile)
			{
				if (previewTile == null || previewTile.ReferenceTile != tile)
				{
					if (previewTile == null)
					{
						previewTile = CreateInstance<PreviewTile>();
					}

					previewTile.ReferenceTile = tile;
					previewTile.LayerType = LayerType.Walls;
				}

				tile = previewTile;
				position.z++; // to draw the object over already existing stuff
			}

			metaTilemap.SetPreviewTile(position, tile, brush.cells[0].matrix);
		}
	}
