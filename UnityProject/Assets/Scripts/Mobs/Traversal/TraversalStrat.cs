using System;
using UnityEngine;

namespace Mobs.Traversal
{
	public interface TraversalStrat
	{
		public Tuple<bool, Component, MetaTile> ObsticalCheck(Vector3Int obsticalPosition, PlayerScript mob);
		public void TraverseObstical(Vector3Int direction, Component obsticalObject, MetaTile obsticalTile, PlayerScript mob);
	}
}