using System.Collections.Generic;
using UnityEngine;
using Systems.Atmospherics;
using ScriptableObjects.Atmospherics;
using Objects.Atmospherics;


namespace UI.Objects.Atmospherics.Acu
{
	/// <summary>
	/// Allows the peeper to view the local air quality as reported by the <see cref="AirController"/>.
	/// </summary>
	public class GUI_AcuOverviewPage : GUI_AcuPage
	{
		[SerializeField]
		private NetLabel modeLabel = default;
		[SerializeField]
		private NetLabel pressureLabel = default;
		[SerializeField]
		private NetLabel temperatureLabel = default;
		[SerializeField]
		private NetLabel compositionLabel = default;

		[SerializeField]
		private EmptyItemList metricsContainer = default;

		public override void OnPageDeactivated()
		{
			metricsContainer.Clear();
		}

		public override void OnPeriodicUpdate()
		{
			modeLabel.SetValueServer($"Mode: {Acu.DesiredMode}");
			UpdateLabels();

			var gasesToDisplay = new List<GasValues>(Acu.AtmosphericAverage.GetGases());
			gasesToDisplay.Sort((gasA, gasB) => gasB.Moles.CompareTo(gasA.Moles));

			if (metricsContainer.Entries.Length != gasesToDisplay.Count)
			{
				metricsContainer.SetItems(gasesToDisplay.Count);
			}

			for (int i = 0; i < metricsContainer.Entries.Length; i++)
			{
				GasSO gas = gasesToDisplay[i].GasSO;
				float ratio = Acu.AtmosphericAverage.GetGasRatio(gas);
				float moles = Acu.AtmosphericAverage.GetGasMoles(gas);
				UpdateGasEntry(i, gas.Name, ratio, moles, Acu.GasLevelStatus[gas]);
			}
		}

		private void UpdateLabels()
		{
			string pressureText = "? kPa";
			string temperatureText = "? °C";
			string compositionText = "Unknown";

			if (Acu.AtmosphericAverage.SampleSize > 0)
			{
				pressureText = $"{Acu.AtmosphericAverage.Pressure, 0:N3} kPa";
				temperatureText = $"{TemperatureUtils.FromKelvin(Acu.AtmosphericAverage.Temperature, TemeratureUnits.C), 0:N1} °C";
				compositionText = Acu.CompositionStatus.ToString();
			}

			pressureLabel.SetValueServer(
					$"Pressure:    {GUI_Acu.ColorStringByStatus(pressureText, Acu.PressureStatus)}");
			temperatureLabel.SetValueServer(
					$"Temperature: {GUI_Acu.ColorStringByStatus(temperatureText, Acu.TemperatureStatus)}");
			compositionLabel.SetValueServer(
					$"Composition: {GUI_Acu.ColorStringByStatus(compositionText, Acu.CompositionStatus)}");
		}

		private void UpdateGasEntry(int index, string name, float ratio, float moles, AcuStatus molStatus)
		{
			DynamicEntry dynamicEntry = metricsContainer.Entries[index];
			var entry = dynamicEntry.GetComponent<GUI_AcuGasEntry>();
			entry.SetValues(name, ratio, moles, molStatus);
		}
	}
}
