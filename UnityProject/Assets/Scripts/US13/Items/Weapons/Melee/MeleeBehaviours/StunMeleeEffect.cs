using System;
using System.Collections.Generic;
using UnityEngine;
using US13.Core.Addressables.Types;
using US13.Core.Attributes;
using US13.HealthV2;
using US13.Managers;
using US13.Player;
using US13.Tilemaps.Behaviours.Objects;
using US13.UI.Systems.MainHUD.UI_Bottom;

namespace US13.Items.Weapons.Melee
{
	[Serializable]
	public class StunMeleeEffect : ICustomMeleeBehaviour
	{
		[SerializeField] private float stunDuration = 4.0f;
		[SerializeField] private bool forceDropItem = true;
		[SerializeField] private bool checkForArmour = true;
		[SerializeField] private bool stopMovement = true;
		[SerializeField] private bool allowPacify = true;

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
			PlayerScript attackerScript = attacker.GetComponent<PlayerScript>();
			if (allowPacify && attackerScript.playerMove.intent == Intent.Help) stats.Damage = 0;
			return stats;
		}

		public void OnHitBehaviour(GameObject attacker, GameObject target, BodyPartType damageZone,
			WeaponNetworkActions.MeleeStats stats)
		{
			RegisterPlayer registerPlayerTarget = target.GetComponent<RegisterPlayer>();
			registerPlayerTarget.ServerStun(stunDuration, forceDropItem, checkForArmour, stopMovement);

			if(useSound != null) SoundManager.PlayNetworkedAtPos(useSound, target.transform.position, sourceObj: target.gameObject);
		}

		public void OnBlockBehaviour(GameObject attacker, GameObject target, BodyPartType damageZone,
			WeaponNetworkActions.MeleeStats stats) { }
	}
}