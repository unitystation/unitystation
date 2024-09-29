using System.Collections.Generic;
using UnityEngine;

namespace Weapons.ActivatableWeapons
{
	public class DropItemSInOtherHandOnActivate : ServerActivatableWeaponComponent
	{
		public override void ServerActivateBehaviour(GameObject performer)
		{
			List<ItemSlot> hands = performer.GetComponent<PlayerScript>().DynamicItemStorage.GetHandSlots();
			hands.Remove(performer.GetComponent<PlayerScript>().DynamicItemStorage.GetActiveHandSlot());

			foreach (var hand in hands)
			{
				Inventory.ServerDrop(hand);
			}
		}

		public override void ServerDeactivateBehaviour(GameObject performer)
		{
			//
		}
	}
}
