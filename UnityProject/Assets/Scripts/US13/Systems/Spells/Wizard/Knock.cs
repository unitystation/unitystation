using UnityEngine;
using US13.HealthV2;
using US13.Managers;
using US13.Objects.Closets;
using US13.Objects.Doors;
using US13.Systems.Spells;
using Util;

public class Knock : Spell
{
	// Radius used to detect nearby doors
	private const float DoorDetectionRadius = 7f;

	public override bool CastSpellServer(PlayerInfo caster, Vector3 clickPosition, BodyPartType targetZone)
	{
		var originPosition = caster.Mind.Body.gameObject.AssumedWorldPosServer();

		// Find all colliders within the radius
		Collider2D[] hits = Physics2D.OverlapCircleAll(originPosition, DoorDetectionRadius);

		bool Adoor = false;

		foreach (Collider2D hit in hits)
		{
			// Try to get a DoorMasterController from the hit collider
			DoorMasterController door = hit.GetComponent<DoorMasterController>();

			if (door == false)
			{
				ClosetControl ClosetControl = hit.GetComponent<ClosetControl>();
				if (ClosetControl )
				{
					ClosetControl.SetDoor(ClosetControl.Door.Opened);
					ClosetControl.SetLock(ClosetControl.Lock.Unlocked);
				}

				continue;
			}

			Adoor = true;

			door.Open();
		}

		return Adoor;
	}
}
