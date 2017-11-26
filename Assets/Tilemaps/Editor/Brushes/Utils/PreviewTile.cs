using Tilemaps.Scripts.Tiles;
using UnityEngine;
using UnityEngine.Tilemaps;

namespace Tilemaps.Editor.Brushes.Utils
{
    public class PreviewTile : LayerTile
    {
        public LayerTile ReferenceTile;
        
        public override void GetTileData(Vector3Int position, ITilemap tilemap, ref TileData tileData)
        {
            tileData.sprite = ReferenceTile.PreviewSprite;
        }
    }
}