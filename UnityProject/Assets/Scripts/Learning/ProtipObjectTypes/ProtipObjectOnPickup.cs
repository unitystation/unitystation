using System;
using System.Collections.Generic;
using UnityEngine;

namespace Learning.ProtipObjectTypes
{
	public class ProtipObjectOnPickup : ProtipObject
	{
		public void OnEnable()
		{
			gameObject.PickupableOrNull().OnMoveToPlayerInventory += Trigger;
		}

		private void OnDisable()
		{
			gameObject.PickupableOrNull().OnMoveToPlayerInventory -= Trigger;
		}

		private void Trigger()
		{
			var pickup = gameObject.PickupableOrNull();
			if (pickup == null || pickup.ItemSlot == null || pickup.ItemSlot.Player?.PlayerScript.gameObject == null) return;
			TriggerTip(pickup.ItemSlot.Player?.PlayerScript.gameObject);
		}
	}
}