using System;
using UnityEngine;
using UnityEngine.Tilemaps;

namespace Tilemaps.Scripts.Tiles
{
    public enum ConnectCategory
    {
        Walls,
        Windows,
        Tables,
        Floors,
        None
    }

    public enum ConnectType
    {
        ToAll,
        ToSameCategory,
        ToSelf
    }

    public class ConnectedTile : BasicTile
    {

        public Texture2D spriteSheet;
        public string texturePath;

        public ConnectCategory connectCategory = ConnectCategory.None;
        public ConnectType connectType = ConnectType.ToAll;

        public override Sprite PreviewSprite => sprites[0];

        private static int[] map =
        {
            0, 2, 4, 8, 1, 255,
            3, 6, 12, 9, 10, 5,
            7, 14, 13, 11, 15, 19,
            38, 76, 137, 23, 39, 46,
            78, 77, 141, 139, 27, 31,
            47, 79, 143, 63, 111, 207,
            159, 191, 127, 239, 223, 55,
            110, 205, 155, 175, 95
        };

        private Sprite[] _sprites;

        private Sprite[] sprites
        {
            get
            {
                if (_sprites == null || _sprites.Length == 0)
                {
                    _sprites = Resources.LoadAll<Sprite>(texturePath + "/" + spriteSheet.name);
                }
                return _sprites;
            }
        }

        public override void GetTileData(Vector3Int position, ITilemap tilemap, ref TileData tileData)
        {
            if (tilemap.GetComponent<Tilemap>().name == "Layer1")
            {
                // don't connect while in palette
                base.GetTileData(position, tilemap, ref tileData);
                return;
            }

            int mask = (HasSameTile(position + Vector3Int.up, tilemap) ? 1 : 0)
                       + (HasSameTile(position + Vector3Int.right, tilemap) ? 2 : 0)
                       + (HasSameTile(position + Vector3Int.down, tilemap) ? 4 : 0)
                       + (HasSameTile(position + Vector3Int.left, tilemap) ? 8 : 0);

            if ((mask & 3) == 3)
            {
                mask += (HasSameTile(position + Vector3Int.right + Vector3Int.up, tilemap) ? 16 : 0);
            }
            if ((mask & 6) == 6)
            {
                mask += (HasSameTile(position + Vector3Int.right + Vector3Int.down, tilemap) ? 32 : 0);
            }
            if ((mask & 12) == 12)
            {
                mask += (HasSameTile(position + Vector3Int.left + Vector3Int.down, tilemap) ? 64 : 0);
            }
            if ((mask & 9) == 9)
            {
                mask += (HasSameTile(position + Vector3Int.left + Vector3Int.up, tilemap) ? 128 : 0);
            }

            int i = Array.IndexOf(map, mask);

            if (i >= 0)
            {
                tileData.sprite = sprites[i];
                tileData.flags = TileFlags.LockAll;
				// create collider for tiles, None, Sprite or Grid
				tileData.colliderType = Tile.ColliderType.Grid;
            }
        }

        private bool HasSameTile(Vector3Int position, ITilemap tilemap)
        {
            var tile = tilemap.GetTile(position);

            if (tile == null)
            {
                return false;
            }

            switch (connectType)
            {
                case ConnectType.ToAll:
                    return true;
                case ConnectType.ToSameCategory:
                    var t = tile as ConnectedTile;
                    return t != null && t.connectCategory == connectCategory;
                case ConnectType.ToSelf:
                    return tile == this;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }
    }
}