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

		public RegisterTile FollowTarget;

		public float PriorityBalance = 25;






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
		public float TargetDistance()
		{
			return Vector3.Distance(FollowTarget.WorldPositionServer, MobTile.WorldPositionServer);
		}

		public override void ContemplatePriority()
		{
			if (FollowTarget == null)
			{
				Priority = 0;
			}
			else
			{
				var Distance = TargetDistance();
				if (Distance > 15)
				{
					FollowTarget = null;
					Priority = 0;
					return;
				}
				else
				{
					if (Distance > 1.5f)
					{
						Priority += PriorityBalance;
					}
					else
					{
						Priority += PriorityBalance / 10;
					}
				}
			}
		}



		public override void DoAction()
		{
			var moveToRelative = (MobTile.WorldPositionServer - FollowTarget.WorldPositionServer).ToNonInt3();
			moveToRelative.Normalize();
			var stepDirectionWorld = ChooseDominantDirection(moveToRelative);
			var moveTo = MobTile.WorldPositionServer + stepDirectionWorld;
			var localMoveTo = moveTo.ToLocal(MobTile.Matrix).RoundToInt();

			if (MobTile.Matrix.MetaTileMap.IsPassableAtOneTileMap(MobTile.LocalPositionServer, localMoveTo, true))
			{
				Move(stepDirectionWorld);
			}
			else
			{
				Move(Directions.PickRandom());
			}
		}
	}
}
