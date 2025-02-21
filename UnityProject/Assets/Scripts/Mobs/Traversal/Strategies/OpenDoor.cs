using System;
using Doors;
using UnityEngine;

namespace Mobs.Traversal.Strategies
{
	public class OpenDoor : TraversalStrat
	{
		public Tuple<bool, Component, MetaTile> ObsticalCheck(Vector3Int obsticalPosition, PlayerScript mob)
		{
			var door = mob.RegisterPlayer.Matrix.GetFirst<DoorMasterController>(obsticalPosition, true);
			return new Tuple<bool, Component, MetaTile>(door != null, door, null);
		}

		public void TraverseObstical(Vector3Int direction, Component obsticalObject, MetaTile obsticalTile, PlayerScript mob)
		{
			var door = obsticalObject as DoorMasterController;
			door?.TryOpen(mob.gameObject);
		}
	}
}