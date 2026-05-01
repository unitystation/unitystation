using JetBrains.Annotations;
using UnityEngine;

namespace US13.Items.Food.ConsumptionEffect
{
	/// <summary>
	/// Immutable context for a consumption event, passed to <see cref="IConsumptionEffect.Apply"/>.
	/// </summary>
	public readonly struct ConsumptionContext
	{
		public GameObject Eater { get; }
		public GameObject ConsumedObject { get; }

		/// <summary>The character that fed the item, or <see langword="null"/> for projectile/self-consumption.</summary>
		[CanBeNull] public GameObject Feeder { get; }

		public ConsumptionContext(GameObject eater, GameObject consumedObject, [CanBeNull] GameObject feeder)
		{
			Eater = eater;
			ConsumedObject = consumedObject;
			Feeder = feeder;
		}
	}
}