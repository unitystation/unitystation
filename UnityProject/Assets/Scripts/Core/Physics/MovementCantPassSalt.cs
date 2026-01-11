using System.Collections.Generic;
using System.Linq;
using Objects;
using TileManagement;
using UnityEngine;

namespace Movement
{
	public class MovementCantPassSalt : MonoBehaviour, ICustomTilePassable
	{
		public bool OverridesBehaviour => false;

		public bool IsCustomPassableAtOrthogonalTileV2(Vector3Int origin, Vector3Int to, CollisionType colliderType,
			List<IBumpableObject> Bumps, MetaTileMap Associated)
		{
			var Tiles = Associated.GetOverlayTilesByType(to, LayerType.UnderObjectsEffects, OverlayType.Reagents);
			if (Tiles.Count == 0) return true;
			return Tiles.All(x => x != CommonTiles.Instance.PowderSalt);
		}

	}

}
