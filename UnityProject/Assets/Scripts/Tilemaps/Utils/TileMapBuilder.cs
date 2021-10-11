using TileManagement;
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
			PlaceTile(position, tile, OrientationEnum.Default);
		}

		public void PlaceTile(Vector3Int position, GenericTile tile, OrientationEnum orientation)
		{
			if (tile is LayerTile)
			{
				PlaceLayerTile(position, (LayerTile) tile, orientation);
			}
			else if (tile is MetaTile)
			{
				PlaceMetaTile(position, (MetaTile) tile, orientation);
			}
		}

		private void PlaceMetaTile(Vector3Int position, MetaTile metaTile, OrientationEnum orientation)
		{
			foreach (LayerTile tile in metaTile.GetTiles())
			{
				PlaceLayerTile(position, tile, orientation);
			}
		}

		private void PlaceLayerTile(Vector3Int position, LayerTile tile, OrientationEnum orientation)
		{
			if (!importMode)
			{
				metaTileMap.RemoveTileWithlayer(position, tile.LayerType);
			}
			SetTile(position, tile, orientation);
		}

		private void SetTile(Vector3Int position, LayerTile tile, OrientationEnum orientation)
		{
			foreach (LayerTile requiredTile in tile.RequiredTiles)
			{
				SetTile(position, requiredTile, orientation);
			}

			metaTileMap.SetTile(position, tile, orientation);
		}
	}
