using Systems.Clearance;
using UI.Core.NetUI;

namespace UI.Items
{
	public class GUI_AirlockElectronicsEntry : DynamicEntry
	{
		public NetLabel AccessName;

		private Clearance clearance;

		private GUI_AirlockElectronics gUI;

		public void SetValues(Clearance clearanceToSet, GUI_AirlockElectronics guiToSet)
		{
			clearance = clearanceToSet;
			gUI = guiToSet;
			AccessName.SetValueServer(clearance.ToString());
		}
		public void ChangeAccess()
		{
			gUI.ServerSetAccess(clearance);
		}
	}
}
