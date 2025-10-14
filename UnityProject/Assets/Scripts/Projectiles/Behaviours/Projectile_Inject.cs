using Chemistry;
using Chemistry.Components;
using HealthV2;
using Logs;
using UnityEngine;
using Weapons;
using Weapons.Projectiles.Behaviours;

public class Projectile_Inject : MonoBehaviour, IOnShoot,IOnHit
{
	public ReagentMix ReagentMix;
	private BodyPartType targetZone;
	public void OnShoot(Vector2 direction, GameObject shooter, Gun weapon, MagazineBehaviour MagazineBehaviour, BodyPartType targetZone = BodyPartType.Chest)
	{

		var Container = MagazineBehaviour.GetComponent<ReagentContainer>();
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
		if (health != null)
		{
			health.reagentPoolSystem.BloodPool.Add(ReagentMix);
			Chat.AddThrowHitMsgToChat(gameObject, coll.gameObject, targetZone);
			return true;
		}

		return false;
	}
}
