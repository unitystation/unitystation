using System;
using Logs;
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
		private ProjectileKineticDamageCalculation projectileKineticDamage;
		private BodyPartType targetZone;

		[SerializeField] private DamageData damageData = null;

		public void OnShoot(Vector2 direction, GameObject shooter, Gun weapon, BodyPartType targetZone = BodyPartType.Chest)
		{
			this.targetZone = targetZone;
		}

		public bool OnHit(MatrixManager.CustomPhysicsHit  hit)
		{
			return TryDamage(hit);
		}

		private bool TryDamage(MatrixManager.CustomPhysicsHit  hit)
		{
			var coll = hit.CollisionHit.GameObject;
			if (coll == null)
			{
				return false;
			}
			var integrity = coll.GetComponent<Integrity>();
			if (integrity == null)
			{
				return false;
			}

			float newDamage = projectileKineticDamage.DamageByPressureModifier(damageData.Damage);

			integrity.ApplyDamage(newDamage, damageData.AttackType, damageData.DamageType);

			Chat.AddThrowHitMsgToChat(gameObject, coll.gameObject, targetZone);

			Loggy.LogTraceFormat(
				"Hit {0} for {1} with Integrity! bullet absorbed",
				Category.Firearms,
				integrity.gameObject.name,
				newDamage);

			return true;
		}

		private void Awake()
		{
			projectileKineticDamage = GetComponent<ProjectileKineticDamageCalculation>();
		}
	}
}

