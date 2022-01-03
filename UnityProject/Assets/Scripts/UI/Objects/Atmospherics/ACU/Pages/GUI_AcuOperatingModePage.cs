using Objects.Atmospherics;

namespace UI.Objects.Atmospherics.Acu
{
	/// <summary>
	/// Allows the peeper to set the <see cref="AirController"/>'s operating mode.
	/// </summary>
	public class GUI_AcuOperatingModePage : GUI_AcuPage
	{
		public override bool IsProtected => true;

		private static readonly string m = "<mark=#00FF0040>";
		private static readonly string um = "</mark>";

		public override void OnPeriodicUpdate()
		{
			UpdateLabels((int) Acu.DesiredMode);
		}

		public void BtnSetMode(int mode)
		{
			AcuUi.PlayTap();
			SetMode(mode);
		}

		private void SetMode(int mode)
		{
			if (Acu.IsWriteable == false) return;

			Acu.SetOperatingMode((AcuMode)mode);
			UpdateLabels(mode);
		}

		private void UpdateLabels(int mode)
		{
			var labels = transform.parent.GetComponentsInChildren<NetLabel>();
			var requestedMode = (mode == 0 ? labels.Length : mode) - 1;
			foreach (var label in labels)
			{
				if (label != labels[requestedMode])
				{
					label.SetValueServer(label.Value
							.Replace("(*)", "( )")
							.Replace(m, string.Empty)
							.Replace(um, string.Empty));
					continue;
				}

				if (label.Value.Contains("( )"))
				{
					label.SetValueServer($"{m}{label.Value.Replace("( )", "(*)")}{um}");
				}
			}
		}
	}
}
