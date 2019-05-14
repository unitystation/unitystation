using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;

public class HighVoltageCable : CableInheritance
{
	public override void _OnStartServer()
	{
		CanConnectTo = new HashSet<PowerTypeCategory>()
		{
			PowerTypeCategory.PowerGenerator,
			PowerTypeCategory.RadiationCollector,
			PowerTypeCategory.HighVoltageCable,
			PowerTypeCategory.Transformer,
			PowerTypeCategory.PowerSink,
		};
		ApplianceType = PowerTypeCategory.HighVoltageCable;
		CableType = WiringColor.high;
		wireConnect.InData.CanConnectTo = CanConnectTo;
		wireConnect.InData.Categorytype = ApplianceType;
	}

}