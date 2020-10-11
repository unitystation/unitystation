using PathFinding;
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

		public bool OnHit(RaycastHit2D hit)
		{
			return TryDamage(hit);
		}

		private bool TryDamage(RaycastHit2D hit)
		{
			var coll = hit.collider;
			var livingHealth = coll.GetComponent<LivingHealthBehaviour>();
			if (livingHealth == null) return false;


			livingHealth.ApplyDamageToBodypart(shooter, damageData.Damage, damageData.AttackType, damageData.DamageType, targetZone);

			Chat.AddThrowHitMsgToChat(gameObject, coll.gameObject, targetZone);
			Logger.LogTraceFormat("Hit {0} for {1} with HealthBehaviour! bullet absorbed", Category.Firearms,
				livingHealth.gameObject.name, damageData.Damage);

			return true;
		}

		private void OnDisable()
		{
			shooter = null;
		}
	}
}