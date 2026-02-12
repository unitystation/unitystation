using US13.UI.Core.Net.Page;

namespace US13.UI.Objects.Cargo
{
	public class GUI_CargoPage : NetPage
	{
		public GUI_Cargo cargoGUI;

		/// <summary>
		/// Method to update tab info on it's opening
		/// Called from GUI_Cargo on SwitchTab()
		/// </summary>
		public virtual void OpenTab() { }

		public virtual void UpdateTab() { }
	}
}
