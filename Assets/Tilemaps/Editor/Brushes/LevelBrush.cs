﻿using System;
using Tilemaps.Scripts;
using Tilemaps.Scripts.Behaviours;
using Tilemaps.Scripts.Behaviours.Layers;
using Tilemaps.Scripts.Tiles;
using UnityEditor;
using UnityEngine;
using UnityEngine.Tilemaps;

namespace Tilemaps.Editor.Brushes
{
    [CustomGridBrush(false, true, true, "Level Brush")]
    public class LevelBrush : GridBrush
    {
        public override void Paint(GridLayout grid, GameObject layer, Vector3Int position)
        {
            BoxFill(grid, layer, new BoundsInt(position, Vector3Int.one));
        }

        public override void Erase(GridLayout grid, GameObject layer, Vector3Int position)
        {
            BoxErase(grid, layer, new BoundsInt(position, Vector3Int.one));
        }

        public override void BoxFill(GridLayout grid, GameObject layer, BoundsInt area)
        {
            if (cellCount > 0 && cells[0].tile != null)
            {
                var tile = cells[0].tile;

                var metaTileMap = grid.GetComponent<MetaTileMap>();

                if (!metaTileMap)
                    return;

                foreach (var position in area.allPositionsWithin)
                {
                    if (tile is LayerTile)
                    {
                        PlaceLayerTile(metaTileMap, position, (LayerTile) tile);
                    }
                    else if (tile is MetaTile)
                    {
                        PlaceMetaTile(metaTileMap, position, (MetaTile) tile);
                    }
                }
            }
        }

        public override void BoxErase(GridLayout grid, GameObject layer, BoundsInt area)
        {
            var metaTileMap = grid.GetComponent<MetaTileMap>();

            foreach (var position in area.allPositionsWithin)
            {
                if (metaTileMap)
                {
                    metaTileMap.RemoveTile(position, LayerType.None);
                }
                else
                {
                    layer.GetComponent<Tilemap>().SetTile(position, null);
                }
            }
        }

        public override void Flip(FlipAxis flip, GridLayout.CellLayout layout)
        {
            if (Event.current.character == '>')
            {
                // TODO flip?
            }
        }
        
        public override void Rotate(RotationDirection direction, GridLayout.CellLayout layout)
        {
            if (Event.current.character == '<')
            {
                var tile = cells[0].tile as LayerTile;

                if (tile != null) 
                    cells[0].matrix = tile.Rotate(cells[0].matrix, direction == RotationDirection.Clockwise);
            }
        }

        private void PlaceMetaTile(MetaTileMap metaTileMap, Vector3Int position, MetaTile metaTile)
        {
            foreach (var tile in metaTile.GetTiles())
            {
                PlaceLayerTile(metaTileMap, position, tile);
            }
        }

        private void PlaceLayerTile(MetaTileMap metaTileMap, Vector3Int position, LayerTile tile)
        {
            metaTileMap.RemoveTile(position, tile.LayerType);
            SetTile(metaTileMap, position, tile);
        }

        private void SetTile(MetaTileMap metaTileMap, Vector3Int position, LayerTile tile)
        {
            foreach (var requiredTile in tile.RequiredTiles)
            {
                SetTile(metaTileMap, position, requiredTile);
            }

            metaTileMap.SetTile(position, tile, cells[0].matrix);
        }
    }
}