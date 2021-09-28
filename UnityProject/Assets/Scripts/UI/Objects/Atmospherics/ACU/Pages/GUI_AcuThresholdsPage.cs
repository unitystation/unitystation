using System.Linq;
using UnityEngine;
using Objects.Atmospherics;
using ScriptableObjects.Atmospherics;


namespace UI.Objects.Atmospherics.Acu
{
	/// <summary>
	/// The type of threshold. Each type is handled slightly differently.
	/// </summary>
	public enum ThresholdType
	{
		Linebreak = 0,
		Pressure = 1,
		Temperature = 2,
		Gas = 3,
	}

	/// <summary>
	/// Allows the peeper to manage the <see cref="AcuThresholds"/>
	/// the <see cref="AirController"/> uses to determine air quality status.
	/// </summary>
	public class GUI_AcuThresholdsPage : GUI_AcuPage
	{
		[SerializeField]
		private EmptyItemList thresholdsContainer = default;

		public override bool IsProtected => true;

		// The threshold list is dynamic, so keep the UI on its toes.
		public override void OnPeriodicUpdate()
		{
			PopulateThresholds();
		}

		/// <summary>
		/// Opens a modal for the peeper to edit the entry's given threshold.
		/// </summary>
		public void SetThreshold(GUI_AcuThresholdEntry entry, int thresholdIndex)
		{
			float oldValue = entry.Values[thresholdIndex];
			AcuUi.EditValueModal.Open(float.IsNaN(oldValue) ? "?" : oldValue.ToString(), (string stringValue) =>
			{
				if (float.TryParse(stringValue, out float newValue) == false) return;
				if (AcuUi.Acu.IsWriteable == false) return;

				newValue = newValue.Clamp(-1, 1000);
				switch (entry.Type)
				{
					case ThresholdType.Pressure:
						AcuUi.Acu.Thresholds.Pressure[thresholdIndex] = newValue;
						break;
					case ThresholdType.Temperature:
						AcuUi.Acu.Thresholds.Temperature[thresholdIndex] = newValue;
						break;
					case ThresholdType.Gas:
						AcuUi.Acu.Thresholds.GasMoles[entry.Gas][thresholdIndex] = newValue;
						break;
				}

				UpdateThresholdEntry(entry.Index, entry.Type, entry.Name, entry.Values, entry.Gas);
			});
		}

		private void PopulateThresholds()
		{
			if (thresholdsContainer.Entries.Length != Acu.Thresholds.GasMoles.Count + 3)
			{
				thresholdsContainer.SetItems(Acu.Thresholds.GasMoles.Count + 3);
			}
			
			UpdateThresholdEntry(0, ThresholdType.Pressure, "Pressure", Acu.Thresholds.Pressure, default);
			UpdateThresholdEntry(1, ThresholdType.Temperature, "Temperature", Acu.Thresholds.Temperature, default);
			UpdateThresholdEntry(2, ThresholdType.Linebreak, default, default, default);

			for (int i = 3; i < thresholdsContainer.Entries.Length; i++)
			{
				var gas = Acu.Thresholds.GasMoles.Keys.ElementAt(i - 3);
				var values = Acu.Thresholds.GasMoles.Values.ElementAt(i - 3);

				UpdateThresholdEntry(i, ThresholdType.Gas, gas.Name, values, gas);
			}
		}

		private void UpdateThresholdEntry(int index, ThresholdType type, string thresholdName, float[] values, GasSO gas)
		{
			DynamicEntry dynamicEntry = thresholdsContainer.Entries[index];
			var entry = dynamicEntry.GetComponent<GUI_AcuThresholdEntry>();
			entry.SetValues(this, index, type, thresholdName, values, gas);
		}

		#region Buttons

		public void BtnResetThresholds()
		{
			AcuUi.PlayTap();
			Acu.ResetThresholds();
			PopulateThresholds();
		}

		#endregion
	}
}
