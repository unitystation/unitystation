using UnityEngine;

namespace Tilemaps.Scripts.Tiles
{
    public class SimpleTile : BasicTile
    {
        public Sprite sprite;

        public override Sprite PreviewSprite => sprite;
    }
}