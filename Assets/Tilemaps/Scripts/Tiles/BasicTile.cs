﻿using UnityEngine;
using UnityEngine.Tilemaps;

namespace Tilemaps.Scripts.Tiles
{
    public abstract class BasicTile : LayerTile
    {
        public bool Passable;
        public bool AtmosPassable;
        public bool IsSealed;
        
        public override void RefreshTile(Vector3Int position, ITilemap tilemap)
        {
            foreach (var p in new BoundsInt(-1, -1, 0, 3, 3, 1).allPositionsWithin)
            {
                tilemap.RefreshTile(position + p);
            }
        }
        
        public bool IsPassable()
        {
            return Passable;
        }

        public bool IsAtmosPassable()
        {
            return AtmosPassable;
        }

        public bool IsSpace()
        {
            return IsAtmosPassable() && !IsSealed;
        }
    }
}