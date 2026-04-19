using UnityEngine;
using US13.HealthV2;

namespace US13.Items.Weapons.Melee
{
	public interface IHitRequirement
	{
		public bool CanHit(GameObject attacker, GameObject target, BodyPartType damageZone, WeaponNetworkActions.MeleeStats stats);
	}
}