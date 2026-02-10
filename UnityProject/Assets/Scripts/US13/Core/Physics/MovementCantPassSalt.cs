using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using US13.Managers.MatrixManager;
using US13.Objects;
using US13.ScriptableObjects;
using US13.Tilemaps.Behaviours.Layers;
using US13.Tilemaps.Utils;

namespace US13.Core.Physics
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
