using System;
using Logs;
using UnityEngine;
using US13.Core.Admin.Logs;
using US13.Core.Chat;
using US13.Health.Living;
using US13.HealthV2;
using US13.HealthV2.Living;
using US13.Items.Weapons;
using US13.Managers.MatrixManager;
using US13.Player;
using US13.ScriptableObjects.Gun;
using Util;

namespace US13.Projectiles.Behaviours
{
	/// <summary>
	/// Damages health on collision
	/// </summary>
	public class ProjectileDamageLivingHealth : MonoBehaviour, IOnShoot, IOnHit
	{
		private GameObject shooter;
		private BodyPartType targetZone;
		private GameObject Target;

		[SerializeField] private DamageData damageData = null;

		public void OnShoot(Vector2 direction, GameObject shooter, Gun weapon, MagazineBehaviour MagazineBehaviour , BodyPartType targetZone = BodyPartType.Chest, GameObject Target = null)
		{
			this.shooter = shooter;
			this.targetZone = targetZone;
			this.Target = Target;
		}

		public bool OnHit(MatrixManager.CustomPhysicsHit hit)
		{
			return TryDamage(hit);
		}

		private bool TryDamage(MatrixManager.CustomPhysicsHit hit)
		{
			var coll = hit.CollisionHit.GameObject;
			if (coll == null) return false;

			if (damageData.Damage <= 0)
			{
				HealTarget(coll);
				return true;
			}

			//TODO REMOVE AFTER SWITCHING MOBS TO LivingHealthMasterBase or else guns wont kill them
			var livingHealth = coll.GetComponent<LivingHealthBehaviour>();
			if (livingHealth != null)
			{
				livingHealth.ApplyDamageToBodyPart(shooter, damageData.Damage, damageData.AttackType, damageData.DamageType, targetZone);

				AdminLogsManager.AddNewLog(shooter , " Shot ", livingHealth.gameObject, $" Using {this.gameObject.ExpensiveName()} Damage {damageData.Damage} DamageType {damageData.DamageType} targetZone {targetZone} ", LogCategory.MobDamage );

				Chat.AddThrowHitMsgToChat(gameObject, coll.gameObject, targetZone);
				Loggy.Trace().Format("Hit {0} for {1} with HealthBehaviour! bullet absorbed", Category.Firearms,
					livingHealth.gameObject.name, damageData.Damage);

				return true;
			}

			//TODO REMOVE AFTER SWITCHING MOBS TO
			var health = coll.GetComponent<LivingHealthMasterBase>();
			if (health != null && health.Hitble(Target))
			{
				health.ApplyDamageToBodyPart(shooter, damageData.Damage,
					damageData.AttackType, damageData.DamageType, targetZone, default, 50,
					TraumaticDamageTypes.PIERCE);

				Chat.AddThrowHitMsgToChat(gameObject, coll.gameObject, targetZone);
				Loggy.Trace().Format("Hit {0} for {1} with HealthBehaviour! bullet absorbed", Category.Firearms,
					health.gameObject.name, damageData.Damage);

				return true;
			}
			return false;
		}

		private void HealTarget(GameObject hit)
		{
			if (hit.TryGetComponent<LivingHealthMasterBase>(out var livingHealth) == false) return;
			livingHealth.HealDamageOnAll(gameObject, Math.Abs(damageData.Damage), damageData.DamageType);
			livingHealth.HealDamageOnAll(gameObject, Math.Abs(damageData.Damage), DamageType.Burn);
		}

		private void OnDisable()
		{
			shooter = null;
		}
	}
}