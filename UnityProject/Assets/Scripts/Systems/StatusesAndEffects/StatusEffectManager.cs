using System.Collections.Generic;
using Systems.StatusesAndEffects.Interfaces;
using UnityEngine;

namespace Systems.StatusesAndEffects
{
	public class StatusEffectManager : MonoBehaviour
	{
		public HashSet<StatusEffect> Statuses { get; } = new();

		public void AddStatus(StatusEffect status)
		{
			HandleExpirableStatusAddition(status);
			HandleStackableStatusAddition(status);
			HandleImmediateStatusAddition(status);

			if (HasStatus(status)) return;
			status.Initialize(gameObject);
			Statuses.Add(status);
		}

		private void HandleExpirableStatusAddition(StatusEffect status)
		{
			if (status is IExpirableStatus expirable)
			{
				expirable.Expired += OnExpiredStatus;
			}
		}

		private void HandleStackableStatusAddition(StatusEffect status)
		{
			if (status is not IStackableStatus newStackable) return;
			if (Statuses.TryGetValue(status, out var oldStatus))
			{
				if (oldStatus is IStackableStatus oldStackable)
				{
					oldStackable.AddStack(newStackable.InitialStacks);
				}
			}
			else
			{
				newStackable.AddStack(newStackable.InitialStacks);
			}
		}

		private void HandleImmediateStatusAddition(StatusEffect status)
		{
			if (status is not IImmediateEffect) return;
			status.DoEffect();
		}

		public void RemoveStatus(StatusEffect status)
		{
			status.OnRemoved();
			Statuses.Remove(status);
		}

		private void OnExpiredStatus(IExpirableStatus expirable)
		{
			void RemoveExpiredStatus()
			{
				if (expirable is StatusEffect status) RemoveStatus(status);
			}

			if (expirable is IStackableStatus stackable)
			{
				stackable.RemoveStack(1);
				if (stackable.Stacks > 0) return;
			}

			RemoveExpiredStatus();
		}

		public bool HasStatus(StatusEffect status)
		{
			return Statuses.Contains(status);
		}
	}
}
