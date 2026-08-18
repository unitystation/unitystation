using System;
using UnityEngine;
using US13.Systems.StatusesAndEffects;
using US13.Systems.StatusesAndEffects.Interfaces;

namespace Tests.StatusAndEffectsFramework
{
	public class MockStatus : StatusEffect
	{

	}

	public class ImmediateStatusEffect : StatusEffect, IImmediateEffect
	{
		public bool DidEffect { get; private set; } = false;
		public int EffectCount { get; private set; }

		public override void DoEffect(GameObject target)
		{
			DidEffect = true;
			EffectCount++;
		}
	}

	public class StackableStatusEffect: StatusEffect, IStackableStatus
	{
		public int InitialStacks { get; set; } = 1;
		public int Stacks { get; set; }

		public void AddStack(int amount)
		{
			Stacks += amount;
		}

		public void RemoveStack(int amount)
		{
			Stacks -= amount;
		}
	}

	public class ExpirableStatusEffect : StatusEffect, IExpirableStatus
	{
		private Action<IExpirableStatus> expired;

		public int SubscriberCount { get; private set; }
		public float Duration => 30f;
		public DateTime DeathTime { get; set; }

		public event Action<IExpirableStatus> Expired
		{
			add
			{
				expired += value;
				SubscriberCount++;
			}
			remove
			{
				expired -= value;
				SubscriberCount--;
			}
		}

		public void CheckExpiration()
		{
			expired?.Invoke(this);
		}
	}
}
