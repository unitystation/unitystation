using System;
using System.Collections.Generic;
using UnityEngine;

namespace Learning.ProtipObjectTypes
{
	public class ProtipObjectOnPickup : ProtipObject
	{
		public void OnEnable()
		{
			gameObject.PickupableOrNull().OnMoveToPlayerInventory += TriggerTip;
		}

		private void OnDisable()
		{
			gameObject.PickupableOrNull().OnMoveToPlayerInventory -= TriggerTip;
		}
	}
}