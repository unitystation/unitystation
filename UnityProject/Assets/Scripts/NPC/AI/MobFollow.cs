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

		public RegisterTile MobTile;
		public RegisterTile FollowTarget;

		public float PriorityBalance = 25;

		public Directional directional;

		private List<Vector3Int> Directions = new List<Vector3Int>()
		{
			new Vector3Int(1, 0, 0),
			new Vector3Int(-1, 0, 0),
			new Vector3Int(0, 1, 0),
			new Vector3Int(0, -1, 0),
		};

		public void Awake()
		{
			MobTile = GetComponent<RegisterTile>();
			directional = GetComponent<Directional>();
		}

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

					var MoveToRelative = (MobTile.WorldPositionServer - FollowTarget.WorldPositionServer).ToNonInt3();
					MoveToRelative.Normalize();
					var StepDirectionWorld = ChooseDominantDirection(MoveToRelative);
					var MoveTo = MobTile.WorldPositionServer + StepDirectionWorld;
					var LocalMoveTo = MoveTo.ToLocal(MobTile.Matrix).RoundToInt();

					if (MobTile.Matrix.MetaTileMap.IsPassableAtOneTileMap(MobTile.LocalPositionServer, LocalMoveTo, true))
					{
						Move(StepDirectionWorld);
					}
					else
					{
						Move(Directions.PickRandom());
					}
				}
			}
		}

		public void Move(Vector3Int dirToMove)
		{
			var dest = MobTile.LocalPositionServer + (Vector3Int)dirToMove;

			if (!MobTile.customNetTransform.Push(dirToMove.To2Int(), context: gameObject))
			{

				DoorController tryGetDoor =
					MobTile.Matrix.GetFirst<DoorController>(
						dest, true);
				if (tryGetDoor)
				{
					tryGetDoor.MobTryOpen(gameObject);
				}

				//New doors
				DoorMasterController tryGetDoorMaster =
					MobTile.Matrix.GetFirst<DoorMasterController>(
						dest, true);
				if (tryGetDoorMaster)
				{
					tryGetDoorMaster.Bump(gameObject);
				}
			}

			if (directional != null)
			{
				directional.FaceDirection(Orientation.From(dirToMove.To2Int()));
			}
		}


		public Vector3Int ChooseDominantDirection(Vector3 InD)
		{
			if (Mathf.Abs(InD.x) > Mathf.Abs(InD.y))
			{
				if (InD.x > 0)
				{
					return new Vector3Int(1, 0, 0);
				}
				else
				{
					return new Vector3Int(-1, 0, 0);
				}
			}
			else
			{
				if (InD.y > 0)
				{
					return new Vector3Int(0, 1, 0);
				}
				else
				{
					return new Vector3Int(0, -1, 0);
				}
			}
		}

		public override void DoAction()
		{

		}
	}
}
