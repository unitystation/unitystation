using System;
using JetBrains.Annotations;
using UnityEngine;

namespace US13.Items.Food.ConsumptionEffect
{
	/// <summary>
	/// Polymorphic consumption effect applied when a <see cref="Consumable"/> is eaten or drunk.
	/// Implementations must be <c>[Serializable]</c> for <c>[SerializeReference]</c> usage in
	/// <see cref="EffectOnConsumed"/>.
	/// </summary>
	public interface IConsumptionEffect
	{
		void Apply(ConsumptionContext context);
	}
}