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
using PathFinding;
using Systems.Faith;
using Systems.Spawns;
using UnityEngine;

namespace Mobs.BrainAI.States.Arial
{
	public class LookForTroubleArialAi : BrainMobState
	{
		private GameObject target;
		private MobTraversal pathfinder => master.Traversal;
		[SerializeField] private BrainWanderState wanderState;
		[SerializeField] private CauseTroubleArialAi troubelState;
		[SerializeField] private List<AddressableAudioSource> stateEnterSounds = new List<AddressableAudioSource>();

		[SerializeReference, SelectImplementation(typeof(ITraversalStrat))]
		public List<ITraversalStrat> TraversalStrategies = new List<ITraversalStrat>();

		private bool isTraversing = false;

		public override void OnRemovedFromBody(LivingHealthMasterBase livingHealth, GameObject source = null)
		{
			UnsubscribeToPathfinderEvents();
			base.OnRemovedFromBody(livingHealth, source);
		}

		public override void OnEnterState()
		{
			SubscribeToPathfinderEvents();
			target = null;
			isTraversing = false;
			target = DecideTarget();
			if (target == null)
			{
				//enter wander state.
				master.AddRemoveState(null, wanderState);
			}
			SoundManager.PlayNetworkedAtPos(stateEnterSounds.PickRandom(),
				LivingHealthMaster.gameObject.AssumedWorldPosServer());
		}

		public override void OnExitState()
		{
			UnsubscribeToPathfinderEvents();
			target = null;
		}

		public override void OnUpdateTick()
		{
			if (IsStillTraversing()) return;
			if (LivingHealthMaster.IsSoftCrit || LivingHealthMaster.IsCrit || LivingHealthMaster.IsDead)
			{
				isTraversing = false;
				return;
			}

			if (target == null)
			{
				target = DecideTarget();
				if (target != null)
				{
					master.AddRemoveState(wanderState, null);
				}
				return;
			}

			if (pathfinder.QueueMovementGoal(target.gameObject.TileLocalPosition().To3Int(),
				    () => OnDoneTraversalToLocation(Vector3Int.zero),
				    null, TraversalStrategies, true))
			{
				isTraversing = true;
			}
			else
			{
				TeleportToRandomPlaceOnStation();
				return;
			}

			if (Vector3.Distance(target.AssumedWorldPosServer(), master.gameObject.AssumedWorldPosServer()) < 3.75f)
			{
				troubelState.Target = target;
				master.AddRemoveState(this, troubelState);
			}
		}

		private void TeleportToRandomPlaceOnStation()
		{
			if (DMMath.Prob(9))
			{
				List<SpawnPointCategory> spawnPointCategory = new List<SpawnPointCategory>
				{
					SpawnPointCategory.Bartender,
					SpawnPointCategory.Assistant,
					SpawnPointCategory.Botanist,
					SpawnPointCategory.Chaplain,
					SpawnPointCategory.Cook,
				};
				List<Transform> points = new List<Transform>();
				foreach (var point in spawnPointCategory)
				{
					points.AddRange(SpawnPoint.GetPointsForCategory(point).ToList());
				}

				LivingHealthMaster.playerScript.playerMove.SetTransform(
					points.PickRandom().gameObject.AssumedWorldPosServer(), true);
				SoundManager.PlayNetworkedAtPos(stateEnterSounds.PickRandom(),
					LivingHealthMaster.gameObject.AssumedWorldPosServer());
			}

			target = DecideTarget();
		}

		public override bool HasGoal()
		{
			return target is not null;
		}

		private GameObject DecideTarget()
		{
			foreach (var player in LivingHealthMaster.RegisterTile.Matrix.PresentPlayers)
			{
				if (player == master.Traversal.Mob.RegisterPlayer) continue;
				if (Vector3.Distance(player.gameObject.AssumedWorldPosServer(),
					    master.gameObject.AssumedWorldPosServer()) < 12)
				{
					return player.gameObject;
				}
			}

			var edibles = ComponentsTracker<Edible>.GetAllNearbyTypesToTarget(master.gameObject, 20, false);
			return edibles?.Count > 5 ? edibles.PickRandom().gameObject : null;
		}

		private bool IsStillTraversing()
		{
			if (pathfinder == null || isTraversing == false) return false;
			if (pathfinder.QueuedTargets != 0) return true;
			isTraversing = false;
			return false;
		}

		private void SubscribeToPathfinderEvents()
		{
			isTraversing = false;
			if (pathfinder == null) return;
			pathfinder.OnDoneTraversalToLocation += OnDoneTraversalToLocation;
			pathfinder.OnTraversalFailedCompletely += OnDoneTraversalToLocation;
		}

		private void UnsubscribeToPathfinderEvents()
		{
			isTraversing = false;
			if (pathfinder == null) return;
			pathfinder.OnDoneTraversalToLocation -= OnDoneTraversalToLocation;
			pathfinder.OnTraversalFailedCompletely -= OnDoneTraversalToLocation;
		}

		private void OnDoneTraversalToLocation(Vector3Int pos)
		{
			isTraversing = false;
		}
	}
}