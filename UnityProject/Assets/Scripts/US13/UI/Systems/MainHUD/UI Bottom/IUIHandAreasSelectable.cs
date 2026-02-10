using US13.Systems.Inventory;

namespace US13.UI.Systems.MainHUD.UI_Bottom
{
	public interface IUIHandAreasSelectable
	{

		public void DeSelect(NamedSlot Hand);

		public void SwapHand();

		public UI_DynamicItemSlot GetHand(NamedSlot Hand);
	}
}
