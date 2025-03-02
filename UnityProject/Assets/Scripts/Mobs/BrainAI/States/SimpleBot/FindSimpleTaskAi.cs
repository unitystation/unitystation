using System;
using System.Collections.Generic;
using System.Linq;
using AddressableReferences;
using Core;
using Core.Editor.Attributes;
using HealthV2;
using Items.Food;
using Logs;
using Mobs.AI;
using Mobs.Traversal;
using Mobs.Traversal.Strategies;
using NUnit.Framework.Constraints;
using PathFinding;
using Systems.Faith;
using Systems.Spawns;
using UnityEngine;

namespace Mobs.BrainAI.States.SimpleBot
{
	public class FindSimpleTaskAi : BrainMobState
	{
		private enum BotType
		{
			FloorBot = 0,
			CleanBot = 1,
			Medibot = 2,
		}

		[SerializeField] private BotType botType = BotType.FloorBot;

		private Vector3Int targetCell;
		private Matrix targetMatrix;

		private bool foundTarget = false;

		private MobTraversal pathfinder => master.Traversal;

		[SerializeField] private BrainWanderState wanderState;
		private SimpleBotTaskAi taskState;

		[SerializeReference, SelectImplementation(typeof(ITraversalStrat))]
		public List<ITraversalStrat> TraversalStrategies = new List<ITraversalStrat>();

		private bool isTraversing = false;

		private void Awake()
		{
			switch (botType)
			{
				case BotType.Medibot:
					taskState = GetComponent<FloorBotTaskAi>();
					break;
				case BotType.CleanBot:
					taskState = GetComponent<CleanBotTaskAi>();
					break;
				case BotType.FloorBot:
				default:
					taskState = GetComponent<FloorBotTaskAi>();
					break;
			}
			if (taskState == null)
			{
				Loggy.Error(
					$"FindSimpleTaskAi: Tried to find task state for {gameObject.name} of type {botType}, but the taskState component could not be found.");
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
			foundTarget = taskState.FindTarget(out targetCell, out targetMatrix);
			if (foundTarget == false) master.AddRemoveState(null, wanderState);
			else OnUpdateTick();
		}

		public override void OnExitState()
		{
			UnsubscribeToPathfinderEvents();
			targetCell = Vector3Int.zero;
			targetMatrix = null;
			foundTarget = false;
		}

		public override void OnUpdateTick()
		{
			if (IsStillTraversing()) return;
			if (LivingHealthMaster.IsSoftCrit || LivingHealthMaster.IsCrit || LivingHealthMaster.IsDead)
			{
				isTraversing = false;
				return;
			}

			if (foundTarget == false)
			{
				foundTarget = taskState.FindTarget(out targetCell, out targetMatrix);
				if (foundTarget) master.AddRemoveState(wanderState, null);
				return;
			}

			if (Vector3.Distance(targetCell.ToWorld(targetMatrix), LivingHealthMaster.gameObject.AssumedWorldPosServer()) <= 1.1f)
			{
				master.AddRemoveState(this, taskState);
			}

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
			OnUpdateTick();
		}

		public override bool HasGoal()
		{
			return foundTarget;
		}
	}
}