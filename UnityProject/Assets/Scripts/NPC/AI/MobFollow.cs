using System;
using System.Collections.Generic;
using Doors;
using UnityEngine;

namespace Systems.MobAIs
{
	/// <summary>
	/// AI brain specifically trained to perform
	/// following behaviours
	/// </summary>
	public class MobFollow : MobObjective
	{
		[NonSerialized]
		public RegisterTile FollowTarget;

		public float PriorityBalance = 25;
		public int startMovingDistance = 2;

		/// <summary>
		/// Make the mob start following a target
		/// </summary>
		public void StartFollowing(GameObject target)
		{
			FollowTarget = target.GetComponent<RegisterTile>();
		}

		/// <summary>
		/// Returns the distance between the mob and the target
		/// </summary>
		protected float TargetDistance()
		{
			return Vector3.Distance(FollowTarget.WorldPositionServer, mobTile.WorldPositionServer);
		}

		public override void ContemplatePriority()
		{
			if (FollowTarget == null)
			{
				Priority = 0;
				return;
			}

			var distance = TargetDistance();

			//We lose target if they are 15 tiles away
			if (distance > 15)
			{
				FollowTarget = null;
				Priority = 0;
				return;
			}

			Priority += PriorityBalance;
		}

		public override void DoAction()
		{
			if (FollowTarget == null) return;
			var moveToRelative = (FollowTarget.WorldPositionServer - mobTile.WorldPositionServer).To3();
			moveToRelative.Normalize();
			var stepDirectionWorld = ChooseDominantDirection(moveToRelative);
			var moveTo = mobTile.WorldPositionServer + stepDirectionWorld;
			var localMoveTo = moveTo.ToLocal(mobTile.Matrix).RoundToInt();

			var distance = TargetDistance();
			if (distance > startMovingDistance)
			{
				if (mobTile.Matrix.MetaTileMap.IsPassableAtOneTileMap(mobTile.LocalPositionServer, localMoveTo, true))
				{
					Move(stepDirectionWorld);
				}
				else
				{
					Move(Directions.PickRandom());
				}
			}
			else
			{
				if(!GameManager.Instance.onTuto)
					Move(stepDirectionWorld);
			}
		}
	}
}
