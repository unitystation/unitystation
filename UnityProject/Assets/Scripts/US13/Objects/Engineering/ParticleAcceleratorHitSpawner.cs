using UnityEngine;
using US13.Core.Lifecycle;
using US13.Health.Objects;
using US13.Projectiles.Behaviours;
using US13.Tilemaps.Behaviours.Objects;

namespace US13.Objects.Engineering
{
	public class ParticleAcceleratorHitSpawner : MonoBehaviour, IOnHitDetect
	{
		[SerializeField]
		private GameObject prefabToSpawn = null;

		private RegisterTile registerTile;

		private float damageTaken;

		[SerializeField]
		private float radDamageNeedToSpawn = 200f;

		private void Awake()
		{
			registerTile = GetComponent<RegisterTile>();
		}

		public void OnHitDetect(OnHitDetectData data)
		{
			if (data.DamageData.AttackType != AttackType.Rad) return;

			damageTaken += data.DamageData.Damage;

			if (damageTaken >= radDamageNeedToSpawn)
			{
				SpawnPrefab();
			}
		}

		private void SpawnPrefab()
		{
			Spawn.ServerPrefab(prefabToSpawn, registerTile.WorldPositionServer, transform.parent.transform);
			_ = Despawn.ServerSingle(gameObject);
		}
	}
}
