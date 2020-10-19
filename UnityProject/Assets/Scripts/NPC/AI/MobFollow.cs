using UnityEngine;

namespace Systems.MobAIs
{
	/// <summary>
	/// AI brain specifically trained to perform
	/// following behaviours
	/// </summary>
	public class MobFollow : MobAgent
	{
		public Transform followTarget;

		private float distanceCache = 0;

		public override void AgentReset()
		{
			distanceCache = 0;
			base.AgentReset();
		}

		protected override void AgentServerStart()
		{
			//begin following:
			if (followTarget != null)
			{
				activated = true;
			}
		}

		/// <summary>
		/// Make the mob start following a target
		/// </summary>
		public void StartFollowing(Transform target)
		{
			followTarget = target;
			Activate();
		}

		public override void CollectObservations()
		{
			//You need to feed ML agents null obs
			//if the follow target is null
			//otherwise ML agents will break
			if (followTarget == null)
			{
				AddVectorObs(0f);
				AddVectorObs(0f);
				AddVectorObs(Vector2.zero);
				ObserveAdjacentTiles(true);
				return;
			}

			var curDist = Vector2.Distance(followTarget.transform.position, transform.position);
			if (distanceCache == 0)
			{
				distanceCache = curDist;
			}

			AddVectorObs(curDist / 100f);
			AddVectorObs(distanceCache / 100f);
			//Observe the direction to target
			AddVectorObs(((Vector2)(followTarget.transform.position - transform.position)).normalized);

			ObserveAdjacentTiles(true, followTarget);
		}

		public override void AgentAction(float[] vectorAction, string textAction)
		{
			if (followTarget == null) return;

			PerformMoveAction(Mathf.FloorToInt(vectorAction[0]));
		}

		protected override void OnPushSolid(Vector3Int destination)
		{
			if (destination == Vector3Int.RoundToInt(followTarget.transform.localPosition))
			{
				SetReward(1f);
			}
		}

		protected override void OnTileReached(Vector3Int tilePos)
		{
			if (!activated || followTarget == null) return;

			var compareDist = Vector2.Distance(followTarget.transform.position, transform.position);

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
