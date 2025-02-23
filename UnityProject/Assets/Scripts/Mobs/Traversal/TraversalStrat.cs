using System;
using Tiles;
using UnityEngine;

namespace Mobs.Traversal
{
	public interface TraversalStrat
	{
		public Tuple<bool, Component, LayerTile> ObsticalCheck(Vector3Int obsticalPosition, PlayerScript mob);
		public void TraverseObstical(Vector3Int direction, Component obsticalObject, LayerTile obsticalTile, PlayerScript mob);
	}
}