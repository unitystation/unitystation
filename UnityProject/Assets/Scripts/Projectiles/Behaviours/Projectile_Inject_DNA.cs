using System.Collections.Generic;
using Chemistry;
using Chemistry.Components;
using HealthV2;
using UnityEngine;
using Weapons;
using Weapons.Projectiles.Behaviours;

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
