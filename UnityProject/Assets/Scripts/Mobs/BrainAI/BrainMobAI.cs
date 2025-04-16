using System.Collections.Generic;
using System.Linq;
using HealthV2;
using Items.Implants.Organs;
using Logs;
using Mobs.Traversal;
using NaughtyAttributes;
using UnityEngine;

namespace Mobs.BrainAI
{
	public class BrainMobAI : BodyPartFunctionality
	{
		[field: ReadOnly, SerializeField] public List<BrainMobState> CurrentActiveStates { get; private set; } = new List<BrainMobState>();
		[field: SerializeField] public List<BrainMobState> MobStates { get; private set; } = new List<BrainMobState>();
		[field: SerializeField] public BrainMobState thinkingState { get; private set; }

		//Inelegant to set these on start instead of using properties, but was the least clunky way to get these fields editable in the inspector
		[field: SerializeField] private PathfinderType InitialPathfindingMethod = PathfinderType.AStar;
		[field: SerializeField] private bool InitialDontCheckForDoorsOverride = false;

		public Brain Brain;
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

			Traversal.PathfindingMethod = InitialPathfindingMethod;
			Traversal.DontCheckForDoorsOverride = InitialDontCheckForDoorsOverride;
		} //Warning only add body parts do not remove body parts in this


		private void Start()
		{
			Brain = this.GetComponent<Brain>();
			GatherAllStates();
			RemoveAddState(null, thinkingState);
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


		public void OnDestroy()
		{
			foreach (var State in CurrentActiveStates.ToArray())
			{
				RemoveState(State);
			}
		}
	}
}
