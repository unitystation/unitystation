using System;
using Logs;
using UnityEngine;
using US13.Systems.StatusesAndEffects;
using Util;

namespace US13.Items.Food.ConsumptionEffect
{
	/// <summary>
	/// Adds a status effect on the eater when the item has been consumed.
	/// </summary>
	[Serializable]
	public class ApplyStatusEffect: IConsumptionEffect
	{
		[SerializeField] private StatusEffect statusEffect;

		public void Apply(ConsumptionContext context)
		{
			var statusManager = context.Eater.GetCachedComponent<StatusEffectManager>();
			if  (statusManager == null)
			{
				Loggy.Warning().Format("Game object {0} has no StatusEffectManager",
					Category.Objects,
					context.Eater.name);
				return;
			}

			statusManager.AddStatus(statusEffect);
		}
	}
}
