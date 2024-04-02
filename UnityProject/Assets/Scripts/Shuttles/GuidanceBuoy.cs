using System;
using System.Collections;
using System.Collections.Generic;
using Mirror;
using Shared.Systems.ObjectConnection;
using UnityEngine;

public class GuidanceBuoy : NetworkBehaviour, IMultitoolMasterable
{
	public GuidanceBuoyMoveStep Out;
	public GuidanceBuoyMoveStep In;

	[field: SerializeField] public bool CanRelink { get; set; } = true;
	public MultitoolConnectionType ConType => MultitoolConnectionType.APC;

	int IMultitoolMasterable.MaxDistance => 30;

	bool IMultitoolMasterable.IgnoreMaxDistanceMapper => true;

}


