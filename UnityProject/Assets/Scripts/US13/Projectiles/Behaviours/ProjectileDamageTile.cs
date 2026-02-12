using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using US13.Managers.MatrixManager;
using US13.ScriptableObjects.Gun;
using US13.Tilemaps.Behaviours;
using US13.Tilemaps.Behaviours.Layers;
using US13.Tilemaps.Utils;

namespace US13.Projectiles.Behaviours
{
	/// <summary>
	/// Script to damage specific tile projectile collided with
	/// </summary>
	public class ProjectileDamageTile : MonoBehaviour, IOnHitInteractTile
	{
		[SerializeField] private DamageData damageData = null;

		[Tooltip("Tile layers to damage(Walls, Window, etc.)")]
		[SerializeField] private LayerType[] layersToHit = null;

		public bool Interact(MatrixManager.CustomPhysicsHit hit, InteractableTiles interactableTiles, Vector3 worldPosition)
		{
			var layerToHit = GetLayerToHitOrGetNull(interactableTiles.MetaTileMap.DamageableLayers);
			if (layerToHit == null) return false;

			layerToHit.TilemapDamage.ApplyDamage(damageData.Damage, damageData.AttackType, worldPosition);

            return true;
		}

		private Layer GetLayerToHitOrGetNull(IEnumerable<Layer> layers)
		{
			return layers.FirstOrDefault(layer => layersToHit.Any(l => l == layer.LayerType));
		}
	}
}