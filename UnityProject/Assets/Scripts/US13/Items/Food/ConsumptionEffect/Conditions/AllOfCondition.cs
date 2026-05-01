using System.Collections.Generic;
using UnityEngine;
using US13.Core.Attributes;

namespace US13.Items.Food.ConsumptionEffect.Conditions
{
	/// <summary>
	/// Passes only if every sub-condition is valid (AND logic).
	/// </summary>
	public class AllOfCondition: IConsumptionEffectCondition
	{
		[SerializeReference]
		[SelectImplementation(typeof(IConsumptionEffectCondition))]
		private List<IConsumptionEffectCondition> conditions;

		public bool IsValid(ConsumptionContext context)
		{
			foreach (var condition in conditions)
			{
				if (condition.IsValid(context) == false) return false;
			}

			return true;
		}
	}
}