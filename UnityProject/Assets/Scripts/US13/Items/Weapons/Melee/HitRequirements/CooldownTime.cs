using System;
using UnityEngine;
using US13.Core.Chat;
using US13.HealthV2;

namespace US13.Items.Weapons.Melee
{
	[Serializable]
	public class CooldownTime : IHitRequirement
	{
		[SerializeField] private float cooldownTime = 1.0f;
		private DateTime cooldownEndTime;

		public bool CanHit(GameObject attacker, GameObject target, BodyPartType damageZone, WeaponNetworkActions.MeleeStats stats)
		{
			var remaining = cooldownEndTime - DateTime.Now;
			if (remaining.Seconds > 0.0f)
			{
				Chat.AddWarningMsgFromServer(attacker, $"Still on cooldown! Remaining: {remaining.Seconds}s");
				return false;
			}
			cooldownEndTime = DateTime.Now.Add(TimeSpan.FromSeconds(cooldownTime));

			return true;
		}
	}
}