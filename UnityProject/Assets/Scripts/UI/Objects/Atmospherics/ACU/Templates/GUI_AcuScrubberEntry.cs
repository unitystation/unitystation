using System.Collections.Generic;
using System.Text;
using UnityEngine;
using ScriptableObjects.Atmospherics;
using Systems.Atmospherics;
using Objects.Atmospherics;


namespace UI.Objects.Atmospherics.Acu
{
	/// <summary>
	/// An entry for the <see cref="GUI_AcuDevicesPage"/>.
	/// Allows the peeper to view and configure the settings for the associated scrubber.
	/// </summary>
	public class GUI_AcuScrubberEntry : DynamicEntry
	{
		[SerializeField]
		private NetLabel label = default;

		private static readonly string m = "<mark=#00FF0040>";
		private static readonly string um = "</mark>";

		private static Dictionary<GasSO, string> gasFormulaRT;

		// TODO: consider generating these dynamically.
		private static GasSO[] line1;
		private static GasSO[] line2;
		private static GasSO[] line3;

		private GUI_Acu acuUi;
		private Scrubber scrubber;

		private void Awake()
		{
			if (gasFormulaRT != null) return;

			gasFormulaRT = new Dictionary<GasSO, string>()
			{
				{ Gas.Oxygen, "O<sub>2</sub>" },
				{ Gas.Nitrogen, "N<sub>2</sub>" },
				{ Gas.CarbonDioxide, "CO<sub>2</sub>" },
				{ Gas.WaterVapor, "H<sub>2</sub>O" },
				{ Gas.NitrousOxide, "N<sub>2</sub>O" },
				{ Gas.Nitryl, "NO<sub>2</sub>" },
				{ Gas.Hydrogen, "H<sub>2</sub>" },
			};

			line1 = new GasSO[] { Gas.Oxygen, Gas.Nitrogen, Gas.CarbonDioxide, Gas.WaterVapor, Gas.NitrousOxide, Gas.Nitryl, Gas.Hydrogen };
			line2 = new GasSO[] { Gas.Plasma, Gas.Tritium, Gas.Freon, Gas.Miasma, Gas.BZ };
			line3 = new GasSO[] { Gas.Pluoxium, Gas.Stimulum, Gas.HyperNoblium };
		}

		public void SetValues(GUI_Acu acuUi, Scrubber scrubber)
		{
			this.acuUi = acuUi;
			this.scrubber = scrubber;
			UpdateText();
		}

		public void UpdateText()
		{
			var onStr = scrubber.IsTurnedOn
					? $"{m}[ On ]{um} Off  "
					: $"  On {m}[ Off ]{um}";
			var modeStr = scrubber.OperatingMode == Scrubber.Mode.Scrubbing
					? $"{m}[ Scrubbing ]{um}   Siphoning  "
					: $"  Scrubbing   {m}[ Siphoning ]{um}";
			var rangeStr = scrubber.IsExpandedRange
					? $"   Normal     {m}[ Expanded  ]{um}"
					: $"{m}[  Normal   ]{um}   Expanded   ";

			List<StringBuilder> filteredGasLines = new List<StringBuilder>(3);
			foreach (GasSO[] line in new GasSO[][] { line1, line2, line3 })
			{
				var filteredGasesStr = new StringBuilder();
				foreach (GasSO gas in line)
				{
					string gasName = gasFormulaRT.ContainsKey(gas) ? gasFormulaRT[gas] : gas.Name;
					string filterStr = scrubber.FilteredGases.Contains(gas)
							? $"{m}{gasName}[*]{um} "
							: $"{gasName}[ ] ";
					filteredGasesStr.Append(filterStr);
				}
				filteredGasLines.Add(filteredGasesStr);
			}

			// "Scrubber - " as per NameValidator tool.
			string scrubberName = scrubber.gameObject.name;
			scrubberName = scrubberName.StartsWith("Scrubber - ") ? scrubberName.Substring("Scrubber - ".Length) : scrubberName;

			// We use blank subscripts to get back to correct monospacing (each subscript is 0.5 monospace units)
			string str =
					"---------------------------------------------------\n" +
					$"| {scrubberName, -35} {onStr}|\n" +
					"|                                                 |\n" +
					$"| Mode:       {modeStr}         |\n" +
					$"| Range:      {rangeStr}         |\n" +
					"|                                                 |\n" +
					$"| {filteredGasLines[0]}<sub> </sub>     |\n" + 
					$"| {filteredGasLines[1]}  |\n" +
					$"| {filteredGasLines[2]}       |\n" +
					"---------------------------------------------------\n";

			label.SetValueServer(str);
		}

		private void SetSetting(System.Action callback)
		{
			acuUi.PlayTap();
			if (acuUi.Acu.IsWriteable == false) return;

			callback.Invoke();

			UpdateText();
		}

		#region Buttons

		public void BtnSetTurnedOn(bool isOn)
		{
			SetSetting(() =>
			{
				scrubber.SetTurnedOn(isOn);
			});
		}

		public void BtnSetOperatingMode(int mode)
		{
			SetSetting(() =>
			{
				scrubber.SetOperatingMode((Scrubber.Mode)mode);
			});
		}

		public void BtnSetRange(bool isExpanded)
		{
			SetSetting(() =>
			{
				scrubber.SetExpandedRange(isExpanded);
			});
		}

		public void BtnToggleGasFilter(int gasIndex)
		{
			SetSetting(() =>
			{
				GasSO gas = Gas.Gases[gasIndex];
				if (scrubber.FilteredGases.Contains(gas))
				{
					scrubber.FilteredGases.Remove(gas);
				}
				else
				{
					scrubber.FilteredGases.Add(gas);
				}
			});
		}

		#endregion
	}
}
