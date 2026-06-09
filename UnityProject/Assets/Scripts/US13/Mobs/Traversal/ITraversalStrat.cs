using System;
using UnityEngine;
using US13.Player;
using US13.Tilemaps.Tiles;

namespace US13.Mobs.Traversal
{
	public interface ITraversalStrat
	{
		/// <summary>
		/// Check if we can use this strategy.
		/// </summary>
		/// <param name="obstaclePosition">The local cords of the obstical. (Avoid using World Positions)</param>
		/// <param name="mob">The mob that's traversing.</param>
		/// <returns>Item1 is if we can use this strategy, Item2 is if the obstical an object, Item3 is if the obstical a tile (like a wall).</returns>
		public Tuple<bool, Component, LayerTile> ObstacleCheck(Vector3Int obstaclePosition, PlayerScript mob);

		/// <summary>
		/// Run this strategy to try and overcome an obstical in our way while traversing.
		/// </summary>
		/// <param name="direction"></param>
		/// <param name="obstacleObject"></param>
		/// <param name="obstacleTile"></param>
		/// <param name="mob"></param>
		/// <returns></returns>
		public int TraverseObstacle(Vector3Int direction, Component obstacleObject, LayerTile obstacleTile, PlayerScript mob);
	}
}