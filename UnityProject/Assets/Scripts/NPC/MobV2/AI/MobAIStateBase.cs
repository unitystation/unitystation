using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Systems.Mob.MobV2.AI
{
	public class MobAIStateBase : MonoBehaviour
	{
		/// <summary>
		/// What is the name of this state? (I.e: Idle, attacking, eating, etc)
		/// </summary>
		public string StateName = "???";

		/// <summary>
		/// Does this state run on the UpdateManager rather than the state machine?
		/// </summary>
		public bool IsIndependent = false;

		/// <summary>
		/// Do you want this state to not repeat itself twice in a row?
		/// </summary>
		public bool NoRepeatTwiceInARow = true;

		/// <summary>
		/// Is this AI doing stuff?
		/// </summary>
		public bool IsActive = false;

		/// <summary>
		/// The priority that this action should be done next
		/// </summary>
		public int Priority;

		/// <summary>
		/// Data container to help cache and process all sorts of data for AI
		/// </summary>
		protected AiBlackboard blackBoard;

		/// <summary>
		/// To replicate the same slowness of SS13 stop mobs from constantly doing stuff every frame, we force mobs to
		/// only act once every 3 seconds if they're acting outside the state machine.
		/// </summary>
		protected const float INDEPENDENT_UPDATE_TIME = 3f;

		public MobStateMachine OwnerMachine;

		/// <summary>
		/// The amount of time before this state can be run again.
		/// </summary>
		public float StateCooldown = 5f;

		public bool IsOnCooldown = false;

		private void Awake()
		{
			blackBoard = GetComponentInParent<AiBlackboard>();
			if(OwnerMachine == null) Logger.LogError("[MobAI] - MISSING STATE MACHINE REFERENCE.");
		}

		private void OnEnable()
		{
			if(IsIndependent) UpdateManager.Add(IndependentBehavior, INDEPENDENT_UPDATE_TIME);
		}

		private void OnDisable()
		{
			if(IsIndependent) UpdateManager.Remove(CallbackType.PERIODIC_UPDATE, IndependentBehavior);
		}

		public virtual bool TryAction()
		{
			//insert your logic here
			StartCoroutine(DoAction());
			return true;
		}

		public virtual IEnumerator DoAction()
		{
			//insert your logic here
			IsActive = true;
			StartCoroutine(Cooldown());
			OnComplete();
			yield return null;
		}
		protected virtual void IndependentBehavior() { }

		protected virtual void OnComplete()
		{
			IsActive = false;
		}

		private IEnumerator Cooldown()
		{
			IsOnCooldown = true;
			yield return WaitFor.Seconds(StateCooldown);
			IsOnCooldown = false;
		}
	}
}