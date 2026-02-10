using System.Collections.Generic;
using Logs;
using UnityEngine;
using US13.HealthV2.Living;
using US13.Managers.NetworkManagement;
using US13.Managers.UpdateManager;
using Util;

namespace US13.Mobs.BrainAI
{
	public abstract class BrainMobState : BodyPartFunctionality
	{
		[field: SerializeField] public List<BrainMobState> Blacklist { get; private set; } = new List<BrainMobState>();
		[field: SerializeField] public CallbackType UpdateType { get; private set; } = CallbackType.PERIODIC_UPDATE;
		[field: SerializeField] public float PeriodicUpdateInterval { get; set; } = 1.5f;

		[field: SerializeField] public BrainMobAI master { get; set; }

		public void OnEnterStateInternal()
		{
			if (CustomNetworkManager.IsServer == false) return;
			Loggy.Info($"Registering periodic update for {GetType().Name} with update type: {UpdateType}", Category.Mobs);
			if (UpdateType == CallbackType.PERIODIC_UPDATE)
			{
				UpdateManager.Add(InternalOnUpdateTick, PeriodicUpdateInterval);
			}
			else
			{
				UpdateManager.Add(UpdateType, InternalOnUpdateTick);
			}
			OnEnterState();
		}


		public void OnExitStateInternal()
		{
			UpdateManager.Remove(UpdateType, InternalOnUpdateTick);
			OnExitState();
		}
		public abstract void OnEnterState();
		public abstract void OnExitState();

		public void InternalOnUpdateTick()
		{
			if (LivingHealthMaster?.RegisterTile?.Matrix?.PresentPlayers?.Count == 0) return;
			if (master.OrNull()?.Body.OrNull()?.LivingHealth == null) return;
			if (master.IsControlledByPlayer) return;
			if (master.Body.LivingHealth.IsDead) return;
			if (master.Body.UniversalObjectPhysics.IsVisible == false) return;
			OnUpdateTick();
		}

		public abstract void OnUpdateTick();
		public abstract bool HasGoal();
	}
}
