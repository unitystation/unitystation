using System;
using UnityEngine;
using US13.Core.Chat;
using US13.HealthV2;
using YamlDotNet.Core.Tokens;

namespace US13.Items.Weapons.Melee
{
	[Serializable]
	public class CooldownTime : IHitRequirement
	{
		[SerializeField] private float cooldownTime = 1.0f;

		private DateTime cooldownEndTime;

		[SerializeField] private IHitRequirement.HitRequirementType hitRequirement = IHitRequirement.HitRequirementType.OnHit;
		IHitRequirement.HitRequirementType IHitRequirement.EffectedMethods
		{
			get => hitRequirement;
			set => hitRequirement = value;
		}

		public bool CanHit( IHitRequirement.HitRequirementType type, GameObject attacker, GameObject target, BodyPartType damageZone, WeaponNetworkActions.MeleeStats stats)
		{
			if (hitRequirement.HasFlag(type) == false) return true;

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