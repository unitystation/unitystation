using System.Collections.Generic;
using System.Linq;
using AddressableReferences;
using Core;
using Items.Food;
using Logs;
using Mobs.AI;
using PathFinding;
using Systems.Spawns;
using UnityEngine;

namespace Mobs.BrainAI.States.Arial
{
	public class LookForTroubleArialAi : BrainMobState
	{
		private GameObject target;
		[SerializeField] private BrainWanderState wanderState;
		[SerializeField] private CauseTroubleArialAi troubelState;
		[SerializeField] private MobPathfinderV2 pathfinder;
		[SerializeField] private List<AddressableAudioSource> stateEnterSounds = new List<AddressableAudioSource>();

		private List<Node> path = new List<Node>();

		public void Start()
		{
			base.Awake();
			pathfinder = LivingHealthMaster.playerScript.GetComponent<MobPathfinderV2>();
			if (pathfinder == null)
			{
				Loggy.LogError("[LookForTroubleArialAi] - NO PATHFINDER DETECTED. ARIAL WILL BE STUCK.");
			}
		}

		public override void OnEnterState()
		{
			target = DecideTarget();
			if (target == null)
			{
				//enter wander state.
				master.AddRemoveState(null, wanderState);
			}
			SoundManager.PlayNetworkedAtPos(stateEnterSounds.PickRandom(), LivingHealthMaster.gameObject.AssumedWorldPosServer());
		}

		public override void OnExitState()
		{
			target = null;
			path = null;
		}

		public override void OnUpdateTick()
		{
			if (LivingHealthMaster.IsSoftCrit || LivingHealthMaster.IsCrit || LivingHealthMaster.IsDead) return;
			if (target == null)
			{
				target = DecideTarget();
				if (target != null)
				{
					master.AddRemoveState(wanderState, null);
				}
				return;
			}
			path = pathfinder.FindNewPath(
				MatrixManager.WorldToLocal(LivingHealthMaster.playerScript.playerMove.OfficialPosition,
					LivingHealthMaster.RegisterTile.Matrix).RoundTo2Int(),
				target.gameObject.AssumedWorldPosServer().RoundTo2Int());
			Loggy.Log($"{target} - {target.OrNull()?.AssumedWorldPosServer()}");
			if (path != null)
			{
				pathfinder.FollowPath(path);
			}
			else
			{
				if (DMMath.Prob(5))
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
					LivingHealthMaster.playerScript.playerMove.SetTransform(points.PickRandom().gameObject.AssumedWorldPosServer(), true);
					SoundManager.PlayNetworkedAtPos(stateEnterSounds.PickRandom(), LivingHealthMaster.gameObject.AssumedWorldPosServer());
				}
				target = DecideTarget();
				return;
			}
			if (Vector3.Distance(target.AssumedWorldPosServer(), master.gameObject.AssumedWorldPosServer()) < 3.75f)
			{
				troubelState.Target = target;
				master.AddRemoveState(this, troubelState);
			}
		}

		public override bool HasGoal()
		{
			return target is not null;
		}

		private GameObject DecideTarget()
		{
			foreach (var player in PlayerList.Instance.GetAlivePlayers())
			{
				if (Vector3.Distance(player.Script.gameObject.AssumedWorldPosServer(), master.gameObject.AssumedWorldPosServer()) < 12)
				{
					return player.Script.gameObject;
				}
			}
			var edibles = ComponentsTracker<Edible>.GetAllNearbyTypesToTarget(master.gameObject, 20, false);
			return edibles?.Count > 5 ? edibles.PickRandom().gameObject : null;
		}
	}
}