
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Chemistry
{
	[Serializable]
	public class ReagentMix
	{
		public const float ZERO_CELSIUS_IN_KELVIN = 273.15f;

		[field: SerializeField] public float Temperature { get; set; } = ZERO_CELSIUS_IN_KELVIN;
		[SerializeField] public DictionaryReagentFloat reagents = new DictionaryReagentFloat();

		public void Add(ReagentMix b)
		{
			Temperature = (
				              Temperature * CalculateTotal() +
				              b.Temperature * b.CalculateTotal()
				              ) /
			              (CalculateTotal() + b.CalculateTotal());

			foreach (var reagent in b.reagents)
			{
				if (reagents.TryGetValue(reagent.Key, out var value))
				{
					reagents[reagent.Key] = reagent.Value + value;
				}
				else
				{
					reagents[reagent.Key] = reagent.Value;
				}
			}
		}

		// Inefficient for now, can replace with a caching solution later.
		public float CalculateTotal()
		{
			return reagents.Sum(kvp => kvp.Value);
		}
	}


	[Serializable]
	public class DictionaryReagentInt : SerializableDictionary<Reagent, int>
	{

	}

	[Serializable]
	public class DictionaryReagentFloat : SerializableDictionary<Reagent, float>
	{

	}
}