using System.Collections.Generic;
using Core.Editor.Attributes;
using Core.Utils;
using HealthV2;
using Logs;
using Mobs.Traversal;
using UnityEngine;
using Systems.Character;
using UI.CharacterCreator;

namespace Mobs.BrainAI.States.SimpleBot
{
	public class FindSimpleTaskAi : BrainMobState, ICanBeEmaggedMob
	{
		private Vector3Int targetCell;
		private Matrix targetMatrix;

		private bool foundTarget = false;

		private MobTraversal pathfinder => master.Traversal;

		[SerializeField] private BrainWanderState wanderState;
		private SimpleBotTaskAi taskState;

		[SerializeReference, SelectImplementation(typeof(ITraversalStrat))]
		public List<ITraversalStrat> TraversalStrategies = new List<ITraversalStrat>();

		private bool isTraversing = false;
		private int refuseReturn = 0;

		[SerializeField] private List<BotDialogue> idleDialogue = new List<BotDialogue>();
		[SerializeField] List<BotDialogue> idleEmaggedDialogue = new List<BotDialogue>();
		[SerializeField] private float dialogueChancePercent = 20;

		private void Start()
		{
			taskState = GetComponent<SimpleBotTaskAi>();
			if (taskState == null)
			{
				Loggy.Error(
					$"FindSimpleTaskAi: Tried to find task state for {gameObject.name}, but the taskState component could not be found.");
			}
		}

		public override void OnRemovedFromBody(LivingHealthMasterBase livingHealth, GameObject source = null)
		{
			UnsubscribeToPathfinderEvents();
			base.OnRemovedFromBody(livingHealth, source);
		}

		public override void OnEnterState()
		{
			SubscribeToPathfinderEvents();

			isTraversing = false;
		}

		public override void OnExitState()
		{
			UnsubscribeToPathfinderEvents();
			foundTarget = false;
		}

		public override void OnUpdateTick()
		{
			if (taskState == false) return;

			if (DMMath.Prob(dialogueChancePercent))
			{
				BotDialogue toSay = taskState.IsEmagged ? idleEmaggedDialogue.PickRandom() : idleDialogue.PickRandom();
				taskState.Speak(toSay.Transcription);

				if(toSay.audioSource != null) SoundManager.PlayNetworkedAtPosAsync(toSay.audioSource,
					LivingHealthMaster.gameObject.AssumedWorldPosServer(), global: false);
			}

			if (IsStillTraversing()) return;

			if (LivingHealthMaster.IsSoftCrit || LivingHealthMaster.IsCrit || LivingHealthMaster.IsDead)
			{
				isTraversing = false;
				return;
			}

			if (foundTarget == false && HasGoal() == false)
			{
				master.RemoveAddState(this, wanderState);
				return;
			}

			isTraversing = pathfinder.QueueMovementGoal(targetCell, () => OnDoneTraversalToLocation(Vector3Int.zero), null, TraversalStrategies, true);

			if (isTraversing == false)
			{
				master.RemoveAddState(this, wanderState);
				refuseReturn = 7;
			}
		}


		private bool IsStillTraversing()
		{
			if (pathfinder == false || isTraversing == false) return false;
			if (pathfinder.QueuedTargets != 0) return true;
			isTraversing = false;
			return false;
		}

		private void SubscribeToPathfinderEvents()
		{
			isTraversing = false;
			if (pathfinder == false) return;
			pathfinder.OnDoneTraversalToLocation += OnDoneTraversalToLocation;
			pathfinder.OnTraversalFailedCompletely += OnDoneTraversalToLocation;
		}

		private void UnsubscribeToPathfinderEvents()
		{
			isTraversing = false;
			if (pathfinder == false) return;
			pathfinder.OnDoneTraversalToLocation -= OnDoneTraversalToLocation;
			pathfinder.OnTraversalFailedCompletely -= OnDoneTraversalToLocation;
		}

		private void OnDoneTraversalToLocation(Vector3Int pos)
		{
			isTraversing = false;
			if(master.CurrentActiveStates.Contains(this) == false) return;

			if (Vector3.Distance(targetCell.ToWorld(targetMatrix), master.gameObject.AssumedWorldPosServer()) <= 1.5f)
			{
				master.RemoveAddState(this, taskState);
			}
		}

		public override bool HasGoal()
		{
			//If the bot was unable to path find to a target, we force it to wander for awhile so it doesn't get stuck
			//repeatedly trying to reach an unreachable target
			if (refuseReturn-- > 0) return false;

			foundTarget = taskState.FindTarget(out targetCell, out targetMatrix);
			return foundTarget;
		}

		public void EmagMob()
		{
			taskState?.SetEmagState(true);
		}
	}

	public interface ICanBeEmaggedMob
	{
		public void EmagMob();
	}
}