using Systems.StatusesAndEffects;
using UnityEngine;

namespace Weapons.Projectiles.Behaviours
{
	public class ProjectileStatusEffect : MonoBehaviour, IOnHit
	{
		[Header("Inflicts a status effect when it hits a living thing.")]

		[SerializeField]
		private StatusEffect statusEffect;

		public bool OnHit(MatrixManager.CustomPhysicsHit hit)
		{
			var coll = hit.CollisionHit.GameObject;
			if (coll == null) return false;

			var playerScript = coll.GetComponent<PlayerScript>();
			if (playerScript != null)
			{
				var status = Instantiate(statusEffect);
				playerScript.StatusEffectManager.AddStatus(status);
				Chat.AddThrowHitMsgToChat(gameObject, coll.gameObject);
				return true;
			}
			return false;
		}
	}
}
