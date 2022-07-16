using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Weapons.Projectiles.Behaviours;


namespace Weapons.Projectiles
{
	public class ProjectileManager : MonoBehaviour
	{
		public static GameObject InstantiateAndShoot(GameObject projectile, Vector2 finalDirection, GameObject shooter,
			Gun fromWeapon, BodyPartType targetZone = BodyPartType.Chest, float Rangeoverride = -1f)
		{

			GameObject Newprojectile = Spawn.ServerPrefab(projectile,
				shooter.transform.position, parent: shooter.transform.parent).GameObject;
			Projectile projectileComponent = Newprojectile.GetComponent<Projectile>();
			projectileComponent.Shoot(finalDirection, shooter, fromWeapon, targetZone);
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
			Gun fromWeapon, BodyPartType targetZone = BodyPartType.Chest, float Rangeoverride = -1f)
		{
			return InstantiateAndShoot(Spawn.GetPrefabByName(projectile), finalDirection, shooter, fromWeapon,
				targetZone, Rangeoverride);
		}
	}
}

