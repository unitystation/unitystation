using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class LandingZoneManager : MonoBehaviour
{
	private static LandingZoneManager instance;
	public static LandingZoneManager Instance => instance;

	public IDictionary<LandingZone, Vector3> landingZones = new Dictionary<LandingZone, Vector3>();

	public IDictionary<LandingZone, Vector3> spaceTeleportPoints = new Dictionary<LandingZone, Vector3>();

	public Vector3 centcomDockingPos;

	private void Awake()
	{
		if (instance == null)
		{
			instance = this;
		}
		else
		{
			Destroy(this);
		}
	}
}
