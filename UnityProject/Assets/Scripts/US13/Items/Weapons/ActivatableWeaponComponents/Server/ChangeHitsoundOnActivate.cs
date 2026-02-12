using System.Collections.Generic;
using UnityEngine;
using US13.Core.Addressables.Types;

namespace US13.Items.Weapons.ActivatableWeaponComponents.Server
{
	public class ChangeHitsoundOnActivate : ServerActivatableWeaponComponent
	{
		private ItemAttributesV2 itemAttributes;

		[SerializeField] private AddressableAudioSource activatedHitsound;
		private AddressableAudioSource defaultHitsound;

		[SerializeField] private List<string> activatedAttackVerbs;
		private IEnumerable<string> defaultAttackVerbs;


		private void Start()
		{
			itemAttributes = GetComponent<ItemAttributesV2>();
			defaultHitsound = itemAttributes.ServerHitSound;
			defaultAttackVerbs = itemAttributes.ServerAttackVerbs;
		}

		public override void ServerActivateBehaviour(GameObject performer)
		{
			itemAttributes.ServerHitSound = activatedHitsound;
			itemAttributes.ServerAttackVerbs = activatedAttackVerbs;
		}

		public override void ServerDeactivateBehaviour(GameObject performer)
		{
			itemAttributes.ServerHitSound = defaultHitsound;
			itemAttributes.ServerAttackVerbs = defaultAttackVerbs;
		}
	}
}