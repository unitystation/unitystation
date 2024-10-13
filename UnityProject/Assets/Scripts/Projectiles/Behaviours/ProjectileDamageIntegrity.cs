using Logs;
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

		private Vector2 direction;

		public DamageData damageData = null;

		public void OnShoot(Vector2 direction, GameObject shooter, Gun weapon, BodyPartType targetZone = BodyPartType.Chest)
		{
			this.targetZone = targetZone;
			this.direction = direction;
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

			var thisGameObject = gameObject;

			var data = new OnHitDetectData(damageData, thisGameObject.name, direction, hit.Normal, thisGameObject,hit.HitWorld);

			var allowDamage = true;

			foreach (var hitDetect in coll.GetComponents<IOnPreHitDetect>())
			{
				var result = hitDetect.OnPreHitDetect(data);

				//If one blocks damage then always block it
				if (result == false) allowDamage = false;
			}

			//Return true if we are blocking damage so we despawn
			if (allowDamage == false) return true;

			integrity.ApplyDamage(damageData.Damage, damageData.AttackType, damageData.DamageType);

			if (integrity.DoDamageMessage)
			{
				Chat.AddThrowHitMsgToChat(thisGameObject, coll.gameObject, targetZone);
			}

			Loggy.LogTraceFormat("Hit {0} for {1} with Integrity! bullet absorbed", Category.Firearms,
				integrity.gameObject.name, damageData.Damage);

			foreach (var hitDetect in coll.GetComponents<IOnHitDetect>())
			{
				hitDetect.OnHitDetect(data);
			}

			return true;
		}
	}
}