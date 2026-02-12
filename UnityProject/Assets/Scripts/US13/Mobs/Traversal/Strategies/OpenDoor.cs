using System;
using UnityEngine;
using US13.Objects.Doors;
using US13.Player;
using US13.Tilemaps.Tiles;

namespace US13.Mobs.Traversal.Strategies
{
	/// <summary>
	/// Traversal strategy for opening doors.
	/// Does not force open doors, and will check mob's clearance.
	/// </summary>
	public class OpenDoor : ITraversalStrat
	{
		public Tuple<bool, Component, LayerTile> ObsticalCheck(Vector3Int obsticalPosition, PlayerScript mob)
		{
			var door = mob.RegisterPlayer.Matrix.GetFirst<DoorMasterController>(obsticalPosition, true);
			return new Tuple<bool, Component, LayerTile>(door != null, door, null);
		}

		public int TraverseObstical(Vector3Int direction, Component obsticalObject, LayerTile obsticalTile, PlayerScript mob)
		{
			var door = obsticalObject as DoorMasterController;
			door?.PulseTryOpen(mob.gameObject);
			return 0;
		}
	}
}