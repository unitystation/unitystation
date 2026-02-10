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
	public class ProjectileDamageTileKinetic: MonoBehaviour, IOnHitInteractTile
	{
		[SerializeField] private DamageData damageData = null;

		[Tooltip("Tile layers to damage(Walls, Window, etc.)")]
		[SerializeField] private LayerType[] layersToHit = null;

		[SerializeField] private int numberOfTilesAffected = 1;

		private ProjectileKineticDamageCalculation projectileKineticDamage;

		public bool Interact(MatrixManager.CustomPhysicsHit hit, InteractableTiles interactableTiles, Vector3 worldPosition)
		{
			var layerToHit = GetLayerToHitOrGetNull(interactableTiles.MetaTileMap.DamageableLayers);
			if (layerToHit == null) return false;

			float newDamage = projectileKineticDamage.DamageByPressureModifier(damageData.Damage);

			layerToHit.TilemapDamage.ApplyDamage(damageData.Damage, damageData.AttackType, worldPosition);

			if (numberOfTilesAffected > 1)
			{
				for (int i = 0; i < numberOfTilesAffected; i++)
				{
					Vector3[] positionsToDamage = new Vector3[4]
					{
						new Vector3(worldPosition.x + i, worldPosition.y),
						new Vector3(worldPosition.x - i, worldPosition.y),
						new Vector3(worldPosition.x, worldPosition.y + i),
						new Vector3(worldPosition.x, worldPosition.y - i)
					};
					foreach (var position in positionsToDamage)
					{
						layerToHit.TilemapDamage.ApplyDamage(newDamage, damageData.AttackType, position);
					}
				}
			}

			return true;
		}

		private Layer GetLayerToHitOrGetNull(IEnumerable<Layer> layers)
		{
			return layers.FirstOrDefault(layer => layersToHit.Any(l => l == layer.LayerType));
		}

		private void Awake()
		{
			projectileKineticDamage = GetComponent<ProjectileKineticDamageCalculation>();
		}
	}
}