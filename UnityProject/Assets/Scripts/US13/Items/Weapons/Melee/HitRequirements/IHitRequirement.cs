using System;
using UnityEngine;
using US13.HealthV2;

namespace US13.Items.Weapons.Melee
{
	public interface IHitRequirement
	{
		[Flags]
		public enum HitRequirementType
		{
			OnHit = 1 << 0,
			OnBlock = 1 << 1,
			Behaviour = 1 << 2,
		}

		public HitRequirementType EffectedMethods { get; set; }
		public bool CanHit(HitRequirementType type, GameObject attacker, GameObject target, BodyPartType damageZone, WeaponNetworkActions.MeleeStats stats);
	}
}