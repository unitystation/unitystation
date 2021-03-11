using UnityEngine;

namespace Systems.MobAIs
{
	/// <summary>
	/// AI brain specifically trained to perform
	/// following behaviours
	/// </summary>
	public class MobFollow : MobAgent
	{
		public GameObject FollowTarget;
		protected RegisterTile TargetTile;

		private float distanceCache = 0;

		public override void OnEnable()
		{
			base.OnEnable();
			OriginTile = GetComponent<RegisterTile>();
		}
		public override void AgentReset()
		{
			distanceCache = 0;
			base.AgentReset();
		}

		protected override void AgentServerStart()
		{
			//begin following:
			if (FollowTarget != null)
			{
				activated = true;
			}
		}

		/// <summary>
		/// Make the mob start following a target
		/// </summary>
		public void StartFollowing(GameObject target)
		{
			FollowTarget = target;
			TargetTile = FollowTarget.GetComponent<RegisterTile>();
			Activate();
		}

		/// <summary>
		/// Returns the distance between the mob and the target
		/// </summary>
		public float TargetDistance()
		{
			return Vector3.Distance(TargetTile.WorldPositionServer, OriginTile.WorldPositionServer);
		}

		public override void CollectObservations()
		{
			//You need to feed ML agents null obs
			//if the follow target is null
			//otherwise ML agents will break
			if (FollowTarget == null)
			{
				AddVectorObs(0f);
				AddVectorObs(0f);
				AddVectorObs(Vector2.zero);
				ObserveAdjacentTiles(true);
				return;
			}

			var curDist = TargetDistance();
			if (distanceCache == 0)
			{
				distanceCache = curDist;
			}

			AddVectorObs(curDist / 100f);
			AddVectorObs(distanceCache / 100f);
			//Observe the direction to target
			AddVectorObs((TargetTile.WorldPositionServer - OriginTile.WorldPositionServer).NormalizeTo2Int());

			ObserveAdjacentTiles(true, TargetTile);
		}

		public override void AgentAction(float[] vectorAction, string textAction)
		{
			if (FollowTarget == null) return;

			PerformMoveAction(Mathf.FloorToInt(vectorAction[0]));
		}

		protected override void OnPushSolid(Vector3Int destination)
		{
			if (destination == Vector3Int.RoundToInt(TargetTile.WorldPositionServer))
			{
				SetReward(1f);
			}
		}

		protected override void OnTileReached(Vector3Int tilePos)
		{
			if (!activated || FollowTarget == null) return;

			var compareDist = TargetDistance();

			if (compareDist < distanceCache)
			{
				SetReward(calculateReward(compareDist));
				distanceCache = compareDist;
			}

			if (compareDist < 0.5f)
			{
				Done();
				SetReward(2f);
			}
			base.OnTileReached(tilePos);
		}

		float calculateReward(float dist)
		{
			float reward = 0f;
			if (dist > 50f)
			{
				return reward;
			}
			else
			{
				reward = Mathf.Lerp(1f, 0f, dist / 50f);
			}

			return reward;
		}
	}
}
