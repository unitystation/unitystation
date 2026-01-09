using System.Collections.Generic;
using Objects;
using TileManagement;
using UnityEngine;

public interface ICustomTilePassable
{
	public bool OverridesBehaviour { get; }
	public bool IsCustomPassableAtOrthogonalTileV2(Vector3Int origin, Vector3Int to, CollisionType colliderType, List<IBumpableObject> Bumps, MetaTileMap Associated);
}
