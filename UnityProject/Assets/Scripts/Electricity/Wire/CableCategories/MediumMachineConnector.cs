using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;


public class MediumMachineConnector : NetworkBehaviour 
{

	public WireConnect RelatedWire; //!!!!
	public PowerTypeCategory ApplianceType = PowerTypeCategory.MediumMachineConnector;
	public HashSet<PowerTypeCategory> CanConnectTo = new HashSet<PowerTypeCategory>(){
		PowerTypeCategory.StandardCable,
		PowerTypeCategory.LowVoltageCable,
	};
	public override void OnStartClient()
	{
		base.OnStartClient();
		RelatedWire.CanConnectTo = CanConnectTo;
		RelatedWire.Categorytype = ApplianceType;
		RelatedWire.DirectionEnd = 9;
		RelatedWire.DirectionStart = 0;
	}

	private void OnDisable()
	{
	}
	public void OnDestroy(){
		ElectricalSynchronisation.StructureChangeReact = true;
		ElectricalSynchronisation.ResistanceChange = true;
		ElectricalSynchronisation.CurrentChange = true;
		//Then you can destroy
	}
}

