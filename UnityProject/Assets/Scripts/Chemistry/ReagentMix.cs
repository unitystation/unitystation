using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Google.Protobuf.WellKnownTypes;
using UnityEngine;

namespace Chemistry
{
	[Serializable]
	public class ReagentMix : IEnumerable<KeyValuePair<Reagent, float>>
	{
		[Temperature]
		[SerializeField] private float temperature = TemperatureUtils.ZERO_CELSIUS_IN_KELVIN;

		[SerializeField]
		private DictionaryReagentFloat reagents;

		public ReagentMix(DictionaryReagentFloat reagents, float temperature = TemperatureUtils.ZERO_CELSIUS_IN_KELVIN)
		{
			Temperature = temperature;
			this.reagents = reagents;
		}

		public ReagentMix(Reagent reagent, float amount, float temperature = TemperatureUtils.ZERO_CELSIUS_IN_KELVIN)
		{
			Temperature = temperature;
			reagents = new DictionaryReagentFloat { [reagent] = amount };
		}

		public ReagentMix(float temperature = TemperatureUtils.ZERO_CELSIUS_IN_KELVIN)
		{
			Temperature = temperature;
			reagents = new DictionaryReagentFloat();
		}

		public float this[Reagent reagent] => reagents.TryGetValue(reagent, out var amount) ? amount : 0;

		/// <summary>
		/// Returns current temperature mix in Kelvin
		/// </summary>
		public float Temperature
		{
			get => temperature;
			set => temperature = value;
		}

		public float WholeHeatCapacity	//this is the heat capacity for the entire Fluid mixture, in Joules/Kelvin. gets very big with lots of gas.
		{
			get
			{
				float capacity = 0f;
				foreach (var reagent in reagents)
				{
					capacity += reagent.Key.heatDensity * reagent.Value;
				}
				return capacity;
			}
		}


		/// <summary>
		/// Returns current temperature mix in Kelvin
		/// </summary>
		public float InternalEnergy
		{
			get
			{
				return (WholeHeatCapacity *Temperature);
			}

			set
			{
				Temperature = (value / WholeHeatCapacity);
			}
		}

		/// <summary>
		/// Return reagent with biggest amount in mix
		/// </summary>
		public Reagent MajorMixReagent
		{
			get
			{
				var reagent = reagents.OrderByDescending(p => p.Value)
					.FirstOrDefault();

				return reagent.Value > 0 ? reagent.Key : null;
			}
		}

		/// <summary>
		/// Average of all reagent colors in a mix
		/// </summary>
		public Color MixColor
		{
			get
			{
				var avgColor = new Color();
				var totalAmount = Total;

				foreach (var reagent in reagents)
				{
					var percent = reagent.Value / totalAmount;
					var colorStep = percent * reagent.Key.color;

					avgColor += colorStep;
				}

				return avgColor;
			}
		}

		/// <summary>
		/// The name(s) of the reagents inside the container.
		/// Note: Probably only use for containers that only hold one specific thing.
		/// </summary>
		public String MixName
		{
			get
			{
				return MajorMixReagent.Name;
			}
		}


		/// <summary>
		/// Average state of all reagents in mix
		/// </summary>
		public ReagentState MixState
		{
			get
			{
				// Fallback for empty mix
				if (reagents.Count == 0)
					return ReagentState.Solid;

				// Just shortcut to avoid all calculations bellow
				if (reagents.Count == 1)
					return reagents.First().Key.state;

				// First group all reagents by their state
				var groupedByState = reagents.GroupBy(x => x.Key.state);

				// Next - get sum for each state
				var volumeByState = groupedByState.Select((group) =>
				{
					return new KeyValuePair<ReagentState, float>
					(group.Key, group.Sum(r => r.Value));
				}).ToArray();

				// Now get state with the biggest sum
				var mostState = volumeByState.OrderByDescending(group => group.Value).First();
				return mostState.Key;
			}
		}

		public void Add(ReagentMix b)
		{
			if (Total == 0)
			{
				Temperature = b.Temperature;
			}
			else
			{
				Temperature = (Temperature * Total + b.Temperature * b.Total) / (Total + b.Total); //TODO Change to use different formula Involving Heat capacity of each Reagent
			}


			foreach (var reagent in b.reagents)
			{
				Add(reagent.Key, reagent.Value);
			}
		}

		public void Add(Reagent reagent, float amount)
		{
			if (amount < 0f)
			{
				Debug.LogError($"Trying to add negative {amount} amount of {reagent}");
				return;
			}

			if (!reagents.ContainsKey(reagent))
			{
				reagents.Add(reagent, amount);
			}
			else
			{
				reagents[reagent] += amount;
			}
		}

		public void Subtract(ReagentMix b)
		{
			foreach (var reagent in b.reagents)
			{
				Subtract(reagent.Key, reagent.Value);
			}
		}

		public float Subtract(Reagent reagent, float subAmount)
		{
			if (subAmount < 0)
			{
				Debug.LogErrorFormat("Trying to subtract negative {0} amount of {1}. Use positive amount instead.", subAmount, reagent);
				return 0;
			}

			if (reagents.TryGetValue(reagent, out var amount))
			{
				var newAmount = amount - subAmount;

				if (newAmount <= 0)
				{
					// nothing left, remove reagent - it became zero
					// remove amount that was before
					reagents.Remove(reagent);
					return amount;
				}

				// change amount to subtraction result
				reagents[reagent] = newAmount;
				return amount;
			}

			// have nothing to remove, just return zero
			return 0;
		}

		/// <summary>
		/// Multiply each reagent amount by multiplier
		/// </summary>
		public void Multiply(float multiplier)
		{
			if (multiplier < 0f)
			{
				Debug.LogError($"Trying to multiply reagentmix by {multiplier}");
				return;
			}

			if (multiplier == 0f)
			{
				// if multiply by zero - clear all reagents
				Clear();
				return;
			}

			foreach (var key in reagents.Keys.ToArray())
			{
				reagents[key] *= multiplier;
			}
		}

		/// <summary>
		/// Multiply each reagent amount by multiplier
		/// </summary>
		public void Divide(float Divider)
		{
			if (Divider < 0f)
			{
				Debug.LogError($"Trying to Divide reagentmix by {Divider}");
				return;
			}

			if (Divider == 0f)
			{
				//Better than dealing with errors
				return;
			}

			foreach (var key in reagents.Keys.ToArray())
			{
				reagents[key] /= Divider;
			}
		}



		/// <summary>
		/// Transfer part of reagent mix
		/// </summary>
		/// <returns>Transfered reagent mix</returns>
		public ReagentMix TransferTo(ReagentMix toTransfer, float amount)
		{
			var toTransferMix = this.Clone();

			// can't allow to transfer more than Total
			var toTransferAmount = Math.Min(amount, Total);
			toTransferMix.Max(toTransferAmount, out _);

			Subtract(toTransferMix);
			toTransfer.Add(toTransferMix);
			return toTransferMix;
		}

		public ReagentMix Take(float amount)
		{
			var taken = new ReagentMix();
			TransferTo(taken, amount);
			return taken;
		}

		public void RemoveVolume(float amount)
		{
			var multiplier = (Total - amount) / Total;
			if (float.IsNaN(multiplier))
			{
				multiplier = 0;
			}
			Multiply(multiplier);
		}

		/// <summary>
		/// Try to remove max avaliable amount of mix
		/// </summary>
		public void Max(float max, out float removed)
		{
			removed = Math.Max(Total - max, 0);
			RemoveVolume(removed);
		}

		public void Clear()
		{
			reagents.Clear();
		}

		// Inefficient for now, can replace with a caching solution later.

		public float Total
		{
			get { return Mathf.Clamp( reagents.Sum(kvp => kvp.Value), 0, float.MaxValue); }
		}

		public ReagentMix Clone()
		{
			return new ReagentMix(new DictionaryReagentFloat(reagents), Temperature);
		}

		public IEnumerator<KeyValuePair<Reagent, float>> GetEnumerator()
		{
			return reagents.GetEnumerator();
		}

		IEnumerator IEnumerable.GetEnumerator()
		{
			return GetEnumerator();
		}

		public override string ToString()
		{
			return "Temperature > " + Temperature + " reagents > " + "{" + string.Join(",", reagents.Select(kv => kv.Key + "=" + kv.Value).ToArray()) + "}";;
		}
	}


	[Serializable]
	public class DictionaryReagentInt : SerializableDictionary<Reagent, int>
	{
	}

	[Serializable]
	public class DictionaryReagentFloat : SerializableDictionary<Reagent, float>
	{
		public DictionaryReagentFloat()
		{
		}

		public DictionaryReagentFloat(IDictionary<Reagent, float> dict) : base(dict)
		{
		}
	}
}