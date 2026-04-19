using System;
using System.Collections.Generic;
using UnityEngine;
using US13.Core.Addressables.Types;
using US13.Core.Attributes;
using US13.Health;
using US13.HealthV2;
using US13.Managers;
using US13.Player;
using Util;

namespace US13.Items.Weapons.Melee
{
	[Serializable]
	public class ElectrocuteMeleeEffect : ICustomMeleeBehaviour
	{
		[SerializeField] private float voltage = 1500.0f;

		[SerializeReference, SelectImplementation(typeof(IHitRequirement))]
		private List<IHitRequirement> hitRequirements;

		[SerializeField] private AddressableAudioSource useSound = null;

		private bool isEnabled = true;

		bool ICustomMeleeBehaviour.IsEnabled
		{
			get => isEnabled;
			set => isEnabled = value;
		}

		public void SetNewVoltage(float newVoltage)
		{
			voltage = newVoltage;
		}

		List<IHitRequirement> ICustomMeleeBehaviour.Requirements
		{
			get => hitRequirements;
			set => hitRequirements = value;
		}

		public WeaponNetworkActions.MeleeStats CustomMeleeBehaviour(GameObject attacker,
			GameObject target, BodyPartType damageZone, WeaponNetworkActions.MeleeStats stats)
		{
			if (target.TryGetComponent<PlayerScript>(out var playerScript) == false) return stats;
			Electrocution newElectrocution = new Electrocution(voltage, target.AssumedWorldPosServer(), stats.DamageSourceName, true);
			playerScript.playerHealth?.Electrocute(newElectrocution);
			if(useSound != null) SoundManager.PlayNetworkedAtPos(useSound, target.transform.position, sourceObj: target.gameObject);
			return stats;
		}
	}
}