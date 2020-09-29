using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ModuleSupplyingDevice : ElectricalModuleInheritance
{
	public bool StartOnStartUp = false;

	[HideInInspector]
	public float Previouscurrent = 0;
	public float current = 0;

	[HideInInspector]
	public float PreviousSupplyingVoltage = 0;
	public float SupplyingVoltage = 0;

	[HideInInspector]
	public float PreviousInternalResistance = 0;
	public float InternalResistance = 0;

	[HideInInspector]
	public float PreviousProducingWatts = 0;
	public float ProducingWatts = 0;

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
	[RightClickMethod]
	public override void TurnOnSupply()
	{
		//Logger.Log("TurnOnSupply");
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

	[RightClickMethod]
	public override void TurnOffSupply()
	{
		if (InternalResistance > 0)
		{
			foreach (PowerTypeCategory Connecting in ControllingNode.CanConnectTo)
			{
				ControllingNode.RestoreResistance(Connecting);
			}
		}


		// On some transitional scenes like the DontDestroyOnLoad scene, we don't have an ElectricalManager.
		// We should not try to update it in those cases.
		if (ElectricalManager.Instance?.electricalSync?.NUResistanceChange != null)
		{
			ElectricalManager.Instance.electricalSync.NUResistanceChange.Add(ControllingNode);
			PowerSupplyFunction.TurnOffSupply(this);
		}
	}
	public override void PowerNetworkUpdate()
	{
		if (
			current != Previouscurrent
		    || SupplyingVoltage != PreviousSupplyingVoltage
		    || InternalResistance != PreviousInternalResistance
		    || ProducingWatts != PreviousProducingWatts)
		{
			ControllingNode.Node.InData.Data.SupplyingCurrent = current;
			Previouscurrent = current;

			ControllingNode.Node.InData.Data.SupplyingVoltage = SupplyingVoltage;
			PreviousSupplyingVoltage = SupplyingVoltage;

			ControllingNode.Node.InData.Data.InternalResistance = InternalResistance;
			PreviousInternalResistance = InternalResistance;

			ControllingNode.Node.InData.Data.ProducingWatts = ProducingWatts;
			PreviousProducingWatts = ProducingWatts;
			//Logger.Log("Add ddddd");
			ElectricalManager.Instance.electricalSync.NUCurrentChange.Add(ControllingNode.Node.InData.ControllingDevice);
		}
	}

	public float GetVoltage()
	{
		if (ControllingNode.Node.InData.Data.SupplyDependent.ContainsKey(ControllingNode.Node) && ControllingNode.Node.InData.Data.SupplyDependent[ControllingNode.Node].Downstream.Count > 0)
		{
			var DownNode = ControllingNode.Node.InData.Data.SupplyDependent[ControllingNode.Node].Downstream.PickRandom();
			ElectricityFunctions.WorkOutActualNumbers(DownNode);
			return (DownNode.Data.ActualVoltage);
		}
		return (0);
	}
	public float GetCurrente()
	{
		if (ControllingNode.Node.InData.Data.SupplyDependent.ContainsKey(ControllingNode.Node) && ControllingNode.Node.InData.Data.SupplyDependent[ControllingNode.Node].Downstream.Count > 0)
		{
			var DownNode = ControllingNode.Node.InData.Data.SupplyDependent[ControllingNode.Node].Downstream.PickRandom();
			ElectricityFunctions.WorkOutActualNumbers(DownNode);
			return (DownNode.Data.CurrentInWire);
		}
		return (0);
	}

	public float GetResistance()
	{
		if (ControllingNode.Node.InData.Data.SupplyDependent.ContainsKey(ControllingNode.Node) && ControllingNode.Node.InData.Data.SupplyDependent[ControllingNode.Node].Downstream.Count > 0)
		{
			var DownNode = ControllingNode.Node.InData.Data.SupplyDependent[ControllingNode.Node].Downstream.PickRandom();
			ElectricityFunctions.WorkOutActualNumbers(DownNode);
			return (DownNode.Data.EstimatedResistance);
		}
		return (0);
	}

}