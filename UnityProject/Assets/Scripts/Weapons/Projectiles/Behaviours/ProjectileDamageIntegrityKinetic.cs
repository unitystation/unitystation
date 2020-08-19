using System;
using UnityEngine;
using ScriptableObjects.Gun;

namespace Weapons.Projectiles.Behaviours
{
	/// <summary>
	/// Damages integrity on collision
	/// only for kinetic weapons
	/// </summary>
	public class ProjectileDamageIntegrityKinetic : MonoBehaviour, IOnShoot, IOnHit
	{
		private GameObject shooter;
		private Gun weapon;
		private ProjectileKineticDamageCalculation projectileKineticDamage;

		[SerializeField] private DamageData damageData = null;

		public void OnShoot(Vector2 direction, GameObject shooter, Gun weapon, BodyPartType targetZone = BodyPartType.Chest)
		{
			this.shooter = shooter;
			this.weapon = weapon;
		}

		public bool OnHit(RaycastHit2D hit)
		{
			return TryDamage(hit);
		}

		private bool TryDamage(RaycastHit2D hit)
		{
			var coll = hit.collider;
			var integrity = coll.GetComponent<Integrity>();
			if (integrity == null)
			{
				return false;
			}

			float newDamage = projectileKineticDamage.DamageByPressureModifier(damageData.Damage);

			integrity.ApplyDamage(newDamage, damageData.AttackType, damageData.DamageType);

			Chat.AddAttackMsgToChat(
				shooter,
				coll.gameObject,
				BodyPartType.None,
				weapon.gameObject);

			Logger.LogTraceFormat(
				"Hit {0} for {1} with Integrity! bullet absorbed",
				Category.Firearms,
				integrity.gameObject.name,
				newDamage);

			return true;
		}

		private void OnDisable()
		{
			weapon = null;
			shooter = null;
		}

		private void Awake()
		{
			projectileKineticDamage = GetComponent<ProjectileKineticDamageCalculation>();
		}
	}
}

