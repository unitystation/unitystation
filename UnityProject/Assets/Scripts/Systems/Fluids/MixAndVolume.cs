using System;
using System.Collections.Generic;
using UnityEngine;
using Core.Utils;
using ScriptableObjects.Atmospherics;
using Chemistry;
using Logs;
using Systems.Atmospherics;


namespace Systems.Pipes
{
	[Serializable]
	public class MixAndVolume
	{
		private float Volume => gasMix.Volume;
		[SerializeField] private ReagentMix mix = new ReagentMix();
		[SerializeField] private GasMix gasMix = new GasMix();

		public float InternalEnergy
		{
			get => mix.InternalEnergy + gasMix.InternalEnergy;
			set
			{

				if (float.IsNormal(value) == false && value != 0)
				{
					Loggy.LogError($"AAAAAAAAAAAAA REEEEEEEEE MixAndVolume InternalEnergy Invalid number!!!! {value}");
					return;
				}

				var InWholeHeatCapacity = WholeHeatCapacity;
				if (MathUtils.IsEqual(InWholeHeatCapacity, 0))
				{
					mix.Temperature = 0;
					gasMix.SetTemperature(0);
					return;
				}

				var temperature = (value / InWholeHeatCapacity);
				mix.Temperature = temperature;
				gasMix.SetTemperature(temperature);
			}
		}

		public float TheVolume => Volume;

		public float Temperature
		{
			get
			{
				if (MathUtils.IsEqual(WholeHeatCapacity, 0))
				{
					return 0;
				}
				else
				{
					return Mathf.Clamp(InternalEnergy / WholeHeatCapacity, 0, Single.MaxValue);
				}
			}
			set
			{
				if (float.IsNormal(value) == false && value != 0)
				{
					Loggy.LogError($"AAAAAAAAAAAAA REEEEEEEEE MixAndVolume Temperature Invalid number!!!! {value}");
					return;
				}

				mix.Temperature = value;
				gasMix.Temperature = value;
			}
		}

		public float WholeHeatCapacity => mix.WholeHeatCapacity + gasMix.WholeHeatCapacity;

		public Vector2 Total => new Vector2(mix.Total, gasMix.Moles);

		public Vector2 Density()
		{
			return new Vector2(mix.Total / Volume, gasMix.Pressure);
		}

		/// <summary>
		/// Only use this if you know what you're doing
		/// </summary>
		/// <returns></returns>
		public GasMix GetGasMix()
		{
			return gasMix;
		}

		/// <summary>
		/// Only use this if you know what you're doing
		/// </summary>
		/// <returns></returns>
		public void SetGasMix(GasMix newGas)
		{
			gasMix = newGas;
		}

		/// <summary>
		/// Only use this if you know what you're doing
		/// </summary>
		/// <returns></returns>
		public ReagentMix GetReagentMix()
		{
			return mix;
		}

		/// <summary>
		/// Only use this if you know what you're doing
		/// </summary>
		/// <returns></returns>
		public void SetReagentMix(ReagentMix newReagentMix)
		{
			mix = newReagentMix;
		}

		public void Add(MixAndVolume mixAndVolume, bool changeVolume = true)
		{
			float internalEnergy = mixAndVolume.InternalEnergy + this.InternalEnergy;
			float gasVolume = gasMix.Volume;
			if (changeVolume)
			{
				gasVolume = gasVolume + mixAndVolume.gasMix.Volume;
			}

			mix.Add(mixAndVolume.mix);

			var newOne = new GasData();
			lock (gasMix.GasesArray) //is ok is new GasData
			{
				for (int i = gasMix.GasesArray.Count - 1; i >= 0; i--)
				{
					var gasData = gasMix.GasesArray[i];
					newOne.SetMoles(gasData.GasSO, gasMix.GasData.GetGasMoles(gasData.GasSO) + mixAndVolume.gasMix.GasData.GetGasMoles(gasData.GasSO));
				}
			}


			gasMix = GasMix.FromTemperature(newOne, gasMix.Temperature, gasVolume);
			this.InternalEnergy = internalEnergy;
		}

		public Tuple<ReagentMix, GasMix> Take(MixAndVolume inMixAndVolume, bool removeVolume = true)
		{
			if (MathUtils.IsEqual(Volume, 0))
			{
				Loggy.LogError("Tried to take from pipe but its volume was 0!", Category.Pipes);
			}

			var percentage = inMixAndVolume.Volume / Volume;
			var removeGasVolume = gasMix.Volume;
			var gasVolume = gasMix.Volume;
			if (removeVolume)
			{
				removeGasVolume = removeGasVolume * percentage;
				gasVolume = gasVolume * (1 - percentage);
			}

			var returnMix = mix.Take(mix.Total * percentage);

			var newOne = new GasData();
			var removeNewOne = new GasData();

			lock (gasMix.GasesArray) //is ok is new GasData
			{
				for (int i =  gasMix.GasesArray.Count - 1; i >= 0; i--)
				{
					var gasData = gasMix.GasesArray[i];
					var moles = gasMix.GasData.GetGasMoles(gasData.GasSO);
					removeNewOne.SetMoles(gasData.GasSO, moles * percentage);
					newOne.SetMoles(gasData.GasSO, moles * (1 - percentage));
				}
			}


			gasMix = GasMix.FromTemperature(newOne, gasMix.Temperature, gasVolume);
			Temperature = Temperature; //Reinitialise is temperature to equal out oddities
			return new Tuple<ReagentMix, GasMix>(returnMix,
				GasMix.FromTemperature(removeNewOne, gasMix.Temperature, removeGasVolume));
		}

		public void Divide(float divideAmount, bool changeVolume = true)
		{

			if (float.IsNormal(divideAmount) == false)
			{
				Loggy.LogError($"AAAAAAAAAAAAA REEEEEEEEE MixAndVolume divideAmount Invalid number!!!! {divideAmount}");
				return;
			}

			if (MathUtils.IsEqual(divideAmount, 0))
			{
				Loggy.LogError("Tried to divide pipe contents, but the amount to divide by was 0!", Category.Pipes);
			}

			float gasVolume = gasMix.Volume;
			if (changeVolume)
			{
				gasVolume = gasVolume / divideAmount;
			}

			mix.Divide(divideAmount);

			var newOne = new GasData();
			lock (gasMix.GasesArray) //is ok is new GasData
			{
				for (int i = gasMix.GasesArray.Count - 1; i >= 0; i--)
				{
					var gasData = gasMix.GasesArray[i];
					newOne.SetMoles(gasData.GasSO, gasMix.GasData.GetGasMoles(gasData.GasSO) / divideAmount);
				}

			}

			gasMix = GasMix.FromTemperature(newOne, gasMix.Temperature, gasVolume);
			Temperature = Temperature; //Reinitialise is temperature to equal out oddities
		}

		public void Multiply(float multiplyAmount, bool changeVolume = true)
		{

			if (float.IsNormal(multiplyAmount) == false)
			{
				Loggy.LogError($"AAAAAAAAAAAAA REEEEEEEEE MixAndVolume multiplyAmount Invalid number!!!! {multiplyAmount}");
				return;
			}

			var gasVolume = gasMix.Volume;
			if (changeVolume)
			{
				gasVolume = gasVolume * multiplyAmount;
			}

			mix.Multiply(multiplyAmount);

			var newOne = new GasData();
			lock (gasMix.GasesArray)  //is ok is new GasData
			{
				for (int i = gasMix.GasesArray.Count - 1; i >= 0; i--)
				{
					var gasData = gasMix.GasesArray[i];
					newOne.SetMoles(gasData.GasSO, gasMix.GasData.GetGasMoles(gasData.GasSO) * multiplyAmount);
				}
			}

			gasMix = GasMix.FromTemperature(newOne, gasMix.Temperature, gasVolume);
			Temperature = Temperature; //Reinitialise is temperature to equal out oddities
		}

		public MixAndVolume Clone()
		{
			var mixV = new MixAndVolume();
			mixV.gasMix = GasMix.NewGasMix(gasMix);
			mixV.mix = mix.Clone();
			return (mixV);
		}

		public void Empty()
		{
			gasMix.SetToEmpty();
			mix.Multiply(0);
		}

		public void TransferSpecifiedTo(MixAndVolume toTransfer, GasSO specifiedGas = null,
			Reagent reagent = null, Vector2? amount = null)
		{
			if (specifiedGas != null)
			{
				float toRemoveGas = 0;
				var gas = specifiedGas == null ? Systems.Atmospherics.Gas.Oxygen : specifiedGas;
				if (amount != null)
				{
					toRemoveGas = amount.Value.y;
				}
				else
				{
					toRemoveGas = gasMix.GasData.GetGasMoles(gas);
				}

				var transferredEnergy = toRemoveGas * gas.MolarHeatCapacity * Temperature;

				gasMix.RemoveGas(gas, toRemoveGas);

				var cachedInternalEnergy = this.InternalEnergy + transferredEnergy;

				toTransfer.gasMix.AddGas(gas, toRemoveGas);
				toTransfer.InternalEnergy = cachedInternalEnergy;
			}

			if (reagent != null)
			{
				float toRemoveGas = 0;

				if (amount != null)
				{
					toRemoveGas = amount.Value.x;
				}
				else
				{
					toRemoveGas = mix[reagent];
				}


				var transferredEnergy = toRemoveGas * reagent.heatDensity * mix.Temperature;

				var cachedInternalEnergy = InternalEnergy;
				mix.Subtract(reagent, toRemoveGas);
				InternalEnergy = cachedInternalEnergy - transferredEnergy;



				cachedInternalEnergy = toTransfer.InternalEnergy;

				toTransfer.mix.Add(reagent, toRemoveGas);
				cachedInternalEnergy += transferredEnergy;
				toTransfer.InternalEnergy = cachedInternalEnergy;
			}
		}

		public void TransferTo(MixAndVolume toTransfer, Vector2 amount)
		{
			if (float.IsNaN(amount.x) == false)
			{
				mix.TransferTo(toTransfer.mix, amount.x);
			}

			if (float.IsNaN(amount.y) == false)
			{
				GasMix.TransferGas(toTransfer.gasMix, gasMix, amount.y);
			}

			Temperature = Temperature; //Reinitialise is temperature to equal out oddities

		}

		public GasMix EqualiseWithExternal(GasMix inGasMix)
		{
			var mix = gasMix.MergeGasMix(inGasMix);
			Temperature = Temperature; //Reinitialise is temperature to equal out oddities
			return mix;

		}

		public void EqualiseWith(PipeData another, bool equaliseGas, bool equaliseLiquid)
		{
			if (equaliseGas)
			{
				PipeFunctions.PipeOrNet(another).gasMix = gasMix.MergeGasMix(PipeFunctions.PipeOrNet(another).gasMix);
			}

			if (equaliseLiquid)
			{
				float totalVolume = Volume + PipeFunctions.PipeOrNet(another).Volume;
				float totalReagents = mix.Total + PipeFunctions.PipeOrNet(another).mix.Total;
				if (MathUtils.IsEqual(totalVolume, 0))
				{
					Loggy.LogError("Tried to equalise two pipes, but their total volume was 0!", Category.Pipes);
				}

				float targetDensity = totalReagents / totalVolume;

				float thisAmount = targetDensity * Volume;
				float anotherAmount = targetDensity * PipeFunctions.PipeOrNet(another).Volume;

				if (thisAmount > mix.Total)
				{
					PipeFunctions.PipeOrNet(another).mix
						.TransferTo(mix, PipeFunctions.PipeOrNet(another).mix.Total - anotherAmount);
				}
				else
				{
					this.mix.TransferTo(PipeFunctions.PipeOrNet(another).mix,
						anotherAmount - PipeFunctions.PipeOrNet(another).mix.Total);
				}
			}
			Temperature = Temperature; //Reinitialise is temperature to equal out oddities
			PipeFunctions.PipeOrNet(another).Temperature = PipeFunctions.PipeOrNet(another).Temperature; //Reinitialise is temperature to equal out oddities
		}

		public void EqualiseWithMultiple(List<PipeData> others, bool equaliseGas, bool equaliseLiquid)
		{
			if (equaliseGas)
			{
				gasMix.MergeGasMixes(others);
			}

			if (equaliseLiquid)
			{
				var totalVolume = Volume;
				foreach (var pipe in others)
				{
					totalVolume += PipeFunctions.PipeOrNet(pipe).Volume;
				}

				var totalReagents = mix.Total;
				foreach (var pipe in others)
				{
					totalReagents += PipeFunctions.PipeOrNet(pipe).mix.Total;
				}

				if (MathUtils.IsEqual(totalVolume, 0))
				{
					Loggy.LogError("Tried to equalise multiple pipes, but their total volume was 0!", Category.Pipes);
				}

				var targetDensity = totalReagents / totalVolume;


				foreach (var pipe in others)
				{
					PipeFunctions.PipeOrNet(pipe).mix.TransferTo(mix, PipeFunctions.PipeOrNet(pipe).mix.Total);
				}

				foreach (var pipe in others)
				{
					mix.TransferTo(PipeFunctions.PipeOrNet(pipe).mix,
						targetDensity * PipeFunctions.PipeOrNet(pipe).Volume);
				}
			}

			Temperature = Temperature; //Reinitialise is temperature to equal out oddities
		}

		public void EqualiseWithOutputs(List<PipeData> others)
		{
			bool differenceInPressure = false;
			bool differenceInDensity = false;
			Vector2 density = this.Density();
			foreach (var pipe in others)
			{
				var densityDelta = (PipeFunctions.PipeOrNet(pipe).Density() - density);
				if (Mathf.Abs(densityDelta.x) > 0.001f)
				{
					differenceInDensity = true;
					if (differenceInPressure)
					{
						break;
					}
				}

				if (Mathf.Abs(densityDelta.y) > 0.001f)
				{
					differenceInPressure = true;
					if (differenceInDensity)
					{
						break;
					}
				}
			}

			if (differenceInPressure || differenceInDensity)
			{
				if (others.Count > 1)
				{
					EqualiseWithMultiple(others, differenceInPressure, differenceInDensity);
				}
				else if (others.Count > 0)
				{
					EqualiseWith(others[0], differenceInPressure, differenceInDensity);
				}
			}
		}

		public void SetVolume(float newVolume)
		{
			gasMix.Volume = newVolume;
			Temperature = Temperature; //Reinitialise is temperature to equal out oddities
		}

		public override string ToString()
		{
			string Returnstring = "";
			foreach (var gv in gasMix.GasData.GasesArray)
			{
				Returnstring += $" Name : {gv.GasSO.Name} Amount : {gv.Moles}";
			}

			return $"Volume > {Volume} Mix > {mix} gasMix > {gasMix.ToString()} gases > " + Returnstring;
		}
	}
}
