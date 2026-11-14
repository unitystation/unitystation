using System.Collections.Generic;
using System.Linq;
using Logs;
using NaughtyAttributes;
using UnityEngine;
using US13.Core.Utils;
using US13.HealthV2.Living;
using US13.Items.Implants.Organs;
using US13.Mobs.Traversal;

namespace US13.Mobs.BrainAI
{
	[System.Serializable]
	public class BrainMobAI : BodyPartBehavior
	{
		[field: ReadOnly, SerializeReference] public List<BrainMobState> CurrentActiveStates { get; private set; } = new List<BrainMobState>();
		[field: SerializeReference] public List<BrainMobState> MobStates { get; private set; } = new List<BrainMobState>();
		[field: SerializeReference] public BrainMobState thinkingState { get; private set; }

		[UnityEngine.SerializeReference] public Brain Brain;
		public MobTraversal Traversal => Brain.Traversal;

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

		public override void OnRemovedFromBody(LivingHealthMasterBase livingHealth, GameObject source = null)
		{
			Body = null;
		}

		public override void OnAddedToBody(LivingHealthMasterBase livingHealth)
		{
			Body = livingHealth.GetComponent<CommonComponents>();
		} //Warning only add body parts do not remove body parts in this


		public override void OnBehaviorStarted()
		{
			Brain = GetBodyPartBehavior<Brain>();
			GatherAllStates();
			RemoveAddState(null, thinkingState);
		}

		private void GatherAllStates()
		{
			var states = GetBodyPartBehaviorsInChildren<BrainMobState>();
			foreach (var state in states)
			{
				if (MobStates.Contains(state)) continue;
				MobStates.Add(state);
				state.master = this;
			}
		}

		public void RemoveAddState(BrainMobState oldState, BrainMobState newState)
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
			if (CurrentActiveStates.Contains(newState))
			{
				Loggy.Warning($"Can't add state {newState.GetType().Name} to {gameObject.name} because it's already in the list.");
				return;
			}
			CurrentActiveStates.Add(newState);
			newState.OnEnterStateInternal();
		}


		public override void OnBehaviorDestroyed()
		{
			foreach (var State in CurrentActiveStates.ToArray())
			{
				RemoveState(State);
			}
		}
	}
}
