using System.Collections.Generic;
using Logs;
using NaughtyAttributes;
using UnityEngine;
using US13.Core.Attributes;
using US13.HealthV2.Living;
using US13.Mobs.Traversal;
using US13.Tilemaps.Behaviours.Layers;
using Util;

namespace US13.Mobs.BrainAI.States.SimpleBot
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

		private List<Vector3Int> selectedPath = null;
		private MobTraversal.TraversalDetails traversalDetails;
		private bool isTraversing = false;

		[SerializeField, MinMaxSlider(2, 10)] private Vector2Int hesitation = new Vector2Int(5, 7);
		private int hesitance = 0; //How many updates remain until it looks again

		[SerializeField] private List<AudibleMobDialogue> idleDialogue = new List<AudibleMobDialogue>();
		[SerializeField] List<AudibleMobDialogue> idleEmaggedDialogue = new List<AudibleMobDialogue>();

		[SerializeField] protected List<AudibleMobDialogue> foundTargetDialogue = new List<AudibleMobDialogue>();

		[SerializeField] private float dialogueChancePercent = 50;

		private void Start()
		{
			traversalDetails = new MobTraversal.TraversalDetails
			{
				OnTraversalFinalStep = () => OnDoneTraversalToLocation(Vector3Int.zero),
				OnRetryMoveToDirection = null,
				Strats = TraversalStrategies,
				CancelOnSlip = true,
				Algorithm = PathfinderType.AStar
			};
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
			if (taskState == false) return;
			if (DMMath.Prob(dialogueChancePercent))
			{
				AudibleMobDialogue toSay = taskState.IsEmagged ? idleEmaggedDialogue.PickRandom() : idleDialogue.PickRandom();
				taskState.Speak(toSay);
			}
		}

		public override void OnUpdateTick()
		{
			if (taskState == false) return;

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

			traversalDetails.TargetPosition = targetCell;
			isTraversing = pathfinder.QueueMovementGoalFromPath(traversalDetails, selectedPath);
			taskState.Speak(foundTargetDialogue.PickRandom());

			if (isTraversing == false)
			{
				master.RemoveAddState(this, wanderState);
				hesitance = Random.Range(hesitation.x, hesitation.y + 1);
			}
		}


		private bool IsStillTraversing()
		{
			if (pathfinder == false || isTraversing == false) return false;
			if (pathfinder.QueuedTargets != 0) return true;
			isTraversing = false;
			return false;
		}

		private void TraversalFailed(Vector3Int pos)
		{
			isTraversing = false;
			if(master.CurrentActiveStates.Contains(this) == false) return;

			if (foundTarget && Vector3.Distance(targetCell.ToWorld(targetMatrix), master.gameObject.AssumedWorldPosServer()) <= 1.5f)
			{
				master.RemoveAddState(this, taskState);
			}
			else
			{
				master.RemoveAddState(this, wanderState);
				hesitance = Random.Range(hesitation.x, hesitation.y + 1);
			}
		}

		private void SubscribeToPathfinderEvents()
		{
			isTraversing = false;
			if (pathfinder == false) return;
			pathfinder.OnDoneTraversalToLocation += OnDoneTraversalToLocation;
			pathfinder.OnTraversalFailedCompletely += TraversalFailed;

		}

		private void UnsubscribeToPathfinderEvents()
		{
			isTraversing = false;
			if (pathfinder == false) return;
			pathfinder.OnDoneTraversalToLocation -= OnDoneTraversalToLocation;
			pathfinder.OnTraversalFailedCompletely -= TraversalFailed;
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
			if (hesitance-- > 0) return false;

			selectedPath = taskState.FindTarget(out targetCell, out targetMatrix);
			selectedPath?.RemoveAt(selectedPath.Count - 1); //Stop one tile early as we have a range of 1 and the target might not be passable.

			return selectedPath is { Count: > 0 };
		}

		public void EmagMob()
		{
			taskState?.SetEmagState(true);
		}
	}
}