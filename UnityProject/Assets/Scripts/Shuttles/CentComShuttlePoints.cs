using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CentComShuttlePoints : MonoBehaviour
{
	public bool isDockingLocation;

	private void Start()
	{
		if (isDockingLocation)
		{
			LandingZoneManager.Instance.centcomDockingPos = transform.position;
		}
	}
}
