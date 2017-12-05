using UnityEngine;
using UnityEngine.Tilemaps;

namespace Tilemaps.Scripts.Tiles
{
    public abstract class GenericTile : TileBase
    {
        public virtual Sprite PreviewSprite { get; protected set; }

        public override void GetTileData(Vector3Int position, ITilemap tilemap, ref TileData tileData)
        {
            tileData.sprite = PreviewSprite;
            tileData.flags = TileFlags.None;
            tileData.colliderType = Tile.ColliderType.None;
        }
    }
}