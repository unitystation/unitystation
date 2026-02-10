using US13.UI.Core;

namespace US13.UI.Systems.MainHUD.TabDropdown
{
	public class ClientTab : Tab {
		public ClientTabType Type;
	}

	public enum ClientTabType {
		Stats = 0,
		More,
		ItemList,
		ControlInformation,
		Admin,
		Notes
		//add your tabs here
	}
}