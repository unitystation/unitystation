using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Mirror;

public class HighVoltageCable : CableInheritance
{
	public new HashSet<PowerTypeCategory> CanConnectTo = new HashSet<PowerTypeCategory>()
	{
			PowerTypeCategory.PowerGenerator,
			PowerTypeCategory.RadiationCollector,
			PowerTypeCategory.HighVoltageCable,
			PowerTypeCategory.Transformer,
			PowerTypeCategory.PowerSink,
			PowerTypeCategory.VoltageProbe,
	};

	void Awake()
	{
		ApplianceType = PowerTypeCategory.HighVoltageCable;
		CableType = WiringColor.high;
		wireConnect.InData.CanConnectTo = CanConnectTo;
		wireConnect.InData.Categorytype = ApplianceType;
	}

}