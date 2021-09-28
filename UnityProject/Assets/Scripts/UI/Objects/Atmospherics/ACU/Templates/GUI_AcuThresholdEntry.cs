using System.Text;
using UnityEngine;
using Objects.Atmospherics;
using ScriptableObjects.Atmospherics;

namespace UI.Objects.Atmospherics.Acu
{
	/// <summary>
	/// An entry for the <see cref="GUI_AcuThresholdsPage"/>.
	/// Allows the peeper to view and configure the thresholds
	/// the <see cref="AirController"/> uses to determine local air quality status.
	/// </summary>
	public class GUI_AcuThresholdEntry : DynamicEntry
	{
		[SerializeField]
		private NetLabel label = default;

		private GUI_AcuThresholdsPage thresholdsPage;

		public int Index { get; private set; }
		public ThresholdType Type { get; private set; }
		public string Name { get; private set; }
		public float[] Values { get; private set; }
		public GasSO Gas { get; private set; }

		public void SetValues(
				GUI_AcuThresholdsPage thresholdsPage, int index, ThresholdType type,
				string thresholdName, float[] values, GasSO gas = default)
		{
			this.thresholdsPage = thresholdsPage;
			Index = index;
			Type = type;
			Name = thresholdName;
			Values = values;
			Gas = gas;

			if (type == ThresholdType.Linebreak)
			{
				label.SetValueServer(string.Empty);
				return;
			}

			StringBuilder sb = new StringBuilder($"{thresholdName, -14} | ", 55);
			foreach (float value in values)
			{
				// Use a '?' to represent a gas for which the ACU thresholds does not have values for.
				// Allows a technician to set appropriate values for this discovered gas.
				sb.Append($"{(float.IsNaN(value) ? "?" : value.ToString()), -7} | ");
			}
			label.SetValueServer(sb.Remove(sb.Length - 3, 3).ToString());
		}

		#region Buttons

		public void BtnSetThreshold(int thresholdIndex)
		{
			if (Type == ThresholdType.Linebreak) return;

			thresholdsPage.AcuUi.PlayTap();
			thresholdsPage.SetThreshold(this, thresholdIndex);
		}

		#endregion
	}
}
