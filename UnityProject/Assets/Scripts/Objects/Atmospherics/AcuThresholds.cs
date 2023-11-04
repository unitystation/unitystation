using System;
using UnityEngine;
using NaughtyAttributes;
using Newtonsoft.Json;
using ScriptableObjects.Atmospherics;

namespace Objects.Atmospherics
{
	/// <summary>
	/// Stores threshold values by which an <seealso cref="AirController"/> can determine air quality.
	/// <para>Threshold values are in the order of
	/// <c>AlertMin</c>, <c>CautionMin</c>, <c>CautionMax</c>, <c>AlertMax</c>.</para>
	/// </summary>
	[Serializable]
	public class AcuThresholds
	{
		[InfoBox("Threshold values are in the order of AlertMin, CautionMin, CautionMax, AlertMax.")]

		[Tooltip("In kPa.")]
		public float[] Pressure = new float[4]
		{
			AtmosConstants.HAZARD_LOW_PRESSURE,
			AtmosConstants.WARNING_LOW_PRESSURE,
			AtmosConstants.WARNING_HIGH_PRESSURE,
			AtmosConstants.HAZARD_HIGH_PRESSURE,
		};

		[Tooltip("In Kelvin.")]
		public float[] Temperature = new float[4] { 273.15f, 283.15f, 313.15f, 339.15f };

		[Tooltip("In moles per tile.")]
		public GasThresholdsDictionary GasMoles;

		/// <summary> Useful for newly-added gases where thresholds are unknown and should be set by the technician. </summary>
		public static readonly float[] UnknownValues = { float.NaN, float.NaN, float.NaN, float.NaN };

		public AcuThresholds Clone()
		{
			var data = new GasThresholdsDictionary();
				data.CopyFrom(this.GasMoles);

			return new AcuThresholds()
			{
				Pressure = (float[]) this.Pressure.Clone(),
				Temperature = (float[]) this.Temperature.Clone(),
				GasMoles = data,
			};
		}
	}

	[Serializable]
	public class GasThresholdsStorage : SerializableDictionary.Storage<float[]> { }

	/// <summary>
	/// Serializable dictionary to map a <see cref="GasSO"/> to an array of thresholds.
	/// </summary>
	[Serializable]
	public class GasThresholdsDictionary : SerializableDictionary<GasSO, float[], GasThresholdsStorage> { }
}
