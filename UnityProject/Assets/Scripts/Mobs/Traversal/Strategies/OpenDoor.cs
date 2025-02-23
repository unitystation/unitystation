using System;
using Doors;
using Tiles;
using UnityEngine;

namespace Mobs.Traversal.Strategies
{
	public class OpenDoor : TraversalStrat
	{
		public Tuple<bool, Component, LayerTile> ObsticalCheck(Vector3Int obsticalPosition, PlayerScript mob)
		{
			var door = mob.RegisterPlayer.Matrix.GetFirst<DoorMasterController>(obsticalPosition, true);
			return new Tuple<bool, Component, LayerTile>(door != null, door, null);
		}

		public void TraverseObstical(Vector3Int direction, Component obsticalObject, LayerTile obsticalTile, PlayerScript mob)
		{
			var door = obsticalObject as DoorMasterController;
			door?.TryOpen(mob.gameObject);
		}
	}
}