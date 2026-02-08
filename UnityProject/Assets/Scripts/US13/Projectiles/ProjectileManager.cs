using UnityEngine;
using US13.Core.Lifecycle;
using US13.HealthV2;
using US13.Items.Weapons;
using US13.Projectiles.Behaviours;
using Util;

namespace US13.Projectiles
{
	public class ProjectileManager : MonoBehaviour
	{
		public static GameObject InstantiateAndShoot(GameObject projectile, Vector2 finalDirection, GameObject shooter,
			Gun fromWeapon, BodyPartType targetZone = BodyPartType.Chest, float Rangeoverride = -1f, Vector3? ShootWorldPosition = null)
		{

			if (ShootWorldPosition == null)
			{
				ShootWorldPosition = shooter.transform.position;
			}

			GameObject Newprojectile = Spawn.ServerPrefab(projectile,
				ShootWorldPosition, parent: shooter.transform.parent).GameObject;
			Projectile projectileComponent = Newprojectile.GetComponent<Projectile>();
			projectileComponent.Shoot(finalDirection, shooter, fromWeapon, null, targetZone);
			if (Rangeoverride != -1f)
			{
				if (projectileComponent.TryGetComponent<ProjectileRangeLimited>(out var rangeLimited))
				{
					rangeLimited.SetDistance(Rangeoverride);
				}
			}

			return Newprojectile;
		}

		public static GameObject InstantiateAndShoot(string projectile, Vector2 finalDirection, GameObject shooter,
			Gun fromWeapon, BodyPartType targetZone = BodyPartType.Chest, float Rangeoverride = -1f, Vector3? ShootWorldPosition = null)
		{
			return InstantiateAndShoot(Spawn.GetPrefabByName(projectile), finalDirection, shooter, fromWeapon,
				targetZone, Rangeoverride, ShootWorldPosition);
		}

		public static GameObject CloneAndShoot(OnHitDetectData data, string projectile, Vector2 finalDirection, GameObject shooter,
			Gun fromWeapon, BodyPartType targetZone = BodyPartType.Chest, float Rangeoverride = -1f,
			Vector3? ShootWorldPosition = null)
		{
			var  Newprojectile = InstantiateAndShoot(Spawn.GetPrefabByName(projectile), finalDirection, shooter, fromWeapon,
			targetZone, Rangeoverride, ShootWorldPosition);

			var ToCopys = data.BulletObject.GetComponents<ICloneble>();

			foreach (var ToCopy in ToCopys)
			{
				ToCopy.CloneTo(Newprojectile);
			}

			return Newprojectile;
		}

	}
}

