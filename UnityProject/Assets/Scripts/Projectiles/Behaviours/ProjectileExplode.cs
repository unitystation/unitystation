using System.Collections;
using UnityEngine;
using NaughtyAttributes;
using Systems.Explosions;

namespace Weapons.Projectiles.Behaviours
{
	public class ProjectileExplode : MonoBehaviour, IOnHit
	{
		[Header("Creates an explosion when the projectile hits something.")]

		[SerializeField]
		private bool useExplosionPrefab = false;

		[SerializeField, ShowIf(nameof(useExplosionPrefab))]
		private GameObject explosionPrefab = default;

		[SerializeField, HideIf(nameof(useExplosionPrefab))]
		private int explosionStrength = 80;

		[SerializeField, Tooltip("When true the projectile will also explode when it reaches its maximum range (ProjectileRangeLimited).")]
		private bool explodeOnMaxRange = false;

		private ProjectileRangeLimited rangeLimited;
		private bool hasExploded;

		private void OnEnable()
		{
			if (!explodeOnMaxRange) return;

			rangeLimited = GetComponent<ProjectileRangeLimited>();
			if (rangeLimited != null)
			{
				rangeLimited.OnMaxDistanceReached += HandleMaxRangeReached;
			}
		}

		private void OnDisable()
		{
			if (rangeLimited != null)
			{
				rangeLimited.OnMaxDistanceReached -= HandleMaxRangeReached;
			}
		}

		private void HandleMaxRangeReached()
		{
			// use transform.position as the explosion point when range is reached
			Explode(transform.position);
		}

		public bool OnHit(MatrixManager.CustomPhysicsHit hit)
		{
			if (hit.CollisionHit.GameObject == null)
			{
				// Don't explode on top of the tile, but on the tile adjacent to it.
				Explode((Vector2) hit.HitWorld + (hit.Normal * 0.2f));
			}
			else
			{
				Explode(hit.HitWorld);
			}

			return true;
		}

		private void Explode(Vector3 worldPosition)
		{
			if (CustomNetworkManager.IsServer == false) return;
			if (hasExploded) return;
			hasExploded = true;

			Vector3Int worldTilePosition = worldPosition.CutToInt();

			if (useExplosionPrefab)
			{
				GameObject explosionObject = Spawn.ServerPrefab(explosionPrefab, worldPosition).GameObject;
				ExplosionComponent explosion = explosionObject.GetComponent<ExplosionComponent>();
				explosion.Explode();
				return;
			}

			Explosion.StartExplosion(worldTilePosition, explosionStrength, stunNearbyPlayers: explosionStrength > 400);
		}
	}
}
