using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;


public class MediumMachineConnector : NetworkBehaviour , IDeviceControl
{
	private bool SelfDestruct = false;

	public WireConnect RelatedWire; 
	public PowerTypeCategory ApplianceType = PowerTypeCategory.MediumMachineConnector;
	public HashSet<PowerTypeCategory> CanConnectTo = new HashSet<PowerTypeCategory>(){
		PowerTypeCategory.StandardCable,
		PowerTypeCategory.SMES,
		PowerTypeCategory.SolarPanelController,
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
	}

	//FIXME:
	public void OnDestroy(){
		SelfDestruct = true;
	}
	public void TurnOffCleanup (){
	}
}

