using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;


namespace Mobs.AI
{
	public class MobAI : MonoBehaviour
	{
		public List<MobState> CurrentActiveStates { get; private set; } = new List<MobState>();
		public List<MobState> MobStates { get; private set; } = new List<MobState>();
		[field: SerializeField] public Mob Mob { get; private set; } = null;
		[SerializeField] private MobState thinkingState;

		private void Awake()
		{
			Mob ??= GetComponent<Mob>();
		}

		private void Start()
		{
			GatherAllStates();
			SwitchState(null, thinkingState);
		}

		private void GatherAllStates()
		{
			var states = GetComponentsInChildren<MobState>();
			foreach (var state in states)
			{
				if (MobStates.Contains(state)) continue;
				MobStates.Add(state);
			}
		}

		public void SwitchState(MobState oldState, MobState newState)
		{
			if (newState is null) return;
			if (CurrentActiveStates.Contains(newState)) return;
			if (CurrentActiveStates.Any(x => x.Blacklist.Contains(newState))) return;

			if (oldState != null && CurrentActiveStates.Contains(oldState))
			{
				RemoveState(oldState);
			}
			AddState(newState);
		}

		public void RemoveState(MobState oldState)
		{
			UpdateManager.Remove(oldState.UpdateType, () => oldState.OnUpdateTick(this));
			oldState.OnExitState(this);
			CurrentActiveStates.Remove(oldState);
		}

		public void AddState(MobState newState)
		{
			CurrentActiveStates.Add(newState);
			newState.OnEnterState(this);

			if (newState.UpdateType == CallbackType.PERIODIC_UPDATE)
			{
				UpdateManager.Add(()=> newState.OnUpdateTick(this), newState.PeriodicUpdateInterval);
			}
			else
			{
				UpdateManager.Add(newState.UpdateType, ()=> newState.OnUpdateTick(this));
			}
		}
	}
}
