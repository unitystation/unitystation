using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ModuleSupplyingDevice : ElectricalModuleInheritance
{
	public bool StartOnStartUp = false;

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
		if (StartOnStartUp)
		{
			TurnOnSupply();
		}
	}

	public override void PowerUpdateStructureChange()
	{
		var sync = ElectricalManager.Instance.electricalSync;
		ControllingNode.Node.InData.FlushConnectionAndUp();
		sync.NUStructureChangeReact.Add(ControllingNode);
		sync.NUResistanceChange.Add(ControllingNode);
		sync.NUCurrentChange.Add(ControllingNode);
	}

	public override void PowerUpdateStructureChangeReact()
	{
		PowerSupplyFunction.PowerUpdateStructureChangeReact(this);
		var sync = ElectricalManager.Instance.electricalSync;
		sync.NUResistanceChange.Add(ControllingNode);
		sync.NUCurrentChange.Add(ControllingNode);
	}

	public override void OnDespawnServer(DespawnInfo info)
	{
		ElectricalManager.Instance.electricalSync.RemoveSupply(ControllingNode, ControllingNode.ApplianceType);
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
			ElectricalManager.Instance.electricalSync.NUResistanceChange.Add(ControllingNode);
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
		ElectricalManager.Instance.electricalSync.NUResistanceChange.Add(ControllingNode);
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

			ElectricalManager.Instance.electricalSync.NUCurrentChange.Add(ControllingNode.Node.InData.ControllingDevice);
		}
	}
}