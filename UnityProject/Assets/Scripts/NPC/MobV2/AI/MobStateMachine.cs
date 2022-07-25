using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Systems.Mob.MobV2.AI
{
	public class MobStateMachine : MonoBehaviour
	{
		private List<MobAIStateBase> mobAIStates = new List<MobAIStateBase>();
		[SerializeField] private Transform mobStates;

		private MobAIStateBase currentState;

		public string CurrentState => currentState != null ? currentState.StateName : "idle";

		[HideInInspector] public bool ProccessingStates = false;

		private MobAIStateBase lastActionTaken;

		private void Awake()
		{
			mobAIStates = mobStates.GetComponentsInChildren<MobAIStateBase>().ToList();
			SortPriorities();
		}

		private void SortPriorities()
		{
			mobAIStates = mobAIStates.OrderBy(o=>o.Priority).ToList();
		}

		public void ForceIdle()
		{
			currentState.IsActive = false;
			currentState = null;
		}

		public void SetState(MobAIStateBase state)
		{
			currentState = state;
			StartCoroutine(state.DoAction());
		}

		public IEnumerator CycleThroughStates()
		{
			ProccessingStates = true;
			foreach (var state in mobAIStates)
			{
				if (lastActionTaken == state)
				{
					lastActionTaken = null;
					continue;
				}
				if(state.IsActive || state.IsIndependent || state.IsOnCooldown) continue;
				yield return WaitFor.EndOfFrame;
				if (state.TryAction() == false) continue;
				currentState = state;
				if(state.NoRepeatTwiceInARow) lastActionTaken = state;
				break;
			}
			ProccessingStates = false;
		}
	}
}