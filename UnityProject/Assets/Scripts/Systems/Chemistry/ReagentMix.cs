using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Chemistry
{
	[Serializable]
	public class ReagentMix
	{
		[Temperature]
		[SerializeField] private float temperature = TemperatureUtils.ToKelvin(20f, TemeratureUnits.C);

		[SerializeField]
		public DictionaryReagentFloat reagents;

		//should only be accessed when locked so should be okay
		private Dictionary<Reagent, float> TEMPReagents = new Dictionary<Reagent, float>();

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

		public float this[Reagent reagent] => reagents.m_dict.TryGetValue(reagent, out var amount) ? amount : 0;

		/// <summary>
		/// Returns current temperature mix in Kelvin
		/// </summary>
		public float Temperature
		{
			get => temperature;
			set
			{
				temperature = value;
				if (float.IsNaN(Temperature))
				{
					Logger.LogError("Temperature is NAN", Category.Chemistry);
				}
			}
		}

		public float WholeHeatCapacity	//this is the heat capacity for the entire Fluid mixture, in Joules/Kelvin. gets very big with lots of gas.
		{
			get
			{
				float capacity = 0f;
				lock (reagents)
				{
					foreach (var reagent in reagents.m_dict)
					{
						if (reagent.Key == null) continue;
						capacity += reagent.Key.heatDensity * reagent.Value;
					}
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
				if (WholeHeatCapacity == 0)
				{
					Temperature = 0;
				}
				else
				{
					Temperature =(value / WholeHeatCapacity);
					if (float.IsNaN(Temperature))
					{
						Logger.LogError($"Temperature is NAN", Category.Chemistry);
					}
				}

			}
		}

		/// <summary>
		/// Return reagent with biggest amount in mix
		/// </summary>
		public Reagent MajorMixReagent
		{
			get
			{
				lock (reagents)
				{
					var reagent = reagents.m_dict.OrderByDescending(p => p.Value)
						.FirstOrDefault();
					return reagent.Value > 0 ? reagent.Key : null;
				}
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
				lock (reagents)
				{
					foreach (var reagent in reagents.m_dict)
					{
						var percent = reagent.Value / totalAmount;
						var colorStep = percent * reagent.Key.color;

						avgColor += colorStep;
					}
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
				if (reagents.m_dict.Count == 0)
					return ReagentState.Solid;

				// Just shortcut to avoid all calculations bellow
				if (reagents.m_dict.Count == 1)
					return reagents.m_dict.First().Key.state;

				// First group all reagents by their state
				var groupedByState = reagents.m_dict.GroupBy(x => x.Key.state);

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
			if (Total == 0 || b.Total == 0)
			{
				if (float.IsNaN(b.Temperature) == false)
				{
					Temperature = b.Temperature;
				}
			}
			else
			{
				Temperature = (Temperature * Total + b.Temperature * b.Total) / (Total + b.Total); //TODO Change to use different formula Involving Heat capacity of each Reagent
			}

			lock (reagents)
			{
				foreach (var reagent in b.reagents.m_dict)
				{
					Add(reagent.Key, reagent.Value);
				}
			}
		}

		public void Add(Reagent reagent, float amount)
		{
			if (amount < 0f)
			{
				Logger.LogError($"Trying to add negative {amount} amount of {reagent}", Category.Chemistry);
				return;
			}

			if (float.IsNaN(amount) || float.IsInfinity(amount))
			{
				Logger.LogError($"Trying to add {amount} amount of {reagent}", Category.Chemistry);
				return;
			}

			if (!reagents.m_dict.ContainsKey(reagent))
			{
				lock (reagents)
				{
					reagents.m_dict.Add(reagent, amount);
				}
			}
			else
			{
				lock (reagents)
				{
					reagents.m_dict[reagent] += amount;
				}
			}
		}


		public float Remove(Reagent reagent, float amount)
		{
			if (amount < 0f)
			{
				Debug.LogError($"Trying to remove Negative {amount} amount of {reagent}");
				return 0;
			}

			if (float.IsNaN(amount) || float.IsInfinity(amount))
			{
				Logger.LogError($"Trying to remove {amount} amount of {reagent}", Category.Chemistry);
				return 0;
			}

			if (!reagents.m_dict.ContainsKey(reagent))
			{
				//Debug.LogError($"Trying to move {reagent} from container doesn't contain it ");
				return 0;
			}
			else
			{
				lock (reagents)
				{
					amount = Math.Min(reagents.m_dict[reagent], amount);
					reagents.m_dict[reagent] -= amount;


					if (reagents.m_dict[reagent] <= 0)
					{
						reagents.m_dict.Remove(reagent);
					}
				}


				return amount;
			}
		}

		public void Subtract(ReagentMix b)
		{
			lock (reagents)
			{
				foreach (var reagent in b.reagents.m_dict)
				{
					Subtract(reagent.Key, reagent.Value);
				}
			}
		}

		public float Subtract(Reagent reagent, float subAmount)
		{
			if (subAmount < 0)
			{
				Logger.LogErrorFormat("Trying to subtract negative {0} amount of {1}. Use positive amount instead.", Category.Chemistry,
					subAmount, reagent);
				return 0;
			}

			if (float.IsNaN(subAmount) || float.IsInfinity(subAmount))
			{
				Logger.LogError($"Trying to subtract {subAmount} amount of {reagent}", Category.Chemistry);
				return 0;
			}

			if (reagents.m_dict.TryGetValue(reagent, out var amount))
			{
				var newAmount = amount - subAmount;

				if (newAmount <= 0)
				{
					// nothing left, remove reagent - it became zero
					// remove amount that was before
					lock (reagents)
					{
						reagents.m_dict.Remove(reagent);
					}

					return amount;
				}

				lock (reagents)
				{
					// change amount to subtraction result
					reagents.m_dict[reagent] = newAmount;
				}

				return subAmount;
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
				Logger.LogError($"Trying to multiply reagentmix by {multiplier}", Category.Chemistry);
				return;
			}

			if (float.IsNaN(multiplier) || float.IsInfinity(multiplier))
			{
				Logger.LogError($"Trying to Multiply by {multiplier}", Category.Chemistry);
				return;
			}

			if (multiplier == 0f)
			{
				// if multiply by zero - clear all reagents
				Clear();
				return;
			}

			lock (reagents)
			{
				TEMPReagents.Clear();
				foreach (var key in reagents.m_dict.Keys)
				{
					var nuber = reagents.m_dict[key];
					nuber = nuber * multiplier;
					TEMPReagents[key] = nuber;
				}

				//man, I wish changing the value of the key didn't modify the order
				foreach (var key in TEMPReagents.Keys)
				{
					reagents.m_dict[key] = TEMPReagents[key];
				}
			}
		}

		/// <summary>
		/// Divide each reagent amount by Divider
		/// </summary>
		public void Divide(float Divider)
		{
			if (Divider < 0f)
			{
				Logger.LogError($"Trying to Divide reagentmix by {Divider}", Category.Chemistry);
				return;
			}

			if (Divider == 0f)
			{
				//Better than dealing with errors
				return;
			}

			if (float.IsNaN(Divider) || float.IsInfinity(Divider))
			{
				Logger.LogError($"Trying to Divide by {Divider}", Category.Chemistry);
				return;
			}

			lock (reagents)
			{
				TEMPReagents.Clear();
				foreach (var key in reagents.m_dict.Keys)
				{
					var nuber = reagents.m_dict[key];
					nuber = nuber / Divider;
					TEMPReagents[key] = nuber;
				}
				//man, I wish changing the value of the key didn't modify the order
				foreach (var key in TEMPReagents.Keys)
				{
					reagents.m_dict[key] = TEMPReagents[key];
				}
			}
		}

		[HideInInspector]
		public List<Reagent> reagentKeys = new List<Reagent>();
		public void TransferTo(ReagentMix target, float amount)
		{
			if (amount == 0 || amount < 0)
			{
				return;
			}

			if (float.IsNaN(amount) || float.IsInfinity(amount))
			{
				Logger.LogError($"Trying to Transfer by {amount}", Category.Chemistry);
				return;
			}

			var total = Total;

			//temperature change
			var targetTotal = target.Total;
			if (targetTotal == 0)
			{
				target.temperature = temperature;
			}
			else
			{
				target.temperature = (target.temperature * targetTotal + temperature * amount) / (targetTotal + amount);
			}

			//reagent transfer
			if (amount < total)
			{
				var multiplier = amount / total;
				lock (reagents)
				{
					reagentKeys.Clear();
					foreach (var key in reagents.m_dict.Keys)
					{
						reagentKeys.Add(key);
					}

					foreach (var reagent in reagentKeys)
					{
						var reagentAmount = reagents.m_dict[reagent] * multiplier;
						reagents.m_dict[reagent] -= reagentAmount;
						target.Add(reagent, reagentAmount);
					}
				}
			}
			else
			{
				lock (reagents)
				{
					foreach (var reagent in reagents.m_dict)
					{
						target.Add(reagent.Key, reagent.Value);
					}

					reagents.m_dict.Clear();
				}
			}
		}

		public ReagentMix Take(float amount)
		{
			if (amount == 0 || amount < 0)
			{
				return new ReagentMix();
			}

			if (float.IsNaN(amount) || float.IsInfinity(amount))
			{
				Logger.LogError($"Trying to Take {amount}", Category.Chemistry);
				return new ReagentMix();;
			}

			var taken = new ReagentMix();
			TransferTo(taken, amount);
			return taken;
		}

		public void RemoveVolume(float amount)
		{
			if (amount == 0)
			{
				return;
			}

			if (float.IsNaN(amount) || float.IsInfinity(amount))
			{
				Logger.LogError($"Trying to RemoveVolume {amount}", Category.Chemistry);
				return;
			}

			if (amount > Total)
			{
				amount = Total;
			}

			if (Total == 0)
			{
				return;
			}

			var multiplier = (Total - amount) / Total;
			if (float.IsNaN(multiplier))
			{
				multiplier = 1;
			}

			if (multiplier < 0)
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

		/// <summary>
		/// Get the what fraction of the total a specific reagent is
		/// </summary>
		public float GetPercent(Reagent reagent)
		{
			return reagents.m_dict[reagent] / Total;
		}


		public void Clear()
		{
			lock (reagents)
			{
				reagents.m_dict.Clear();
			}
		}

		// Inefficient for now, can replace with a caching solution later.

		public float Total
		{
			get
			{
				float total = 0;
				lock (reagents)
				{
					foreach (var reagent in reagents.m_dict)
					{
						total += reagent.Value;
					}
				}

				total = Mathf.Clamp(total, 0, float.MaxValue);
				return total;
			}
		}

		public ReagentMix Clone()
		{
			return new ReagentMix(new DictionaryReagentFloat(reagents.m_dict), Temperature);
		}

		public bool ContentEquals (ReagentMix b)
		{
			if (ReferenceEquals(this, null) || ReferenceEquals(b, null))
			{
				return ReferenceEquals(this, null) && ReferenceEquals(b, null);
			}

			if (Temperature != b.Temperature)
			{
				return false;
			}

			lock (reagents)
			{
				foreach (var reagent in reagents.m_dict)
				{
					if (b[reagent.Key] != this[reagent.Key])
					{
						return false;
					}
				}
			}

			return true;
		}

		public override string ToString()
		{
			return "Temperature > " + Temperature + " reagents > " + "{" + string.Join(",", reagents.m_dict.Select(kv => kv.Key + "=" + kv.Value).ToArray()) + "}";
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
