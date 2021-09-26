using System.Collections;
using UnityEngine;
using Objects.Atmospherics;


namespace UI.Objects.Atmospherics.Acu
{
	/// <summary>
	/// An entry for the <see cref="GUI_AcuDevicesPage"/>.
	/// Allows the peeper to view and configure the settings for the associated vent.
	/// </summary>
	public class GUI_AcuVentEntry : DynamicEntry
	{
		[SerializeField]
		private NetLabel label = default;

		private GUI_Acu acuUi;
		private AirVent vent;

		private static readonly string m = "<mark=#00FF0040>";
		private static readonly string um = "</mark>";

		public void SetValues(GUI_Acu acuUi, AirVent vent)
		{
			this.acuUi = acuUi;
			this.vent = vent;
			UpdateText();
		}

		public void UpdateText()
		{
			string disabledColor = GUI_Acu.GetHtmlColorByStatus(AcuStatus.Off);

			var onStr = vent.IsTurnedOn
					? $"{m}[ On ]{um} Off  "
					: $"  On {m}[ Off ]{um}";
			var modeStr = vent.OperatingMode == AirVent.Mode.Out
					? $"{m}[ Pressurizing ]{um}   Siphoning  "
					: $"  Pressurizing   {m}[ Siphoning ]{um}";
			var internalStr = vent.InternalEnabled
					? $"{m}[   Internal   ]{um}"
					: "    Internal    ";
			var externalStr = vent.ExternalEnabled
					? $"{m}[ External  ]{um}"
					: "  External   ";
			var internalTargetStr = vent.InternalTarget.ToString("0.###");
			var externalTargetStr = vent.ExternalTarget.ToString("0.###");
			var internalResetStr = vent.InternalEnabled == false || vent.InternalTarget.Approx(0)
					? $"<color=#{disabledColor}>< Reset ></color>"
					: "< Reset >";
			var externalResetStr = vent.ExternalEnabled == false || vent.ExternalTarget.Approx(101.325f)
					? $"<color=#{disabledColor}>< Reset ></color>"
					: "< Reset >";

			var internalTargetLine = $"Internal Target: {internalTargetStr, 9} kPa        {internalResetStr}";
			if (vent.InternalEnabled == false)
			{
				internalTargetLine = $"<color=#{disabledColor}>{internalTargetLine}</color>";
			}
			var externalTargetLine = $"External Target: {externalTargetStr, 9} kPa        {externalResetStr}";
			if (vent.ExternalEnabled == false)
			{
				externalTargetLine = $"<color=#{disabledColor}>{externalTargetLine}</color>";
			}

			// "Vent - " as per NameValidator tool.
			string ventName = vent.gameObject.name;
			ventName = ventName.StartsWith("Vent - ") ? ventName.Substring("Vent - ".Length) : ventName;

			string str =
					"---------------------------------------------------\n" +
					$"| {ventName, -35} {onStr}|\n" +
					"|                                                 |\n" +
					$"| Mode:       {modeStr}      |\n" +
					$"| Regulator:  {internalStr} {externalStr}      |\n" +
					"|                                                 |\n" +
					$"| {externalTargetLine} |\n" +
					$"| {internalTargetLine} |\n" +
					"---------------------------------------------------\n";

			label.SetValueServer(str);
		}

		private void DoAction(System.Action callback)
		{
			acuUi.PlayTap();
			if (acuUi.Acu.IsWriteable == false) return;

			callback.Invoke();

			UpdateText();
		}

		#region Buttons

		public void BtnSetTurnedOn(bool isOn)
		{
			DoAction(() =>
			{
				vent.SetTurnedOn(isOn);
			});
		}

		public void BtnSetOperatingMode(int mode)
		{
			DoAction(() =>
			{
				vent.SetOperatingMode((AirVent.Mode)mode);
			});
		}

		public void BtnToggleExternalRegulator()
		{
			DoAction(() =>
			{
				vent.ExternalEnabled = !vent.ExternalEnabled;
			});
		}

		public void BtnToggleInternalRegulator()
		{
			DoAction(() =>
			{
				vent.InternalEnabled = !vent.InternalEnabled;
			});
		}

		public void BtnEditExternalRegulator()
		{
			DoAction(() =>
			{
				acuUi.EditValueModal.Open(vent.ExternalTarget.ToString(), (str) =>
				{
					if (float.TryParse(str, out float value))
					{
						vent.ExternalTarget = Mathf.Clamp(value, 0, AtmosConstants.ONE_ATMOSPHERE * 50);
						UpdateText();
					}
				});
			});
		}

		public void BtnEditInternalRegulator()
		{
			DoAction(() =>
			{
				acuUi.EditValueModal.Open(vent.InternalTarget.ToString(), (str) =>
				{
					if (float.TryParse(str, out float value))
					{
						vent.InternalTarget = Mathf.Clamp(value, 0, AtmosConstants.ONE_ATMOSPHERE * 50);
						UpdateText();
					}
				});
			});
		}

		public void BtnResetExternalRegulator()
		{
			DoAction(() =>
			{
				vent.ExternalTarget = AtmosConstants.ONE_ATMOSPHERE;
			});
		}

		public void BtnResetInternalRegulator()
		{
			DoAction(() =>
			{
				vent.InternalTarget = 0;
			});
		}

		#endregion
	}
}
