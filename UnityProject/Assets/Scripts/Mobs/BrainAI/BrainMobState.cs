using System.Collections;
using System.Collections.Generic;
using HealthV2;
using Mobs.BrainAI;
using UnityEngine;

public abstract class BrainMobState : BodyPartFunctionality
{
	[field: SerializeField] public List<BrainMobState> Blacklist { get; private set; } = new List<BrainMobState>();
	[field: SerializeField] public CallbackType UpdateType { get; private set; } = CallbackType.PERIODIC_UPDATE;
	[field: SerializeField] public float PeriodicUpdateInterval { get; set; } = 1.5f;

	[field: SerializeField] public BrainMobAI master { get; set; }

	public void OnEnterStateInternal()
	{
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
		if (master.OrNull()?.Body.OrNull()?.LivingHealth == null) return;
		if (master.IsControlledByPlayer) return;
		if (master.Body.LivingHealth.IsDead) return;
		if (master.Body.UniversalObjectPhysics.IsVisible == false) return;
		OnUpdateTick();

	}

	public abstract void OnUpdateTick();
	public abstract bool HasGoal();
}
