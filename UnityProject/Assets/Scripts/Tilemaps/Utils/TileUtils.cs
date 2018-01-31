using System.Linq;
using Light2D;
using Tilemaps.Tiles;
using UnityEngine;

namespace Tilemaps.Utils
{
	public static class TileUtils
	{
		public static bool IsPassable(params BasicTile[] tile)
		{
			return tile.All(t => !t || t.IsPassable());
		}
		
		public static bool IsAtmosPassable(params BasicTile[] tile)
		{
			return tile.All(t => !t || t.IsAtmosPassable());
		}
		
		public static bool IsSpace(params BasicTile[] tile)
		{
			return tile.All(t => !t || t.IsSpace());
		}
		
	}
}