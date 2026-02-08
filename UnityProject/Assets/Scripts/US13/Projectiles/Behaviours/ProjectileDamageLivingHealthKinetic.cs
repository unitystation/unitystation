using Logs;
using UnityEngine;
using US13.Core.Chat;
using US13.Health.Living;
using US13.HealthV2;
using US13.HealthV2.Living;
using US13.Items.Weapons;
using US13.Managers.MatrixManager;
using US13.ScriptableObjects.Gun;

namespace US13.Projectiles.Behaviours
{
	/// <summary>
	/// Damages health on collision
	/// only for kinetic weapons
	/// </summary>
	public class ProjectileDamageLivingHealthKinetic : MonoBehaviour, IOnShoot, IOnHit
	{
		private GameObject shooter;
		private BodyPartType targetZone;
		private ProjectileKineticDamageCalculation projectileKineticDamage;


		[SerializeField] private DamageData damageData = null;

		public void OnShoot(Vector2 direction, GameObject shooter, Gun weapon, MagazineBehaviour MagazineBehaviour, BodyPartType targetZone = BodyPartType.Chest)
		{
			this.shooter = shooter;
			this.targetZone = targetZone;
		}

		public bool OnHit(MatrixManager.CustomPhysicsHit  hit)
		{
			return TryDamage(hit);
		}

		private bool TryDamage(MatrixManager.CustomPhysicsHit  hit)
		{
			var coll = hit.CollisionHit.GameObject;
			if (coll == null) return true;
			//TODO REMOVE AFTER CHANGING MOBS OVER TO NEW HEALTH
			var livingHealth = coll.GetComponent<LivingHealthBehaviour>();
			var health = coll.GetComponent<LivingHealthMasterBase>();
			if (livingHealth == null && health == null)
			{
				return false;
			}

			var newDamage = projectileKineticDamage.DamageByPressureModifier(damageData.Damage);

			//TODO REMOVE AFTER CHANGING MOBS OVER TO NEW HEALTH
			if (livingHealth != null)
			{
				livingHealth.ApplyDamageToBodyPart(shooter, newDamage, damageData.AttackType, damageData.DamageType, targetZone);

				Chat.AddThrowHitMsgToChat(gameObject, coll.gameObject, targetZone);

				Loggy.Trace().Format(
					"Hit {0} for {1} with HealthBehaviour! bullet absorbed",
					Category.Firearms,
					livingHealth.gameObject.name,
					newDamage);

				return true;
			}

			health.ApplyDamageToBodyPart(shooter, newDamage, damageData.AttackType, damageData.DamageType, targetZone);

			Chat.AddThrowHitMsgToChat(gameObject, coll.gameObject, targetZone);

			Loggy.Trace().Format(
				"Hit {0} for {1} with HealthBehaviour! bullet absorbed",
				Category.Firearms,
				health.gameObject.name,
				newDamage);

			return true;
		}

		private void OnDisable()
		{
			shooter = null;
		}

		private void Awake()
		{
			projectileKineticDamage = GetComponent<ProjectileKineticDamageCalculation>();
		}
	}
}

