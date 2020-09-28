using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class LandingZone : MonoBehaviour
{
	public string landingZoneName;
	public TeleportPointType teleportPointType;
	public bool isUnlocked;

	private void Start()
	{
		switch (teleportPointType)
		{
			case TeleportPointType.Ground:
				LandingZoneManager.Instance.landingZones.Add(this, transform.position);
				break;
			case TeleportPointType.Space:
				LandingZoneManager.Instance.spaceTeleportPoints.Add(this, transform.position);
				break;
		}
	}

	public enum TeleportPointType
	{
		Space,
		Ground
	}
}
