using JetBrains.Annotations;
using UnityEngine;
using US13.Core.Attributes;
using US13.Core.Lifecycle;
using US13.Items.Food.ConsumptionEffect;
using US13.Items.Food.ConsumptionEffect.Conditions;
using Util;

namespace US13.Items.Food
{
	/// <summary>
	/// Bridge between <see cref="Consumable"/> and <see cref="IConsumptionEffect"/>.
	/// Subscribes to <see cref="Consumable.OnConsumed"/> and dispatches the configured effect,
	/// optionally gated by a <see cref="IConsumptionEffectCondition"/>.
	/// Multiple instances can be added to the same GameObject for stacking effects.
	/// </summary>
	[RequireComponent(typeof(Consumable))]
	public class EffectOnConsumed: MonoBehaviour, IServerLifecycle
	{
		[SerializeReference]
		[SelectImplementation(typeof(IConsumptionEffect))]
		private IConsumptionEffect consumptionEffect;

		[SerializeReference]
		[SelectImplementation(typeof(IConsumptionEffectCondition))]
		private IConsumptionEffectCondition condition = new AlwaysTrue();

		private Consumable consumable;

		private void OnConsumed(GameObject eater, GameObject feeder)
		{
			ConsumptionContext context = new(eater, gameObject, feeder);
			if (condition != null && condition.IsValid(context) == false) return;
			consumptionEffect.Apply(context);
		}

		public void OnSpawnServer(SpawnInfo info)
		{
			consumable = this.GetCachedComponent<Consumable>();
			consumable.OnConsumed += OnConsumed;
		}

		public void OnDespawnServer(DespawnInfo info)
		{
			consumable.OnConsumed -= OnConsumed;
		}
	}
}
