using System.Collections;
using UnityEngine;
using Objects.Atmospherics;

namespace UI.Objects.Atmospherics.Acu
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
			menuLabels.SetValueServer(Color.clear);
		}

		public override void OnPageDeactivated()
		{
			menuLabels.SetValueServer(previousMenuColor);
		}
	}
}
