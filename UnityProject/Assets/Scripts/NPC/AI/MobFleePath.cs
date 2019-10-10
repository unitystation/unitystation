using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Experimental.XR;

public class MobFleePath : MobPathFinder
{
	public Transform fleeTarget;

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
		var doorMask = LayerMask.GetMask("Door Open", "Door Closed");
		var possibleDoors = Physics2D.OverlapCircleAll(transform.position, 10f, doorMask);

		//See if the door is visible to npc:
		List<Collider2D> visibleDoors = new List<Collider2D>();

		var obstacleMask = LayerMask.GetMask("Walls", "Machines", "Windows", "Furniture", "Objects");
		foreach (Collider2D coll in possibleDoors)
		{
			RaycastHit2D hit = Physics2D.Raycast(transform.position, coll.transform.position - transform.position, 12f,
				obstacleMask);

			if (hit.collider != null)
			{
				if (hit.collider == coll)
				{
					//Can the NPC access this door
					if (CanNPCAccessDoor(coll.GetComponent<DoorController>()))
					{
						visibleDoors.Add(coll);
						Debug.Log("Found escape route: Npc Can Access this door");
					}
				}
			}

			yield return WaitFor.EndOfFrame;
		}
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