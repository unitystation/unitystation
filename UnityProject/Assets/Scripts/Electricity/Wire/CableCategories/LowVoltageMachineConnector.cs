using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Mirror;


public class LowVoltageMachineConnector : NetworkBehaviour  , IDeviceControl
{
	private bool SelfDestruct = false;

	public WireConnect RelatedWire; //!!!!
	public PowerTypeCategory ApplianceType = PowerTypeCategory.LowMachineConnector;
	public HashSet<PowerTypeCategory> CanConnectTo = new HashSet<PowerTypeCategory>(){
		PowerTypeCategory.DepartmentBattery,
		PowerTypeCategory.LowVoltageCable,
		PowerTypeCategory.APC,
	};

	public void PotentialDestroyed(){
		if (SelfDestruct) {
			//Then you can destroy
		}
	}

	public override void OnStartServer()
	{
		base.OnStartServer();
		RelatedWire.InData.CanConnectTo = CanConnectTo;
		RelatedWire.InData.Categorytype = ApplianceType;
		RelatedWire.WireEndA = Connection.MachineConnect;
		RelatedWire.WireEndB = Connection.Overlap;
		//RelatedWire.InData.ControllingDevice = this;
	}

	//Fixme:
	public void OnDestroy(){
//		ElectricalSynchronisation.StructureChangeReact = true;
//		ElectricalSynchronisation.ResistanceChange = true;
//		ElectricalSynchronisation.CurrentChange = true;
		SelfDestruct = true;

		//Make Invisible
	}
	public void TurnOffCleanup (){
	}
}

