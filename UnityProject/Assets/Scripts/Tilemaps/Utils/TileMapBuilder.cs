using TileManagement;
using Tiles;
using UnityEngine;


public class TileMapBuilder
	{
		private readonly bool importMode;
		private readonly MetaTileMap metaTileMap;

		public TileMapBuilder(MetaTileMap tilemap, bool importMode = false)
		{
			metaTileMap = tilemap;
			this.importMode = importMode;
		}

		public void PlaceTile(Vector3Int position, GenericTile tile)
		{
			PlaceTile(position, tile, Matrix4x4.identity);
		}

		public void PlaceTile(Vector3Int position, GenericTile tile, Matrix4x4 matrixTransform)
		{
			if (tile is LayerTile)
			{
				PlaceLayerTile(position, (LayerTile) tile, matrixTransform);
			}
			else if (tile is MetaTile)
			{
				PlaceMetaTile(position, (MetaTile) tile, matrixTransform);
			}
		}

		private void PlaceMetaTile(Vector3Int position, MetaTile metaTile, Matrix4x4 matrixTransform)
		{
			foreach (LayerTile tile in metaTile.GetTiles())
			{
				PlaceLayerTile(position, tile, matrixTransform);
			}
		}

		private void PlaceLayerTile(Vector3Int position, LayerTile tile, Matrix4x4 matrixTransform)
		{
			if (!importMode)
			{
				metaTileMap.RemoveTileWithlayer(position, tile.LayerType);
			}
			SetTile(position, tile, matrixTransform);
		}

		private void SetTile(Vector3Int position, LayerTile tile, Matrix4x4 matrixTransform)
		{
			foreach (LayerTile requiredTile in tile.RequiredTiles)
			{
				if (metaTileMap.HasTile(position, requiredTile.LayerType) == false)
				{
					SetTile(position, requiredTile, matrixTransform);
				}
			}

			metaTileMap.SetTile(position, tile, matrixTransform);
		}
	}
