using Items;
using UnityEngine;

namespace Weapons.ActivatableWeapons
{
	public class ChangeBlockOnActivate : ServerActivatableWeaponComponent
	{
		private ItemAttributesV2 itemAttributes;

		[SerializeField]
		[Range(0, 100)]
		private float activatedBlockChance = 50;

		private void Start()
		{
			itemAttributes = GetComponent<ItemAttributesV2>();
		}

		public override void ServerActivateBehaviour(GameObject performer)
		{
			itemAttributes.ServerBlockChance.RecordPosition(this, activatedBlockChance);
		}

		public override void ServerDeactivateBehaviour(GameObject performer)
		{
			itemAttributes.ServerBlockChance.RemovePosition(this);
		}
	}
}
