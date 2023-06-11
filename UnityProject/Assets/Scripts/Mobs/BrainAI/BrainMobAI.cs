using System;
using System.Collections.Generic;
using System.Linq;
using HealthV2;
using Items.Implants.Organs;
using NaughtyAttributes;
using UnityEngine;

namespace Mobs.BrainAI
{
	public class BrainMobAI : BodyPartFunctionality
	{
		[field: ReadOnly] public List<BrainMobState> CurrentActiveStates { get; private set; } = new List<BrainMobState>();
		[field: ReadOnly] public List<BrainMobState> MobStates { get; private set; } = new List<BrainMobState>();
		[SerializeField] private BrainMobState thinkingState;


		public Brain Brain;

		public bool DEBUGoverride = false;


		public bool IsControlledByPlayer
		{
			get
			{
				if (DEBUGoverride)
				{
					return false;
				}
				if (Brain.PossessingMind == null) return false;
				return Brain.PossessingMind.ControlledBy != null;
			}
		}

		public CommonComponents Body;

		public override void OnRemovedFromBody(LivingHealthMasterBase livingHealth)
		{
			Body = null;
		}

		public override void OnAddedToBody(LivingHealthMasterBase livingHealth)
		{
			Body = livingHealth.GetComponent<CommonComponents>();
		} //Warning only add body parts do not remove body parts in this


		private void Start()
		{
			Brain = this.GetComponent<Brain>();
			GatherAllStates();
			AddRemoveState(null, thinkingState);
		}

		private void GatherAllStates()
		{
			var states = GetComponentsInChildren<BrainMobState>();
			foreach (var state in states)
			{
				if (MobStates.Contains(state)) continue;
				MobStates.Add(state);
				state.master = this;
			}
		}

		public void AddRemoveState(BrainMobState oldState, BrainMobState newState)
		{
			if (newState is null) return;
			if (CurrentActiveStates.Any(x => x.Blacklist.Contains(newState))) return;

			if (oldState != null && CurrentActiveStates.Contains(oldState))
			{
				RemoveState(oldState);
			}
			AddState(newState);
		}

		private void RemoveState(BrainMobState oldState)
		{
			oldState.OnExitStateInternal();
			CurrentActiveStates.Remove(oldState);
		}

		private void AddState(BrainMobState newState)
		{
			CurrentActiveStates.Add(newState);
			newState.OnEnterStateInternal();
		}


		public void OnDestroy()
		{
			foreach (var State in CurrentActiveStates.ToArray())
			{
				RemoveState(State);
			}
		}
	}
}
