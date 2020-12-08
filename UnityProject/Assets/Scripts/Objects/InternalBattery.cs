using UnityEngine;

namespace Objects
{
	/// <summary>
	/// Mainly used for Getting the Battery from a object
	/// </summary>
	public class InternalBattery : MonoBehaviour
	{
		private ItemSlot InternalBatterySlot;
		private ItemStorage BatteryitemStorage;

		private Battery battery;
		// Start is called before the first frame update
		private void Awake()
		{
			BatteryitemStorage = GetComponent<ItemStorage>();
			InternalBatterySlot = BatteryitemStorage.GetIndexedItemSlot(0);
		}

		public Battery GetBattery()
		{
			if (battery == null)
			{
				battery = InternalBatterySlot.Item.GetComponent<Battery>();
			}
			return battery;
		}
	}
}
