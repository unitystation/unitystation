using System;
using System.Collections.Generic;
using Core.Utils;
using Logs;
using NaughtyAttributes;
using Objects.Atmospherics;
using ScriptableObjects.Atmospherics;
using Systems.Pipes;
using UnityEngine;
using UnityEngine.Serialization;


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

		public List<GasValues> GasesArray => GasData.GasesArray;

		[SerializeField, FormerlySerializedAs("Pressure")]
		private float pressure;

		/// <summary>In kPa.</summary>
		public float Pressure
		{
			get => pressure;
			set
			{
				if (float.IsNormal(value) == false && value != 0)
				{
					Loggy.LogError($"AAAAAAAAAAAAA REEEEEEEEE pressure Invalid number!!!! {value}");
					return;
				}

				pressure = Math.Clamp(value, 0, Single.MaxValue);
			}
		}

		/// <summary>In cubic metres.</summary>
		public float Volume;

		[SerializeField, FormerlySerializedAs("Temperature")]
		private float temperature;
		/// <summary>In Kelvin.</summary>
		public float Temperature
		{
			get { return temperature; }
			set
			{
				if (float.IsNormal(value) == false && value != 0)
				{
					Loggy.LogError($"AAAAAAAAAAAAA REEEEEEEEE Temperature Invalid number!!!! {value}");
					return;
				}

				temperature = Math.Clamp(value, 0, Single.MaxValue);
			}
		}


		private HashSet<GasSO> cache = new HashSet<GasSO>();
		private HashSet<GasSO> pipeCache = new HashSet<GasSO>();

		public float Moles
		{
			get
			{
				float value = 0;
				lock (GasesArray)
				{
					foreach (var a in GasesArray) //doesn't appear to modify list while iterating
					{
						value += a.Moles;
					}
				}

				if (float.IsNaN(value))
				{
					return 0;
				}

				return value;
			}
		}

		public float WholeHeatCapacity
			//this is the heat capacity for the entire gas mixture, in Joules/Kelvin. gets very big with lots of gas.
		{
			get
			{
				float capacity = 0f;

				lock (GasesArray)
				{
					foreach (var gas in GasesArray) //doesn't appear to modify list while iterating
					{
						capacity += gas.GasSO.MolarHeatCapacity * gas.Moles;
					}
				}


				return capacity;
			}
		}

		public float InternalEnergy //This is forgetting the amount of energy inside of the Gas
		{
			get => (WholeHeatCapacity * Temperature);

			set
			{
				if (float.IsNormal(value) == false && value != 0)
				{
					Loggy.LogError($"AAAAAAAAAAAAA REEEEEEEEE InternalEnergy Invalid number!!!! {value}");
					return;
				}

				if (WholeHeatCapacity == 0)
				{
					Temperature = 0;
				}
				else
				{
					Temperature = (value / WholeHeatCapacity);
				}
				SetTemperature(Temperature);
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

		public static GasMix FromTemperatureAndPressure(GasData gases ,  float temperature,float pressure, float volume = AtmosConstants.TileVolume)
		{
			var NeededMoles = AtmosUtils.CalcMoles(pressure, volume, temperature);
			var ActualMoles = 0f;
			foreach (var GV in gases.GasesArray)
			{
				ActualMoles += GV.Moles;
			}

			if (Mathf.Approximately(ActualMoles, 0))
			{
				Loggy.LogError("Inappropriate Input for FromTemperatureAndPressure ", Category.Atmos);
				return GasMixesSingleton.Instance.air.BaseGasMix;
			}

			var multiplier = NeededMoles / ActualMoles;
			var Copygases = gases.Copy();
			foreach (var GV in Copygases.GasesArray)
			{
				GV.Moles *= multiplier;
			}

			var gaxMix = new GasMix();
			gaxMix.GasData = Copygases;
			gaxMix.Pressure = pressure;
			gaxMix.Volume = volume;
			gaxMix.Temperature = temperature;
			return gaxMix;
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
		public static void TransferGas(GasMix target, GasMix source, float molesToTransfer,
			bool doNotTouchOriginalMix = false)
		{
			if (target == source)
			{
				Loggy.LogError("oh god You're transferring a gas mixture itself!!!");
				return;
			}

			var sourceStartMoles = source.Moles;
			molesToTransfer = molesToTransfer.Clamp(0, sourceStartMoles);
			if (MathUtils.IsEqual(molesToTransfer, 0) || MathUtils.IsEqual(sourceStartMoles, 0))
				return;
			var ratio = molesToTransfer / sourceStartMoles;
			var targetStartMoles = target.Moles;

			var Listsource = AtmosUtils.CopyGasArray(source.GasData);

			for (int i = Listsource.List.Count - 1; i >= 0; i--)
			{
				var gas = Listsource.List[i];
				if (gas.GasSO == null) continue;

				var sourceMoles = source.GetMoles(gas.GasSO);
				if (MathUtils.IsEqual(sourceMoles, 0)) continue;

				var transfer = sourceMoles * ratio;

				//Add to target
				target.GasData.ChangeMoles(gas.GasSO, transfer);

				if (doNotTouchOriginalMix == false)
				{
					//Remove from source
					source.GasData.ChangeMoles(gas.GasSO, -transfer);
				}
			}


			Listsource.Pool();

			if (MathUtils.IsEqual(target.Temperature, source.Temperature))
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

			if (doNotTouchOriginalMix == false)
			{
				if (MathUtils.IsEqual(ratio, 1)) //transferred everything, source is empty
				{
					source.SetPressure(0);
				}
				else
				{
					source.RecalculatePressure();
				}
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

			var GasesArrayCopy = AtmosUtils.CopyGasArray(this.GasData);

			for (int i = GasesArrayCopy.List.Count - 1; i >= 0; i--)
			{
				var gas = GasesArrayCopy.List[i];
				var gasMoles = GasData.GetGasMoles(gas.GasSO);
				gasMoles += otherGas.GasData.GetGasMoles(gas.GasSO);
				gasMoles /= totalVolume;

				cache.Add(gas.GasSO);

				GasData.SetMoles(gas.GasSO, gasMoles * Volume);
				otherGas.GasData.SetMoles(gas.GasSO, gasMoles * otherGas.Volume);
			}

			GasesArrayCopy.Pool();


			var otherGasGasesArrayCopy = AtmosUtils.CopyGasArray(otherGas.GasData);


			for (int i = otherGasGasesArrayCopy.List.Count - 1; i >= 0; i--)
			{
				var gas = otherGasGasesArrayCopy.List[i];
				//Check if already merged
				if (cache.Contains(gas.GasSO)) continue;

				var gasMoles = GasData.GetGasMoles(gas.GasSO);
				gasMoles += otherGas.GasData.GetGasMoles(gas.GasSO);
				gasMoles /= totalVolume;

				GasData.SetMoles(gas.GasSO, gasMoles * Volume);
				otherGas.GasData.SetMoles(gas.GasSO, gasMoles * otherGas.Volume);
			}


			otherGasGasesArrayCopy.Pool();

			//Clear for next use
			cache.Clear();

			SetTemperature(newTemperature);
			otherGas.SetTemperature(newTemperature);
			return otherGas;
		}

		public void MultiplyGas(float factor)
		{
			lock (GasesArray)
			{
				for (int i = 0; i < GasesArray.Count; i++)
				{
					GasesArray[i].Moles *= factor;
				}
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

			var newTemperature = 0f;
			if (totalWholeHeatCapacity != 0)
			{
				newTemperature = totalInternalEnergy / totalWholeHeatCapacity;
			}


			var List = AtmosUtils.CopyGasArray(this.GasData);


			//First do the gases in THIS gas mix and merge them with all pipes
			for (int i = List.List.Count - 1; i >= 0; i--)
			{
				var gas = List.List[i];
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

			List.Pool();


			//Next loop through all pipes
			foreach (var gasMix in otherGas)
			{
				//Loop through their contained gases
				var InGasesArray = AtmosUtils.CopyGasArray(PipeFunctions.PipeOrNet(gasMix).GetGasMix().GasData);


				foreach (var gas in InGasesArray.List)
				{
					//Only do gases we haven't done yet
					if (pipeCache.Contains(gas.GasSO)) continue;

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
				InGasesArray.Pool();
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
		/// Gets the biggest gas's type in the GasMix Currently
		/// </summary>
		/// <returns>Biggest gas's GasSO</returns>
		public GasSO GetBiggestGasSOInMix()
		{
			//If there are no gasses, return null
			if(GasData.GasesArray.Count == 0)
			{
				return null;
			}

			GasSO bigGas = GasData.GasesArray[0].GasSO; //Get the first GasSO in the array
			float lastBigNumber = 0f; //The last big number of moles detected in the array
			foreach (var gas in GasData.GasesArray)
			{
				if (gas.Moles < lastBigNumber) continue; //if the moles is less than the last big mole number, skip
				bigGas = gas.GasSO;
				lastBigNumber = gas.Moles; //Rememeber the last big number checked to skip smaller numbers in the next entry
			}
			return bigGas; //The returned GasSO will be the gas with the biggest mole count in GasData.GasesArray
		}


		public static GasMix GetEnvironmentalGasMixForObject(UniversalObjectPhysics gameObject)
		{
			GasMix ambientGasMix;
			if (gameObject.ContainedInObjectContainer != null && //Make generic function
			    gameObject.ContainedInObjectContainer.TryGetComponent<GasContainer>(out var gasContainer))
			{
				ambientGasMix = gasContainer.GasMix;
			}
			else
			{
				var matrix = gameObject.registerTile.Matrix;
				Vector3Int localPosition = MatrixManager.WorldToLocalInt(gameObject.OfficialPosition, matrix);
				ambientGasMix = matrix.MetaDataLayer.Get(localPosition).GasMix;
			}

			return ambientGasMix;
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

		public void AddGasWithTemperature(GasSO gas, float moles, float kelvinTemperature)
		{
			AddGas(gas, moles, gas.MolarHeatCapacity * moles * kelvinTemperature);
		}



		public void AddGas(GasSO gas, float moles, float energyOfAddedGas)
		{
			var newInternalenergy = InternalEnergy + energyOfAddedGas;
			GasData.ChangeMoles(gas, moles);
			InternalEnergy = newInternalenergy;
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

		/// <summary>
		///
		/// </summary>
		/// <param name="gas"></param>
		/// <param name="moles">  Warning!!!! This will have incorrect results if you take more moles than is in the container </param>
		/// <returns></returns>
		public float TakeGasReturnEnergy(GasSO gas, float moles)
		{
			var energyOfTakingGaslEnergy = moles * gas.MolarHeatCapacity * Temperature;
			GasData.ChangeMoles(gas, -moles);
			RecalculatePressure();
			return energyOfTakingGaslEnergy;
		}

		public void CopyFrom(GasMix other)
		{
			other.GasData.CopyTo(GasData);
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
			Temperature = 0;

			GasData.Clear();
			Pressure = 0;
			Volume = 0;
		}
	}
}