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
            Vector3Int pos = tilemap.WorldToCell(position) + new Vector3Int(1, 1);
            return tilemap.GetTile(pos);
        }

        public static bool IsInBounds(this MetaTileMap tilemap, Vector3Int position)
        {
            return tilemap.GetLocalBounds().Contains(position);
        }

        public static bool IsInBounds(this MetaTileMap tilemap, Vector3 position)
        {
            Vector3Int pos = tilemap.WorldToCell(position) + new Vector3Int(1, 1);
            return tilemap.GetLocalBounds().Contains(pos);
        }

        /// <summary>
        /// Checks if the cell is in bounds and is not set with a tile.
        /// </summary>
        public static bool IsCellEmpty(this MetaTileMap tilemap, Vector3Int position)
        {
            return tilemap.IsInBounds(position) && IsBlockedTile(tilemap, position);
        }

        public static bool IsBlockedTile(this MetaTileMap tilemap, Vector3Int position)
        {
	        if (tilemap.GetTile(position) == null) return false;
	        return tilemap.GetTile(position).LayerType is LayerType.Walls or LayerType.Windows or LayerType.Tables;
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
            Vector3 worldPos = tilemap.WorldToCell(position) + new Vector3Int(1, 1);
            return Utils.GetComponentAtPosition2D<T>(worldPos);
        }
    }
}
