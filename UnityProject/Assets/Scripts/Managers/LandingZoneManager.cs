﻿using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Managers;
using UnityEngine;

public class LandingZoneManager : SingletonManager<LandingZoneManager>
{
	public IDictionary<LandingZone, Vector3> landingZones = new Dictionary<LandingZone, Vector3>();

	public IDictionary<LandingZone, Vector3> spaceTeleportPoints = new Dictionary<LandingZone, Vector3>();

	[HideInInspector]
	public Vector3 centcomDockingPos;

	[HideInInspector]
	public CentComShuttlePoints centcomDocking;
}
