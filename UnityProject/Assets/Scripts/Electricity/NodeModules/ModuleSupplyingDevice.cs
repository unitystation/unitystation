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

	public Current CurrentSource = new Current();

	public virtual void BroadcastSetUpMessage(ElectricalNodeControl Node)
	{
		RequiresUpdateOn = new HashSet<ElectricalUpdateTypeCategory>
		{
			ElectricalUpdateTypeCategory.PowerUpdateStructureChange,
			ElectricalUpdateTypeCategory.PowerUpdateStructureChangeReact,
			ElectricalUpdateTypeCategory.PowerUpdateCurrentChange,
			ElectricalUpdateTypeCategory.TurnOnSupply,
			ElectricalUpdateTypeCategory.TurnOffSupply,
			ElectricalUpdateTypeCategory.PowerNetworkUpdate,
			ElectricalUpdateTypeCategory.PotentialDestroyed,
			ElectricalUpdateTypeCategory.GoingOffStage,
			ElectricalUpdateTypeCategory.ObjectStateChange,
		};
		ModuleType = ElectricalModuleTypeCategory.SupplyingDevice;
		ControllingNode = Node;
		ControllingNode.Node.InData.Data.SupplyingVoltage = SupplyingVoltage;
		ControllingNode.Node.InData.Data.InternalResistance = InternalResistance;
		ControllingNode.Node.InData.Data.SupplyingCurrent = current;
		Node.AddModule(this);
	}

	public override void PowerUpdateStructureChange()
	{
		ControllingNode.Node.InData.FlushConnectionAndUp();
		ElectricalSynchronisation.NUStructureChangeReact.Add(ControllingNode);
		ElectricalSynchronisation.NUResistanceChange.Add(ControllingNode);
		ElectricalSynchronisation.NUCurrentChange.Add(ControllingNode);
	}

	public override void PowerUpdateStructureChangeReact()
	{
		PowerSupplyFunction.PowerUpdateStructureChangeReact(this);
		ElectricalSynchronisation.NUResistanceChange.Add(ControllingNode);
		ElectricalSynchronisation.NUCurrentChange.Add(ControllingNode);
	}

	public override void OnDespawnServer(DespawnInfo info)
	{
		ElectricalSynchronisation.RemoveSupply(ControllingNode, ControllingNode.ApplianceType);
		ControllingNode.Node.InData.FlushSupplyAndUp(ControllingNode.Node);
	}

	[RightClickMethod]
	public void FlushSupplyAndUp() {
		ControllingNode.Node.InData.FlushSupplyAndUp(ControllingNode.Node);
	}

	public override void PowerUpdateCurrentChange()
	{
		PowerSupplyFunction.PowerUpdateCurrentChange(this);
	}

	public override void TurnOnSupply()
	{
		if (InternalResistance > 0)
		{
			foreach (PowerTypeCategory Connecting in ControllingNode.CanConnectTo)
			{
				ControllingNode.OverlayInternalResistance(InternalResistance, Connecting);
			}
			ElectricalSynchronisation.NUResistanceChange.Add(ControllingNode);
		}
		PowerSupplyFunction.TurnOnSupply(this);
	}

	public override void TurnOffSupply()
	{
		if (InternalResistance > 0)
		{
			foreach (PowerTypeCategory Connecting in ControllingNode.CanConnectTo)
			{
				ControllingNode.RestoreResistance(Connecting);
			}
		}
		ElectricalSynchronisation.NUResistanceChange.Add(ControllingNode);
		PowerSupplyFunction.TurnOffSupply(this);
	}
	public override void PowerNetworkUpdate()
	{
		if (current != Previouscurrent | SupplyingVoltage != PreviousSupplyingVoltage | InternalResistance != PreviousInternalResistance)
		{
			ControllingNode.Node.InData.Data.SupplyingCurrent = current;
			Previouscurrent = current;

			ControllingNode.Node.InData.Data.SupplyingVoltage = SupplyingVoltage;
			PreviousSupplyingVoltage = SupplyingVoltage;

			ControllingNode.Node.InData.Data.InternalResistance = InternalResistance;
			PreviousInternalResistance = InternalResistance;

			ElectricalSynchronisation.NUCurrentChange.Add(ControllingNode.Node.InData.ControllingDevice);
		}
	}
}