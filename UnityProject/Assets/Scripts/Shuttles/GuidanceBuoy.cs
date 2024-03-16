using System;
using System.Collections;
using System.Collections.Generic;
using Mirror;
using UnityEngine;

public class GuidanceBuoy : NetworkBehaviour
{
	public GuidanceBuoyMoveStep Out;
	public GuidanceBuoyMoveStep In;
	public RegisterTile RegisterTile;

	public void Awake()
	{
		RegisterTile = this.GetComponent<RegisterTile>();
	}
}


