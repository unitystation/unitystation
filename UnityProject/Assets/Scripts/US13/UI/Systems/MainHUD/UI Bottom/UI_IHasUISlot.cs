using US13.Systems.Inventory;

namespace US13.UI.Systems.MainHUD.UI_Bottom
{
	public interface IHasUISlot
	{
		ItemStorage ItemStorage { get; set; }
		NamedSlot SlotName { get; set; }
	}
}
