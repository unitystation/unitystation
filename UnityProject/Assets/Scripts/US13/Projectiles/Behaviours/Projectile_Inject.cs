using Chemistry;
using UnityEngine;
using US13.ChemistryComponents;
using US13.Core.Chat;
using US13.HealthV2;
using US13.HealthV2.Living;
using US13.Items.Weapons;
using US13.Managers.MatrixManager;
using Util;

namespace US13.Projectiles.Behaviours
{
	public class Projectile_Inject : MonoBehaviour, IOnShoot,IOnHit
	{
		public ReagentMix ReagentMix;
		private BodyPartType targetZone;
		private GameObject target;

		public void OnShoot(Vector2 direction, GameObject shooter, Gun weapon, MagazineBehaviour MagazineBehaviour, BodyPartType targetZone = BodyPartType.Chest, GameObject Target = null)
		{

			var Container = MagazineBehaviour.GetCachedComponent<ReagentContainer>(includeDisabled: false);
			if (Container != null)
			{
				ReagentMix = Container.CurrentReagentMix.Clone();
			}


			this.targetZone = targetZone;
		}

		public bool OnHit(MatrixManager.CustomPhysicsHit hit)
		{

			var coll = hit.CollisionHit.GameObject;
			if (coll == null) return false;
			var health = coll.GetComponent<LivingHealthMasterBase>();
			if (health != null && health.Hitble(target))
			{
				health.reagentPoolSystem.BloodPool.Add(ReagentMix);
				Chat.AddThrowHitMsgToChat(gameObject, coll.gameObject, targetZone);
				return true;
			}

			return false;
		}
	}
}
