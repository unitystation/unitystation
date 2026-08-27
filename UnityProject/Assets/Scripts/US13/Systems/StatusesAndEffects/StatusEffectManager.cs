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
		private readonly List<StatusEffect> statusesToTick = new();

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
			statusesToTick.Clear();
			statusesToTick.AddRange(Statuses);
			foreach (var effect in statusesToTick)
			{
				if (effect == false || Statuses.Contains(effect) == false) continue;
				effect.DoEffectTick(gameObject);
			}
		}

		public void AddStatus(StatusEffect status)
		{
			if (status == false) return;

			if (TryGetActiveStatus(status, out var activeStatus))
			{
				HandleStackableStatusAddition(status, activeStatus);
				HandleImmediateStatusAddition(activeStatus);
				return;
			}

			activeStatus = CreateActiveStatus(status);
			HandleExpirableStatusAddition(activeStatus);
			HandleStackableStatusAddition(activeStatus, null);
			HandleImmediateStatusAddition(activeStatus);
			activeStatus.Initialize(gameObject);
			Statuses.Add(activeStatus);
		}

		private void HandleExpirableStatusAddition(StatusEffect status)
		{
			if (status is IExpirableStatus expirable)
			{
				expirable.Expired += OnExpiredStatus;
			}
		}

		private void HandleStackableStatusAddition(StatusEffect status, StatusEffect activeStatus)
		{
			if (status is not IStackableStatus newStackable) return;
			if (activeStatus is IStackableStatus oldStackable)
			{
				oldStackable.AddStack(newStackable.InitialStacks);
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
			if (gameObject == null ) return;
			if (TryGetActiveStatus(status, out var activeStatus) == false) return;
			if (activeStatus is IExpirableStatus expirable)
			{
				expirable.Expired -= OnExpiredStatus;
			}
			activeStatus.OnRemoved(gameObject);
			Statuses.Remove(activeStatus);
			DestroyStatusInstance(activeStatus);
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
			return TryGetActiveStatus(status, out _);
		}

		public bool HasStatusByName(string statusName)
		{
			foreach (var status in Statuses)
			{
				if (GetStatusName(status) == statusName) return true;
			}
			return false;
		}

		private StatusEffect CreateActiveStatus(StatusEffect status)
		{
			var activeStatus = IsRuntimeClone(status) ? status : Instantiate(status);
			activeStatus.name = GetStatusName(status);
			return activeStatus;
		}

		private bool TryGetActiveStatus(StatusEffect status, out StatusEffect activeStatus)
		{
			activeStatus = null;
			if (status == false) return false;
			var statusName = GetStatusName(status);
			foreach (var existingStatus in Statuses)
			{
				if (existingStatus == false) continue;
				if (GetStatusName(existingStatus) != statusName) continue;
				activeStatus = existingStatus;
				return true;
			}
			return false;
		}

		private static string GetStatusName(StatusEffect status)
		{
			var statusName = status.name ?? string.Empty;
			const string cloneSuffix = "(Clone)";
			if (statusName.EndsWith(cloneSuffix, StringComparison.Ordinal) == false) return statusName;
			return statusName.Substring(0, statusName.Length - cloneSuffix.Length).TrimEnd();
		}

		private static bool IsRuntimeClone(StatusEffect status)
		{
			return (status.name ?? string.Empty).EndsWith("(Clone)", StringComparison.Ordinal);
		}

		private static void DestroyStatusInstance(StatusEffect status)
		{
			if (Application.isPlaying)
			{
				Destroy(status);
				return;
			}
			DestroyImmediate(status);
		}
	}
}
