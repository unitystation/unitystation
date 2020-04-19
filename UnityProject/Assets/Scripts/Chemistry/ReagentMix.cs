using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Chemistry
{
	[Serializable]
	public class ReagentMix : IEnumerable<KeyValuePair<Reagent, float>>
	{
		[Temperature]
		[SerializeField] private float temperature = TemperatureUtils.ZERO_CELSIUS_IN_KELVIN;

		public float Temperature
		{
			get => temperature;
			set => temperature = value;
		}

		/// <summary>
		/// Return reagent with biggest amount in mix
		/// </summary>
		public Reagent MajorMixReagent
		{
			get
			{
				var reag = reagents.OrderByDescending((p) => p.Value)
					.FirstOrDefault();

				if (reag.Value > 0f)
					return reag.Key;
				else
					return null;
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

				foreach (var reag in reagents)
				{
					var percent = reag.Value / totalAmount;
					var colorStep = percent * reag.Key.color;

					avgColor += colorStep;
				}

				return avgColor;
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
			reagents = new DictionaryReagentFloat {[reagent] = amount};
		}

		public ReagentMix(float temperature = TemperatureUtils.ZERO_CELSIUS_IN_KELVIN)
		{
			Temperature = temperature;
			reagents = new DictionaryReagentFloat();
		}

		public float this[Reagent reagent]
		{
			get
			{
				if (!reagents.ContainsKey(reagent))
					return 0f;

				return reagents[reagent];
			}
			set
			{
				if (!reagents.ContainsKey(reagent))
				{
					if (value > 0)
						reagents.Add(reagent, value);
				}
				else
				{
					if (value > 0)
						reagents[reagent] = value;
					else
						reagents.Remove(reagent);
				}
			}
		}

		public void Add(ReagentMix b)
		{
			Temperature = (Temperature * Total + b.Temperature * b.Total) /
			              (Total + b.Total);

			foreach (var reagent in b.reagents)
			{
				this[reagent.Key] += reagent.Value;
			}
		}

		public void Subtract(ReagentMix b)
		{
			foreach (var reagent in b.reagents)
			{
				this[reagent.Key] -= reagent.Value;
			}
		}

		public void Multiply(float multiplier)
		{
			foreach (var key in reagents.Keys.ToArray())
			{
				this[key] *= multiplier;
			}
		}

		public ReagentMix TransferTo(ReagentMix b, float amount)
		{
			var toTransferMix = Clone();

			// can't allow to transfer more than Total
			var toTransferAmount = Math.Min(amount, Total);
			toTransferMix.Max(toTransferAmount, out _);

			Subtract(toTransferMix);
			b.Add(toTransferMix);
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
			get { return reagents.Sum(kvp => kvp.Value); }
		}

		public bool Contains(Reagent reagent)
		{
			return reagents.ContainsKey(reagent);
		}

		public bool Contains(Reagent reagent, float amount)
		{
			if (reagents.TryGetValue(reagent, out var value))
			{
				return value >= amount;
			}

			return false;
		}

		public bool ContainsMoreThan(Reagent reagent, float amount)
		{
			if (reagents.TryGetValue(reagent, out var value))
			{
				return value > amount;
			}

			return false;
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