using System.Collections.Generic;
using Core.Editor.Attributes;
using HealthV2;
using Logs;
using Mobs.Traversal;
using UnityEngine;
using Systems.Character;
using UI.CharacterCreator;

namespace Mobs.BrainAI.States.SimpleBot
{
	public class FindSimpleTaskAi : BrainMobState
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

			if(foundTarget) OnUpdateTick();
		}

		public override void OnExitState()
		{
			UnsubscribeToPathfinderEvents();
			targetCell = Vector3Int.zero;
			targetMatrix = null;
		}

		public override void OnUpdateTick()
		{
			Loggy.Error("Enter Tick Update");
			if (taskState == false) return;
			Loggy.Error("Has Task State");
			if (IsStillTraversing()) return;
			Loggy.Error("Is not traversing");
			if (LivingHealthMaster.IsSoftCrit || LivingHealthMaster.IsCrit || LivingHealthMaster.IsDead)
			{
				isTraversing = false;
				return;
			}
			Loggy.Error("Is Alive");
			if (foundTarget == false && HasGoal() == false)
			{
				Loggy.Error("No target, wandering");
				master.AddRemoveState(this, wanderState);
				return;
			}
		    Loggy.Error("Pathfinding to target");
			isTraversing = pathfinder.QueueMovementGoal(targetCell, () => OnDoneTraversalToLocation(Vector3Int.zero), null, TraversalStrategies, true);
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

			if (Vector3.Distance(targetCell.ToWorld(targetMatrix), master.gameObject.AssumedWorldPosServer()) <= 1.1f)
			{
				master.AddRemoveState(this, taskState);
			}
			else OnUpdateTick();
		}

		public override bool HasGoal()
		{
			foundTarget = taskState.FindTarget(out targetCell, out targetMatrix);
			return foundTarget;
		}
	}
}