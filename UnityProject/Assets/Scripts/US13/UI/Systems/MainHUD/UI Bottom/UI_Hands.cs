using US13.Items.Implants.Organs;
using US13.Systems.Inventory;

namespace US13.UI.Systems.MainHUD.UI_Bottom
{
	public class UI_Hands : UI_DynamicItemSlot
	{
		public void SetUpHand(IDynamicItemSlotS bodyPartUISlots,
			BodyPartUISlots.StorageCharacteristics StorageCharacteristics)
		{
			SetupSlot(bodyPartUISlots, StorageCharacteristics);
		}

	}
}
