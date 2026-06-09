using System;
using System.Collections.Generic;
using UnityEngine;
using US13.HealthV2;
using US13.HealthV2.Living;
using Util;

namespace US13.Items.Food.ConsumptionEffect
{
	/// <summary>
	/// Heals every body part of the eater for each configured damage type and amount.
	/// </summary>
	[Serializable]
	public class HealAllBodyPartsEffect: IConsumptionEffect
	{
		[SerializeField] private List<HealData> heals = new();

		[Serializable]
		private class HealData
		{
			[field: SerializeField] public DamageType DamageType { get; private set; }
			[field: SerializeField] public float Amount { get; private set; }
		}

		public void Apply(ConsumptionContext context)
		{
			var health = context.Eater.GetCachedComponent<LivingHealthMasterBase>();
			foreach (HealData heal in heals)
			{
				health.HealDamageOnAll(context.ConsumedObject, heal.Amount, heal.DamageType);
			}
		}
	}


}