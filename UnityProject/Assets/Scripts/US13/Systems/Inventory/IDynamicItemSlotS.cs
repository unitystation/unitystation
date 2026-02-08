using System.Collections.Generic;
using UnityEngine;
using US13.Items.Implants.Organs;

namespace US13.Systems.Inventory
{
	public interface IDynamicItemSlotS
	{
		GameObject GameObject { get; }
		ItemStorage RelatedStorage { get; }
		List<BodyPartUISlots.StorageCharacteristics> Storage { get; }

		int InterfaceGetInstanceID { get; }
	}
}
