using System.Collections.Generic;
using Mirror;
using TileMap.Behaviours;
using UnityEngine;

public class AdminJail : ItemMatrixSystemInit
{
	public static AdminJail AdminJailLocation;

	public Dictionary<string, Vector3> JailedLocations = new Dictionary<string, Vector3>();

	public override void Start()
	{
		AdminJailLocation = this;

	}

	public override void OnDestroy()
	{
		base.OnDestroy();
		if (AdminJailLocation == this)
		{
			AdminJailLocation = null;
		}

	}
}
