using System.Collections.Generic;
using Items;

namespace Weapons.WeaponAttachments
{
	public class Bayonet : WeaponAttachment
	{
		private float defaultHitDamage;
		private DamageType defaultDamageType;
		private IEnumerable<string> defaultAttackVerbs;

		protected void Awake()
		{
			InteractionKey = "Remove Bayonet";
			AttachmentType = AttachmentType.Bayonet;
			AttachmentSlot = AttachmentSlot.Suppressor;
		}
		
		public override void AttachBehaviour(InventoryApply interaction, Gun gun)
		{
			defaultHitDamage = gun.attributes.ServerHitDamage;
			defaultDamageType = gun.attributes.ServerDamageType;
			defaultAttackVerbs = gun.attributes.ServerAttackVerbs;
			var melee = GetComponent<ItemAttributesV2>();
			gun.attributes.ServerHitDamage = melee.ServerHitDamage;
			gun.attributes.ServerDamageType = melee.ServerDamageType;
			gun.attributes.ServerAttackVerbs = melee.ServerAttackVerbs;
		}
		
		public override void DetachBehaviour(ContextMenuApply interaction, Gun gun)
		{
			gun.attributes.ServerHitDamage = defaultHitDamage;
			gun.attributes.ServerDamageType = defaultDamageType;
			gun.attributes.ServerAttackVerbs = defaultAttackVerbs;
		}
	}
}