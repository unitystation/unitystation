using System.Collections;
using System.Collections.Generic;
using HealthV2;
using UnityEngine;

namespace Systems.MobAIs
{
	/// <summary>
	/// Enemy Statue NPC's
	/// Will attack any human that they see
	/// </summary>
	[RequireComponent(typeof(MobMeleeAction))]
	[RequireComponent(typeof(ConeOfSight))]
	public class StatueAI : GenericHostileAI
	{
		protected override void UpdateMe()
		{
			if (!isServer) return;

			if (IsDead || IsUnconscious)
			{
				HandleDeathOrUnconscious();
			}

			StatusLoop();
		}

		private void StatusLoop()
		{
			if (currentStatus == MobStatus.None || currentStatus == MobStatus.Attacking)
			{
				MonitorIdleness();
				return;
			}

			if (currentStatus == MobStatus.Searching)
			{
				moveWaitTime += Time.deltaTime;
				if (moveWaitTime >= movementTickRate)
				{
					moveWaitTime = 0f;
				}

				searchWaitTime += Time.deltaTime;
				if (searchWaitTime >= searchTickRate)
				{
					searchWaitTime = 0f;
					var findTarget = SearchForTarget();
					if (findTarget != null)
					{
						BeginAttack(findTarget);
					}
					else
					{
						BeginSearch();
					}
				}
			}
		}

		private bool IsSomeoneLookingAtMe()
		{
			var hits = coneOfSight.GetObjectsInSight(hitMask, LayerTypeSelection.None , directional.CurrentDirection.Vector, 10f);
			if (hits.Count == 0) return false;

			foreach (var coll in hits)
			{
				if (coll == null) continue;

				var dir = (transform.position - coll.transform.position).normalized;

				if (coll.layer == playersLayer
				    && !coll.GetComponent<LivingHealthMasterBase>().IsDead
				    && coll.GetComponent<Directional>()?.CurrentDirection == orientations[DirToInt(dir)])
				{
					Freeze();
					return true;
				}
			}

			return false;
		}

		protected override void MonitorIdleness()
		{

			if (!mobMeleeAction.performingDecision && mobMeleeAction.FollowTarget == null && !IsSomeoneLookingAtMe())
			{
				BeginSearch();
			}
		}

		private void Freeze()
		{
			ResetBehaviours();
			currentStatus = MobStatus.None;
			mobMeleeAction.FollowTarget = null;
		}

		protected override void BeginAttack(GameObject target)
		{
			ResetBehaviours();
			currentStatus = MobStatus.Attacking;
			StartCoroutine(StatueStalk(target));
		}

		private IEnumerator StatueStalk(GameObject stalked)
		{
			while (!IsSomeoneLookingAtMe())
			{
				if(mobMeleeAction.FollowTarget == null)
				{
					mobMeleeAction.StartFollowing(stalked);
				}
				yield return WaitFor.Seconds(.2f);
			}

			Freeze();
			yield break;
		}

		private int DirToInt(Vector3 direction)
		{
			var angleOfDir = Vector3.Angle((Vector2) direction, transform.up);
			if (direction.x < 0f)
			{
				angleOfDir = -angleOfDir;
			}
			if (angleOfDir > 180f)
			{
				angleOfDir = -180 + (angleOfDir - 180f);
			}

			if (angleOfDir < -180f)
			{
				angleOfDir = 180f + (angleOfDir + 180f);
			}

			switch (angleOfDir)
			{
				case 0:
					return 1;
				case float n when n == -180f || n == 180f:
					return 3;
				case float n when n > 0f:
					return 2;
				case float n when n < 0f:
					return 4;
				default:
					return 2;

			}
		}

		private readonly Dictionary<int, Orientation> orientations = new Dictionary<int, Orientation>()
		{
			{1, Orientation.Up},
			{2, Orientation.Right},
			{3, Orientation.Down},
			{4, Orientation.Left}
		};
	}
}
