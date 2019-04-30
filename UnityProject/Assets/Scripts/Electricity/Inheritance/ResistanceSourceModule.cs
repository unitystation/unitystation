using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ResistanceSourceModule : ElectricalModuleInheritance
{
	//[Header("Transformer Settings")]
	public Resistance resistance = new Resistance();
	private float _resistance = 9999999999;
	/// <summary>
	/// The current resistance of devices connected to this APC.
	/// </summary>
	public float Resistance
	{
		get
		{
			return _resistance;
		}
		set
		{
			if (value != _resistance)
			{
				if ( double.IsInfinity(value))//value == 0 ||
				{
					if (_resistance != 9999999999)
					{
						dirtyResistance = true;
						_resistance = 9999999999;
					}
				}
				else
				{
					dirtyResistance = true;
					_resistance = value;
				}
			}
		}
	}

	/// <summary>
	/// Flag to determine if ElectricalSynchronisation has processed the resistance change yet
	/// </summary>
	private bool dirtyResistance = true;
	public PowerInputReactions ReactionTo;

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
		resistance.Ohms = ReactionTo.ResistanceReactionA.Resistance.Ohms;
		ReactionTo.ResistanceReactionA.Resistance = resistance;
		ControllingNode.Node.InData.ConnectionReaction[ReactionTo.ConnectingDevice] = ReactionTo;
		ElectricalSynchronisation.PoweredDevices.Add(ControllingNode);
		Node.AddModule(this);
	}

	public override void InitialPowerUpdateResistance()
	{
		foreach (KeyValuePair<ElectricalOIinheritance, HashSet<PowerTypeCategory>> Supplie in ControllingNode.Node.Data.ResistanceToConnectedDevices)
		{
			ControllingNode.Node.ResistanceInput(1.11111111f, Supplie.Key.GameObject(), null);
			ElectricalSynchronisation.NUCurrentChange.Add(Supplie.Key.InData.ControllingDevice);
		}
	}

	public override void PowerUpdateResistanceChange()
	{
		foreach (KeyValuePair<ElectricalOIinheritance, HashSet<PowerTypeCategory>> Supplie in ControllingNode.Node.Data.ResistanceToConnectedDevices)
		{
			ControllingNode.Node.ResistanceInput(1.11111111f, Supplie.Key.GameObject(), null);
			ElectricalSynchronisation.NUCurrentChange.Add(Supplie.Key.InData.ControllingDevice);
		}

	}
	public override void PowerNetworkUpdate()
	{
		//Logger.Log("yo");
		if (dirtyResistance)
		{
			//Logger.Log("dirtyResistance!");
			resistance.Ohms = Resistance;
			dirtyResistance = false;
			ElectricalSynchronisation.ResistanceChange.Add(ControllingNode);
			foreach (KeyValuePair<ElectricalOIinheritance, HashSet<PowerTypeCategory>> Supplie in ControllingNode.Node.Data.ResistanceToConnectedDevices)
			{
				if (Supplie.Value.Contains(PowerTypeCategory.StandardCable))
				{
					ElectricalSynchronisation.NUCurrentChange.Add(Supplie.Key.InData.ControllingDevice);
				}
			}
		}
	}
}
