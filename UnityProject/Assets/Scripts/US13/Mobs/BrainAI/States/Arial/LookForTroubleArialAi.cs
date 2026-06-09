using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using US13.Core;
using US13.Core.Addressables.Types;
using US13.Core.Attributes;
using US13.HealthV2.Living;
using US13.Items.Food;
using US13.Managers;
using US13.Managers.NetworkManagement;
using US13.Mobs.BrainAI.States.GenericStates;
using US13.Mobs.Traversal;
using Util;

namespace US13.Mobs.BrainAI.States.Arial
{
	public class LookForTroubleArialAi : BrainMobState
	{
		private GameObject target;
		private MobTraversal pathfinder => master.Traversal;
		[SerializeField] private BrainWanderState wanderState;
		[SerializeField] private CauseTroubleArialAi troubelState;
		[SerializeField] private GenericAiStateFollowMob stalkState;
		[SerializeField] private List<AddressableAudioSource> stateEnterSounds = new List<AddressableAudioSource>();

		[SerializeReference, SelectImplementation(typeof(ITraversalStrat))]
		public List<ITraversalStrat> TraversalStrategies = new List<ITraversalStrat>();

		private bool isTraversing = false;
		private bool isStalking = true;
		private bool isPranking = false;

		private int ticksElapsed = 0;

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
				master.RemoveAddState(null, wanderState);
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

			ticksElapsed++;
			if (ticksElapsed > 50)
			{
				ticksElapsed = 0;
				isStalking = DMMath.Prob(50);
				isPranking = !isStalking;
			}

			if (target == null)
			{
				target = DecideTarget();
				if (target != null)
				{
					master.RemoveAddState(wanderState, null);
				}
				return;
			}

			StalkBehavior();
			PrankBehavior();
		}

		/// <summary>
		/// Makes Arials follow a target until they decide to prank them.
		/// </summary>
		private void StalkBehavior()
		{
			//(Max): List checks maybe not optimal? Find better way to check if stalkState is active without asking a list if needed.
			if (isPranking && master.CurrentActiveStates.Contains(stalkState))
			{
				master.RemoveAddState(stalkState, null);
				return;
			}
			else if (master.CurrentActiveStates.Contains(stalkState) == false)
			{
				master.RemoveAddState(null, stalkState);
			}
		}

		private void PrankBehavior()
		{
			if (isStalking) return;
			if (pathfinder.QueueMovementGoal(target.gameObject.TileLocalPosition().To3Int(),
				    () => OnDoneTraversalToLocation(Vector3Int.zero),
				    null, TraversalStrategies, PathfinderType.AStar, true))
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
				master.RemoveAddState(this, troubelState);
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
					stalkState.MobToFollow = player.PlayerScript.playerHealth;
					return player.gameObject;
				}
			}
			stalkState.MobToFollow = null;
			var edibles = ComponentsTracker<Edible>.GetAllNearbyTypesToTarget(master.gameObject, 20, false);
			return edibles?.Count > 5 ? edibles.PickRandom()?.gameObject : null;
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