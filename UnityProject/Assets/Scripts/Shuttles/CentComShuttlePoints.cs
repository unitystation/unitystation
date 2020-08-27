using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CentComShuttlePoints : MonoBehaviour
{
	/// <summary>
	/// The SIDE of centcom the shuttle is docking at, NOT DIRECTION.
	/// </summary>
	public OrientationEnum orientation = OrientationEnum.Down;

	private void Start()
	{
		LandingZoneManager.Instance.centcomDockingPos = transform.position;
		LandingZoneManager.Instance.centcomDocking = this;
	}
}
