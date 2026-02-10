using UnityEngine;
using US13.Systems.Construction.Parts;
using US13.Systems.Inventory;

namespace US13.Objects
{
	/// <summary>
	/// Mainly used for Getting the Battery from a object
	/// </summary>
	public class InternalBattery : MonoBehaviour, IChargeable
	{
		private ItemSlot InternalBatterySlot;

		private Battery battery;
		// Start is called before the first frame update
		private void Awake()
		{
			ItemStorage BatteryitemStorage = GetComponent<ItemStorage>();
			InternalBatterySlot = BatteryitemStorage.GetIndexedItemSlot(0);
		}

		public bool IsFullyCharged => battery.IsFullyCharged;

		public void ChargeBy(float watts) => battery.ChargeBy(watts);

		public Battery GetBattery()
		{
			//don't cash this since battery Can change
			battery = InternalBatterySlot.Item.GetComponent<Battery>();
			return battery;
		}
	}
}
