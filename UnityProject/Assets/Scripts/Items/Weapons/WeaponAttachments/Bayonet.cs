using System;
using System.Collections.Generic;
using AddressableReferences;
using Items;

namespace Weapons.WeaponAttachments
{
	public class Bayonet : WeaponAttachment
	{
		private float defaultHitDamage;
		private DamageType defaultDamageType;
		private IEnumerable<string> defaultAttackVerbs;
		[NonSerialized] private AddressableAudioSource defaultHitSound;

		private ItemAttributesV2 gunAttributes;

		protected void Awake()
		{
			InteractionKey = "Remove Bayonet";
			AttachmentType = AttachmentType.Bayonet;
		}
		
		public override void AttachBehaviour(Gun gun)
		{
			gunAttributes = gun.GetComponent<ItemAttributesV2>();
			defaultHitDamage = gunAttributes.ServerHitDamage;
			defaultDamageType = gunAttributes.ServerDamageType;
			defaultAttackVerbs = gunAttributes.ServerAttackVerbs;
			defaultHitSound = gunAttributes.ServerHitSound;
			var melee = GetComponent<ItemAttributesV2>();
			gunAttributes.ServerHitDamage = melee.ServerHitDamage;
			gunAttributes.ServerDamageType = melee.ServerDamageType;
			gunAttributes.ServerAttackVerbs = melee.ServerAttackVerbs;
			gunAttributes.ServerHitSound = melee.ServerHitSound;
		}
		
		public override void DetachBehaviour(Gun gun)
		{
			gunAttributes.ServerHitDamage = defaultHitDamage;
			gunAttributes.ServerDamageType = defaultDamageType;
			gunAttributes.ServerAttackVerbs = defaultAttackVerbs;
			gunAttributes.ServerHitSound = defaultHitSound;
		}
	}
}