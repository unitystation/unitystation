using ScriptableObjects.Gun;
using UnityEngine;

namespace Weapons.Projectiles.Behaviours
{
	/// <summary>
	/// Damages integrity on collision
	/// </summary>
	public class ProjectileDamageIntegrity : MonoBehaviour, IOnShoot, IOnHit
	{
		private BodyPartType targetZone;

		public DamageData damageData = null;

		public void OnShoot(Vector2 direction, GameObject shooter, Gun weapon, BodyPartType targetZone = BodyPartType.Chest)
		{
			this.targetZone = targetZone;
		}

		public bool OnHit(MatrixManager.CustomPhysicsHit  hit)
		{
			return TryDamage(hit);
		}

		private bool TryDamage(MatrixManager.CustomPhysicsHit hit)
		{
			if (hit.CollisionHit.GameObject == null) return false;
			var coll = hit.CollisionHit.GameObject;
			var integrity = coll.GetComponent<Integrity>();
			if (integrity == null) return false;
			if (damageData == null) return true;

			integrity.ApplyDamage(damageData.Damage, damageData.AttackType, damageData.DamageType);

			Chat.AddThrowHitMsgToChat(gameObject, coll.gameObject, targetZone);
			Logger.LogTraceFormat("Hit {0} for {1} with Integrity! bullet absorbed", Category.Firearms,
				integrity.gameObject.name, damageData.Damage);

			foreach (var hitDetect in coll.GetComponents<IOnHitDetect>())
			{
				hitDetect.OnHitDetect(damageData);
			}

			return true;
		}
	}
}