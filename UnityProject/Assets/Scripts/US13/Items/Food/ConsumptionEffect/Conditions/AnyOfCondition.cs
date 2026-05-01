using System.Collections.Generic;
using UnityEngine;
using US13.Core.Attributes;

namespace US13.Items.Food.ConsumptionEffect.Conditions
{
	/// <summary>
	/// Passes if at least one sub-condition is valid (OR logic).
	/// </summary>
	public class AnyOfCondition: IConsumptionEffectCondition
	{
		[SerializeReference]
		[SelectImplementation(typeof(IConsumptionEffectCondition))]
		private List<IConsumptionEffectCondition> conditions;

		public bool IsValid(ConsumptionContext context)
		{
			foreach (var condition in conditions)
			{
				if (condition.IsValid(context)) return true;
			}

			return false;
		}
	}
}