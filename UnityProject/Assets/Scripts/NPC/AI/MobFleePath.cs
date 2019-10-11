using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using PathFinding;

[RequireComponent(typeof(ConeOfSight))]
public class MobFleePath : MobPathFinder
{
	private ConeOfSight coneOfSight;
	public Transform fleeTarget;
	private int doorMask;
	private int obstacleMask;
	private int doorAndObstacleMask;

	public override void OnEnable()
	{
		base.OnEnable();
		coneOfSight = GetComponent<ConeOfSight>();
		doorMask = LayerMask.GetMask("Door Open", "Door Closed", "Windows");
		obstacleMask = LayerMask.GetMask("Walls", "Machines", "Windows", "Furniture", "Objects");
		doorAndObstacleMask = LayerMask.GetMask("Walls", "Machines", "Windows", "Furniture", "Objects", "Door Open",
			"Door Closed");
	}

	[ContextMenu("Force Activate")]
	void ForceActivate()
	{
		TryToFlee();
	}

	void TryToFlee()
	{
		var oppositeDir = ((fleeTarget.localPosition - transform.localPosition).normalized) * -1f;
		StartCoroutine(FindValidWayPoint(oppositeDir));
	}

	IEnumerator FindValidWayPoint(Vector2 oppositeDir)
	{
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
						if (CanNPCAccessDoor(tryGetDoor))
						{
							visibleDoors.Add(coll);
							Debug.Log("Found escape route: Npc Can Access this door");
						}
					}
					else
					{
						Debug.Log($"This is not a door? {coll.gameObject.name}");
					}
				}
			}

			yield return WaitFor.EndOfFrame;
		}

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
		}

		var tryGetGoalPos =
			Vector3Int.RoundToInt(coneOfSight.GetFurthestPositionInSight(refPoint + (Vector3)oppositeDir, doorAndObstacleMask, oppositeDir, 20f, 10));

		Debug.Log($"1: CurrentPos: {transform.position} goalPos {tryGetGoalPos}");
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
						RaycastHit2D hit = Physics2D.Linecast(refPoint + (Vector3)oppositeDir, (Vector3) checkPos, doorAndObstacleMask);
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

		Debug.Log($"2: CurrentPos: {transform.position} goalPos {tryGetGoalPos}");

		cnt.SetPosition(tryGetGoalPos);


		/*
		//Lets try to get a path:
		var path = FindNewPath((Vector2Int) registerTile.LocalPositionServer,
			(Vector2Int) registerTile.LocalPositionServer + Vector2Int.RoundToInt(tryGetGoalPos - transform.position));

		if (path.Count == 0)
		{
			Debug.Log("Path not found! oh well");
		}
		else
		{
			FollowPath(path);
		}
		*/
	}

	private bool CanNPCAccessDoor(DoorController doorController)
	{
		if ((int) doorController.AccessRestrictions.restriction == 0)
		{
			return true;
		}
		else
		{
			return false;
		}
	}
}