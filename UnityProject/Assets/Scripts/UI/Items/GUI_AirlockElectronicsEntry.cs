using UI.Core.NetUI;

namespace UI.Items
{
	public class GUI_AirlockElectronicsEntry : DynamicEntry
	{
		public NetLabel AccessName;

		private Access access;

		private GUI_AirlockElectronics gUI;

		public void SetValues(Access accessToSet, GUI_AirlockElectronics guiToSet)
		{
			access = accessToSet;
			gUI = guiToSet;
			AccessName.SetValueServer(access.ToString());
		}
		public void ChangeAccess()
		{
			gUI.ServerSetAccess(access);
		}
	}
}
