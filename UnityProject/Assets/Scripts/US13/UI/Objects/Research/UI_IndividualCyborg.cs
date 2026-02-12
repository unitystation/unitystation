using US13.Items.Implants.Organs;
using US13.UI.Core.Net.Elements;
using US13.UI.Core.Net.Elements.Dynamic;
using Util;

namespace US13.UI.Objects.Research
{
	public class UI_IndividualCyborg : DynamicEntry
	{
		public NetText_label EnableDisable;
		public NetText_label Name;

		public RemotelyControlledBrain AssociatedBrain;
		public GUI_RemoteSyntheticControl AssociatedRemoteSyntheticControl;
		public void Setup(RemotelyControlledBrain Brain, GUI_RemoteSyntheticControl GUI_RemoteSyntheticControl)
		{
			AssociatedRemoteSyntheticControl = GUI_RemoteSyntheticControl;
			AssociatedBrain = Brain;

			UpdateValues();

		}

		public void UpdateValues()
		{
			if (AssociatedBrain.Lockdown)
			{
				EnableDisable.MasterSetValue("Enable");
			}
			else
			{
				EnableDisable.MasterSetValue("Disable");
			}

			var AName = AssociatedBrain.GetComponent<Brain>()?.PossessingMind.OrNull()?.name;

			if (string.IsNullOrEmpty(AName))
			{
				AName = "Unknown";
			}
			Name.MasterSetValue("AName");
		}

		public void ToggleLock()
		{

			if (AssociatedRemoteSyntheticControl.AssociatedConsole.CyborgsOnMatrix.Contains(AssociatedBrain) == false)
			{
				AssociatedRemoteSyntheticControl.UpdateButton();
			}

			AssociatedBrain.ToggleLockdown();
			UpdateValues();
		}
	}
}
