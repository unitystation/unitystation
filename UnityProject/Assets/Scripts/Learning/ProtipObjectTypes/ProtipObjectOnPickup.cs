using System.Collections.Generic;
using UnityEngine;

namespace Learning.ProtipObjectTypes
{
	public class ProtipObjectOnPickup : ProtipObject
	{
		public void Start()
		{
			gameObject.PickupableOrNull().OnMoveToPlayerInventory.AddListener(TriggerTip);
		}
	}
}