using System;
using Tiles;
using UnityEngine;

namespace Mobs.Traversal
{
	public interface ITraversalStrat
	{
		/// <summary>
		/// Check if we can use this strategy.
		/// </summary>
		/// <param name="obsticalPosition">The local cords of the obstical. (Avoid using World Positions)</param>
		/// <param name="mob">The mob that's traversing.</param>
		/// <returns>Item1 is if we can use this strategy, Item2 is if the obstical an object, Item3 is if the obstical a tile (like a wall).</returns>
		public Tuple<bool, Component, LayerTile> ObsticalCheck(Vector3Int obsticalPosition, PlayerScript mob);

		/// <summary>
		/// Run this strategy to try and overcome an obstical in our way while traversing.
		/// </summary>
		/// <param name="direction"></param>
		/// <param name="obsticalObject"></param>
		/// <param name="obsticalTile"></param>
		/// <param name="mob"></param>
		/// <returns></returns>
		public int TraverseObstical(Vector3Int direction, Component obsticalObject, LayerTile obsticalTile, PlayerScript mob);
	}
}