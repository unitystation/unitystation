﻿using System.Collections.Generic;
using Tilemaps.Scripts.Utils;
using UnityEditor;

namespace Tilemaps.Scripts.Tiles
{
    public class MetaTile : GenericTile
    {
        public LayerTile Structure;
        public LayerTile Object;
        public LayerTile Floor;
        public LayerTile Base;

        private LayerTile _structureCurrent;
        private LayerTile _objectCurrent;
        private LayerTile _floorCurrent;
        private LayerTile _baseCurrent;

        private void OnValidate()
        {
            CheckTileType(ref Structure, LayerType.Structures);
            CheckTileType(ref Object, LayerType.Objects);
            CheckTileType(ref Floor, LayerType.Floors);
            CheckTileType(ref Base, LayerType.Base);
            
            if (Structure != _structureCurrent || Object != _objectCurrent || Floor != _floorCurrent || Base != _baseCurrent )
            {
                if (_structureCurrent == null && _objectCurrent == null && _floorCurrent == null && _baseCurrent == null)
                {
                    // if everything is null, it could be that it's loading on startup, so there already should be an preview sprite to load
                    EditorApplication.delayCall+=()=>
                    {
                        PreviewSprite = PreviewSpriteBuilder.LoadSprite(this) ?? PreviewSpriteBuilder.Create(this);;
                    };
                }
                else
                {
                    // something changed, so create a new preview sprite
                    EditorApplication.delayCall+=()=>
                    {
                        PreviewSprite = PreviewSpriteBuilder.Create(this);
                    };
                }
            }

            _structureCurrent = Structure;
            _objectCurrent = Object;
            _floorCurrent = Floor;
            _baseCurrent = Base;
        }

        private static void CheckTileType(ref LayerTile tile, LayerType requiredType)
        {
            if (tile != null && tile.LayerType != requiredType)
            {
                tile = null;
            }
        }

        public IEnumerable<LayerTile> GetTiles()
        {
            var list = new List<LayerTile>();

            if(Base) list.Add(Base);
            if(Floor) list.Add(Floor);
            if(Object) list.Add(Object);
            if(Structure) list.Add(Structure);

            return list.ToArray();
        }
    }
}