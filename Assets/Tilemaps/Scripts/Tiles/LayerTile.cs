﻿using System.Collections.Generic;
using Tilemaps.Scripts.Behaviours.Layers;
using UnityEngine;
using UnityEngine.Tilemaps;

namespace Tilemaps.Scripts.Tiles
{
    public enum LayerType
    {
        Structures,
        Objects,
        Floors,
        Base,
        None
    }

    public class LayerTile : GenericTile
    {
        private static LayerTile _emptyTile;
        public static LayerTile EmptyTile => _emptyTile ?? (_emptyTile = CreateInstance<LayerTile>());
        
        public LayerType LayerType;
        
        public LayerTile[] RequiredTiles;
        

        public virtual Matrix4x4 Rotate(Matrix4x4 transformMatrix, bool clockwise)
        {
            return transformMatrix;
        }
    }
}