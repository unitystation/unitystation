using UnityEngine;
using Systems.StatusesAndEffects.Implementations;

namespace Weapons.Projectiles.Behaviours
{
	public class ProjectileMark : MonoBehaviour, IOnHit
	{
		[Header("Adds the marked status effect when it hits a living thing.")]

		[SerializeField]
		private Marked statusEffect;

		public bool OnHit(MatrixManager.CustomPhysicsHit hit)
		{
			var coll = hit.CollisionHit.GameObject;
			if (coll == null) return false;

			var playerScript = coll.GetComponent<PlayerScript>();
			if (playerScript != null)
			{
				var mark = Instantiate(statusEffect);
				playerScript.StatusEffectManager.AddStatus(mark);
				Chat.AddThrowHitMsgToChat(gameObject, coll.gameObject);
				return true;
			}
			return false;
		}
	}
}
