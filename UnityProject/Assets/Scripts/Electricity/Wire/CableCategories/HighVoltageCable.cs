using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;

public class HighVoltageCable : CableInheritance
{
	public HashSet<PowerTypeCategory> CanConnectTo = new HashSet<PowerTypeCategory>()
	{
		PowerTypeCategory.PowerGenerator,
		PowerTypeCategory.RadiationCollector,
		PowerTypeCategory.HighVoltageCable,
			PowerTypeCategory.Transformer,
	};
		public override void _OnStartServer()
	{
		ApplianceType = PowerTypeCategory.HighVoltageCable;
		CableType = WiringColor.high;
		wireConnect.InData.CanConnectTo = CanConnectTo;
		wireConnect.InData.Categorytype = ApplianceType;
	}

}