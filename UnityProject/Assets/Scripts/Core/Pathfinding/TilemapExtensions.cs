using System.Collections.Generic;
using TileManagement;
using UnityEngine;
using UnityEngine.Tilemaps;

namespace Core.Pathfinding
{
    public static class TilemapExtensions
    {
        public static TileBase GetTile(this MetaTileMap tilemap, Vector3 position)
        {
            Vector3Int pos = tilemap.WorldToCell(position);
            return tilemap.GetTile(pos);
        }

        public static void CopyBounds(this MetaTileMap tilemap, Tilemap other)
        {
            tilemap.origin = other.origin;
            tilemap.size = other.size;
        }

        public static bool IsInBounds(this MetaTileMap tilemap, Vector3Int position)
        {
            return tilemap.cellBounds.Contains(position);
        }

        public static bool IsInBounds(this MetaTileMap tilemap, Vector3 position)
        {
            Vector3Int pos = tilemap.WorldToCell(position);
            return tilemap.cellBounds.Contains(pos);
        }

        /// <summary>
        /// Checks if the cell is in bounds and is not set with a tile.
        /// </summary>
        public static bool IsCellEmpty(this MetaTileMap tilemap, Vector3Int position)
        {
            return tilemap.IsInBounds(position) && tilemap.GetTile(position) == null;
        }

        /// <summary>
        /// Checks if the cell is in bounds and is not set with a tile.
        /// </summary>
        public static bool IsCellEmpty(this MetaTileMap tilemap, Vector3 position)
        {
            Vector3Int pos = tilemap.WorldToCell(position);
            return tilemap.IsInBounds(pos) && tilemap.GetTile(pos) == null;
        }

        public static Vector3Int MousePositionToCell(this MetaTileMap tilemap)
        {
            return tilemap.WorldToCell(UnityEngine.Camera.main.ScreenToWorldPoint(Input.mousePosition));
        }

        public static T GetComponentAtCell<T>(this MetaTileMap tilemap, Vector3Int position)
        {
            Vector3 worldPos = tilemap.GetCellCenterWorld(position);
            return Utils.GetComponentAtPosition2D<T>(worldPos);
        }

        public static void DebugDraw(this MetaTileMap tilemap, float size, Color color = default(Color), float duration = 0.0f, bool depthTest = true)
        {
            BoundsInt bounds = tilemap.cellBounds;
            TileBase[] allTiles = tilemap.GetTilesBlock(bounds);

            for (int x = 0; x < bounds.size.x; x++)
            {
                for (int y = 0; y < bounds.size.y; y++)
                {
                    TileBase tile = allTiles[x + y * bounds.size.x];

                    if (tile != null)
                    {
                        Vector3Int cell = new Vector3Int(bounds.x + x, bounds.y + y, bounds.z);
                        Vector3 pos = tilemap.GetCellCenterWorld(cell);

                        Utils.DebugDrawCross(pos, size, color, duration, depthTest);
                    }
                }
            }
        }
    }
}
