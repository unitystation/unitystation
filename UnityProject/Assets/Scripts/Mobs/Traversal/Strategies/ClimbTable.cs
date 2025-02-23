using System;
using Tiles;
using UnityEngine;

namespace Mobs.Traversal.Strategies
{
	public class ClimbTable : TraversalStrat
	{
		public Tuple<bool, Component, LayerTile> ObsticalCheck(Vector3Int obsticalPosition, PlayerScript mob)
		{
			var table = mob.RegisterPlayer.Matrix.IsTableAt(obsticalPosition);
			return new Tuple<bool, Component, LayerTile>(table != null, null, table);
		}

		public int TraverseObstical(Vector3Int direction, Component obsticalObject, LayerTile obsticalTile, PlayerScript mob)
		{
			if (obsticalTile is BasicTile table)
			{
				foreach (var interaction in table.TileInteractions)
				{
					if (interaction is TableInteractionClimb climbInteraction)
					{
						climbInteraction.StartClimbing(false, mob, direction, direction, table,
							mob.ObjectPhysics, mob.RegisterPlayer.Matrix.TileChangeManager);
						return 3135;
					}
				}
			}
			return 0;
		}
	}
}