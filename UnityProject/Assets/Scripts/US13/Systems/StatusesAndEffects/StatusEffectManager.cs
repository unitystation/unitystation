using System;
using System.Collections.Generic;
using UnityEngine;
using US13.Managers.UpdateManager;
using US13.Player;
using US13.Systems.StatusesAndEffects.Interfaces;

namespace US13.Systems.StatusesAndEffects
{
	public class StatusEffectManager : MonoBehaviour
	{
		public HashSet<StatusEffect> Statuses { get; } = new();

		private void Start()
		{
			UpdateManager.Add(TickStatusUpdates, 1f);
		}

		private void OnDisable()
		{
			UpdateManager.Remove(CallbackType.PERIODIC_UPDATE, TickStatusUpdates);
		}

		private void TickStatusUpdates()
		{
			foreach (var effect in Statuses)
			{
				effect?.DoEffectTick(gameObject);
			}
		}

		public void AddStatus(StatusEffect status)
		{
			if (status == null) return;
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
			status.DoEffect(gameObject);
		}

		public void RemoveStatus(StatusEffect status)
		{
			if (status == false) return;
			if (status == null) return;
			if (gameObject == null ) return;
			status.OnRemoved(gameObject);
			Statuses.Remove(status);
		}

		private void OnExpiredStatus(IExpirableStatus expirable)
		{
			if (expirable is IStackableStatus stackable)
			{
				stackable.RemoveStack(1);
				if (stackable.Stacks > 0) return;
			}
			if (expirable is StatusEffect status) RemoveStatus(status);
		}

		public bool HasStatus(StatusEffect status)
		{
			return Statuses.Contains(status);
		}

		public bool HasStatusByName(string statusName)
		{
			foreach (var status in Statuses)
			{
				if (status.name == statusName) return true;
			}
			return false;
		}
	}
}
