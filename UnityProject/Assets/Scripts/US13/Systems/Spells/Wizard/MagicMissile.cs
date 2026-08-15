using UnityEngine;
using US13.Core;
using US13.HealthV2;
using US13.HealthV2.Living;
using US13.Managers;
using US13.Projectiles;
using US13.Projectiles.Behaviours;
using Util;

namespace US13.Systems.Spells.Wizard
{
	public class MagicMissile : Spell
	{
		public GameObject projectilePrefab;

		public override bool CastSpellServer(PlayerInfo caster, Vector3 clickPosition, BodyPartType targetZone)
		{
			var casterWorldPos = caster.Script.gameObject.AssumedWorldPosServer();

			var Nearby = ComponentsTracker<LivingHealthMasterBase>.GetAllNearbyTypesToTarget(caster.Script.gameObject, 10f);

			foreach (var target in Nearby)
			{
				if (target == false)
				{
					continue;
				}

				// Skip the caster itself
				if (target.gameObject == caster.GameObject)
				{
					continue;
				}


				Vector2 castVector = (Vector2)target.gameObject.transform.position - (Vector2)casterWorldPos;

				var Projectile = ProjectileManager.InstantiateAndShoot(projectilePrefab, castVector, caster.GameObject,
					null, targetZone);

				Projectile.GetComponent<TrackingMovingProjectile>().Target = target.gameObject;
				Projectile.GetComponent<HitPlayerTarget>().target = target.gameObject;
			}

			return Nearby.Count > 0;
		}
	}
}
