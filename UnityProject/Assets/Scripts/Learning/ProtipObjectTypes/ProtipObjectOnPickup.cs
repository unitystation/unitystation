using System;
using System.Collections.Generic;
using UnityEngine;

namespace Learning.ProtipObjectTypes
{
	public class ProtipObjectOnPickup : ProtipObject
	{
		public void OnEnable()
		{
			gameObject.PickupableOrNull().OnMoveToPlayerInventory.AddListener(Trigger);
		}

		private void OnDisable()
		{
			gameObject.PickupableOrNull().OnMoveToPlayerInventory.RemoveListener(Trigger);
		}

		private void Trigger(GameObject picker)
		{
			TriggerTip(picker);
		}
	}
}