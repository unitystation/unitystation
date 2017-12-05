using Tilemaps.Scripts.Behaviours.Layers;
using Tilemaps.Scripts.Tiles;
using UnityEngine;

namespace Tilemaps.Scripts.Utils
{
    public class TileMapBuilder
    {
        private MetaTileMap _metaTileMap;
        private bool _importMode;

        public TileMapBuilder(MetaTileMap tilemap, bool importMode = false)
        {
            _metaTileMap = tilemap;
            _importMode = importMode;
        }

        public void PlaceTile(Vector3Int position, GenericTile tile)
        {
            PlaceTile(position, tile, Matrix4x4.identity);
        }

        public void PlaceTile(Vector3Int position, GenericTile tile, Matrix4x4 matrixTransform)
        {
            if (tile is LayerTile)
            {
                PlaceLayerTile(position, (LayerTile)tile, matrixTransform);
            }
            else if (tile is MetaTile)
            {
                PlaceMetaTile(position, (MetaTile)tile, matrixTransform);
            }
        }

        private void PlaceMetaTile(Vector3Int position, MetaTile metaTile, Matrix4x4 matrixTransform)
        {
            foreach (var tile in metaTile.GetTiles())
            {
                PlaceLayerTile(position, tile, matrixTransform);
            }
        }

        private void PlaceLayerTile(Vector3Int position, LayerTile tile, Matrix4x4 matrixTransform)
        {
            if (!_importMode)
            {
                _metaTileMap.RemoveTile(position, tile.LayerType);
            }
            SetTile(position, tile, matrixTransform);
        }

        private void SetTile(Vector3Int position, LayerTile tile, Matrix4x4 matrixTransform)
        {
            foreach (var requiredTile in tile.RequiredTiles)
            {
                SetTile(position, requiredTile, matrixTransform);
            }

            _metaTileMap.SetTile(position, tile, matrixTransform);
        }
    }
}