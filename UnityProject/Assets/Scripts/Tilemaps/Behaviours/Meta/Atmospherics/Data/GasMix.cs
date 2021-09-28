using System;
using System.Collections.Generic;
using NaughtyAttributes;
using ScriptableObjects.Atmospherics;
using Systems.Pipes;


namespace Systems.Atmospherics
{
	/// <summary>
	/// Represents a mix of gases
	/// </summary>
	[Serializable]
	public class GasMix
	{
		[InfoBox("Gas data container", EInfoBoxType.Normal)]
		public GasData GasData;
		public GasValues[] GasesArray => GasData.GasesArray;

		/// <summary>In kPa.</summary>
		public float Pressure;
		/// <summary>In cubic metres.</summary>
		public float Volume;
		/// <summary>In Kelvin.</summary>
		public float Temperature;

		private HashSet<GasSO> cache = new HashSet<GasSO>();
		private HashSet<GasSO> pipeCache = new HashSet<GasSO>();

		public float Moles
		{
			get
			{
				float value = 0;
				foreach (var a in GasesArray)
				{
					value += a.Moles;
				}

				if (float.IsNaN(value))
				{
					return 0;
				}

				return value;
			}
		}

		public float WholeHeatCapacity //this is the heat capacity for the entire gas mixture, in Joules/Kelvin. gets very big with lots of gas.
		{
			get
			{
				float capacity = 0f;

				foreach (var gas in GasesArray)
				{
					capacity += gas.GasSO.MolarHeatCapacity * gas.Moles;
				}

				return capacity;
			}
		}

		public float InternalEnergy //This is forgetting the amount of energy inside of the Gas
		{
			get => (WholeHeatCapacity * Temperature);

			set
			{
				if (WholeHeatCapacity == 0)
				{
					Temperature = 0;
				}
				else
				{
					Temperature = (value / WholeHeatCapacity);
				}
			}
		}

		public GasMix(float volume = AtmosConstants.TileVolume, float temperature = Reactions.KOffsetC + 20)
		{
			GasData = new GasData();
			Volume = volume;
			Temperature = temperature;
		}

		/// <summary>
		/// Changes the pressure by the specified value
		/// </summary>
		/// <param name="changePressure">The change of the pressure, can be + or - </param>
		public void ChangePressure(float changePressure)
		{
			SetPressure(Temperature + changePressure);
		}

		/// <summary>
		/// Changes the volume by the specified value
		/// </summary>
		/// <param name="changeVolume">The change of the volume, can be + or - </param>
		public void ChangeVolumeValue(float changeVolume)
		{
			Volume += changeVolume;
			RecalculatePressure();
		}

		/// <summary>
		/// Changes the temperature by the specified value
		/// </summary>
		/// <param name="changeTemperature">The change of the temperature, can be + or - </param>
		public void ChangeTemperature(float changeTemperature)
		{
			SetTemperature(Temperature + changeTemperature);
		}

		public void SetTemperature(float newTemperature)
		{
			Temperature = newTemperature;
			RecalculatePressure();
		}

		public void SetPressure(float newPressure)
		{
			Pressure = newPressure;
			Temperature = AtmosUtils.CalcTemperature(Pressure, Volume, Moles);
		}

		private void RecalculatePressure()
		{
			Pressure = AtmosUtils.CalcPressure(Volume, Moles, Temperature);
		}

		/// <summary>
		/// Returns a clone of the specified gas mix.
		/// </summary>
		public static GasMix NewGasMix(GasMix other)
		{
			return FromPressure(other.GasData.Copy(), other.Pressure, other.Volume);
		}

		public static GasMix FromTemperature(GasData gases, float temperature, float volume = AtmosConstants.TileVolume)
		{
			float pressure = 0;

			if (temperature >= 0)
			{
				pressure = AtmosUtils.CalcPressure(volume, gases.Sum(), temperature);
			}

			return FromPressure(gases, pressure, volume);
		}

		public static GasMix FromPressure(GasData gases, float pressure,
			float volume = AtmosConstants.TileVolume)
		{
			var gaxMix = new GasMix();
			gaxMix.GasData = gases.Copy();
			gaxMix.Pressure = pressure;
			gaxMix.Volume = volume;
			gaxMix.Temperature = AtmosUtils.CalcTemperature(gaxMix.Pressure, gaxMix.Volume, gaxMix.GasData.Sum());
			return gaxMix;
		}

		/// <summary>
		/// Transfers moles from one gas to another
		/// </summary>
		public static void TransferGas(GasMix target, GasMix source, float molesToTransfer, bool DoNotTouchOriginalMix = false)
		{
			var sourceStartMoles = source.Moles;
			molesToTransfer = molesToTransfer.Clamp(0, sourceStartMoles);
			if (CodeUtilities.IsEqual(molesToTransfer, 0) || CodeUtilities.IsEqual(sourceStartMoles, 0))
				return;
			var ratio =  molesToTransfer / sourceStartMoles;
			var targetStartMoles = target.Moles;

			foreach (var gas in source.GasesArray)
			{
				if(gas.GasSO == null) continue;

				var sourceMoles = source.GetMoles(gas.GasSO);
				if (CodeUtilities.IsEqual(sourceMoles, 0)) continue;

				var transfer = sourceMoles * ratio;

				//Add to target
				target.GasData.ChangeMoles(gas.GasSO, transfer);

				if (DoNotTouchOriginalMix == false)
				{
					//Remove from source
					source.GasData.ChangeMoles(gas.GasSO, -transfer);
				}

			}

			if (CodeUtilities.IsEqual(target.Temperature, source.Temperature))
			{
				target.RecalculatePressure();
			}
			else
			{
				var energyTarget = targetStartMoles * target.Temperature;
				var energyTransfer = molesToTransfer * source.Temperature;
				var targetTempFinal = (energyTransfer + energyTarget) / (targetStartMoles + molesToTransfer);
				target.SetTemperature(targetTempFinal);
			}

			if (CodeUtilities.IsEqual(ratio, 1)) //transferred everything, source is empty
			{
				source.SetPressure(0);
			}
			else
			{
				source.RecalculatePressure();
			}
		}

		/// <summary>
		/// Source and target gas mixes interchange their moles
		/// </summary>
		/// <param name="otherGas"></param>
		public GasMix MergeGasMix(GasMix otherGas)
		{
			var totalInternalEnergy = InternalEnergy + otherGas.InternalEnergy;
			var totalWholeHeatCapacity = WholeHeatCapacity + otherGas.WholeHeatCapacity;
			var newTemperature = totalWholeHeatCapacity > 0 ? totalInternalEnergy / totalWholeHeatCapacity : 0;
			var totalVolume = Volume + otherGas.Volume;

			foreach (var gas in GasesArray)
			{
				var gasMoles = GasData.GetGasMoles(gas.GasSO);
				gasMoles += otherGas.GasData.GetGasMoles(gas.GasSO);
				gasMoles /= totalVolume;

				cache.Add(gas.GasSO);

				GasData.SetMoles(gas.GasSO, gasMoles * Volume);
				otherGas.GasData.SetMoles(gas.GasSO, gasMoles * otherGas.Volume);
			}

			foreach (var gas in otherGas.GasesArray)
			{
				//Check if already merged
				if(cache.Contains(gas.GasSO)) continue;

				var gasMoles = GasData.GetGasMoles(gas.GasSO);
				gasMoles += otherGas.GasData.GetGasMoles(gas.GasSO);
				gasMoles /= totalVolume;

				GasData.SetMoles(gas.GasSO, gasMoles * Volume);
				otherGas.GasData.SetMoles(gas.GasSO, gasMoles * otherGas.Volume);
			}

			//Clear for next use
			cache.Clear();

			SetTemperature(newTemperature);
			otherGas.SetTemperature(newTemperature);
			return otherGas;
		}

		public void MultiplyGas(float factor)
		{
			for (int i = 0; i < GasesArray.Length; i++)
			{
				GasesArray[i].Moles *= factor;
			}

			SetPressure(Pressure * factor);
		}

		public void SetToEmpty()
		{
			MultiplyGas(0);
		}

		public float GetPressure(GasSO gas)
		{
			if (Moles.Approx(0)) return 0;

			return Pressure * (GetMoles(gas) / Moles);
		}

		public float GetMoles(GasSO gas)
		{
			return GasData.GetGasMoles(gas);
		}

		/// <summary>
		/// Returns the gas as a percentage of the gas in the mix
		/// </summary>
		/// <returns>The ratio of the gas</returns>
		/// <param name="gasIndex">Gas index.</param>
		public float GasRatio(GasSO gasIndex)
		{
			return GetMoles(gasIndex) / Moles;
		}

		/// <summary>
		///  Ensures that all containers have the same pressure
		/// </summary>
		/// <param name="otherGas"></param>
		public void MergeGasMixes(List<PipeData> otherGas)
		{
			float totalVolume = Volume;
			float totalInternalEnergy = InternalEnergy;
			float totalWholeHeatCapacity = WholeHeatCapacity;
			foreach (var gasMix in otherGas)
			{
				totalInternalEnergy += PipeFunctions.PipeOrNet(gasMix).GetGasMix().InternalEnergy;
				totalWholeHeatCapacity += PipeFunctions.PipeOrNet(gasMix).GetGasMix().WholeHeatCapacity;
				totalVolume += PipeFunctions.PipeOrNet(gasMix).GetGasMix().Volume;
			}

			var newTemperature = totalInternalEnergy / totalWholeHeatCapacity;

			//First do the gases in THIS gas mix and merge them with all pipes
			foreach (var gas in GasesArray)
			{
				var gasMoles = GasData.GetGasMoles(gas.GasSO);

				foreach (var gasMix in otherGas)
				{
					gasMoles += PipeFunctions.PipeOrNet(gasMix).GetGasMix().GasData.GetGasMoles(gas.GasSO);
				}

				gasMoles /= totalVolume;
				GasData.SetMoles(gas.GasSO, gasMoles * Volume);

				foreach (var gasMix in otherGas)
				{
					var inGas = PipeFunctions.PipeOrNet(gasMix).GetGasMix();
					inGas.GasData.SetMoles(gas.GasSO, gasMoles * inGas.Volume);
					PipeFunctions.PipeOrNet(gasMix).SetGasMix(inGas);
				}

				//Since we've done the merge of THIS gas mix and pipes for the gases in THIS mix
				//we only need to do gases which are present in pipes but not in THIS gas mix, so we add the current gas to the block hashset
				pipeCache.Add(gas.GasSO);
			}

			//Next loop through all pipes
			foreach (var gasMix in otherGas)
			{
				//Loop through their contained gases
				foreach (var gas in PipeFunctions.PipeOrNet(gasMix).GetGasMix().GasData.GasesArray)
				{
					//Only do gases we haven't done yet
					if(pipeCache.Contains(gas.GasSO)) continue;

					//We DONT add THIS Gas Mix moles value as it will be 0 as all gases (0 >) in THIS as they were already
					//merged in the first merge loop
					var gasMoles = 0f;

					foreach (var gasPipeMix in otherGas)
					{
						gasMoles += PipeFunctions.PipeOrNet(gasPipeMix).GetGasMix().GasData.GetGasMoles(gas.GasSO);
					}

					gasMoles /= totalVolume;

					//We do however still need to merge it back into THIS gas mix
					GasData.SetMoles(gas.GasSO, gasMoles * Volume);

					//Merge to all pipes
					foreach (var gasPipeMix in otherGas)
					{
						var inGas = PipeFunctions.PipeOrNet(gasPipeMix).GetGasMix();
						inGas.GasData.SetMoles(gas.GasSO, gasMoles * inGas.Volume);
						PipeFunctions.PipeOrNet(gasPipeMix).SetGasMix(inGas);
					}

					//We add the gas we've done as we've merged to THIS mix and all the other pipes
					pipeCache.Add(gas.GasSO);

					//Now we continue checking for gases in the other pipes which still need to be merged
				}
			}

			//Clear for next use
			pipeCache.Clear();

			SetTemperature(newTemperature);
			foreach (var pipeData in otherGas)
			{
				var getMixAndVolume = pipeData.GetMixAndVolume;
				var gasMix = getMixAndVolume.GetGasMix();
				gasMix.SetTemperature(newTemperature);
				getMixAndVolume.SetGasMix(gasMix);
			}
		}

		/// <summary>
		/// Set the moles value of a gas inside of a GasMix.
		/// </summary>
		/// <param name="gas">The gas you want to set.</param>
		/// <param name="moles">The amount to set the gas.</param>
		public void SetGas(GasSO gas, float moles)
		{
			GasData.SetMoles(gas, moles);
			RecalculatePressure();
		}

		public void AddGas(GasSO gas, float moles)
		{
			GasData.ChangeMoles(gas, moles);
			RecalculatePressure();
		}

		public void RemoveGas(GasSO gas, float moles)
		{
			GasData.ChangeMoles(gas, -moles);
			RecalculatePressure();
		}

		public void Copy(GasMix other)
		{
			GasData = other.GasData.Copy();
			Pressure = other.Pressure;
			Temperature = other.Temperature;
			Volume = other.Volume;
		}

		public override string ToString()
		{
			return $"{Pressure} kPA, {Temperature} K, {Moles} mol, {Volume}m^3 ";
		}

		public void Clear()
		{
			Temperature = AtmosDefines.SPACE_TEMPERATURE;

			GasData.GasesArray = new GasValues[0];
			GasData.GasesDict.Clear();
			Pressure = 0;
			Volume = 0;
		}
	}
}
