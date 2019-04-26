using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ModuleSupplyingDevice : ElectricalModuleInheritance
{
	public float current = 0;
	public float Previouscurrent = 0;
	public float SupplyingVoltage = 0;
	public float PreviousSupplyingVoltage = 0;
	public float PreviousInternalResistance = 0;
	public float InternalResistance = 0;

	public virtual void BroadcastSetUpMessage(ElectricalNodeControl Node)
	{
		RequiresUpdateOn = new HashSet<ElectricalUpdateTypeCategory>
		{
			ElectricalUpdateTypeCategory.PowerUpdateStructureChangeReact,
			ElectricalUpdateTypeCategory.PowerUpdateCurrentChange,
			ElectricalUpdateTypeCategory.TurnOnSupply,
			ElectricalUpdateTypeCategory.TurnOffSupply, 
			ElectricalUpdateTypeCategory.PowerNetworkUpdate,
		};
		ModuleType = ElectricalModuleTypeCategory.SupplyingDevice;
		ControllingNode = Node;
		Node.AddModule(this);
	}

	public override void PowerUpdateStructureChangeReact() {
		PowerSupplyFunction.PowerUpdateStructureChangeReact(ControllingNode.Node);
		ElectricalSynchronisation.NUStructureChangeReact.Add(ControllingNode.Node.InData.ControllingUpdate);
		ElectricalSynchronisation.NUResistanceChange.Add(ControllingNode.Node.InData.ControllingUpdate);
		ElectricalSynchronisation.NUCurrentChange.Add(ControllingNode.Node.InData.ControllingUpdate);
	}
	public override void PowerUpdateCurrentChange()
	{
		PowerSupplyFunction.PowerUpdateCurrentChange (ControllingNode.Node);
	}

	public override void TurnOnSupply()
	{
		PowerSupplyFunction.TurnOnSupply(ControllingNode.Node);
	}

	public override void TurnOffSupply()
	{
		PowerSupplyFunction.TurnOffSupply(ControllingNode.Node);
	}
	public override void PowerNetworkUpdate()
	{
		if (current != Previouscurrent | SupplyingVoltage != PreviousSupplyingVoltage | InternalResistance != PreviousInternalResistance)
		{
			ControllingNode.Node.Data.SupplyingCurrent = current;
			Previouscurrent = current;

			ControllingNode.Node.Data.SupplyingVoltage = SupplyingVoltage;
			PreviousSupplyingVoltage = SupplyingVoltage;

			ControllingNode.Node.Data.InternalResistance = InternalResistance;
			PreviousInternalResistance = InternalResistance;

			ElectricalSynchronisation.NUCurrentChange.Add(ControllingNode.Node.InData.ControllingUpdate);
		}
	}
}