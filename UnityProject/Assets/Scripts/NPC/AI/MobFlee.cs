using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(ConeOfSight))]
public class MobFlee : MobPathFinder
{
	private ConeOfSight coneOfSight;
	public Transform fleeTarget;
	private int doorMask;
	private int doorAndObstacleMask;

	private DateTime lastFlee;
	private float fleeCoolDown = 4f;
	/// <summary>
	/// attemps made at fleeing
	/// </summary>
	private int attemps = 0;
	private int delay = 5;
	private bool attemptReset;
	public override void OnEnable()
	{
		base.OnEnable();
		lastFlee = DateTime.Now;
		coneOfSight = GetComponent<ConeOfSight>();
		doorMask = LayerMask.GetMask("Door Open", "Door Closed", "Windows");
		doorAndObstacleMask = LayerMask.GetMask("Walls", "Machines", "Windows", "Furniture", "Objects", "Door Open",
			"Door Closed", "Players");
	}

	[ContextMenu("Force Activate")]
	void ForceActivate()
	{
		Activate();
		TryToFlee();
	}

	public void FleeFromTarget(Transform target)
	{
		var totalSeconds = (DateTime.Now - lastFlee).TotalSeconds;
		if (totalSeconds < fleeCoolDown) return;
		lastFlee = DateTime.Now;
		fleeTarget = target;
		Activate();
		TryToFlee();
	}

	protected override void FollowCompleted()
	{
		if (health.IsDead || health.IsCrit || health.IsCardiacArrest || fleeTarget == null) return;
		TryToFlee();
	}

	void TryToFlee()
	{
		if (!activated) return;

		var oppositeDir = ((fleeTarget.localPosition - transform.localPosition).normalized) * -1f;
		StartCoroutine(FindValidWayPoint(oppositeDir));
	}

	IEnumerator FindValidWayPoint(Vector2 oppositeDir)
	{
		if (attemps >= 10)
		{
			if (attemptReset) yield break;
			attemptReset = true;
			yield return new WaitForSeconds(delay);

			attemps = 0;
			attemptReset = false;
			yield break;
		}
		//First try to escape the room by looking for a door
		var possibleDoors = Physics2D.OverlapCircleAll(transform.position, 20f, doorMask);

		//See if the door is visible to npc:
		List<Collider2D> visibleDoors = new List<Collider2D>();

		foreach (Collider2D coll in possibleDoors)
		{
			RaycastHit2D hit = Physics2D.Linecast(transform.position, coll.transform.position, doorAndObstacleMask);

			if (hit.collider != null)
			{
				if (hit.collider == coll)
				{
					var tryGetDoor = coll.GetComponent<DoorController>();
					if (tryGetDoor != null)
					{
						//Can the NPC access this door
						if (CanNPCAccessDoor(tryGetDoor) && tryGetDoor.doorType != DoorType.sliding)
						{
							visibleDoors.Add(coll);
						}
					}
				}
			}
		}

		yield return WaitFor.EndOfFrame;

		//Find a decent reference point to scan for a good goal waypoint
		var refPoint = transform.position;

		//See if there is a decent door for the ref point
		if (visibleDoors.Count != 0)
		{
			var door = 0;
			var dist = 0f;
			for (int i = 0; i < visibleDoors.Count; i++)
			{
				var checkDist = Vector3.Distance(fleeTarget.position, visibleDoors[i].transform.position);
				var checkNpcDist = Vector3.Distance(transform.position, visibleDoors[i].transform.position);

				//if check dist is smaller then the checkNpcDist that means the danger
				//is between the npc and the door, so avoid it
				if (checkDist > dist && checkDist > checkNpcDist)
				{
					dist = checkDist;
					door = i;
				}
			}

			//Try to escape through the furthest door:
			refPoint = visibleDoors[door].transform.position;
			//find the correct leaving direction from the door:
			var doorOppositeDir = ((visibleDoors[door].transform.position - transform.position).normalized) * -1f;
			//Get the degree quadrant this dir is pointing through from the door
			var angleOfDir = Vector3.Angle(doorOppositeDir, transform.up);
			var cw = Vector3.Cross(transform.up, doorOppositeDir).z < 0f;
			if (!cw)
			{
				angleOfDir = -angleOfDir;
			}

			//Get new direction from the door
			var tryNewDir = GetDirFromDoor(angleOfDir, refPoint);
			if (tryNewDir != Vector2.zero)
			{
				oppositeDir = tryNewDir;
			}
			else
			{
				//ignore the door, can't pass through it
				refPoint = transform.position;
			}
		}

		var tryGetGoalPos =
			Vector3Int.RoundToInt(coneOfSight.GetFurthestPositionInSight(refPoint + (Vector3) oppositeDir,
				doorAndObstacleMask, oppositeDir, 20f, 10));

		if (!MatrixManager.IsPassableAt(tryGetGoalPos, true))
		{
			// Not passable! Try to find an adjacent tile:
			for (int y = 1; y > -2; y--)
			{
				for (int x = -1; x < 2; x++)
				{
					if (x == 0 && y == 0) continue;

					var checkPos = tryGetGoalPos;
					checkPos.x += x;
					checkPos.y += y;

					if (MatrixManager.IsPassableAt(checkPos, true))
					{
						RaycastHit2D hit = Physics2D.Linecast(refPoint + (checkPos - refPoint).normalized, (Vector3) checkPos,
							doorAndObstacleMask);
						if (hit.collider == null)
						{
							tryGetGoalPos = checkPos;
							y = -100;
							x = 100;
						}
					}
				}
			}
		}

		//Lets try to get a path:
		var path = FindNewPath((Vector2Int) registerTile.LocalPositionServer,
			(Vector2Int) registerTile.LocalPositionServer + Vector2Int.RoundToInt(tryGetGoalPos - transform.position));

		if (path != null)
		{
			if (path.Count == 0)
			{
				attemps++;
				TryToFlee();
			}
			else
			{
				FollowPath(path);
			}
		}
		else
		{
			attemps++;
			TryToFlee();
			
		}
	}

	private bool CanNPCAccessDoor(DoorController doorController)
	{
		if (doorController.AccessRestrictions == null) return false;

		if ((int) doorController.AccessRestrictions.restriction == 0)
		{
			return true;
		}
		else
		{
			return false;
		}
	}

	private Vector2 GetDirFromDoor(float dirDegrees, Vector3 doorWorldPos)
	{
		if (dirDegrees > 0f)
		{
			//Test the tile to the left:
			if (MatrixManager.IsPassableAt(Vector3Int.RoundToInt(doorWorldPos + Vector3.left), true))
			{
				return Vector2.left;
			}
		}
		else
		{
			//Test the tile to the right:
			if (MatrixManager.IsPassableAt(Vector3Int.RoundToInt(doorWorldPos + Vector3.right), true))
			{
				return Vector2.right;
			}
		}

		//It could be up or down:
		if (Mathf.Abs(dirDegrees) > 45f)
		{
			//Test the tile up:
			if (MatrixManager.IsPassableAt(Vector3Int.RoundToInt(doorWorldPos + Vector3.up), true))
			{
				return Vector2.up;
			}
		}
		else
		{
			//Test the tile down:
			if (MatrixManager.IsPassableAt(Vector3Int.RoundToInt(doorWorldPos + Vector3.down), true))
			{
				return Vector2.down;
			}
		}

		return Vector2.zero;
	}
}