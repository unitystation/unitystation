using Systems.Clearance;
using UI.Core.NetUI;

namespace UI.Items
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
