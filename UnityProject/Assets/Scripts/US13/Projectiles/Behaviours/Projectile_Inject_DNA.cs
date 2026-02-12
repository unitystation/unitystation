using System.Collections.Generic;
using UnityEngine;
using US13.Core.Chat;
using US13.HealthV2;
using US13.HealthV2.Living;
using US13.Items.Medical.Genetics;
using US13.Items.Weapons;
using US13.Managers.MatrixManager;
using US13.Objects.Medical;

namespace US13.Projectiles.Behaviours
{
	public class Projectile_Inject_DNA : MonoBehaviour, IOnShoot,IOnHit
	{
		public List<DNAMutationData> DNAPayload = new List<DNAMutationData>();
		private BodyPartType targetZone;
		public void OnShoot(Vector2 direction, GameObject shooter, Gun weapon, MagazineBehaviour MagazineBehaviour, BodyPartType targetZone = BodyPartType.Chest)
		{

			var Container = MagazineBehaviour.GetComponent<MutationInjector>();
			if (Container != null)
			{
				DNAPayload = Container.DNAPayload;
			}


			this.targetZone = targetZone;
		}

		public bool OnHit(MatrixManager.CustomPhysicsHit hit)
		{

			var coll = hit.CollisionHit.GameObject;
			if (coll == null) return false;
			var health = coll.GetComponent<LivingHealthMasterBase>();
			if (health != null)
			{
				health.InjectDna(DNAPayload);
				Chat.AddThrowHitMsgToChat(gameObject, coll.gameObject, targetZone);
				return true;
			}

			return false;
		}
	}
}
