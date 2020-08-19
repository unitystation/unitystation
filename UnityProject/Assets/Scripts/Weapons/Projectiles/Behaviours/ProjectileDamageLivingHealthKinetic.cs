using System;
using ScriptableObjects.Gun;
using UnityEngine;

namespace Weapons.Projectiles.Behaviours
{
	/// <summary>
	/// Damages health on collision
	/// only for kinetic weapons
	/// </summary>
	public class ProjectileDamageLivingHealthKinetic : MonoBehaviour, IOnShoot, IOnHit
	{
		private GameObject shooter;
		private Gun weapon;
		private BodyPartType targetZone;
		private ProjectileKineticDamageCalculation projectileKineticDamage;


		[SerializeField] private DamageData damageData = null;

		public void OnShoot(Vector2 direction, GameObject shooter, Gun weapon, BodyPartType targetZone = BodyPartType.Chest)
		{
			this.shooter = shooter;
			this.weapon = weapon;
			this.targetZone = targetZone;
		}

		public bool OnHit(RaycastHit2D hit)
		{
			return TryDamage(hit);
		}

		private bool TryDamage(RaycastHit2D hit)
		{
			var coll = hit.collider;
			var livingHealth = coll.GetComponent<LivingHealthBehaviour>();
			if (livingHealth == null)
			{
				return false;
			}

			var newDamage = projectileKineticDamage.DamageByPressureModifier(damageData.Damage);

			livingHealth.ApplyDamageToBodypart(shooter, newDamage, damageData.AttackType, damageData.DamageType, targetZone);

			Chat.AddAttackMsgToChat(shooter, coll.gameObject, targetZone, weapon.gameObject);

			Logger.LogTraceFormat(
				"Hit {0} for {1} with HealthBehaviour! bullet absorbed",
				Category.Firearms,
				livingHealth.gameObject.name,
				newDamage);

			return true;
		}

		private void OnDisable()
		{
			shooter = null;
			weapon = null;
		}

		private void Awake()
		{
			projectileKineticDamage = GetComponent<ProjectileKineticDamageCalculation>();
		}
	}
}

