using US13.Systems.Clearance;
using US13.UI.Core.Net.Elements;
using US13.UI.Core.Net.Elements.Dynamic;

namespace US13.UI.Items
{
	public class GUI_AirlockElectronicsEntry : DynamicEntry
	{
		public NetText_label AccessName;

		private Clearance clearance;

		private GUI_AirlockElectronics gUI;

		public void SetValues(Clearance clearanceToSet, GUI_AirlockElectronics guiToSet)
		{
			clearance = clearanceToSet;
			gUI = guiToSet;
			AccessName.MasterSetValue(clearance.ToString());
		}
		public void ChangeAccess()
		{
			gUI.ServerSetAccess(clearance);
		}
	}
}
