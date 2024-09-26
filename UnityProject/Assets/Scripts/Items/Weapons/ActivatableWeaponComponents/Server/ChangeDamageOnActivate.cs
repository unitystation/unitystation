using Items;
using UnityEngine;

namespace Weapons.ActivatableWeapons
{
	public class ChangeDamageOnActivate : ServerActivatableWeaponComponent
	{
		private ItemAttributesV2 itemAttributes;

		public float ActivatedHitDamage => activatedHitDamage;
		[SerializeField] private float activatedHitDamage;
		private float defaultHitDamage;

		[SerializeField] private float activatedThrowDamage;
		private float defaultThrowDamage;

		[SerializeField] private DamageType activatedDamageType;
		private DamageType defaultDamageType;

		private void Start()
		{
			itemAttributes = GetComponent<ItemAttributesV2>();
			defaultHitDamage = itemAttributes.ServerHitDamage;
			defaultThrowDamage = itemAttributes.ServerThrowDamage;
			defaultDamageType = itemAttributes.ServerDamageType;
		}

		public override void ServerActivateBehaviour(GameObject performer)
		{
			itemAttributes.ServerHitDamage = activatedHitDamage;
			itemAttributes.ServerThrowDamage = activatedThrowDamage;
			itemAttributes.ServerDamageType = activatedDamageType;
		}

		public override void ServerDeactivateBehaviour(GameObject performer)
		{
			itemAttributes.ServerHitDamage = defaultHitDamage;
			itemAttributes.ServerThrowDamage = defaultThrowDamage;
			itemAttributes.ServerDamageType = defaultDamageType;
		}
	}
}