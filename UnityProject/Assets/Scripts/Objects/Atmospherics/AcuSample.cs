using System.Collections.Generic;
using System.Linq;
using ScriptableObjects.Atmospherics;
using Systems.Atmospherics;


namespace Objects.Atmospherics
{
	/// <summary>
	/// A single sample of the atmosphere for pressure, temperature and gas moles.
	/// <remarks>See also related classes:<list type="bullet">
	/// <item><description><seealso cref="AcuSampleAverage"/> represents an atmospheric average of <c>ACU</c> samples</description></item>
	/// <item><description><seealso cref="AirController"/> Monitors the local atmosphere and controls related devices</description></item>
	/// <item><description><seealso cref="AcuDevice"/> allows a connection to form between the <c>ACU</c> and devices</description></item>
	/// </list></remarks>
	/// </summary>
	public class AcuSample
	{
		/// <summary>In kPa.</summary>
		public float Pressure = 0;
		/// <summary>In Kelvin.</summary>
		public float Temperature = 0;
		public Dictionary<GasSO, float> GasMoles = new Dictionary<GasSO, float>();

		/// <summary>
		/// Set the sample values based on the provided <see cref="GasMix"/>.
		/// </summary>
		public AcuSample FromGasMix(GasMix mix)
		{
			Clear();

			Pressure = mix.Pressure;
			Temperature = mix.Temperature;
			foreach (GasValues gas in mix.GasesArray)
			{
				GasMoles.Add(gas.GasSO, gas.Moles);
			}

			return this;
		}

		public AcuSample Clear()
		{
			Pressure = 0;
			Temperature = 0;
			GasMoles.Clear();

			return this;
		}
	}

	/// <summary>
	/// A single sample of the atmosphere for pressure, temperature and gas moles.
	/// <remarks>See also related classes:<list type="bullet">
	/// <item><description><seealso cref="AcuSample"/> represents a single sample of the atmosphere</description></item>
	/// <item><description><seealso cref="AirController"/> Monitors the local atmosphere and controls related devices</description></item>
	/// <item><description><seealso cref="AcuDevice"/> allows a connection to form between the <c>ACU</c> and devices</description></item>
	/// </list></remarks>
	/// </summary>
	public class AcuSampleAverage
	{
		/// <summary>In kPa.</summary>
		public float Pressure => SampleSize == 0 ? 0 : pressureSum / SampleSize;
		/// <summary>In Kelvin.</summary>
		public float Temperature => SampleSize == 0 ? 0 : temperatureSum / SampleSize;

		private float pressureSum = 0;
		private float temperatureSum = 0;
		private Dictionary<GasSO, float> gasMolesSum = new Dictionary<GasSO, float>();

		public int SampleSize { get; private set; } = 0;

		public bool HasGas(GasSO gas)
		{
			return gasMolesSum.ContainsKey(gas);
		}

		public float GetGasMoles(GasSO gas)
		{
			return SampleSize == 0 ? 0 : gasMolesSum[gas] / SampleSize;
		}

		/// <summary>
		/// Gets the ratio, as a decimal, of the moles for the given gas in the atmospheric average.
		/// </summary>
		/// <returns>(decimal) moles ratio</returns>
		public float GetGasRatio(GasSO gas)
		{
			var sum = gasMolesSum.Sum(kvp => kvp.Value);

			return sum == 0 ? 0 : gasMolesSum[gas] / sum;
		}

		/// <summary>
		/// Gets a collection of all detected gases in the atmopsheric average.
		/// </summary>
		/// <returns></returns>
		public IEnumerable<GasValues> GetGases()
		{
			foreach (var kvp in gasMolesSum)
			{
				var gasValue = new GasValues();
				gasValue.GasSO = kvp.Key;
				gasValue.Moles = SampleSize == 0 ? 0 : kvp.Value / SampleSize;
				yield return gasValue;
			}
		}

		/// <summary>
		/// Adds a sample to the average. The new average calculation completes on value retrieval.
		/// </summary>
		public void AddSample(AcuSample sample)
		{
			pressureSum += sample.Pressure;
			temperatureSum += sample.Temperature;

			foreach (var kvp in sample.GasMoles)
			{
				gasMolesSum[kvp.Key] = gasMolesSum.TryGetValue(kvp.Key, out float moles) ? moles + kvp.Value : kvp.Value;
			}

			SampleSize++;
		}

		public void Clear()
		{
			pressureSum = 0;
			temperatureSum = 0;
			gasMolesSum.Clear();

			SampleSize = 0;
		}
	}
}
