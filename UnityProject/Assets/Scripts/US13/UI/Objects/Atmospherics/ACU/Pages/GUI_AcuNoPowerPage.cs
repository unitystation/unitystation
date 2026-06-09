using UnityEngine;
using US13.Objects.Atmospherics;
using US13.UI.Core.Net.Elements;

namespace US13.UI.Objects.Atmospherics.ACU.Pages
{
	/// <summary>
	/// Page displayed when the <see cref="AirController"/> is unpowered.
	/// </summary>
	public class GUI_AcuNoPowerPage : GUI_AcuPage
	{
		[SerializeField]
		private NetColorChanger menuLabels = default;

		private Color previousMenuColor = Color.green;

		public override void OnPageActivated()
		{
			// Hide the display's labels for the hardware buttons when there's no power.
			previousMenuColor = menuLabels.Value;
			menuLabels.MasterSetValue(Color.clear);
		}

		public override void OnPageDeactivated()
		{
			menuLabels.MasterSetValue(previousMenuColor);
		}
	}
}
