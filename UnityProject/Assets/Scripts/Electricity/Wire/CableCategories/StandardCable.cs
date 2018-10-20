using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;


public class StandardCable : NetworkBehaviour 
{

	public WireConnect RelatedWire;
	public PowerTypeCategory ApplianceType = PowerTypeCategory.StandardCable;
	public HashSet<PowerTypeCategory> CanConnectTo = new HashSet<PowerTypeCategory>(){
		PowerTypeCategory.StandardCable,
		PowerTypeCategory.FieldGenerator,
		PowerTypeCategory.APC,
	};
	public override void OnStartClient()
	{
		base.OnStartClient();
		RelatedWire.CanConnectTo = CanConnectTo;
		RelatedWire.Categorytype = ApplianceType;
	}

	private void OnDisable()
	{
	}
}

