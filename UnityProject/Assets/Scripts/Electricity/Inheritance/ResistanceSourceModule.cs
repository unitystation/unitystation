using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ResistanceSourceModule : ElectricalModuleInheritance
{
	//[Header("Transformer Settings")]
	public Resistance resistance { get; set; } = new Resistance();
	public float Resistance = 0;
	public float PreviousResistance = 0;

	public void BroadcastSetUpMessage(ElectricalNodeControl Node)
	{
		RequiresUpdateOn = new HashSet<ElectricalUpdateTypeCategory>
		{
			ElectricalUpdateTypeCategory.InitialPowerUpdateResistance,
			ElectricalUpdateTypeCategory.PowerUpdateResistanceChange,
			ElectricalUpdateTypeCategory.PowerNetworkUpdate,
		};
		ModuleType = ElectricalModuleTypeCategory.ResistanceSource;
		ControllingNode = Node;
		Node.AddModule(this);
	}

	public override void InitialPowerUpdateResistance()
	{
		foreach (KeyValuePair<ElectricalOIinheritance, HashSet<PowerTypeCategory>> Supplie in ControllingNode.Node.Data.ResistanceToConnectedDevices)
		{
			ControllingNode.Node.ResistanceInput(1.11111111f, Supplie.Key.GameObject(), null);
			ElectricalSynchronisation.NUCurrentChange.Add(Supplie.Key.InData.ControllingUpdate);
		}
	}

	public override void PowerUpdateResistanceChange()
	{
		foreach (KeyValuePair<ElectricalOIinheritance, HashSet<PowerTypeCategory>> Supplie in ControllingNode.Node.Data.ResistanceToConnectedDevices)
		{
			ControllingNode.Node.ResistanceInput(1.11111111f, Supplie.Key.GameObject(), null);
			ElectricalSynchronisation.NUCurrentChange.Add(Supplie.Key.InData.ControllingUpdate);
		}

	}
	public override void PowerNetworkUpdate()
	{
		if (Resistance != PreviousResistance)
		{
			if (PreviousResistance == 0 && !(Resistance == 0))
			{
				resistance.ResistanceAvailable = true;

			}
			else if (Resistance == 0 && !(PreviousResistance <= 0))
			{
				resistance.ResistanceAvailable = false;
				ElectricalDataCleanup.CleanConnectedDevices(ControllingNode.Node);
			}

			PreviousResistance = Resistance;
			foreach (KeyValuePair<ElectricalOIinheritance, HashSet<PowerTypeCategory>> Supplie in ControllingNode.Node.Data.ResistanceToConnectedDevices)
			{
				if (Supplie.Value.Contains(PowerTypeCategory.StandardCable))//?
				{
					ElectricalSynchronisation.ResistanceChange.Add(Supplie.Key.InData.ControllingUpdate);
					ElectricalSynchronisation.NUCurrentChange.Add(Supplie.Key.InData.ControllingUpdate);
				}
			}
		}
	}
}
