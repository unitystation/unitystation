using System;
using US13.Systems.StatusesAndEffects;
using US13.Systems.StatusesAndEffects.Interfaces;

namespace Tests.StatusAndEffectsFramework
{
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
