using UnityEngine;
using Util;

namespace US13.Learning.ProtipObjectTypes
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