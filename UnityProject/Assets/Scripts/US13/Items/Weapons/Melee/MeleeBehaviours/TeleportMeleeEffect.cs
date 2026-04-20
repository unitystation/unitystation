using System;
using System.Collections.Generic;
using UnityEngine;
using US13.Core.Addressables.Types;
using US13.Core.Attributes;
using US13.HealthV2;
using US13.Managers;
using US13.Player;

namespace US13.Items.Weapons.Melee
{
	[Serializable]
	public class TeleportMeleeEffect : ICustomMeleeBehaviour
	{
		[SerializeField] private int minimumTeleportDistance = 1;
		[SerializeField] private int maximumTeleportDistance = 4;
		[SerializeField] private bool shouldAvoidSpaceTiles = true;
		[SerializeField] private bool shouldAvoidImpassables = true;

		[SerializeReference, SelectImplementation(typeof(IHitRequirement))]
		private List<IHitRequirement> hitRequirements;

		[SerializeField] private AddressableAudioSource useSound = null;

		private bool isEnabled = true;

		bool ICustomMeleeBehaviour.IsEnabled
		{
			get => isEnabled;
			set => isEnabled = value;
		}

		List<IHitRequirement> ICustomMeleeBehaviour.Requirements
		{
			get => hitRequirements;
			set => hitRequirements = value;
		}

		public WeaponNetworkActions.MeleeStats CustomMeleeBehaviour(GameObject attacker,
			GameObject target, BodyPartType damageZone, WeaponNetworkActions.MeleeStats stats)
		{
			return stats;
		}
		public void OnHitBehaviour(GameObject attacker, GameObject target, BodyPartType damageZone, WeaponNetworkActions.MeleeStats stats)
		{
			TeleportUtils.ServerTeleportRandom(target, minimumTeleportDistance, maximumTeleportDistance, shouldAvoidSpaceTiles, shouldAvoidImpassables);
			if(useSound != null) SoundManager.PlayNetworkedAtPos(useSound, target.transform.position, sourceObj: target.gameObject);
		}
		public void OnBlockBehaviour(GameObject attacker, GameObject target, BodyPartType damageZone, WeaponNetworkActions.MeleeStats stats) { }
	}
}