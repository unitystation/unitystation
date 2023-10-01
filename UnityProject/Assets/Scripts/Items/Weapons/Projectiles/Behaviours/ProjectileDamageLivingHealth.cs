using HealthV2;
using Logs;
using ScriptableObjects.Gun;
using UnityEngine;

namespace Weapons.Projectiles.Behaviours
{
	/// <summary>
	/// Damages health on collision
	/// </summary>
	public class ProjectileDamageLivingHealth : MonoBehaviour, IOnShoot, IOnHit
	{
		private GameObject shooter;
		private BodyPartType targetZone;

		[SerializeField] private DamageData damageData = null;

		public void OnShoot(Vector2 direction, GameObject shooter, Gun weapon, BodyPartType targetZone = BodyPartType.Chest)
		{
			this.shooter = shooter;
			this.targetZone = targetZone;
		}

		public bool OnHit(MatrixManager.CustomPhysicsHit hit)
		{
			return TryDamage(hit);
		}

		private bool TryDamage(MatrixManager.CustomPhysicsHit hit)
		{
			var coll = hit.CollisionHit.GameObject;
			if (coll == null) return false;

			//TODO REMOVE AFTER SWITCHING MOBS TO LivingHealthMasterBase or else guns wont kill them
			var livingHealth = coll.GetComponent<LivingHealthBehaviour>();
			if (livingHealth != null)
			{
				livingHealth.ApplyDamageToBodyPart(shooter, damageData.Damage, damageData.AttackType, damageData.DamageType, targetZone);


				Chat.AddThrowHitMsgToChat(gameObject, coll.gameObject, targetZone);
				Loggy.LogTraceFormat("Hit {0} for {1} with HealthBehaviour! bullet absorbed", Category.Firearms,
					livingHealth.gameObject.name, damageData.Damage);

				return true;
			}

			//TODO REMOVE AFTER SWITCHING MOBS TO
			var health = coll.GetComponent<LivingHealthMasterBase>();
			if (health != null)
			{
				health.ApplyDamageToBodyPart(shooter, damageData.Damage,
					damageData.AttackType, damageData.DamageType, targetZone, default, 50,
					TraumaticDamageTypes.PIERCE);

				Chat.AddThrowHitMsgToChat(gameObject, coll.gameObject, targetZone);
				Loggy.LogTraceFormat("Hit {0} for {1} with HealthBehaviour! bullet absorbed", Category.Firearms,
					health.gameObject.name, damageData.Damage);

				return true;
			}


			return false;
		}

		private void OnDisable()
		{
			shooter = null;
		}
	}
}