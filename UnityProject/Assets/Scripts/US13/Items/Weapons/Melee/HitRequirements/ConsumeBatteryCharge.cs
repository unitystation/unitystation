using System;
using System.Collections.Generic;
using UnityEngine;
using US13.Core.Chat;
using US13.HealthV2;
using US13.Objects;
using US13.Systems.Construction.Parts;
using US13.Systems.Inventory;
using Util;

namespace US13.Items.Weapons.Melee
{
	[Serializable]
	public class ConsumeBatteryCharge : IHitRequirement
	{
		[SerializeField] private bool shouldConsumeCharge = true;
		[SerializeField] private int chargeUsage = 1000;
		[SerializeField] private InternalBattery internalBattery = null;

		[SerializeField] private IHitRequirement.HitRequirementType hitRequirement = IHitRequirement.HitRequirementType.OnHit;
		IHitRequirement.HitRequirementType IHitRequirement.EffectedMethods
		{
			get => hitRequirement;
			set => hitRequirement = value;
		}

		public bool CanHit(IHitRequirement.HitRequirementType type, GameObject attacker, GameObject target, BodyPartType damageZone, WeaponNetworkActions.MeleeStats stats)
		{
			if (hitRequirement.HasFlag(type) == false) return true;
			if (shouldConsumeCharge == false) return true;
			if (internalBattery.CurrentCharge < chargeUsage)
			{
				Chat.AddWarningMsgFromServer(attacker, $"Insufficient charge: {(int)(internalBattery.CurrentCharge / 1000.0f)} / {(int)(chargeUsage / 1000.0f)}kJ");
				return false;
			}
			internalBattery.CurrentCharge -= chargeUsage;

			return true;
		}
	}
}