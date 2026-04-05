using UnityEngine;
using US13.Core.Attributes;

namespace US13.Items.Food.ConsumptionEffect.Conditions
{
	/// <summary>
	/// Negates a single sub-condition (NOT logic).
	/// </summary>
	public class NotCondition: IConsumptionEffectCondition
	{
		[SerializeReference]
		[SelectImplementation(typeof(IConsumptionEffectCondition))]
		private IConsumptionEffectCondition condition;

		public bool IsValid(ConsumptionContext context)
		{
			return condition.IsValid(context) == false;
		}
	}
}