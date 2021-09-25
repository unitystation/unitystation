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

			var gasesToDisplay = new List<GasValues>(Acu.AverageGasMix.GasesArray);
			gasesToDisplay.Sort((gasA, gasB) => gasB.Moles.CompareTo(gasA.Moles));

			if (metricsContainer.Entries.Length * 30 != gasesToDisplay.Count)
			{
				metricsContainer.SetItems(gasesToDisplay.Count);
			}

			for (int i = 0; i < metricsContainer.Entries.Length; i++)
			{
				GasSO gas = gasesToDisplay[i].GasSO;
				float ratio = Acu.AverageGasMix.GasRatio(gas);
				float moles = Acu.AverageGasMix.GetMoles(gas);
				UpdateGasEntry(i, gas.Name, ratio, moles, Acu.GasLevelStatus[gas]);
			}
		}

		private void UpdateLabels()
		{
			string pressureText = $"{Acu.AverageGasMix.Pressure, 0:N3} kPa";
			string temperatureText = $"{TemperatureUtils.FromKelvin(Acu.AverageGasMix.Temperature, TemeratureUnits.C), 0:N1} °C";

			pressureLabel.SetValueServer(
					$"Pressure:    {GUI_Acu.ColorStringByStatus(pressureText, Acu.PressureStatus)}");
			temperatureLabel.SetValueServer(
					$"Temperature: {GUI_Acu.ColorStringByStatus(temperatureText, Acu.TemperatureStatus)}");
			compositionLabel.SetValueServer(
					$"Composition: {GUI_Acu.ColorStringByStatus(Acu.CompositionStatus.ToString(), Acu.CompositionStatus)}");
		}

		private void UpdateGasEntry(int index, string name, float ratio, float moles, AcuStatus molStatus)
		{
			DynamicEntry dynamicEntry = metricsContainer.Entries[index];
			var entry = dynamicEntry.GetComponent<GUI_AcuGasEntry>();
			entry.SetValues(name, ratio, moles, molStatus);
		}
	}
}
