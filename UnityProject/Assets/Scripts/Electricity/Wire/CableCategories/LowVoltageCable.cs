using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Mirror;

public class LowVoltageCable : CableInheritance
{
	public new HashSet<PowerTypeCategory> CanConnectTo = new HashSet<PowerTypeCategory>(){
		PowerTypeCategory.LowMachineConnector,
		PowerTypeCategory.LowVoltageCable,
		PowerTypeCategory.SolarPanelController,
		PowerTypeCategory.SolarPanel,
		PowerTypeCategory.PowerSink,
	};
	public override void _OnStartServer()
	{
		ApplianceType = PowerTypeCategory.LowVoltageCable;
		CableType = WiringColor.low;
		wireConnect.InData.CanConnectTo = CanConnectTo;
		wireConnect.InData.Categorytype = ApplianceType;
	}
}
