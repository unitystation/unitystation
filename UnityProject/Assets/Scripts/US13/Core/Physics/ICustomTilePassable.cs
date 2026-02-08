using System.Collections.Generic;
using UnityEngine;
using US13.Managers.MatrixManager;
using US13.Objects;
using US13.Tilemaps.Behaviours.Layers;

namespace US13.Core.Physics
{
	public interface ICustomTilePassable
	{
		public bool OverridesBehaviour { get; }
		public bool IsCustomPassableAtOrthogonalTileV2(Vector3Int origin, Vector3Int to, CollisionType colliderType, List<IBumpableObject> Bumps, MetaTileMap Associated);
	}
}
