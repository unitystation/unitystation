using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ResistanceSourceModule : ElectricalModuleInheritance
{

	private IntrinsicElectronicData ComingFromDevice = new IntrinsicElectronicData();

	public Resistance resistance = new Resistance();
	private float _resistance = 9999999999;

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
				if (double.IsInfinity(value))//value == 0 ||
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

	public float EditorResistance;
	public bool NotEditorResistanceset = true;

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
			ElectricalUpdateTypeCategory.PotentialDestroyed,
		};
		ModuleType = ElectricalModuleTypeCategory.ResistanceSource;
		ControllingNode = Node;
		//resistance.Ohms = ReactionTo.ResistanceReactionA.Resistance.Ohms;
		ReactionTo.ResistanceReactionA.Resistance = resistance;
		if (resistance.Ohms == 0)
		{
			resistance.Ohms = 9999991;
		}
		ControllingNode.Node.InData.ConnectionReaction[ReactionTo.ConnectingDevice] = ReactionTo;
		ElectricalSynchronisation.PoweredDevices.Add(ControllingNode);
		ComingFromDevice.SetDeadEnd();
		Node.AddModule(this);
	}

	public override void OnDespawnServer(DespawnInfo info)
	{
		ElectricalSynchronisation.PoweredDevices.Remove(ControllingNode);
	}

	public override void PotentialDestroyed()
	{
		ElectricalSynchronisation.ResistanceChange.Add(ControllingNode);
	}

	public override void InitialPowerUpdateResistance()
	{
		foreach (var Supplie in ControllingNode.Node.Data.ResistanceToConnectedDevices)
		{
			foreach (var _Resistance in Supplie.Value)
			{

				var Wrap = ElectricalPool.GetResistanceWrap();
				Wrap.Strength = 1;
				Wrap.SplitResistance(_Resistance.Value.Count);
				Wrap.resistance = _Resistance.Key;

				var VIR = new VIRResistances();
				VIR.AddResistance(Wrap);
				ControllingNode.Node.ResistanceInput(VIR, Supplie.Key, ComingFromDevice);
			}

			ElectricalSynchronisation.NUCurrentChange.Add(Supplie.Key.InData.ControllingDevice);
		}

		//foreach (var Supplie in ControllingNode.Node.Data.ResistanceToConnectedDevices)
		//{
		//	foreach (var Connections in Supplie.Value)
		//	{
		//		if (!Connections.Value.BeenProcessed)
		//		{
		//			var TT = new ResistanceWrap();
		//			TT.resistance = resistance;
		//			var ToNext = new List<ElectricalDirectionStep>();
		//			foreach (var Direction in Connections.Value.Steps)
		//			{
		//				if (Direction != null)
		//				{
		//					ToNext.Add(Direction.Upstream);
		//					///Direction.Downstream 
		//					var Step = new ElectricalDirectionStep();
		//					Step.Upstream = Direction;
		//					Step.InData = ComingFromDevice;
		//					Step.Sources.Add(resistance);
		//					Step.resistance.AddResistance(TT);
		//					Direction.Downstream.Add(Step);
		//					Direction.Sources.Add(resistance);
		//					Direction.resistance.AddResistance(TT);
		//					if (Direction.Upstream != null)
		//					{
		//						Direction.Upstream.Downstream.Add(Direction);
		//						Direction.Upstream.Sources.Add(resistance);
		//						Direction.Upstream.resistance.AddResistance(TT);
		//					}
		//				}
		//			}
		//			//ControllingNode.Node.InData.ConnectionReaction[Connections.Key.InData.Categorytype].ResistanceReactionA.Resistance.Ohms = 240;
		//			//Logger.LogError(Connections.Key.name + "FFFFFFFF", Category.Electrical);
		//			ControllingNode.Node.ResistanceInput(
		//				//ControllingNode.Node.InData.ConnectionReaction[Connections.Key.InData.Categorytype].ResistanceReactionA.Resistance,
		//				TT,
		//				Supplie.Key,//Issue needs to get ID out of Thread
		//				ComingFromDevice,
		//				ToNext
		//				);
		//			Connections.Value.BeenProcessed = true;
		//		}
		//	}
		//	ElectricalSynchronisation.NUCurrentChange.Add(Supplie.Key.InData.ControllingDevice);

		//}

	}

	public override void PowerUpdateResistanceChange()
	{
		foreach (var Supplie in ControllingNode.Node.Data.ResistanceToConnectedDevices)
		{
			//foreach (var Connections in Supplie.Value)
			//{

			//	Logger.LogError(Connections.Key.name + "F2222222FFFFFFF", Category.Electrical); 
			//	ComingFromDevice.SetDeadEnd();
			//	ControllingNode.Node.ResistanceInput(
			//		//ControllingNode.Node.InData.ConnectionReaction[Connections.Key.InData.Categorytype].ResistanceReactionA.Resistance.Ohms,
			//		240,
			//		Supplie.Key.GameObject(),
			//		ComingFromDevice,
			//		Connections.Value
			//);
			//}
			ElectricalSynchronisation.NUCurrentChange.Add(Supplie.Key.InData.ControllingDevice);
		}
	}

	public override void PowerNetworkUpdate()
	{
		if (dirtyResistance)
		{
			if (NotEditorResistanceset)
			{
				NotEditorResistanceset = false;
				if (EditorResistance != 0)
				{
					Resistance = EditorResistance;
				}
			}
			resistance.Ohms = Resistance;
			dirtyResistance = false;
			ElectricalSynchronisation.ResistanceChange.Add(ControllingNode);
			//foreach (KeyValuePair<ElectricalOIinheritance, HashSet<PowerTypeCategory>> Supplie in ControllingNode.Node.Data.ResistanceToConnectedDevices)
			//{
			//	if (Supplie.Value.Contains(PowerTypeCategory.StandardCable)) //wtf is here?
			//	{
			//		ElectricalSynchronisation.NUCurrentChange.Add(Supplie.Key.InData.ControllingDevice);
			//	}
			//}
		}
	}
}
