using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Mirror;

public class StandardCable : CableInheritance
{
	public new HashSet<PowerTypeCategory> CanConnectTo = new HashSet<PowerTypeCategory>
	{
		PowerTypeCategory.StandardCable,
		PowerTypeCategory.FieldGenerator,
		PowerTypeCategory.SMES,
		PowerTypeCategory.Transformer,
		PowerTypeCategory.DepartmentBattery,
		PowerTypeCategory.MediumMachineConnector,
		PowerTypeCategory.PowerGenerator,
		PowerTypeCategory.PowerSink
	};

	public override void _OnStartServer()
	{
		ApplianceType = PowerTypeCategory.StandardCable;
		CableType = WiringColor.red;
		wireConnect.InData.CanConnectTo = CanConnectTo;
		wireConnect.InData.Categorytype = ApplianceType;
	}
}