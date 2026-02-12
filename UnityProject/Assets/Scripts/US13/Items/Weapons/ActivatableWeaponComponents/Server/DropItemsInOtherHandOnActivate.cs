using System.Collections.Generic;
using UnityEngine;
using US13.Player;
using US13.Systems.Inventory;

namespace US13.Items.Weapons.ActivatableWeaponComponents.Server
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
